# Changelog

## 2.3.0 - 2026-08-16

- Separated the start-road source from the output-road prefab.
- Added explicit pending, locked, and follow-start road-selection states.
- Prevented start-node and Free Draw inheritance from overwriting a confirmed manual road.
- Added a persistent Simplified Chinese confirmation card showing output road and start source separately.
- Blocked placement while a newly selected road is still awaiting confirmation.
- Invalidated stale previews when the output road changes and logged the resolved build-road snapshot.
- Added five road-selection state-machine tests, bringing the core suite to 17 tests.

## 2.2.0 - 2026-08-16

- Added width-aware endpoint candidates that mirror the game's cell-snap and free-width alignment branches.
- Retained centre alignment for mismatched road widths, including four-lane to two-lane connections.
- Selected left, centre, or right alignment from the pointer hit and preserved it through preview and placement.
- Added native connection-compatibility filtering and direction-aware multi-arm junction selection.
- Preserved node-split-only behavior and special non-zonable network handling.
- Added a Simplified Chinese UI translation and an in-panel endpoint-rule notice.
- Added 12 core tests and a seven-target runtime metadata smoke harness.
- Made the build accept explicit base-package and game paths without embedding game assemblies in source control.
- Published a ready-to-use binary package with direct Skyve II installation instructions.
