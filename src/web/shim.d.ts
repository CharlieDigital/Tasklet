// https://unocss.dev/presets/attributify#typescript-support-jsx-tsx

import type { AttributifyAttributes } from "@unocss/preset-attributify";

declare module "@vue/runtime-dom" {
  interface HTMLAttributes extends AttributifyAttributes {}
}
