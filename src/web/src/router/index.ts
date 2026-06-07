import { createRouter, createWebHistory } from "vue-router";
import { routes } from "./routes";
import { useAppStore } from "@/stores/app-store";

const router = createRouter({
  // import.meta.env.BASE_URL is set by Vite from the `base` config option
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

/**
 * Navigation guard for auth-protected routes.
 *
 * It waits for Firebase's initial session restoration before deciding whether
 * to redirect. Logout is represented as a route so menu navigation and direct
 * URL access share one sign-out path.
 */
router.beforeEach(async (to) => {
  const appStore = useAppStore();
  await appStore.ensureAuthReady();

  if (to.name === "Logout") {
    await appStore.logout();
    return { name: "Login" };
  }

  if (!appStore.isAuthenticated && to.name !== "Login") {
    return { name: "Login", query: { redirect: to.fullPath } };
  }

  if (appStore.isAuthenticated && to.name === "Login") {
    const redirect = to.query.redirect;

    return typeof redirect === "string" ? redirect : { name: "Home" };
  }
});

export default router;
