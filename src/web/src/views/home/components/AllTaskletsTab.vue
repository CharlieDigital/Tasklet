<template>
  <div min-h-full py-4>
    <NAlert v-if="error" type="error" mb-4>
      {{ error }}
    </NAlert>

    <TaskletList
      :tasklets="tasklets"
      :loading="loading"
      :quick-add-loading="quickAddLoading"
      :show-empty-quick-add="true"
      :dense="dense"
      empty-title="No tasklets"
      empty-description="Create a tasklet to start tracking your work."
      @pin="emit('pin', $event)"
      @complete="emit('complete', $event)"
      @edit="emit('edit', $event)"
      @delete="emit('delete', $event)"
      @quick-add="emit('quickAdd', $event)"
    />
  </div>
</template>

<script setup lang="ts">
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import TaskletList from "@/views/home/components/TaskletList.vue";

defineProps<{
  tasklets: TaskletResponse[];
  loading: boolean;
  quickAddLoading: boolean;
  error: string | null;
  dense: boolean;
}>();

const emit = defineEmits<{
  pin: [tasklet: TaskletResponse];
  complete: [tasklet: TaskletResponse];
  edit: [tasklet: TaskletResponse];
  delete: [tasklet: TaskletResponse];
  quickAdd: [title: string];
}>();
</script>
