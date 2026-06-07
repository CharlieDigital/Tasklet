import { createRouter, createWebHistory } from "vue-router";
import { routes } from "./routes";
import { useAppStore } from "@/stores/app-store";

const router = createRouter({
  // import.meta.env.BASE_URL is set by Vite from the `base` config option
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

/**
 * Navigation guard that enforces authentication and handles logout.
 */
router.beforeEach(async (to) => {
  const appStore = useAppStore();

  // Logout: clear tokens and fall through to the login redirect.
  if (to.name === "Logout") {
    await appStore.logout();
    return { name: "Login" };
  }

  // TODO: Check if the current user is authenticated (Firebase in app-store.ts and read here)

  // No valid session: send to login (but don't redirect login to itself).
  if (to.name !== "Login") {
    return { name: "Login", query: { redirect: to.fullPath } };
  }
});

export default router;
