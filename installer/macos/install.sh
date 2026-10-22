#!/bin/sh
set -eu

source_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
install_dir="$HOME/Applications/FileOrbit.app"
bin_dir="$HOME/.local/bin"
if [ -e "$install_dir" ] || [ -L "$install_dir" ]; then
    printf '%s\n' "An installation already exists at $install_dir. Run uninstall.sh first."
    exit 1
fi
if [ -e "$bin_dir/fileorbit" ] || [ -L "$bin_dir/fileorbit" ]; then
    printf '%s\n' "$bin_dir/fileorbit already exists. Remove or rename it before installing FileOrbit."
    exit 1
fi
test -x "$source_dir/FileOrbit.app/Contents/MacOS/FileOrbit"
mkdir -p "$HOME/Applications" "$bin_dir"
/usr/bin/ditto "$source_dir/FileOrbit.app" "$install_dir"
ln -s "$install_dir/Contents/MacOS/fileorbit-cli" "$bin_dir/fileorbit"
printf '%s\n' "Installed $install_dir" "CLI: $bin_dir/fileorbit (add $bin_dir to PATH if needed)."
printf '%s\n' 'Open FileOrbit from Applications. Grant Accessibility and Finder Automation access when requested.'
