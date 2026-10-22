# FileOrbit 2.1.0-fileorbit.7

This release repairs the AirDrop action in the Actions wheel on macOS.

## Fixed

- Fixed AirDrop failing with `service.performWithItems is not a function` on recent macOS versions.
- Replaced the short-lived JavaScript/AppleScript AirDrop helper with a native AppKit `NSSharingService` integration inside FileOrbit.
- The AirDrop chooser now belongs to the running FileOrbit process, so it can remain open while you select a nearby device.
- Added a macOS-native regression check that verifies the AirDrop sharing service is available.

## How to use it

Drag one or more files, open the Actions wheel, and release them on AirDrop. Keep Wi-Fi and Bluetooth enabled and make sure the receiving device is discoverable in AirDrop.

## Packages

macOS Apple Silicon and Intel, Windows x64, Linux x64, and Linux ARM64 packages are included. AirDrop is available only on macOS.

macOS builds are ad-hoc signed and not notarized. Windows ZIPs are unsigned.
