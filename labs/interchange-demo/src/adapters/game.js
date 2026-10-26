/** Future adapter contract: snapshot() returns immutable source ids/revision and
 * supported road/lane/terrain data. validateSnapshot(revision) precedes any
 * transaction; apply requires verified atomic rollback and read-back lane graph.
 * Offline plans never implement or imply a CS2 API. */
export class UnavailableGameAdapter {
  capabilities() {
    return { read: false, apply: false, rollback: false, readBack: false };
  }
  async snapshot() {
    throw new Error("UNSUPPORTED: game adapter is not implemented");
  }
  async apply() {
    throw new Error("UNSUPPORTED: no verified transaction/rollback API");
  }
}
