<template>
  <NCard ref="cardElement" class="!rounded-lg" hoverable>
    <NThing class="tasklet-thing">
      <template #avatar>
        <NAvatar :size="28" shrink-0 :style="{ backgroundColor: avatarColor }">
          <NIcon v-if="tasklet.pinned" text-white size="15">
            <Pinned />
          </NIcon>
        </NAvatar>
      </template>

      <template #header>
        <div class="leading-[1.35]" pt-1 wrap-anywhere font-semibold>
          {{ tasklet.title }}
        </div>
      </template>

      <template #header-extra>
        <TaskletActionGroup
          :tasklet="tasklet"
          :visible="isHovered"
          :show-pin-action="showPinAction"
          :show-complete-action="showCompleteAction"
          show-on-hover
          @pin="emit('pin', $event)"
          @complete="emit('complete', $event)"
          @edit="emit('edit', $event)"
          @delete="emit('delete', $event)"
        />
      </template>

      <NText v-if="tasklet.body.trim().length > 0">
        {{ tasklet.body }}
      </NText>
      <NText v-else depth="3">No notes yet.</NText>

      <template #footer>
        <NFlex
          flex-wrap
          class="!gap-y-1.5"
          justify="space-between"
          align="center"
        >
          <NFlex flex-wrap align="center" size="small">
            <NTag size="small" :bordered="false">{{ statusLabel }}</NTag>
            <NTag size="small" :bordered="false" :type="priorityTagType">
              {{ priorityLabel }}
            </NTag>
            <NText depth="3">Created {{ createdRelative }}</NText>
          </NFlex>

          <NFlex flex-wrap ml-auto align="center" size="small">
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
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import TaskletActionGroup from "@/views/home/components/TaskletActionGroup.vue";
import {
  avatarColors,
  priorityLabels,
  priorityTagTypes,
  statusLabels,
} from "@/views/home/components/tasklet-display";
import { useElementHover } from "@vueuse/core";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import { Pinned } from "@vicons/tabler";

dayjs.extend(relativeTime);

const props = withDefaults(
  defineProps<{
    tasklet: TaskletResponse;
    showPinAction?: boolean;
    showCompleteAction?: boolean;
  }>(),
  {
    showPinAction: true,
    showCompleteAction: true,
  },
);

const emit = defineEmits<{
  pin: [tasklet: TaskletResponse];
  complete: [tasklet: TaskletResponse];
  edit: [tasklet: TaskletResponse];
  delete: [tasklet: TaskletResponse];
}>();

const cardElement = useTemplateRef<HTMLElement>("cardElement");
const isHovered = useElementHover(cardElement);

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

@media (max-width: 560px) {
  :deep(.tasklet-thing .n-thing-header) {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
  }

  :deep(.tasklet-thing .n-thing-header__extra) {
    justify-self: start;
  }
}
</style>
