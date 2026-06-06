import { defineConfig } from "@kubb/core";
import { pluginOas } from "@kubb/plugin-oas";
import { pluginTs } from "@kubb/plugin-ts";
import { pluginClient } from "@kubb/plugin-client";

// See root configuration here: https://kubb.dev/kubb/getting-started/configure
export default defineConfig({
  name: "tasklet-kubb",
  root: ".",
  input: {
    path: "./src/api/tasklet-api.json",
  },
  output: {
    path: "./src/api/generated",
    clean: true,
  },
  // See plugin configuration here: https://kubb.dev/kubb/plugins
  plugins: [
    // https://kubb.dev/helpers/oas
    pluginOas(),
    // https://kubb.dev/plugins/plugin-ts/
    pluginTs({
      output: {
        path: "./types",
      },
      enumType: "asConst",
      dateType: "date",
      unknownType: "unknown",
      optionalType: "questionTokenAndUndefined",
    }),
    // https://kubb.dev/kubb/plugins/plugin-client
    pluginClient({
      // Resolved at build time by Vite from .env / .env.production
      baseURL:
        import.meta.env.VITE_API_BASE_URL ?? "http://app.localhost:8085/api",
      contentType: "application/json",
      // Can also use custom client: https://kubb.dev/kubb/guide/fetch
      client: "fetch",
      clientType: "staticClass",
    }),
  ],
});
