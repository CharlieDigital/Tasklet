<template>
  <div min-h-full py-4>
    <NAlert v-if="error" type="error" mb-4>
      {{ error }}
    </NAlert>

    <TaskletList
      :tasklets="tasklets"
      :loading="loading"
      :quick-add-loading="quickAddLoading"
      :dense="dense"
      empty-title="No completed tasklets"
      empty-description="Completed tasklets will appear here."
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
