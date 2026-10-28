# Exact nonstraight proof slice / 精确非直线证明切片

Extends the earlier incomplete straight-only checkpoint. Pure Core, no runtime activation, engine conversion, publication or game permission.

## Certified model

Actual immutable candidate is a counterclockwise quarter-circle centreline radius R about origin, starting(R,0) heading+Y, ending(0,R) heading−X, followed by a westbound finite tail ending(−tail,R). Fixed width w, constant surface height, underside thickness t>0. No crossfall, variable width, piers, terrain or vehicle envelope. Exact annular strip uses (x,y)=((R+u)cosθ,(R+u)sinθ), θ∈[0,π/2], u∈[−w/2,w/2]. Full occupied height is[z−t,z]. There is no sampled approximation of the curve.

For R>w/2, polar coordinates are injective on the entire strip. The arc's open interior has x>0, tail's open interior has x<0; intersection is exactly the common radial cross-section at x=0. Thus all station pairs, including arbitrarily close stations and seam pairs, are covered by this analytic argument without index/station exclusions. This proves absence of illegal self-overlap for this restricted model. It does not prove arbitrary Bezier, hairpins, loops or full circles. Those inputs are not silently converted and remain unverified in the existing whole-route entry.

Planar seam is G1 and has a curvature jump1/R→0. StrictG2 returns Unknown. Radius≤half-width also returns Unknown because radial injectivity fails. Selected node/position/direction/side are never moved: SelectForRequest checks exact declared ports, width, tail, endpoint curvature, revision and mode against actual candidates. This exact family is not a quintic or game format.

## 3D checks and selection

Continuous AABB enclosure includes full width and underside thickness, not centreline height alone. Envelope separation supplies a conservative sufficient clearance lower bound. Overlapping enclosures remain Unknown unless actual radial/vertical interior overlap at a common angle is witnessed. Finite angle witnesses can establish overlap but never certify separation. Contact and insufficient bounds remain Unknown.

SelectForRequest/Select actually require self-certification and separation from every supplied exact-family obstacle before choosing an index. Failed/unknown candidates cannot win; no-collision mode does not bypass proofs. Current obstacle family is co-centred quarter arcs plus tails, not arbitrary oblique ramps. Candidate objects are immutable, selection recomputes current checks; no reusable game permit exists.

Flat candidates at z0/thickness2 and z12/thickness2 have full-area underside gap10. Clearance9 certifies separation; reducing upper surface to1 causes actual interior overlap. Actual selected candidates [colliding low, separated high] choose high after validation. This is selection from supplied profiles, not automatic ramp/profile generation or multi-crossing routing.

## Critical nonzero-grade boundary

Linear height along horizontal centreline station s=Rθ was implemented for diagnostic analysis. Whole-width grade is |g|R/(R−w/2), not centreline |g|. Moreover a nonzero centreline-matched arc/tail grade does not match all radial strip longitudinal derivatives at the seam. Independent review caught this; all nonzero-grade candidates now return Unknown FULL_WIDTH_VERTICAL_SEAM_NOT_G1, with the conservative inner-edge peak reported. Passing profiles are constant height only. This explicitly limits current certification instead of asserting a false full-width smooth proof. A future transition surface/profile must resolve that seam and interval/domain coverage before admission.

## Evidence and remaining scope

Independent review also caught invalid clearance propagation; public Compare now preserves InvalidInput and cannot return a witness for NaN clearance. Tests include request binding, nonstraight full-domain proof, crossing separation/overlap, stale revision, G2 conflict, inner radial fold, folded Bezier non-certification, mode invariance, profile seam rejection and actual candidate selection. Exact model parameters and reasons appear in tests/source and docs/quarter-arc-evidence.

Remaining requested general work: arbitrary curved/oblique/near-parallel intersections, continuous nonconstant profiles with full-width seams, nonlocal multiple-turn domains, general adjacent/higher-order curve proof, multilevel route generation and actual engine integration. These are not completed by this slice. The conservative alternative for unsupported general domains remains Unknown, rather than deleting a diagonal region or treating local curvature regularity as a global proof. Real game dependencies are separate and do not block this mathematical work.
