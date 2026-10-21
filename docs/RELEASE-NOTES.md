# FileOrbit 2.1 preview

A distinct glass-style workspace with native system fonts, blue/teal surfaces, light and dark appearance, and a new orbit icon.

- Fixed delayed Finder selection overwriting a native drag.
- Suppressed repeated preset IDs in wheel faces and nested groups.
- Removed upstream donation controls and promotional popups.
- Routed repository, issues, and release links to FileOrbit.
- Isolated Windows/Linux desktop integration names from upstream.
- Isolated the single-instance channel, preventing interference with upstream installs.
- Hid macOS-only actions on other platforms; Windows uses its own classic Explorer menu.
- Preserved macOS all-Spaces overlay handling.
- Added deterministic bug-regression and light/dark/RTL render checks.

Packages: macOS Apple Silicon/Intel, Windows x64, Linux x64/ARM64.
Linux packages built against Ubuntu 24.04; use Ubuntu 24.04 or newer for this preview.
Wayland global gestures remain experimental and compositor-dependent. In-app drag and conversion are the fallback.
macOS builds are ad-hoc signed, not notarized. Windows ZIPs are unsigned.
Re-enable macOS Accessibility permission after replacing an older app.

The original MIT copyright and third-party notices remain in legal acknowledgments.
