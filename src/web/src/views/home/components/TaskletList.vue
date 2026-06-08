<template>
  <div min-h-full pb-6>
    <NSpin :show="loading">
      <NFlex v-if="tasklets.length > 0 && !dense" vertical size="medium">
        <TaskletCard
          v-for="tasklet in tasklets"
          :key="tasklet.id"
          :tasklet="tasklet"
          :show-pin-action="showPinAction"
          :show-complete-action="showCompleteAction"
          @pin="emit('pin', $event)"
          @complete="emit('complete', $event)"
          @edit="emit('edit', $event)"
          @delete="emit('delete', $event)"
        />
      </NFlex>
      <NDataTable
        v-else-if="tasklets.length > 0"
        class="tasklet-list-table"
        size="small"
        :columns="columns"
        :data="tasklets"
        :row-key="rowKey"
        :pagination="false"
      />

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
import TaskletActionGroup from "@/views/home/components/TaskletActionGroup.vue";
import TaskletCard from "@/views/home/components/TaskletCard.vue";
import {
  avatarColors,
  statusLabels,
} from "@/views/home/components/tasklet-display";
import { Pinned } from "@vicons/tabler";
import {
  NAvatar,
  NEllipsis,
  NFlex,
  NIcon,
  NTag,
  type DataTableColumns,
} from "naive-ui";
import { h } from "vue";

const props = withDefaults(
  defineProps<{
    tasklets: TaskletResponse[];
    loading: boolean;
    quickAddLoading: boolean;
    showEmptyQuickAdd?: boolean;
    emptyTitle: string;
    emptyDescription: string;
    dense: boolean;
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
  quickAdd: [title: string];
}>();

/**
 * Dense mode keeps the same row actions as card mode while reducing each
 * tasklet to the fields that support fast comparison.
 */
const columns = computed<DataTableColumns<TaskletResponse>>(() => [
  {
    title: "Color",
    key: "color",
    width: 132,
    resizable: true,
    render: renderColorCell,
  },
  {
    title: "Title",
    key: "title",
    minWidth: 220,
    resizable: true,
    render: renderTitleCell,
  },
  {
    title: "Status",
    key: "status",
    width: 132,
    resizable: true,
    render: renderStatusCell,
  },
  {
    title: "Actions",
    key: "actions",
    width: actionColumnWidth.value,
    resizable: true,
    render: renderActionsCell,
  },
]);

/**
 * Action visibility is owned by the active tab; dense mode mirrors that same
 * contract with a tighter column when Done only exposes edit/delete.
 */
const actionColumnWidth = computed(() => {
  const visibleActionCount = [
    props.showPinAction,
    props.showCompleteAction,
    true,
    true,
  ].filter(Boolean).length;

  return Math.max(96, visibleActionCount * 42);
});

function rowKey(tasklet: TaskletResponse) {
  return tasklet.id;
}

function renderColorCell(tasklet: TaskletResponse) {
  return h(
    NFlex,
    { align: "center", justify: "center", wrap: false },
    {
      default: () => [
        h(
          NAvatar,
          {
            size: 24,
            style: { backgroundColor: avatarColors[tasklet.color] },
          },
          tasklet.pinned
            ? {
                default: () =>
                  h(
                    NIcon,
                    { color: "white", size: 13 },
                    { default: () => h(Pinned) },
                  ),
              }
            : undefined,
        ),
      ],
    },
  );
}

function renderTitleCell(tasklet: TaskletResponse) {
  return h(
    NEllipsis,
    { class: "tasklet-list-title" },
    { default: () => tasklet.title },
  );
}

function renderStatusCell(tasklet: TaskletResponse) {
  return h(
    NTag,
    { size: "small", bordered: false },
    { default: () => statusLabels[tasklet.status] },
  );
}

function renderActionsCell(tasklet: TaskletResponse) {
  return h(TaskletActionGroup, {
    tasklet,
    showPinAction: props.showPinAction,
    showCompleteAction: props.showCompleteAction,
    onPin: (target: TaskletResponse) => emit("pin", target),
    onComplete: (target: TaskletResponse) => emit("complete", target),
    onEdit: (target: TaskletResponse) => emit("edit", target),
    onDelete: (target: TaskletResponse) => emit("delete", target),
  });
}
</script>

<style scoped>
.tasklet-list-table {
  min-width: 0;
}

:deep(.tasklet-list-title) {
  max-width: 100%;
}
</style>
