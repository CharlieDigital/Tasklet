<template>
  <NForm class="tasklet-form" :show-feedback="false">
    <NGrid responsive="screen" :cols="12" :x-gap="16" :y-gap="16">
      <NFormItemGi
        :span="12"
        label="Title"
        :feedback="validationError ?? undefined"
      >
        <NInput
          v-model:value="form.title.value"
          placeholder="Tasklet title"
          :status="validationError ? 'error' : undefined"
          @keyup.enter="submit"
        />
      </NFormItemGi>

      <NFormItemGi :span="12" label="Notes">
        <NInput
          v-model:value="form.body.value"
          type="textarea"
          placeholder="Optional notes"
          :autosize="{ minRows: 3, maxRows: 6 }"
        />
      </NFormItemGi>

      <NFormItemGi :span="6" label="Status">
        <NSelect v-model:value="form.status.value" :options="statusOptions" />
      </NFormItemGi>

      <NFormItemGi :span="6" label="Priority">
        <NSelect
          v-model:value="form.priority.value"
          :options="priorityOptions"
        />
      </NFormItemGi>

      <NFormItemGi :span="12" label="Color">
        <NSelect
          v-model:value="form.color.value"
          :options="colorOptions"
          :render-label="renderColorLabel"
        />
      </NFormItemGi>

      <NFormItemGi :span="6" label="Due date">
        <NDatePicker
          v-model:value="form.dueAtMs.value"
          type="datetime"
          clearable
          class="tasklet-form-date"
        />
      </NFormItemGi>

      <NFormItemGi :span="6" label="Completed date">
        <NDatePicker
          v-model:value="form.completedAtMs.value"
          type="datetime"
          clearable
          class="tasklet-form-date"
        />
      </NFormItemGi>

      <NFormItemGi :span="12" label="Pinned">
        <NSwitch v-model:value="form.pinned.value" />
      </NFormItemGi>
    </NGrid>

    <NFlex justify="end" align="center" class="tasklet-form-actions">
      <NText v-if="form.isDirty.value" depth="3">Unsaved changes</NText>
      <NButton ghost @click="emit('cancel')"> Cancel </NButton>
      <NButton type="primary" :loading="loading" @click="submit">
        {{ submitLabel }}
      </NButton>
    </NFlex>
  </NForm>
</template>

<script setup lang="ts">
import type { Color } from "@/api/generated/types/Color";
import { colorEnum } from "@/api/generated/types/Color";
import type { CreateTaskletRequest } from "@/api/generated/types/CreateTaskletRequest";
import { priorityEnum } from "@/api/generated/types/Priority";
import { statusEnum } from "@/api/generated/types/Status";
import type { TaskletResponse } from "@/api/generated/types/TaskletResponse";
import type { UpdateTaskletRequest } from "@/api/generated/types/UpdateTaskletRequest";
import {
  useTaskletForm,
  type TaskletFormMode,
} from "@/views/home/composables/useTaskletForm";
import type { SelectOption } from "naive-ui";
import { h } from "vue";

const props = defineProps<{
  mode: TaskletFormMode;
  tasklet?: TaskletResponse;
  loading: boolean;
  submitLabel: string;
}>();

const emit = defineEmits<{
  submit: [request: CreateTaskletRequest | UpdateTaskletRequest];
  cancel: [];
  dirtyChange: [dirty: boolean];
}>();

const form = useTaskletForm(props.mode, props.tasklet);

const statusOptions: SelectOption[] = [
  { label: "Not started", value: statusEnum.NotStarted },
  { label: "In progress", value: statusEnum.InProgress },
  { label: "Completed", value: statusEnum.Completed },
  { label: "Blocked", value: statusEnum.Blocked },
];

const priorityOptions: SelectOption[] = [
  { label: "Eventually", value: priorityEnum.Eventually },
  { label: "Low", value: priorityEnum.Low },
  { label: "Medium", value: priorityEnum.Medium },
  { label: "High", value: priorityEnum.High },
  { label: "Critical", value: priorityEnum.Critical },
];

const colorOptions: SelectOption[] = [
  { label: "Amber", value: colorEnum.Amber },
  { label: "Lime", value: colorEnum.Lime },
  { label: "Emerald", value: colorEnum.Emerald },
  { label: "Cyan", value: colorEnum.Cyan },
  { label: "Blue", value: colorEnum.Blue },
  { label: "Indigo", value: colorEnum.Indigo },
  { label: "Purple", value: colorEnum.Purple },
  { label: "Pink", value: colorEnum.Pink },
  { label: "Rose", value: colorEnum.Rose },
];

const swatchColors: Record<Color, string> = {
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

const validationError = computed(() => form.validationError.value);

watch(
  () => form.isDirty.value,
  (dirty) => emit("dirtyChange", dirty),
  { immediate: true },
);

function submit() {
  if (validationError.value !== null) {
    return;
  }

  emit(
    "submit",
    props.mode === "create"
      ? form.buildCreateRequest()
      : form.buildUpdateRequest(),
  );
}

function renderColorLabel(option: SelectOption) {
  const value = option.value as Color;

  return h("span", { class: "tasklet-color-option" }, [
    h("span", {
      class: "tasklet-color-option-swatch",
      style: { backgroundColor: swatchColors[value] },
    }),
    option.label as string,
  ]);
}
</script>

<style scoped>
.tasklet-form {
  width: 100%;
  max-width: 100%;
}

.tasklet-form-date {
  width: 100%;
}

.tasklet-form-actions {
  margin-top: 20px;
}

:deep(.tasklet-color-option) {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

:deep(.tasklet-color-option-swatch) {
  display: inline-block;
  width: 12px;
  height: 12px;
  border-radius: 999px;
}
</style>
