<template>
  <form
    :class="fullWidth ? 'w-full' : 'w-full max-w-[420px]'"
    @submit.prevent="submit"
  >
    <NInputGroup>
      <NInput
        ref="quickAddInput"
        v-model:value="title"
        :placeholder="shortcutPlaceholder"
        :disabled="loading"
        clearable
      />
      <NButton
        type="primary"
        ghost
        :loading="loading"
        :disabled="title.trim().length === 0"
        attr-type="submit"
      >
        {{ buttonLabel }}
      </NButton>
    </NInputGroup>
  </form>
</template>

<script setup lang="ts">
import type { InputInst } from "naive-ui";

const props = withDefaults(
  defineProps<{
    loading: boolean;
    placeholder?: string;
    buttonLabel?: string;
    fullWidth?: boolean;
  }>(),
  {
    placeholder: "Add a tasklet",
    buttonLabel: "Add",
    fullWidth: false,
  },
);

const emit = defineEmits<{
  submit: [title: string];
}>();

const title = ref("");
const quickAddInput = ref<InputInst | null>(null);
const shortcutLabel = computed(() => (isApplePlatform() ? "CMD+A" : "CTRL+A"));
const shortcutPlaceholder = computed(
  () => `${props.placeholder} (${shortcutLabel.value})`,
);

useEventListener(window, "keydown", handleQuickAddShortcut);

/**
 * Cmd+A and Ctrl+A normally mean "select all", so only claim the shortcut when
 * focus is outside text-editing surfaces and quick-add can actually receive it.
 */
function handleQuickAddShortcut(event: KeyboardEvent) {
  if (
    props.loading ||
    isEditableTarget(event.target) ||
    event.key.toLowerCase() !== "a" ||
    event.altKey ||
    event.shiftKey ||
    shortcutModifierPressed(event) === false
  ) {
    return;
  }

  event.preventDefault();
  quickAddInput.value?.focus();
}

function shortcutModifierPressed(event: KeyboardEvent) {
  return isApplePlatform()
    ? event.metaKey && !event.ctrlKey
    : event.ctrlKey && !event.metaKey;
}

function isApplePlatform() {
  return /Mac|iPhone|iPad|iPod/.test(navigator.platform);
}

function isEditableTarget(target: EventTarget | null) {
  if (
    target instanceof HTMLInputElement ||
    target instanceof HTMLTextAreaElement
  ) {
    return true;
  }

  return target instanceof HTMLElement && target.isContentEditable;
}

/**
 * Quick-add only captures the task title. The Home store boundary fills in API
 * defaults so this component can stay reusable in empty states and card slots.
 */
function submit() {
  const normalizedTitle = title.value.trim();

  if (normalizedTitle.length === 0) {
    return;
  }

  emit("submit", normalizedTitle);
  title.value = "";
}
</script>
