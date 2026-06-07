import type { Color } from "@/api/generated/types/Color";
import { colorEnum } from "@/api/generated/types/Color";
import type { CreateTaskletRequest } from "@/api/generated/types/CreateTaskletRequest";
import type { Priority } from "@/api/generated/types/Priority";
import { priorityEnum } from "@/api/generated/types/Priority";
import type { Status } from "@/api/generated/types/Status";
import { statusEnum } from "@/api/generated/types/Status";
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import type { UpdateTaskletRequest } from "@/api/generated/types/UpdateTaskletRequest";

export type TaskletFormMode = "create" | "edit";

export interface TaskletFormSnapshot {
  title: string;
  body: string;
  status: Status;
  priority: Priority;
  color: Color;
  pinned: boolean;
  dueAtMs: number | null;
  completedAtMs: number | null;
}

/**
 * Keeps create/edit form state aligned with generated Tasklet request models.
 *
 * Dates are held as millisecond values because Naive UI date pickers use that
 * shape; payload builders convert them back to `Date | null` at the boundary.
 */
export function useTaskletForm(
  mode: TaskletFormMode,
  tasklet?: TaskletResponse,
) {
  const initialSnapshot = makeInitialSnapshot(mode, tasklet);

  const title = ref(initialSnapshot.title);
  const body = ref(initialSnapshot.body);
  const status = ref<Status>(initialSnapshot.status);
  const priority = ref<Priority>(initialSnapshot.priority);
  const color = ref<Color>(initialSnapshot.color);
  const pinned = ref(initialSnapshot.pinned);
  const dueAtMs = ref<number | null>(initialSnapshot.dueAtMs);
  const completedAtMs = ref<number | null>(initialSnapshot.completedAtMs);

  const validationError = computed(() => {
    if (title.value.trim().length === 0) {
      return "Title is required.";
    }

    return null;
  });

  const isDirty = computed(
    () =>
      JSON.stringify(snapshot()) !==
      JSON.stringify(normalizeSnapshot(initialSnapshot)),
  );

  function snapshot(): TaskletFormSnapshot {
    return normalizeSnapshot({
      title: title.value,
      body: body.value,
      status: status.value,
      priority: priority.value,
      color: color.value,
      pinned: pinned.value,
      dueAtMs: dueAtMs.value,
      completedAtMs: completedAtMs.value,
    });
  }

  function buildCreateRequest(): CreateTaskletRequest {
    return {
      title: title.value.trim(),
      body: normalizeNullableText(body.value),
      status: status.value,
      priority: priority.value,
      explicitOrder: null,
      pinned: pinned.value,
      color: color.value,
      dueAtUtc: dateFromMilliseconds(dueAtMs.value),
      completedAtUtc: dateFromMilliseconds(completedAtMs.value),
    };
  }

  function buildUpdateRequest(): UpdateTaskletRequest {
    return {
      title: title.value.trim(),
      body: normalizeNullableText(body.value),
      status: status.value,
      priority: priority.value,
      explicitOrder: tasklet?.explicitOrder ?? null,
      pinned: pinned.value,
      color: color.value,
      dueAtUtc: dateFromMilliseconds(dueAtMs.value),
      completedAtUtc: dateFromMilliseconds(completedAtMs.value),
    };
  }

  return {
    title,
    body,
    status,
    priority,
    color,
    pinned,
    dueAtMs,
    completedAtMs,
    validationError,
    isDirty,
    buildCreateRequest,
    buildUpdateRequest,
  };
}

function makeInitialSnapshot(
  mode: TaskletFormMode,
  tasklet?: TaskletResponse,
): TaskletFormSnapshot {
  if (mode === "edit" && tasklet) {
    return normalizeSnapshot({
      title: tasklet.title,
      body: tasklet.body,
      status: tasklet.status,
      priority: tasklet.priority,
      color: tasklet.color,
      pinned: tasklet.pinned,
      dueAtMs: millisecondsFromDate(tasklet.dueAtUtc),
      completedAtMs: millisecondsFromDate(tasklet.completedAtUtc),
    });
  }

  return {
    title: "",
    body: "",
    status: statusEnum.NotStarted,
    priority: priorityEnum.Medium,
    color: colorEnum.Emerald,
    pinned: false,
    dueAtMs: null,
    completedAtMs: null,
  };
}

function normalizeSnapshot(snapshot: TaskletFormSnapshot): TaskletFormSnapshot {
  return {
    ...snapshot,
    title: snapshot.title.trim(),
    body: snapshot.body.trim(),
  };
}

function normalizeNullableText(value: string) {
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : null;
}

function millisecondsFromDate(value: Date | null) {
  return value === null ? null : value.getTime();
}

function dateFromMilliseconds(value: number | null) {
  return value === null ? null : new Date(value);
}
