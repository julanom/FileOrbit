# FileOrbit

FileOrbit is an independent, cross-platform desktop application for offline file conversion and contextual file
actions. It uses its own application identity throughout the source tree, packages, executables, settings, UI, and
release workflow.

## Highlights

- Drag a file and tap **Shift**, **Command**, or **Control** to open the appropriate radial workflow on macOS.
- The wheel follows the pointer across displays, desktop Spaces, Stage Manager sets, and full-screen applications.
- Convert images, documents, archives, audio, video, and other formats with local engines.
- Use native file actions, reusable presets, history, watch folders, crop tools, PDF tools, and the floating shelf.
- Build self-contained packages for macOS, Windows, and Linux from one repository.

## Build and run

Requires the .NET 10 SDK.

```sh
dotnet restore
dotnet run --project src/FileOrbit.App
```

To build a self-contained macOS package:

```sh
./build/build-fileorbit-macos.sh
```

The global macOS drag gestures require FileOrbit in System Settings → Privacy & Security → Accessibility. Finder
selection actions may also request Automation access.

See [the capability checklist](docs/FILEORBIT-CAPABILITIES.md) for supported directions and optional engines.

## License

FileOrbit is distributed under the MIT license. Required copyright and attribution notices are preserved in
`LICENSE` and `THIRD-PARTY-NOTICES.md`.
