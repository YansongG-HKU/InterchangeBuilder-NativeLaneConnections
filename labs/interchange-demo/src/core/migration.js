import { VERSION } from "./model.js";
import { flatVertical } from "./vertical.js";
import { validate } from "./solver.js";
// Returns an explicit proposal; caller must obtain local UI acceptance before commit.
export function legacyProposal(input) {
  if (
    input?.schema !== 1 ||
    input.solver !== "offline-0.1.0" ||
    Object.hasOwn(input, "vertical")
  )
    return null;
  const vertical = flatVertical();
  vertical.profiles.forEach((p) => {
    p.z0 = 0;
    p.z1 = 0;
  });
  vertical.source = "legacy-flat-accepted";
  const proposed = { ...input, schema: 2, solver: VERSION, vertical };
  return validate(proposed).length ? null : proposed;
}
