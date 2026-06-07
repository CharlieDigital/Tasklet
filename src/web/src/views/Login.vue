<!--
  Login page; user redirected here if there is no active session.
-->

<template>
  <NFlex justify="center" align="center" vertical>
    <NCard
      :segmented="{
        content: true,
      }"
      size="small"
      max-w-lg
      mt-24
    >
      <template #header>Login</template>

      <template #default>
        <!-- Error message shown when the popup fails or auth is rejected -->
        <NAlert
          v-if="loginError"
          type="error"
          mb-3
          closable
          @close="clearError"
        >
          {{ loginError }}
        </NAlert>

        <NSpace size="large" vertical>
          <NButton
            secondary
            type="success"
            size="large"
            w-full
            :loading="loginLoading"
            :disabled="loginLoading || isAuthenticated"
            @click="handleLogin()"
          >
            <template #icon>
              <NIcon :component="BrandGoogle" />
            </template>
            Login with Google
          </NButton>
        </NSpace>
      </template>
    </NCard>
  </NFlex>
</template>

<script setup lang="ts">
import { BrandGoogle } from "@vicons/tabler";
import { storeToRefs } from "pinia";
import { useAppStore } from "@/stores/app-store";
import { useRoute, useRouter } from "vue-router";

const appStore = useAppStore();
const router = useRouter();
const route = useRoute();
const { isAuthenticated, loginLoading, loginError } = storeToRefs(appStore);

function clearError() {
  loginError.value = null;
}

async function handleLogin() {
  try {
    await appStore.login();
    const redirect = route.query.redirect;

    await router.push(
      typeof redirect === "string" ? redirect : { name: "Home" },
    );
  } catch {
    // Error is already set on appStore.loginError by the login() action.
    // The NAlert above will display it; no additional handling needed here.
  }
}
</script>
