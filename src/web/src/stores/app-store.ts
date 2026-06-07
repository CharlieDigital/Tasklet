import type { UserInfoResponse } from "@/api/generated/types/UserInfoResponse";
import { renderIcon } from "@/utils/render-utils";
import { Home, Logout } from "@vicons/tabler";
import type { MenuOption } from "naive-ui";
import type { MenuMixedOption } from "naive-ui/es/menu/src/interface";

/**
 * Main store for application level state
 */
export const useAppStore = defineStore("app", () => {
  const isDarkMode = useStorage("isDarkMode", false);

  const loginLoading = ref(false);
  const loginError = ref<string | null>(null);

  /**
   * Current user profile info populated from the backend after front-end login.
   * Make the API call to the .NET backend /me route to get the user profile.  For
   * now, this largely mirrors the front-end claims, but we would move settings here
   * and retrieve after login on the FE.
   */
  const currentUser = ref<UserInfoResponse | null>(null);

  /**
   * Performs login flow via Firebase using the emulator.
   */
  function login() {
    // TODO: Start Firebase auth
  }

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
    currentUser,
    loginLoading,
    loginError,
    login,
    logout,
  };
});
