import { renderIcon } from "@/utils/render-utils";
import { Home, Logout } from "@vicons/tabler";
import type { MenuOption } from "naive-ui";
import type { MenuMixedOption } from "naive-ui/es/menu/src/interface";

/**
 * Main store for application level state
 */
export const useAppStore = defineStore("app", () => {
  const isDarkMode = useStorage("isDarkMode", false);

  /**
   * Performs logout by clearing any relevant state and redirecting to the login page.
   */
  function logout() {
    // TODO: Perform Firebase Auth logout
  }

  /**
   * Use route path as the menu key so NMenu's @update:value can push directly to
   * the router.  RouterLink inside labels won't fire in icon-only collapsed mode.
   */
  function makeMenuItem(
    label: string,
    routePath: string,
    icon: any,
  ): MenuOption {
    return {
      label,
      title: label,
      key: routePath,
      icon: renderIcon(icon),
    };
  }

  const leftMenuOptions = ref<MenuMixedOption[]>([
    makeMenuItem("Home", "/", Home),
  ]);

  const bottomMenuOptions = computed<MenuMixedOption[]>(() => {
    const options: MenuMixedOption[] = [];

    options.push(makeMenuItem("Logout", "/logout", Logout));
    return options;
  });

  return {
    leftMenuOptions,
    bottomMenuOptions,
    isDarkMode,
    logout,
  };
});
