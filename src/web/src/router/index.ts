import { createRouter, createWebHistory } from "vue-router";
import { routes } from "./routes";

const router = createRouter({
  // import.meta.env.BASE_URL is set by Vite from the `base` config option
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

export default router;
