<template>
  <!--
    Home owns task-tab orchestration so child components can stay presentational
    while the store/API boundary is introduced in a later phase.
  -->
  <section class="task-home">
    <NCard
      class="task-home-card"
      content-class="task-home-card-content"
      title="Tasks"
    >
      <template #header-extra>
        <NFlex align="center" gap="small">
          <NTooltip trigger="hover">
            <template #trigger>
              <NSwitch
                v-model:value="denseMode"
                :round="false"
                size="large"
                class="task-density-switch"
                aria-label="Task list density"
              >
                <template #checked>Dense</template>
                <template #unchecked>Normal</template>
              </NSwitch>
            </template>
            {{ denseMode ? "Dense mode" : "Normal mode" }}
          </NTooltip>
          <NTooltip trigger="hover">
            <template #trigger>
              <NButton ghost disabled size="small">
                <template #icon>
                  <NIcon :component="User" :size="18" />
                </template>
              </NButton>
            </template>
            {{ displayName }}
          </NTooltip>
          <NPopconfirm
            positive-text="Logout"
            negative-text="Cancel"
            @positive-click="handleLogout"
          >
            <template #trigger>
              <NButton
                ghost
                size="small"
                aria-label="Logout"
                :loading="loginLoading"
              >
                <template #icon>
                  <NIcon :component="Logout" />
                </template>
              </NButton>
            </template>
            Sign out of Tasklet?
          </NPopconfirm>
        </NFlex>
      </template>

      <template #default>
        <NAlert
          v-if="taskletError"
          type="error"
          closable
          class="task-home-alert"
          @close="taskletStore.clearError"
        >
          {{ taskletError }}
        </NAlert>

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
                  {{ pinnedCount }}
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
                :loading="pinnedLoading"
                :quick-add-loading="saving"
                :error="null"
                :dense="denseMode"
                @pin="handlePin"
                @complete="handleComplete"
                @edit="openEditTab"
                @delete="handleDelete"
                @quick-add="handleQuickCreate($event, true)"
              />
            </NScrollbar>
          </NTabPane>
          <NTabPane name="all">
            <template #tab>
              <span class="task-tab-label">
                All
                <span class="task-tab-count">
                  {{ allCount }}
                </span>
              </span>
            </template>
            <NScrollbar
              trigger="hover"
              class="task-pane-scroll"
              content-class="task-pane-scroll-content"
            >
              <AllTaskletsTab
                :tasklets="activeTasklets"
                :loading="allLoading"
                :quick-add-loading="saving"
                :error="null"
                :dense="denseMode"
                @pin="handlePin"
                @complete="handleComplete"
                @edit="openEditTab"
                @delete="handleDelete"
                @quick-add="handleQuickCreate($event, false)"
              />
            </NScrollbar>
          </NTabPane>
          <NTabPane name="done">
            <template #tab>
              <span class="task-tab-label">
                Done
                <span class="task-tab-count">
                  {{ doneCount }}
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
                :loading="doneLoading"
                :quick-add-loading="saving"
                :error="null"
                :dense="denseMode"
                @pin="handlePin"
                @complete="handleComplete"
                @edit="openEditTab"
                @delete="handleDelete"
                @quick-add="handleQuickCreate($event, false)"
              />
            </NScrollbar>
          </NTabPane>
          <NTabPane name="create" tab="New">
            <NScrollbar
              trigger="hover"
              class="task-pane-scroll"
              content-class="task-pane-scroll-content"
            >
              <CreateTaskletTab
                :key="createFormKey"
                :loading="saving"
                @submit="handleCreate"
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
                :loading="saving"
                @submit="handleEdit(tasklet.id, $event)"
                @cancel="closeEditTab(tasklet.id)"
                @dirty-change="setEditDirty(tasklet.id, $event)"
              />
            </NScrollbar>
          </NTabPane>
        </NTabs>
      </template>

      <template v-if="showFooterQuickAdd" #footer>
        <TaskletQuickAdd
          full-width
          :loading="saving"
          placeholder="Add another tasklet"
          button-label="Add"
          @submit="handleQuickCreate($event, activeTab === 'pinned')"
        />
      </template>
    </NCard>
  </section>
</template>

