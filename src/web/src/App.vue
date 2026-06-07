<!--
Wires up vue-router and naive-ui's NConfigProvider for theme overrides (dark mode)
-->
<template>
  <NConfigProvider :theme-overrides="themeOverrides" :theme>
    <NMessageProvider>
      <RouterView />
    </NMessageProvider>
  </NConfigProvider>
</template>

<script setup lang="ts">
import {
  NConfigProvider,
  NMessageProvider,
  darkTheme,
  type GlobalThemeOverrides,
} from "naive-ui";
import { useAppStore } from "./stores/app-store";

const { isDarkMode } = storeToRefs(useAppStore());

// See: https://www.naiveui.com/en-US/os-theme/docs/customize-theme
const themeOverrides: GlobalThemeOverrides = {
  common: {
    fontFamily: "'Google Sans Flex', sans-serif",
    borderRadius: "6px",
  },
};

const theme = computed(() => (isDarkMode.value ? darkTheme : undefined));
</script>

<style scoped></style>
