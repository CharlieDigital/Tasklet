<template>
  <NCard ref="cardElement" class="tasklet-card" hoverable>
    <NThing class="tasklet-thing">
      <template #avatar>
        <NAvatar
          :size="28"
          class="tasklet-avatar"
          :style="{ backgroundColor: avatarColor }"
        >
          <NIcon v-if="tasklet.pinned" class="tasklet-avatar-icon" size="15">
            <Pinned />
          </NIcon>
        </NAvatar>
      </template>

      <template #header>
        <div class="tasklet-title" pt-1>{{ tasklet.title }}</div>
      </template>

      <template #header-extra>
        <NButtonGroup
          size="small"
          class="tasklet-actions"
          :class="{ 'tasklet-actions-visible': isHovered }"
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
              <NTooltip>
                <template #trigger>
                  <NButton
                    tertiary
                    circle
                    type="error"
                    aria-label="Delete tasklet"
                  >
                    <template #icon>
                      <NIcon>
                        <Trash />
                      </NIcon>
                    </template>
                  </NButton>
                </template>
                Delete
              </NTooltip>
            </template>
            Delete this tasklet?
          </NPopconfirm>
        </NButtonGroup>
      </template>

      <NText v-if="tasklet.body.trim().length > 0">
        {{ tasklet.body }}
      </NText>
      <NText v-else depth="3">No notes yet.</NText>

      <template #footer>
        <NFlex class="tasklet-footer" justify="space-between" align="center">
          <NFlex align="center" size="small" class="tasklet-footer-group">
            <NTag size="small" :bordered="false">{{ statusLabel }}</NTag>
            <NTag size="small" :bordered="false" :type="priorityTagType">
              {{ priorityLabel }}
            </NTag>
            <NText depth="3">Created {{ createdRelative }}</NText>
          </NFlex>

          <NFlex align="center" size="small" class="tasklet-footer-dates">
            <NText v-if="tasklet.dueAtUtc" depth="3">Due {{ dueDate }}</NText>
            <NText v-if="tasklet.completedAtUtc" depth="3">
              Completed {{ completedDate }}
            </NText>
          </NFlex>
        </NFlex>
      </template>
    </NThing>
  </NCard>
</template>

<script setup lang="ts">
import type { Color } from "@/api/generated/types/Color";
import type { Priority } from "@/api/generated/types/Priority";
import type { Status } from "@/api/generated/types/Status";
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import { useElementHover } from "@vueuse/core";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import { Checkbox, Edit, Pin, Pinned, PinnedOff, Trash } from "@vicons/tabler";

dayjs.extend(relativeTime);

const props = defineProps<{
  tasklet: TaskletResponse;
}>();

const emit = defineEmits<{
  pin: [tasklet: TaskletResponse];
  complete: [tasklet: TaskletResponse];
  edit: [tasklet: TaskletResponse];
  delete: [tasklet: TaskletResponse];
}>();

const cardElement = useTemplateRef<HTMLElement>("cardElement");
const isHovered = useElementHover(cardElement);

const avatarColors: Record<Color, string> = {
  Amber: "#f59e0b",
  Lime: "#84cc16",
  Emerald: "#10b981",
  Cyan: "#06b6d4",
  Blue: "#3b82f6",
  Indigo: "#6366f1",
  Purple: "#a855f7",
  Pink: "#ec4899",
  Rose: "#f43f5e",
};

const statusLabels: Record<Status, string> = {
  NotStarted: "Not started",
  InProgress: "In progress",
  Completed: "Completed",
  Blocked: "Blocked",
};

const priorityLabels: Record<Priority, string> = {
  Eventually: "Eventually",
  Low: "Low",
  Medium: "Medium",
  High: "High",
  Critical: "Critical",
};

const priorityTagTypes: Record<
  Priority,
  "default" | "error" | "success" | "warning" | "primary" | "info"
> = {
  Eventually: "default",
  Low: "info",
  Medium: "primary",
  High: "warning",
  Critical: "error",
};

const avatarColor = computed(() => avatarColors[props.tasklet.color]);
const statusLabel = computed(() => statusLabels[props.tasklet.status]);
const priorityLabel = computed(() => priorityLabels[props.tasklet.priority]);
const priorityTagType = computed(
  () => priorityTagTypes[props.tasklet.priority],
);
const createdRelative = computed(() =>
  dayjs(props.tasklet.createdAtUtc).fromNow(),
);
const dueDate = computed(() => formatDate(props.tasklet.dueAtUtc));
const completedDate = computed(() => formatDate(props.tasklet.completedAtUtc));

function formatDate(value: Date | null) {
  if (value === null) {
    return "";
  }

  return dayjs(value).format("MMM D");
}
</script>

<style scoped>
.tasklet-card {
  border-radius: 8px;
}

.tasklet-avatar {
  flex-shrink: 0;
}

.tasklet-avatar-icon {
  color: white;
}

.tasklet-title {
  overflow-wrap: anywhere;
  font-weight: 600;
  line-height: 1.35;
}

.tasklet-footer {
  flex-wrap: wrap;
  row-gap: 6px;
}

.tasklet-footer-group,
.tasklet-footer-dates {
  flex-wrap: wrap;
}

.tasklet-footer-dates {
  margin-left: auto;
}

:deep(.tasklet-thing .n-thing-header) {
  align-items: flex-start;
  gap: 12px;
}

:deep(.tasklet-thing .n-thing-header__title) {
  min-width: 0;
}

:deep(.tasklet-thing .n-thing-header__extra) {
  flex-shrink: 0;
}

.tasklet-actions {
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.16s ease;
}

.tasklet-actions-visible,
.tasklet-actions:focus-within {
  opacity: 1;
  pointer-events: auto;
}

@media (max-width: 560px) {
  :deep(.tasklet-thing .n-thing-header) {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
  }

  :deep(.tasklet-thing .n-thing-header__extra) {
    justify-self: start;
  }

  .tasklet-actions {
    opacity: 1;
    pointer-events: auto;
  }
}
</style>
