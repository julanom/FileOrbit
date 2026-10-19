#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd -P)
output_dir=${1:-"$root/Releases"}
case $(uname -m) in
  arm64) runtime=osx-arm64 ;;
  x86_64) runtime=osx-x64 ;;
  *) printf '%s\n' "Unsupported macOS architecture: $(uname -m)" >&2; exit 1 ;;
esac

version=$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$root/Directory.Build.props" | head -1)
bundle_version=${version%%-*}
package_name="FileOrbit-$version-$runtime"
mkdir -p "$output_dir"
output_dir=$(CDPATH= cd -- "$output_dir" && pwd -P)
archive="$output_dir/$package_name.zip"
checksum="$output_dir/$package_name-SHA256SUMS.txt"
if [ -e "$archive" ] || [ -e "$checksum" ]; then
  printf '%s\n' "Output already exists for $package_name; move it or choose another output directory." >&2
  exit 1
fi

stage=$(mktemp -d "${TMPDIR:-/tmp}/fileorbit-build.XXXXXX")
trap 'rm -rf -- "$stage"' EXIT HUP INT TERM
package="$stage/$package_name"
app="$package/FileOrbit.app"
publish="$app/Contents/MacOS"
resources="$app/Contents/Resources"
mkdir -p "$publish" "$resources"

dotnet publish "$root/src/Filee.App/Filee.App.csproj" -c Release -r "$runtime" --self-contained true \
  -p:Version="$version" -p:FileeRelease=true -p:PublishReadyToRun=false -o "$publish"
dotnet publish "$root/src/Filee.Cli/Filee.Cli.csproj" -c Release -r "$runtime" --self-contained true \
  -p:Version="$version" -p:FileeRelease=true -p:PublishReadyToRun=false -o "$publish"
chmod +x "$publish/Filee" "$publish/filee-cli"

# The portable app depends on these small engines. The Windows installer and the general portable build already
# bundle them; keep the native macOS package equally complete. Read the pinned URLs and hashes from the shared engine
# manifest so a package can never silently pick up an unreviewed engine release.
engine_manifest="$root/src/Filee.Engines/Infrastructure/engines.json"
engine_cache="$root/build/.cache/portable-downloads"
mkdir -p "$engine_cache" "$publish/engines"
fetch_portable_engine() {
  engine_name=$1
  engine_executable=$2
  engine_key="platforms.$runtime.components.$engine_name"
  engine_url=$(plutil -extract "$engine_key.url" raw "$engine_manifest")
  engine_hash=$(plutil -extract "$engine_key.sha256" raw "$engine_manifest" | tr '[:lower:]' '[:upper:]')
  engine_kind=$(plutil -extract "$engine_key.kind" raw "$engine_manifest")
  engine_archive="$engine_cache/$engine_hash.$engine_kind"

  if [ ! -f "$engine_archive" ]; then
    engine_partial=$(mktemp "$engine_cache/$engine_name.XXXXXX.partial")
    if ! curl --fail --location --retry 3 --output "$engine_partial" "$engine_url"; then
      rm -f -- "$engine_partial"
      return 1
    fi
    downloaded_hash=$(shasum -a 256 "$engine_partial" | awk '{print toupper($1)}')
    if [ "$downloaded_hash" != "$engine_hash" ]; then
      rm -f -- "$engine_partial"
      printf '%s\n' "Checksum mismatch for $engine_name ($runtime)." >&2
      return 1
    fi
    mv "$engine_partial" "$engine_archive"
  fi

  cached_hash=$(shasum -a 256 "$engine_archive" | awk '{print toupper($1)}')
  if [ "$cached_hash" != "$engine_hash" ]; then
    printf '%s\n' "Cached engine checksum mismatch: $engine_archive" >&2
    return 1
  fi

  engine_stage="$stage/engine-$engine_name"
  engine_members="$stage/engine-$engine_name.members"
  engine_details="$stage/engine-$engine_name.details"
  mkdir -p "$engine_stage"
  tar -tf "$engine_archive" > "$engine_members"
  while IFS= read -r engine_member; do
    case "$engine_member" in
      /*|../*|*/../*|*/..|*:*)
        printf '%s\n' "Unsafe archive member in $engine_name: $engine_member" >&2
        return 1
        ;;
    esac
  done < "$engine_members"
  tar -tvf "$engine_archive" > "$engine_details"
  if ! awk 'substr($1, 1, 1) != "-" && substr($1, 1, 1) != "d" { bad = 1 } END { exit bad }' "$engine_details"; then
    printf '%s\n' "Links or special files are not allowed in the $engine_name archive." >&2
    return 1
  fi
  tar -xf "$engine_archive" -C "$engine_stage"
  engine_program=$(find "$engine_stage" -type f -name "$engine_executable" -print | head -1)
  if [ -z "$engine_program" ]; then
    printf '%s\n' "$engine_executable was not found in the $engine_name archive." >&2
    return 1
  fi
  ditto "$(dirname "$engine_program")" "$publish/engines/$engine_name"
  chmod +x "$publish/engines/$engine_name/$engine_executable"
  printf '%s\n' "$engine_name ready for $runtime"
}

fetch_portable_engine rhwp rhwp
fetch_portable_engine 7zip 7zz

sed -e "s/__VERSION__/$bundle_version/g" -e "s/__BUNDLE_VERSION__/$bundle_version/g" \
  "$root/installer/macos/Info.plist" > "$app/Contents/Info.plist"
plutil -lint "$app/Contents/Info.plist"

iconset="$stage/fileorbit.iconset"
mkdir -p "$iconset"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$root/src/Filee.App/Assets/Icons/filee.png" \
    --out "$iconset/icon_${size}x${size}.png" >/dev/null
  pixels=$((size * 2))
  sips -z "$pixels" "$pixels" "$root/src/Filee.App/Assets/Icons/filee.png" \
    --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$resources/filee.icns"

for localization in "$root"/installer/macos/*.lproj; do
  cp -R "$localization" "$resources/"
done
cp "$root/LICENSE" "$root/THIRD-PARTY-NOTICES.md" "$package/"
cp "$root/installer/macos/install.sh" "$root/installer/macos/uninstall.sh" "$root/installer/macos/INSTALL.txt" "$package/"
chmod +x "$package/install.sh" "$package/uninstall.sh"

entitlements="$root/installer/macos/entitlements.plist"
find "$publish" -type f ! -path "$publish/Filee" -exec codesign --force --sign - --entitlements "$entitlements" {} \;
codesign --force --sign - --entitlements "$entitlements" "$publish/Filee"
codesign --force --sign - --entitlements "$entitlements" "$app"
codesign --verify --deep --strict "$app"

ditto -c -k --sequesterRsrc --keepParent "$package" "$archive"
hash=$(shasum -a 256 "$archive" | awk '{print $1}')
printf '%s  %s\n' "$hash" "$(basename "$archive")" > "$checksum"
printf '%s\n' "Package: $archive" "Checksum: $checksum"
