<template>
  <!--
    Home owns task-tab orchestration so child components can stay presentational
    while the store/API boundary is introduced in a later phase.
  -->
  <section class="task-home">
    <NCard class="task-home-card" content-class="task-home-card-content">
      <div class="task-home-header">
        <div>
          <div class="task-home-eyebrow">Signed in as {{ displayName }}</div>
          <NH2 class="task-home-title">Tasklets</NH2>
        </div>
      </div>

      <NTabs
        v-model:value="activeTab"
        type="line"
        animated
        class="task-tabs"
        @close="handleCloseEditTab"
      >
        <NTabPane name="pinned">
          <template #tab>
            <span class="task-tab-label">
              Pinned
              <span class="task-tab-count">
                {{ pinnedTasklets.length }}
              </span>
            </span>
          </template>
          <NScrollbar
            trigger="hover"
            class="task-pane-scroll"
            content-class="task-pane-scroll-content"
          >
            <PinnedTaskletsTab
              :tasklets="pinnedTasklets"
              :loading="false"
              :error="null"
              @pin="handleVisualPin"
              @complete="handleVisualComplete"
              @edit="openEditTab"
              @delete="handleVisualDelete"
            />
          </NScrollbar>
        </NTabPane>
        <NTabPane name="all">
          <template #tab>
            <span class="task-tab-label">
              All
              <span class="task-tab-count">
                {{ visualTasklets.length }}
              </span>
            </span>
          </template>
          <NScrollbar
            trigger="hover"
            class="task-pane-scroll"
            content-class="task-pane-scroll-content"
          >
            <AllTaskletsTab
              :tasklets="visualTasklets"
              :loading="false"
              :error="null"
              @pin="handleVisualPin"
              @complete="handleVisualComplete"
              @edit="openEditTab"
              @delete="handleVisualDelete"
            />
          </NScrollbar>
        </NTabPane>
        <NTabPane name="done">
          <template #tab>
            <span class="task-tab-label">
              Done
              <span class="task-tab-count">
                {{ doneTasklets.length }}
              </span>
            </span>
          </template>
          <NScrollbar
            trigger="hover"
            class="task-pane-scroll"
            content-class="task-pane-scroll-content"
          >
            <DoneTaskletsTab
              :tasklets="doneTasklets"
              :loading="false"
              :error="null"
              @pin="handleVisualPin"
              @complete="handleVisualComplete"
              @edit="openEditTab"
              @delete="handleVisualDelete"
            />
          </NScrollbar>
        </NTabPane>
        <NTabPane name="create" tab="Create">
          <NScrollbar
            trigger="hover"
            class="task-pane-scroll"
            content-class="task-pane-scroll-content"
          >
            <CreateTaskletTab
              :key="createFormKey"
              :loading="false"
              @submit="handleVisualCreate"
              @cancel="activeTab = 'pinned'"
            />
          </NScrollbar>
        </NTabPane>
        <NTabPane
          v-for="tasklet in editingTasklets"
          :key="tasklet.id"
          :name="editTabName(tasklet.id)"
          closable
        >
          <template #tab>
            <span class="task-tab-label task-tab-edit-label">
              {{ tasklet.title }}
              <span v-if="editDirtyById[tasklet.id]" class="task-tab-dirty">
                <span class="task-tab-dirty-dot" />
              </span>
            </span>
          </template>

          <NScrollbar
            trigger="hover"
            class="task-pane-scroll"
            content-class="task-pane-scroll-content"
          >
            <EditTaskletTab
              :tasklet="tasklet"
              :loading="false"
              @submit="handleVisualEdit(tasklet.id, $event)"
              @cancel="closeEditTab(tasklet.id)"
              @dirty-change="setEditDirty(tasklet.id, $event)"
            />
          </NScrollbar>
        </NTabPane>
      </NTabs>
    </NCard>
  </section>
</template>

<script setup lang="ts">
import type { CreateTaskletRequest } from "@/api/generated/types/CreateTaskletRequest";
import { colorEnum } from "@/api/generated/types/Color";
import { priorityEnum } from "@/api/generated/types/Priority";
import { statusEnum } from "@/api/generated/types/Status";
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import type { UpdateTaskletRequest } from "@/api/generated/types/UpdateTaskletRequest";
import AllTaskletsTab from "@/views/home/components/AllTaskletsTab.vue";
import CreateTaskletTab from "@/views/home/components/CreateTaskletTab.vue";
import DoneTaskletsTab from "@/views/home/components/DoneTaskletsTab.vue";
import EditTaskletTab from "@/views/home/components/EditTaskletTab.vue";
import PinnedTaskletsTab from "@/views/home/components/PinnedTaskletsTab.vue";
import { taskletFixtures } from "@/views/home/tasklet-fixtures";
import { useAppStore } from "@/stores/app-store";

const appStore = useAppStore();
const { displayName } = storeToRefs(appStore);

const activeTab = ref("pinned");
const visualTasklets = ref<TaskletResponse[]>(
  taskletFixtures.map((tasklet) => ({ ...tasklet })),
);
const editingTaskletIds = ref<string[]>([]);
const editDirtyById = reactive<Record<string, boolean>>({});
const createFormKey = ref(0);

