import {
  defineConfig,
  presetWind4,
  presetAttributify,
  presetTypography,
  presetWebFonts,
  presetTagify,
} from "unocss";

export default defineConfig({
  presets: [
    presetWind4(), // https://unocss.dev/presets/wind4
    presetAttributify(), // https://unocss.dev/presets/attributify
    presetTypography(), // https://unocss.dev/presets/typography
    presetWebFonts({
      provider: "google",
      fonts: { sans: "Google Sans Flex" },
    }), // https://unocss.dev/presets/web-fonts
    presetTagify({
      prefix: "un-",
    }), // https://unocss.dev/presets/tagify
  ],
});
