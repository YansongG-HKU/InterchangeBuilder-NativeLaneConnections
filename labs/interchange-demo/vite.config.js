import { defineConfig } from "vite";
export default defineConfig({
  build: {
    rollupOptions: {
      input: { baseline: "index.html", curves: "curve-lab.html" },
    },
  },
});
