<template>
  <div class="tasklet-tab-pane">
    <TaskletForm
      mode="edit"
      :tasklet="tasklet"
      :loading="loading"
      submit-label="Save changes"
      @submit="emit('submit', $event as UpdateTaskletRequest)"
      @cancel="emit('cancel')"
      @dirty-change="emit('dirtyChange', $event)"
    />
  </div>
</template>

<script setup lang="ts">
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import type { UpdateTaskletRequest } from "@/api/generated/types/UpdateTaskletRequest";
import TaskletForm from "@/views/home/components/TaskletForm.vue";

defineProps<{
  tasklet: TaskletResponse;
  loading: boolean;
}>();

const emit = defineEmits<{
  submit: [request: UpdateTaskletRequest];
  cancel: [];
  dirtyChange: [dirty: boolean];
}>();
</script>

<style scoped>
.tasklet-tab-pane {
  min-height: 100%;
  padding: 16px 0;
}
</style>
