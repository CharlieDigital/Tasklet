import { NIcon } from "naive-ui";

/**
 * A utility function to render icons for NaiveUI.
 * @param icon The icon to render.
 * @returns The icon component rendered into an `NIcon` wrapper
 */
export function renderIcon(icon: Component) {
  return () => h(NIcon, null, { default: () => h(icon) });
}
