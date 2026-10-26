import { cpSync } from "node:fs";
cpSync("examples", "dist/examples", { recursive: true });
cpSync("docs/THIRD_PARTY.md", "dist/THIRD_PARTY.md");
