# Rising Bezier external-domain proof / 升降Bezier外部全域检查

This slice generalizes external separation beyond the exact quarter-arc family. Input is actual immutable line/Bezier segments already inspected against the complete planar request; fixed width, known thickness and linear height z(s)=datum+grade*s in TRUE horizontal arc station. Positive/negative grade supported. No Bezier-parameter substitution or inverse station approximation is used.

The geometric enclosure model is the union of horizontal radius(width/2) disks at every centreline station, vertically extruded from z(s)-thickness to z(s). It encloses the corresponding normal-width ribbon and rounded end caps, possibly conservatively overcovering it. It does NOT certify a smooth road surface, full-width slope, terrain, variable width/crossfall, piers, vehicles or engine meshes. Centreline grade is a profile input, not a full-width slope permit. General self status remains UNKNOWN and CanApplyToGame always false.

## Continuous parameter coverage

Each actual segment is de Casteljau split to depth8 by default; depth0–12 and maximum4096 leaves are explicit budgets. At depthd each segment has2^d parameter leaves in travel order, [j/2^d,(j+1)/2^d]. Certificate records depth, expected leaves, completed envelope list, total station lower/upper and completed/separated pair counts. All leaves times ALL obstacle enclosures are checked. A skipped/invalid leaf or insufficient budget permanently marks Unknown; successful prefixes cannot certify the route.

For each leaf, cumulative chord lengths bound incoming station below, cumulative control-polygon lengths bound it above. Every station inside the leaf lies between incoming lower and incoming upper+current polygon length. Height extrema of this LINEAR station profile occur at those bounds, also for negative grade. Convex control bbox expanded by half width encloses the continuous XY sweep. There is no sparse-sample clearance pass.

Numerical guard: leaf chord reduced by1e-7 (floored0), polygon increased1e-7, XY/Z envelopes expanded1e-7. This covers finite arithmetic residuals generously for the existing ±10000 control domain/depth≤12; it is an engineering floating-point guard, not a formally machine-verified directed-rounding implementation. Near contact/unresolved domains remain Unknown; no claim of universal arbitrary-scale proof.

External obstacle inputs are valid enclosing prisms/boxes from a trusted producer, retained in evidence. This checker does not authenticate caller-asserted bounds of unknown game meshes. Any AABB axis gap exceeding clearance+1e-8 is a sufficient continuous separation certificate. Overlapping boxes yield Unknown, NEVER a collision assertion; this algorithm has no inner-intersection witness producer. Existing exact-prism/arc witness functions are separate.

## Actual nonstraight rising reference

Start(0,0)+X, end(300,60)+X, width8, tail40, zero endpoint planar curvature, strict planarG2, revision1. Actual baseline quintic scale1 plus explicit tail. datum0, grade+.1, thickness2, clearance6. Two lower roads are known horizontal prisms at surface0/thickness2/width8, running from(100,-100)to(100,150) and(200,-100)to(200,150). The curve genuinely rises from ground and bends through both crossing regions. Both full-area crossings are certified by all512 leaf×2 obstacle pairs, not only their centre intersections. Inputs, actual control points, 512 boxes, station interval,1024 checks and profile parameters are saved in external-spatial-evidence/contracts.json.

Negative grade30−.02s also separates. Lower profile, thicker structure, long parallel overlap with low beginning or descending low end, and truncated coverage do not pass. One initial descending regression incorrectly placed its obstacle beside the high beginning; moving it beside the actual low endpoint corrected the reference, without changing the checker.

## Three different outcomes

1. Existing local planar inspector: must pass before this external check. Its documented short-neighbour exclusions remain.
2. External 3D separation: this certificate covers the stated sweep and every supplied obstacle; applies to actual candidate geometry, not preview decoration.
3. Global self certificate: UNKNOWN for these general Beziers. Thus complete candidate placement remains blocked despite a valid external certificate. Existing restricted straight/quarter-arc analytic self proofs remain separate support families; no widening by implication.

This slice does not generate multilevel profiles automatically, route around arbitrary 3D obstacles or certify non-linear Hermite station profiles. Linear station profiles are an explicit supported subset. Full-width seam/slope and general diagonal-parameter self proofs remain unresolved. Real game dependencies and runtime activation remain independent gates; no guessed engine interface.

Independent reviewer checked station bounds (including negative grade), continuous XY/Z enclosure, full leaf×obstacle coverage and fail-closed budgets without finding a mathematical false-pass. Standard build/test evidence is recorded separately. No production JS/UI/dependency/runtime/build/release/CI changes, push, PR, merge or installation.
