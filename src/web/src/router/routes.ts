import AppLayout from "@/layouts/AppLayout.vue";
import type { RouteRecordRaw } from "vue-router";

/**
 * The list of routes in the app.
 */
export const routes: Array<RouteRecordRaw> = [
  {
    path: "/",
    component: AppLayout,
    children: [
      {
        path: "",
        name: "Home",
        component: () => import("@/views/Home.vue"),
      },
    ],
  },
  {
    path: "/login",
    component: AppLayout,
    children: [
      {
        path: "",
        name: "Login",
        component: () => import("@/views/Login.vue"),
      },
    ],
  },
];
