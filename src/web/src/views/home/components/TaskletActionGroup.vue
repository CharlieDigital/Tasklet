<template>
  <NButtonGroup
    :size="size"
    class="tasklet-actions"
    :class="{ 'tasklet-actions-hidden': showOnHover && !visible }"
  >
    <NTooltip>
      <template #trigger>
        <NButton
          tertiary
          circle
          :aria-label="tasklet.pinned ? 'Unpin tasklet' : 'Pin tasklet'"
          @click="emit('pin', tasklet)"
        >
          <template #icon>
            <NIcon>
              <PinnedOff v-if="tasklet.pinned" />
              <Pin v-else />
            </NIcon>
          </template>
        </NButton>
      </template>
      {{ tasklet.pinned ? "Unpin" : "Pin" }}
    </NTooltip>

    <NTooltip>
      <template #trigger>
        <NButton
          tertiary
          circle
          aria-label="Complete tasklet"
          @click="emit('complete', tasklet)"
        >
          <template #icon>
            <NIcon>
              <Checkbox />
            </NIcon>
          </template>
        </NButton>
      </template>
      Complete
    </NTooltip>

    <NTooltip>
      <template #trigger>
        <NButton
          tertiary
          circle
          aria-label="Edit tasklet"
          @click="emit('edit', tasklet)"
        >
          <template #icon>
            <NIcon>
              <Edit />
            </NIcon>
          </template>
        </NButton>
      </template>
      Edit
    </NTooltip>

    <NPopconfirm
      positive-text="Delete"
      negative-text="Cancel"
      @positive-click="emit('delete', tasklet)"
    >
      <template #trigger>
        <NButton
          tertiary
          circle
          type="error"
          title="Delete"
          aria-label="Delete tasklet"
        >
          <template #icon>
            <NIcon>
              <Trash />
            </NIcon>
          </template>
        </NButton>
      </template>
      Delete this tasklet?
    </NPopconfirm>
  </NButtonGroup>
</template>

<script setup lang="ts">
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import { Checkbox, Edit, Pin, PinnedOff, Trash } from "@vicons/tabler";
import type { ButtonGroupProps } from "naive-ui";

withDefaults(
  defineProps<{
    tasklet: TaskletResponse;
    visible?: boolean;
    showOnHover?: boolean;
    size?: ButtonGroupProps["size"];
  }>(),
  {
    visible: true,
    showOnHover: false,
    size: "small",
  },
);

const emit = defineEmits<{
  pin: [tasklet: TaskletResponse];
  complete: [tasklet: TaskletResponse];
  edit: [tasklet: TaskletResponse];
  delete: [tasklet: TaskletResponse];
}>();
</script>

<style scoped>
.tasklet-actions {
  transition: opacity 0.16s ease;
}

.tasklet-actions-hidden {
  opacity: 0;
  pointer-events: none;
}

.tasklet-actions:focus-within {
  opacity: 1;
  pointer-events: auto;
}

@media (max-width: 560px) {
  .tasklet-actions-hidden {
    opacity: 1;
    pointer-events: auto;
  }
}
</style>
