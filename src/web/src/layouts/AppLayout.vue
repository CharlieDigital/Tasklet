<!--
Main outer chrome for the app. Navigation for this slice lives inside the
current view, so the shell keeps only the top-level application header.
-->

<template>
  <NLayout class="app-layout">
    <NLayout>
      <!-- Right side content header -->
      <NLayoutHeader bordered class="app-layout-header" position="absolute">
        <NSpace justify="space-between" h-full items-center>
          <div h-full flex items-center text-xl px-4>
            <span font-600>Tasklet</span>&nbsp;
            <span font-300 text-gray-400>a TODO list app</span>
          </div>
          <NButtonGroup>
            <NButton @click="isDarkMode = !isDarkMode" mr-6>
              <template #icon>
                <Sun v-if="!isDarkMode" />
                <Moon v-else />
              </template>
            </NButton>
          </NButtonGroup>
        </NSpace>
      </NLayoutHeader>
      <!-- Main layout on right side with content -->
      <NLayoutContent class="app-layout-content">
        <div class="app-content">
          <RouterView v-slot="{ Component }">
            <component :is="Component" />
          </RouterView>
        </div>
      </NLayoutContent>
    </NLayout>
  </NLayout>
</template>

<script setup lang="ts">
import {
  NLayout,
  NLayoutHeader,
  NLayoutContent,
  NSpace,
  NButton,
  NButtonGroup,
} from "naive-ui";
import { Sun, Moon } from "@vicons/tabler";
import { useAppStore } from "@/stores/app-store";

const appStore = useAppStore();

useTitle("Tasklet");

const { isDarkMode } = storeToRefs(appStore);
</script>

<style lang="scss" scoped>
.app-layout {
  height: 100vh;

  &-header {
    z-index: 100;
    height: 60px;
  }

  &-content {
    --navbar-height: 60px;
    margin: var(--navbar-height) 8px 0;
    height: calc(100vh - var(--navbar-height));
  }

  .app-content {
    border-radius: 4px;
    margin: 16px;
  }
}
</style>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
