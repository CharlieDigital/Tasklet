<template>
  <div class="tasklet-tab-pane">
    <TaskletForm
      mode="create"
      :loading="loading"
      submit-label="Create tasklet"
      @submit="emit('submit', $event as CreateTaskletRequest)"
      @cancel="emit('cancel')"
    />
  </div>
</template>

<script setup lang="ts">
import type { CreateTaskletRequest } from "@/api/generated/types/CreateTaskletRequest";
import TaskletForm from "@/views/home/components/TaskletForm.vue";

defineProps<{
  loading: boolean;
}>();

const emit = defineEmits<{
  submit: [request: CreateTaskletRequest];
  cancel: [];
}>();
</script>

<style scoped>
.tasklet-tab-pane {
  min-height: 100%;
  padding: 16px 0;
}
</style>