const pinnedTasklets = computed(() =>
  visualTasklets.value.filter((tasklet) => tasklet.pinned),
);
const doneTasklets = computed(() =>
  visualTasklets.value.filter(
    (tasklet) => tasklet.status === statusEnum.Completed,
  ),
);
const editingTasklets = computed(() =>
  editingTaskletIds.value
    .map((id) => visualTasklets.value.find((tasklet) => tasklet.id === id))
    .filter((tasklet): tasklet is TaskletResponse => tasklet !== undefined),
);

function editTabName(id: string) {
  return `edit:${id}`;
}

function openEditTab(tasklet: TaskletResponse) {
  if (!editingTaskletIds.value.includes(tasklet.id)) {
    editingTaskletIds.value.push(tasklet.id);
  }

  activeTab.value = editTabName(tasklet.id);
}

function closeEditTab(id: string) {
  editingTaskletIds.value = editingTaskletIds.value.filter(
    (taskletId) => taskletId !== id,
  );
  delete editDirtyById[id];

  if (activeTab.value === editTabName(id)) {
    activeTab.value = "all";
  }
}

function handleCloseEditTab(name: string | number) {
  const tabName = String(name);

  if (!tabName.startsWith("edit:")) {
    return;
  }

  closeEditTab(tabName.slice("edit:".length));
}

function setEditDirty(id: string, dirty: boolean) {
  editDirtyById[id] = dirty;
}

function handleVisualPin(tasklet: TaskletResponse) {
  replaceVisualTasklet(tasklet.id, { pinned: !tasklet.pinned });
}

function handleVisualComplete(tasklet: TaskletResponse) {
  replaceVisualTasklet(tasklet.id, {
    status: statusEnum.Completed,
    completedAtUtc: new Date(),
  });
}

function handleVisualDelete(tasklet: TaskletResponse) {
  visualTasklets.value = visualTasklets.value.filter(
    (current) => current.id !== tasklet.id,
  );
  closeEditTab(tasklet.id);
}

function handleVisualCreate(request: CreateTaskletRequest) {
  const now = new Date();

  visualTasklets.value = [
    {
      id: crypto.randomUUID(),
      userId: "visual-user",
      title: request.title,
      body: request.body ?? "",
      status: request.status ?? statusEnum.NotStarted,
      priority: request.priority ?? priorityEnum.Medium,
      explicitOrder: request.explicitOrder,
      pinned: request.pinned ?? false,
      color: request.color ?? colorEnum.Emerald,
      createdAtUtc: now,
      completedAtUtc: request.completedAtUtc,
      dueAtUtc: request.dueAtUtc,
    },
    ...visualTasklets.value,
  ];
  createFormKey.value += 1;
  activeTab.value = "all";
}

function handleVisualEdit(id: string, request: UpdateTaskletRequest) {
  replaceVisualTasklet(id, {
    title: request.title,
    body: request.body ?? "",
    status: request.status,
    priority: request.priority,
    explicitOrder: request.explicitOrder,
    pinned: request.pinned,
    color: request.color,
    completedAtUtc: request.completedAtUtc,
    dueAtUtc: request.dueAtUtc,
  });
  closeEditTab(id);
}

function replaceVisualTasklet(
  id: string,
  patch: Partial<Omit<TaskletResponse, "id" | "userId" | "createdAtUtc">>,
) {
  visualTasklets.value = visualTasklets.value.map((tasklet) =>
    tasklet.id === id ? { ...tasklet, ...patch } : tasklet,
  );
}
</script>

<style scoped>
.task-home {
  width: min(100%, 896px);
  height: calc(100vh - 60px - 32px);
  min-height: 0;
  margin: 0 auto;
}

.task-home-card {
  height: 100%;
}

:deep(.task-home-card-content) {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
}

.task-home-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.task-home-eyebrow {
  color: var(--n-text-color-3);
  font-size: 13px;
}

.task-home-title {
  margin: 2px 0 0;
}

.task-tab-label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.task-tab-count {
  display: inline-grid;
  min-width: 18px;
  height: 18px;
  place-items: center;
  border-radius: 999px;
  background: #059669;
  color: white;
  font-size: 11px;
  font-weight: 600;
  line-height: 1;
}

.task-tab-edit-label {
  max-width: 180px;
}

.task-tab-dirty {
  display: inline-grid;
  width: 18px;
  height: 18px;
  place-items: center;
  border-radius: 999px;
  background: #059669;
}

.task-tab-dirty-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
  color: white;
  background: currentColor;
}

.task-tabs {
  min-height: 0;
  flex: 1;
}

:deep(.task-tabs .n-tabs-pane-wrapper) {
  min-height: 0;
  flex: 1;
}

:deep(.task-tabs .n-tab-pane) {
  height: 100%;
  min-height: 0;
  overflow: hidden;
}

.task-pane-scroll {
  height: 100%;
}

:deep(.task-pane-scroll-content) {
  min-height: 100%;
  box-sizing: border-box;
}
</style>
