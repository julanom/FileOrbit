#!/bin/sh
set -eu

data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
install_dir="$data_dir/fileorbit/application"
desktop_file="$data_dir/applications/com.fileorbit.app.desktop"
python3 - "$data_dir" "$install_dir" <<'PY'
import pathlib
import sys

root, target = map(pathlib.Path, sys.argv[1:])
expected = root.resolve() / 'fileorbit' / 'application'
if target.is_symlink() or target.resolve() != expected:
    raise SystemExit('Refusing to remove an installation outside its expected directory.')
if (target / '.fileorbit-package').read_text(encoding='utf-8').strip() != 'com.fileorbit.app':
    raise SystemExit('The directory is not an installation created by FileOrbit.')
PY
"$install_dir/FileOrbit" --uninstall-cleanup
if [ -L "$HOME/.local/bin/fileorbit" ] && [ "$(readlink "$HOME/.local/bin/fileorbit")" = "$install_dir/fileorbit-cli" ]; then
    rm -- "$HOME/.local/bin/fileorbit"
fi
if [ -f "$desktop_file" ] && grep -q '^X-FileOrbit-Managed=true$' "$desktop_file"; then
    rm -- "$desktop_file"
fi
rm -rf -- "$install_dir"
printf '%s\n' 'FileOrbit was removed. Your settings and conversion results were kept.'
