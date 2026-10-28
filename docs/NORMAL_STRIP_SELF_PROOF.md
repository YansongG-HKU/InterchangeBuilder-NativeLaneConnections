# Noncircular global normal-strip certificate / 非圆弧全局法向条带证书

Base f7ed5f81eaaf75d2c6c1854a862401bb952b6e41. This implements a sufficient global certificate for a subset of ordinary forward Bezier turns/shifts, not arbitrary loops. Actual geometry and full immutable request are inspected. No runtime activation or game placement permission. CurveCandidateSearch now computes this separate SelfCertificate for each actual inspected output; unknown self certificates remain distinct from local/external checks.

## Entity and theorem

Centreline C is a regular, globally C1 piecewise Bezier, parameterized by true horizontal arc station s. Fixed half-width r. Actual planar entity is the normal strip F(s,u)=C(s)+uN(s), u∈[-r,r], including flat endpoint cross-sections. Normal disks used in the external 3D checker are conservative supersets; their natural local overlap is NOT the definition of illegal self-overlap.

Suppose all unit tangents lie in one angular interval of width beta<pi/2 and |curvature|≤K globally, with rK<cos(beta). Then F is injective over the ENTIRE parameter rectangle. Proof: for t>s, (C(t)-C(s))·T(s)=integral_s^t T(q)·T(s)dq≥(t-s)cos(beta). If F(s,u)=F(t,v), taking dot T(s) eliminates uN(s), so this positive quantity has magnitude at most r|N(t)-N(s)|≤rK(t-s). Contradiction. At t=s, normal coordinates force u=v. Thus every inter-piece and same-piece pair is covered, including arbitrarily close stations. There is no deleted diagonal band, adjacent-index exemption, fixed station skip or sampled pair assumption.

The proof uses genuine position/tangent continuity. The old local validator's1e-7 tolerance is INSUFFICIENT: an arbitrarily small left corner has overlapping inner strips, and a tiny backward seam gap overlaps too. New ExactSeam additionally requires bit-value identical endpoints and exact positive-collinear endpoint control edges. BigInteger dyadic coordinates (common2^-1074 scale) compute exact differences/cross/dot, so rounded floating zero does not masquerade as collinearity. Unsupported near-seams return Unknown. New fault injections cover angle1e-8 and position5e-8.

## Executable bounds

Derivative control convex hull bounds all tangents. Every derivative control must have positive projection on a normalized reference axis; min/max atan2 bound one continuous cone without angular wrap. Curvature uses max |cross(first-derivative-control,second-derivative-control)| divided by the cube of a positive speed projection lower bound. Bilinearity plus the two convex hulls makes this a conservative continuous curvature bound. Per-piece bounds combine by maximum; exact tangent joins prevent a hidden angular jump. All actual pieces are visited.

Existing ±10000 control, degree1–5 and request domain applies. Projection reduced1e-7, angle bounds expanded2e-7/projection, cross bound expanded1e-7×(derivative norms+1), beta/K additionally expanded1e-8; acceptance margin must exceed1e-7. These are explicit engineering floating-point guards for this bounded arithmetic, not a machine-verified directed-rounding proof. Near-zero speed/cone-boundary/curvature uncertainty returns Unknown. Certificate stores cone, K, positive injection margin, all piece count and immutable actual source geometry. No input is moved or fitted to gain certification.

## Noncircle reference and failures

The existing actual quintic shift from(0,0)+X to(300,60)+X, width8, tail40, endpoint curvature0, scale1 is certified. Bounds beta≈0.9111388477, K≈0.02542247008, cos(beta)-4K≈0.5111563469. This is nonconstant curvature with necessary S sign change, not a circular reference. Exact dyadic de Casteljau split certifies the identical physical route, with tighter hull bounds (beta≈0.4169146724, K≈0.00976750069). Both proofs cover their whole domain, independent of segment labels. General inexact floating subdivision may create a nonexact seam and conservatively lose certification.

Single-segment fold, adjacent reverse, tiny G0/G1 seam jumps and a width50 tight shift do not certify. Unknown is not a collision witness or global infeasibility claim. A cone wider than90degrees, near-reversal, many hairpins or failed sufficient inequality can still describe a physically feasible road; they require another theorem or finer certified construction. This implementation does not automatically subdivide to manufacture exact joins.

## Relation to 3D and remaining gates

If the planar normal-strip map is injective, vertical extrusion at each (s,u) over any FINITE single-valued interval [z(s,u)-thickness(s,u),z(s,u)] with positive thickness is also injective in (s,u,vertical-depth): equality in3D first forces equal XY and thus equal(s,u), then equal depth. This is an absence-of-self-overlap statement for vertical columns; it does not certify surface smoothness, grade, crossfall regularity, external clearance, supports or terrain. Model changes with horizontally displaced thickness cannot inherit it.

The earlier external disk-sweep enclosure includes this normal-strip volume when height/thickness agree. Its external certificate and this self certificate remain independently scoped; neither changes CanApplyToGame=false. General full-width seam/grade, nonlinear profiles, arbitrary self domains, game construction and dependency contracts remain separate obligations. No fake game adapter or universal proof claim.

Independent review found and reproduced the approximate-seam counterexample before final correction. Regression and actual output evidence are in normal-strip-evidence; prior evidence is immutable. No new dependency (existing System.Numerics BigInteger), Lab UI/production JS, runtime/build/release/CI or third-party settings changed. No push/PR/merge/release.
