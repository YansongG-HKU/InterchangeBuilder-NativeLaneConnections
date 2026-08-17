# Changelog

## 2.5.0 - 2026-08-17

- Added direction-aware endpoint ports from the selected prefab's actual driving lanes and the target edge's live composition.
- Covered arbitrary one-way, two-way, odd/even, asymmetric, public-transport, and runtime-generated custom road layouts without fixed lane-count tables.
- Kept every native width/cell candidate and centre alignment; lane metadata adds complete intermediate lane windows and safely falls back when unavailable.
- Corrected physical left/right orientation at both ends of an edge and exposed live Chinese start/end lane mappings in the read-only road summary.
- Added a geometry-and-connectivity chain resolver for offset ports whose generated endpoint node differs from the original centre node.
- Prevented an offset matching timeout from deleting permanent roads that the game already generated.
- Added optional reflection-only Anarchy integration, including custom-tool registration and a live status indicator without auto-enabling or bundling Anarchy.
- Expanded the core suite to 29 tests, including every 1-to-10 one-way lane-count pairing, endpoint orientation, 2-to-8 lane windows, asymmetric direction matching, metadata fallback, offset chain recovery, and disconnected-chain rejection.

## 2.4.0 - 2026-08-17

- Replaced pending/locked/follow-start states with automatic vanilla-toolbar road memory.
- Read the selected network from the game's `NetToolSystem` whenever an InterchangeBuilder mode starts.
- Kept InterchangeBuilder active when another supported network is chosen from the vanilla toolbar.
- Prevented start-node and Free Draw inheritance from changing the remembered panel selection.
- Replaced the duplicate interactive road catalog with a read-only current-road summary and Chinese rule hint.
- Blocked placement only when the vanilla toolbar has no valid network selection.
- Added regression tests for immediate selection, reselection, route restart, source isolation, and world reset.

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