<script setup lang="ts">
import { colorEnum } from "@/api/generated/types/Color";
import type { CreateTaskletRequest } from "@/api/generated/types/CreateTaskletRequest";
import { priorityEnum } from "@/api/generated/types/Priority";
import { statusEnum } from "@/api/generated/types/Status";
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import type { UpdateTaskletRequest } from "@/api/generated/types/UpdateTaskletRequest";
import TaskletQuickAdd from "@/components/TaskletQuickAdd.vue";
import AllTaskletsTab from "@/views/home/components/AllTaskletsTab.vue";
import CreateTaskletTab from "@/views/home/components/CreateTaskletTab.vue";
import DoneTaskletsTab from "@/views/home/components/DoneTaskletsTab.vue";
import EditTaskletTab from "@/views/home/components/EditTaskletTab.vue";
import PinnedTaskletsTab from "@/views/home/components/PinnedTaskletsTab.vue";
import { useAppStore } from "@/stores/app-store";
import { useTaskletStore } from "@/stores/tasklet-store";
import { Logout, User } from "@vicons/tabler";

const appStore = useAppStore();
const router = useRouter();
const { displayName, loginLoading } = storeToRefs(appStore);
const taskletStore = useTaskletStore();
const {
  allTasklets,
  activeTasklets,
  pinnedTasklets,
  doneTasklets,
  allLoading,
  pinnedLoading,
  doneLoading,
  saving,
  taskletError,
  allCount,
  pinnedCount,
  doneCount,
} = storeToRefs(taskletStore);

const activeTab = ref("pinned");
/**
 * Normal cards stay as the default for context-rich scanning; dense mode is an
 * opt-in table for users who want to compare more tasklets at once.
 */
const denseMode = ref(false);
const editingTaskletIds = ref<string[]>([]);
const editDirtyById = reactive<Record<string, boolean>>({});
const createFormKey = ref(0);

/**
 * Edit tabs resolve against every loaded list because a Tasklet may move between
 * Pinned, All, and Done after a quick card action refreshes API data.
 */
const taskletsById = computed(() => {
  const tasklets = new Map<string, TaskletResponse>();

  allTasklets.value.forEach((tasklet) => tasklets.set(tasklet.id, tasklet));
  pinnedTasklets.value.forEach((tasklet) => tasklets.set(tasklet.id, tasklet));
  doneTasklets.value.forEach((tasklet) => tasklets.set(tasklet.id, tasklet));

  return tasklets;
});
const editingTasklets = computed(() =>
  editingTaskletIds.value
    .map((id) => taskletsById.value.get(id))
    .filter((tasklet): tasklet is TaskletResponse => tasklet !== undefined),
);
const hasAnyTasklet = computed(() => allTasklets.value.length > 0);
const showFooterQuickAdd = computed(
  () => hasAnyTasklet.value && activeTab.value !== "done",
);

onMounted(() => {
  void taskletStore.loadTasklets();
});

function editTabName(id: string) {
  return `edit:${id}`;
}

/**
 * Keep sign-out on the Logout route so header actions and direct navigation
 * share the same auth guard behavior.
 */
async function handleLogout() {
  await router.push({ name: "Logout" });
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

async function handlePin(tasklet: TaskletResponse) {
  await runTaskletAction(() => taskletStore.togglePinned(tasklet));
}

async function handleComplete(tasklet: TaskletResponse) {
  await runTaskletAction(() => taskletStore.completeTasklet(tasklet.id));
}

async function handleDelete(tasklet: TaskletResponse) {
  await runTaskletAction(async () => {
    await taskletStore.deleteTasklet(tasklet.id);
    closeEditTab(tasklet.id);
  });
}

async function handleCreate(request: CreateTaskletRequest) {
  await createTasklet(request, "all");
}

async function createTasklet(
  request: CreateTaskletRequest,
  nextTab: string | null,
) {
  await runTaskletAction(async () => {
    await taskletStore.createTasklet(request);
    createFormKey.value += 1;

    if (nextTab !== null) {
      activeTab.value = nextTab;
    }
  });
}

async function handleQuickCreate(title: string, pinned: boolean) {
  await createTasklet(
    {
      title,
      body: null,
      status: statusEnum.NotStarted,
      priority: priorityEnum.Medium,
      explicitOrder: null,
      pinned,
      color: colorEnum.Emerald,
      completedAtUtc: null,
      dueAtUtc: null,
    },
    null,
  );
}

async function handleEdit(id: string, request: UpdateTaskletRequest) {
  await runTaskletAction(async () => {
    await taskletStore.updateTasklet(id, request);
    closeEditTab(id);
  });
}

async function runTaskletAction(action: () => Promise<void>) {
  try {
    await action();
  } catch {
    // The Tasklet store owns user-facing error text for failed API actions.
  }
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

.task-home-alert {
  margin-top: 16px;
}

.task-home-title {
  margin: 2px 0 0;
}

.task-density-switch {
  --n-rail-width: 72px;
}

:deep(.task-density-switch .n-switch__rail),
:deep(.task-density-switch .n-switch__button) {
  border-radius: 4px;
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
  font-size: 10px;
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
