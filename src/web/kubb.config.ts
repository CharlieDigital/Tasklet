import { defineConfig } from "@kubb/core";
import { pluginOas } from "@kubb/plugin-oas";
import { pluginTs } from "@kubb/plugin-ts";
import { pluginClient } from "@kubb/plugin-client";

/**
 * Kubb generation for the Tasklet OpenAPI schema.
 *
 * Generated clients use `tasklet-api-client.ts` so base URL resolution, error
 * handling, and auth headers stay in handwritten runtime code.
 */
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
  plugins: [
    pluginOas(),
    pluginTs({
      output: {
        path: "./types",
      },
      enumType: "asConst",
      dateType: "date",
      unknownType: "unknown",
      optionalType: "questionTokenAndUndefined",
    }),
    pluginClient({
      contentType: "application/json",
      clientType: "staticClass",
      importPath: "../../tasklet-api-client",
    }),
  ],
});
