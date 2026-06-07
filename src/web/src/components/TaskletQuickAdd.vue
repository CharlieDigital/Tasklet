<template>
  <form
    :class="fullWidth ? 'w-full' : 'w-full max-w-[420px]'"
    @submit.prevent="submit"
  >
    <NInputGroup>
      <NInput
        v-model:value="title"
        :placeholder="placeholder"
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
withDefaults(
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
