<template>
  <div class="tasklet-list">
    <NSpin :show="loading">
      <NFlex v-if="tasklets.length > 0" vertical size="medium">
        <TaskletCard
          v-for="tasklet in tasklets"
          :key="tasklet.id"
          :tasklet="tasklet"
          @pin="emit('pin', $event)"
          @complete="emit('complete', $event)"
          @edit="emit('edit', $event)"
          @delete="emit('delete', $event)"
        />
      </NFlex>

      <NEmpty v-else :description="emptyDescription">
        <template #extra>
          <NText depth="3">{{ emptyTitle }}</NText>
        </template>
      </NEmpty>
    </NSpin>
  </div>
</template>

<script setup lang="ts">
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import TaskletCard from "@/views/home/components/TaskletCard.vue";

defineProps<{
  tasklets: TaskletResponse[];
  loading: boolean;
  emptyTitle: string;
  emptyDescription: string;
}>();

const emit = defineEmits<{
  pin: [tasklet: TaskletResponse];
  complete: [tasklet: TaskletResponse];
  edit: [tasklet: TaskletResponse];
  delete: [tasklet: TaskletResponse];
}>();
</script>

<style scoped>
.tasklet-list {
  min-height: 100%;
  padding: 16px 0 24px;
}
</style>
