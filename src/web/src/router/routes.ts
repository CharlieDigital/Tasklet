import AppLayout from "@/layouts/AppLayout.vue";
import type { RouteRecordRaw } from "vue-router";

/**
 * Application routes.
 *
 * Login and Logout are kept under the app layout so the shell remains stable
 * while the router guard handles auth side effects.
 */
export const routes: Array<RouteRecordRaw> = [
  {
    path: "/",
    component: AppLayout,
    children: [
      {
        path: "",
        name: "Home",
        component: () => import("@/views/home/Home.vue"),
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
  {
    path: "/logout",
    component: AppLayout,
    children: [
      {
        path: "",
        name: "Logout",
        component: () => import("@/views/Login.vue"),
      },
    ],
  },
];
