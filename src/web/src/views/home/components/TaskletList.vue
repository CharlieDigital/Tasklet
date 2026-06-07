<template>
  <div min-h-full pb-6>
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
          <NFlex vertical align="center" size="medium">
            <NText depth="3">{{ emptyTitle }}</NText>
            <TaskletQuickAdd
              v-if="showEmptyQuickAdd"
              :loading="quickAddLoading"
              @submit="emit('quickAdd', $event)"
            />
          </NFlex>
        </template>
      </NEmpty>
    </NSpin>
  </div>
</template>

<script setup lang="ts">
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import TaskletQuickAdd from "@/components/TaskletQuickAdd.vue";
import TaskletCard from "@/views/home/components/TaskletCard.vue";

defineProps<{
  tasklets: TaskletResponse[];
  loading: boolean;
  quickAddLoading: boolean;
  showEmptyQuickAdd?: boolean;
  emptyTitle: string;
  emptyDescription: string;
}>();

const emit = defineEmits<{
  pin: [tasklet: TaskletResponse];
  complete: [tasklet: TaskletResponse];
  edit: [tasklet: TaskletResponse];
  delete: [tasklet: TaskletResponse];
  quickAdd: [title: string];
}>();
</script>
