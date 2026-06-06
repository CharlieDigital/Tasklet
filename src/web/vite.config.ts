import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import Components from "unplugin-vue-components/vite";
import AutoImport from "unplugin-auto-import/vite";
import VueRouter from "unplugin-vue-router/vite";
import {
  NaiveUiResolver,
  VueUseComponentsResolver,
} from "unplugin-vue-components/resolvers";
import UnoCSS from "unocss/vite";
import path from "path";

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
    vue(),
    UnoCSS(),
    VueRouter(),
    AutoImport({
      include: [/\.[tj]sx?$/, /\.vue$/, /\.vue\?vue/, /\.vue\.[tj]sx?\?vue/],
      imports: [
        "vue",
        "pinia",
        "vue-router",
        {
          // See: https://www.naiveui.com/en-US/light/docs/import-on-demand
          "naive-ui": [
            "useDialog",
            "useMessage",
            "useNotification",
            "useLoadingBar",
          ],
        },
      ],
    }),
    Components({
      resolvers: [NaiveUiResolver(), VueUseComponentsResolver()],
    }),
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"), // Alias '@' to the 'src' directory
    },
  },
  server: {
    // Yarp fails without this.
    allowedHosts: true,
  },
}));
