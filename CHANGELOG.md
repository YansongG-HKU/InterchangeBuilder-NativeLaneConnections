# Changelog

## 2.2.0 - 2026-08-16

- Added width-aware endpoint candidates that mirror the game's cell-snap and free-width alignment branches.
- Retained centre alignment for mismatched road widths, including four-lane to two-lane connections.
- Selected left, centre, or right alignment from the pointer hit and preserved it through preview and placement.
- Added native connection-compatibility filtering and direction-aware multi-arm junction selection.
- Preserved node-split-only behavior and special non-zonable network handling.
- Added a Simplified Chinese UI translation and an in-panel endpoint-rule notice.
- Added 12 core tests and a seven-target runtime metadata smoke harness.
- Made the build accept user-supplied original-mod and game paths without embedding either dependency in source control.
- Published an authorized, ready-to-use binary package with direct Skyve II installation instructions.
