import { Tasklet } from "@/api/generated/clients/Tasklet";
import { sortDirectionEnum } from "@/api/generated/types/SortDirection";
import { statusEnum } from "@/api/generated/types/Status";
import { taskletSortFieldEnum } from "@/api/generated/types/TaskletSortField";
import type { CreateTaskletRequest } from "@/api/generated/types/CreateTaskletRequest";
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import type { UpdateTaskletRequest } from "@/api/generated/types/UpdateTaskletRequest";

const defaultListParams = {
  sort: taskletSortFieldEnum.CreatedAtUtc,
  direction: sortDirectionEnum.Descending,
};

/**
 * Tasklet API store.
 *
 * Home is the only view that consumes this store directly; children receive
 * task state as props and report user actions through emits.
 */
export const useTaskletStore = defineStore("tasklet", () => {
  const allTasklets = ref<TaskletResponse[]>([]);
  const pinnedTasklets = ref<TaskletResponse[]>([]);
  const doneTasklets = ref<TaskletResponse[]>([]);
  const allLoading = ref(false);
  const pinnedLoading = ref(false);
  const doneLoading = ref(false);
  const saving = ref(false);
  const deletingIds = ref<Set<string>>(new Set());
  const taskletError = ref<string | null>(null);

  /**
   * "All" means all active work in the UI. Completed Tasklets have their own
   * Done tab and should not duplicate into the active work list.
   */
  const activeTasklets = computed(() =>
    allTasklets.value.filter(
      (tasklet) => tasklet.status !== statusEnum.Completed,
    ),
  );
  const allCount = computed(() => activeTasklets.value.length);
  const pinnedCount = computed(() => pinnedTasklets.value.length);
  const doneCount = computed(() => doneTasklets.value.length);

  async function loadAllTasklets() {
    allLoading.value = true;

    try {
      const response = await Tasklet.listTasklets(defaultListParams);
      allTasklets.value = normalizeTaskletDates(response.items);
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
    } finally {
      allLoading.value = false;
    }
  }

  async function loadPinnedTasklets() {
    pinnedLoading.value = true;

    try {
      const response = await Tasklet.listPinnedTasklets(defaultListParams);
      pinnedTasklets.value = normalizeTaskletDates(response.items);
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
    } finally {
      pinnedLoading.value = false;
    }
  }

  async function loadDoneTasklets() {
    doneLoading.value = true;

    try {
      const response = await Tasklet.listDoneTasklets(defaultListParams);
      doneTasklets.value = normalizeTaskletDates(response.items);
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
    } finally {
      doneLoading.value = false;
    }
  }

  async function loadTasklets() {
    clearError();

    await Promise.all([
      loadAllTasklets(),
      loadPinnedTasklets(),
      loadDoneTasklets(),
    ]);
  }

  async function createTasklet(request: CreateTaskletRequest) {
    saving.value = true;
    clearError();

    try {
      await Tasklet.createTasklet(request);
      await loadTasklets();
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
      throw error;
    } finally {
      saving.value = false;
    }
  }

  async function updateTasklet(id: string, request: UpdateTaskletRequest) {
    saving.value = true;
    clearError();

    try {
      await Tasklet.updateTasklet(id, request);
      await loadTasklets();
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
      throw error;
    } finally {
      saving.value = false;
    }
  }

  /**
   * Uses direct pin/unpin operations instead of rebuilding full update payloads.
   * The backend owns the scoped one-field mutation, which avoids stale form data
   * overwriting other Tasklet fields during a quick card action.
   */
  async function togglePinned(tasklet: TaskletResponse) {
    saving.value = true;
    clearError();

    try {
      if (tasklet.pinned) {
        await Tasklet.unpinTasklet(tasklet.id);
      } else {
        await Tasklet.pinTasklet(tasklet.id);
      }

      await loadTasklets();
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
      throw error;
    } finally {
      saving.value = false;
    }
  }

  /**
   * Completion is a direct API action so the server controls the completion
   * timestamp and clients do not need to post every mutable field.
   */
  async function completeTasklet(id: string) {
    saving.value = true;
    clearError();

    try {
      await Tasklet.completeTasklet(id);
      await loadTasklets();
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
      throw error;
    } finally {
      saving.value = false;
    }
  }

  async function deleteTasklet(id: string) {
    deletingIds.value = new Set([...deletingIds.value, id]);
    clearError();

    try {
      await Tasklet.deleteTasklet(id);
      await loadTasklets();
    } catch (error: unknown) {
      taskletError.value = toUserError(error);
      throw error;
    } finally {
      deletingIds.value = new Set(
        [...deletingIds.value].filter((deletedId) => deletedId !== id),
      );
    }
  }

  function clearError() {
    taskletError.value = null;
  }

  return {
    allTasklets,
    activeTasklets,
    pinnedTasklets,
    doneTasklets,
    allLoading,
    pinnedLoading,
    doneLoading,
    saving,
    deletingIds,
    taskletError,
    allCount,
    pinnedCount,
    doneCount,
    loadAllTasklets,
    loadPinnedTasklets,
    loadDoneTasklets,
    loadTasklets,
    createTasklet,
    updateTasklet,
    togglePinned,
    completeTasklet,
    deleteTasklet,
    clearError,
  };
});

function normalizeTaskletDates(tasklets: TaskletResponse[]) {
  return tasklets.map((tasklet) => ({
    ...tasklet,
    createdAtUtc: normalizeRequiredDate(tasklet.createdAtUtc),
    completedAtUtc: normalizeDate(tasklet.completedAtUtc),
    dueAtUtc: normalizeDate(tasklet.dueAtUtc),
  }));
}

function normalizeRequiredDate(value: Date | string) {
  if (typeof value !== "string") {
    return new Date(value);
  }

  return new Date(hasTimeZoneOffset(value) ? value : `${value}Z`);
}

function normalizeDate(value: Date | string | null) {
  return value === null ? null : normalizeRequiredDate(value);
}

function hasTimeZoneOffset(value: string) {
  return /(?:Z|[+-]\d{2}:?\d{2})$/i.test(value);
}

function toUserError(error: unknown) {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return "Unable to update tasklets. Please try again.";
}
