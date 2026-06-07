<template>
  <!--
    Home dashboard for authenticated users. The Code Reviews entry routes to the
    same surface that GitHub request links use after the auth guard completes.
  -->
  <NFlex vertical align="center">
    <NCard max-w-4xl>
      <NH3>Home</NH3>
      <p v-if="displayName">Signed in as {{ displayName }}</p>
    </NCard>
  </NFlex>
</template>

<script setup lang="ts">
import { storeToRefs } from "pinia";
import { useAppStore } from "@/stores/app-store";

const appStore = useAppStore();
const { displayName } = storeToRefs(appStore);
</script>

<style scoped></style>
