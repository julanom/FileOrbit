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
