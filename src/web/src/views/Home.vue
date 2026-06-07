<template>
  <!-- Authenticated landing surface used to verify Firebase and backend profile state. -->
  <section class="home-view">
    <h1>Home</h1>
    <p v-if="displayName">Signed in as {{ displayName }}</p>
  </section>
</template>

<script setup lang="ts">
import { storeToRefs } from "pinia";
import { useAppStore } from "@/stores/app-store";

const appStore = useAppStore();
const { displayName } = storeToRefs(appStore);
</script>

<style scoped>
.home-view {
  display: grid;
  gap: 8px;
}

.home-view h1 {
  font-size: 20px;
  font-weight: 500;
  line-height: 1.4;
  margin: 0;
}

.home-view p {
  margin: 0;
}
</style>
