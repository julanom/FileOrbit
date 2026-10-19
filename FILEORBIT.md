# FileOrbit

FileOrbit is a macOS-focused fork of [Filee](https://github.com/KnifeLemon/Filee) at upstream commit
`a1be20cefad36ea9bfa1b3bb923e3199b78de992` (Filee 1.12.0 plus its Spanish localization). It keeps Filee's offline conversion engine and MIT license, and adds
FileOrbit's three radial workflows and native file actions.

## What changed

- Start a Finder drag, then tap **Shift** for conversions, **Command** for tools, or **Control** for file actions. The
  wheel appears on the key tap, stays open when the key is released and fills from Finder's dragged selection.
- The radial wheel is a transient macOS overlay that follows the pointer across displays, every desktop Space, Stage
  Manager sets and other apps' full-screen Spaces.
- Each gesture is independently editable in Settings → Shortcuts, including the wheel it opens.
- Folders now pass through the radial drop target for applicable actions.
- The Actions wheel implements AirDrop, Shelf, Copy, Move, Copy Path and Open in Terminal.
- The floating Shelf accepts files and folders and lets you drag them back out later.
- Copy and Move use collision-safe names, preserve the source until a complete destination exists, handle Unicode and
  spaces without shell interpolation, and reject copying a folder into itself.
- FileOrbit has its own bundle id (`com.fileorbit.app`), settings directory and app identity, so it can coexist with
  Filee. Upstream self-update UI is disabled because installing a Filee release over this fork would be unsafe.
- The 1.12 update adds PDF compression and PDF-to-PowerPoint, video frame extraction and hardware encoding, lower idle
  memory use, more reliable gesture recovery, long e-book fixes, and Spanish and Polish interfaces.

## Build and run

Requires macOS 14 or later and the .NET 10 SDK.

```sh
dotnet restore
dotnet run --project src/Filee.App
```

For a self-contained, ad-hoc-signed package for the current Mac architecture:

```sh
./build/build-fileorbit-macos.sh
```

Unzip the generated file in `Releases`, then run `./install.sh`, or move `FileOrbit.app` to Applications. The global
drag gestures need FileOrbit enabled in System Settings → Privacy & Security → Accessibility. Finder selection
shortcuts also request Automation access. If permission is unavailable, the main window's ordinary drop zone and the
Finder service remain dependable fallbacks.

Ad-hoc signing is for development only. Public distribution still requires an Apple Developer ID, hardened-runtime
signing and notarization.

See [the capability checklist](docs/FILEORBIT-CAPABILITIES.md) for supported directions and optional engines.

## License and attribution

FileOrbit preserves Filee's MIT `LICENSE` and `THIRD-PARTY-NOTICES.md`. Filee is © 2026 Myeonggu Kim (KnifeLemon)
and contributors. FileOrbit-specific changes are provided under the same MIT license.
