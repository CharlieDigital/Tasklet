import type { UserInfoResponse } from "@/api/generated/types/UserInfoResponse";
import { User } from "@/api/generated/clients/User";
import {
  onFirebaseAuthChanged,
  signInWithGoogle,
  signOutOfFirebase,
  type FirebaseAuthUser,
} from "@/stores/services/firebase-auth-service";
import { renderIcon } from "@/utils/render-utils";
import { Home, Logout } from "@vicons/tabler";
import type { MenuOption } from "naive-ui";
import type { MenuMixedOption } from "naive-ui/es/menu/src/interface";

/**
 * Main store for application-level state.
 *
 * The store owns auth state so routing and views do not need to know Firebase
 * SDK details. Firebase-specific behavior stays in `firebase-auth-service.ts`;
 * backend profile loading uses generated Kubb clients.
 */
export const useAppStore = defineStore("app", () => {
  const isDarkMode = useStorage("isDarkMode", false);

  const loginLoading = ref(false);
  const loginError = ref<string | null>(null);
  const profileLoading = ref(false);
  const firebaseUser = shallowRef<FirebaseAuthUser | null>(null);
  const authReady = ref(false);

  /**
   * Resolves after Firebase reports the initial session state.
   *
   * Router guards await this promise to avoid redirecting to Login while
   * Firebase is still restoring a persisted emulator session.
   */
  const authReadyPromise = new Promise<void>((resolve) => {
    onFirebaseAuthChanged(async (user) => {
      firebaseUser.value = user;

      if (user === null) {
        currentUser.value = null;
        authReady.value = true;
        resolve();
        return;
      }

      /**
       * During explicit popup login, the login action owns backend profile
       * loading so a failed /me request can surface on the login form.
       */
      if (loginLoading.value) {
        return;
      }

      try {
        await loadCurrentUserProfile();
        loginError.value = null;
      } catch (error: unknown) {
        currentUser.value = null;
        loginError.value = getLoginErrorMessage(error);
        await signOutOfFirebase();
      }

      authReady.value = true;
      resolve();
    });
  });

  /**
   * Current backend profile loaded from `/api/v1/me`.
   *
   * The app treats Firebase login as necessary but insufficient; `currentUser`
   * must be populated before the user can enter protected routes.
   */
  const currentUser = ref<UserInfoResponse | null>(null);

  /**
   * True only when both Firebase and backend profile state are ready.
   */
  const isAuthenticated = computed(
    () => firebaseUser.value !== null && currentUser.value !== null,
  );

  /**
   * Display label for the current signed-in user.
   */
  const displayName = computed(
    () =>
      currentUser.value?.email ??
      firebaseUser.value?.displayName ??
      firebaseUser.value?.email ??
      "",
  );

  /**
   * Waits for the first Firebase auth-state callback.
   */
  async function ensureAuthReady() {
    if (authReady.value) {
      return;
    }

    await authReadyPromise;
  }

  /**
   * Converts unknown thrown values into concise login-page text.
   */
  function getLoginErrorMessage(error: unknown) {
    if (error instanceof Error && error.message.trim().length > 0) {
      return error.message;
    }

    return "Unable to complete login. Please try again.";
  }

  /**
   * Loads the backend profile through the generated API client.
   *
   * `tasklet-api-client.ts` attaches Firebase bearer tokens for generated API
   * calls, so store actions do not duplicate auth-header logic.
   */
  async function loadCurrentUserProfile() {
    profileLoading.value = true;

    try {
      if (firebaseUser.value === null) {
        currentUser.value = null;
        throw new Error(
          "Unable to load the current user profile. The Firebase session is missing.",
        );
      }

      currentUser.value = await User.me();
    } finally {
      profileLoading.value = false;
    }
  }

  /**
   * Performs popup login and blocks success until the backend profile loads.
   */
  async function login() {
    loginLoading.value = true;
    loginError.value = null;

    try {
      const user = await signInWithGoogle();
      firebaseUser.value = user;
      await loadCurrentUserProfile();
    } catch (error: unknown) {
      loginError.value = getLoginErrorMessage(error);
      try {
        await signOutOfFirebase();
      } catch {
        // Keep the original login/profile error visible to the user.
      }
      firebaseUser.value = null;
      currentUser.value = null;
      throw error;
    } finally {
      loginLoading.value = false;
    }
  }

  /**
   * Signs out and clears both Firebase and backend profile state.
   */
  async function logout() {
    loginLoading.value = true;

    try {
      await signOutOfFirebase();
      firebaseUser.value = null;
      currentUser.value = null;
      loginError.value = null;
    } finally {
      loginLoading.value = false;
    }
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

    if (isAuthenticated.value) {
      options.push(makeMenuItem("Logout", "/logout", Logout));
    }

    return options;
  });

  return {
    leftMenuOptions,
    bottomMenuOptions,
    isDarkMode,
    currentUser,
    firebaseUser,
    authReady,
    isAuthenticated,
    displayName,
    loginLoading,
    loginError,
    profileLoading,
    ensureAuthReady,
    loadCurrentUserProfile,
    login,
    logout,
  };
});
