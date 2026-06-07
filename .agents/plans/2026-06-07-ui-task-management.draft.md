# Task Management UI Plan

## Overview

The previous plan:  `.agents/plans/2026-06-07-sqlite-storage.log.md` implemented the:

- Sqlite storage provider, including the EF Core context, design-time factory, and service collection extensions.
- Unit tests for the storage provider, including transactional test base and database fixture.
- API endpoints for task management, including handlers for creating, reading, updating, and deleting tasks.

Now we will build the front-end of the application using NaiveUI.

## Implementation Details

`.agents/context/tasklet-frontend-vue-naiveui.md` is a key document here.  We will use NaiveUI components to build the user interface for managing tasks.

The main interface will use NaiveUI tabs to implement the three main views:

1. Pinned tasks: this view shows all pinned tasks, sorted by the date created; most recent first.
2. All tasks: this view shows all tasks, sorted by the date created; most recent first.
3. Create task: this view shows a form to create a new task.

### Tabs

Each `NTabPane` (<https://www.naiveui.com/en-US/os-theme/components/tabs>) should be implemented as a separate component.  Keep the rule in mind: only the root view can access the Pinia store; components can only use props and `defineModel` to get two way binding.  This isolation boundary is important to keep the store from leaking into the components.

For the create form, build a composable to manage the form state and submission logic.

Reorganize the files (and routes):

```text
src/views/home/Home.vue
src/views/home/components/...
src/views/home/composables/...
```

- Use an `NBadge` to show the count (<https://www.naiveui.com/en-US/os-theme/components/badge>) of tasks on the tabs.
- Use `NEmpty` (<https://www.naiveui.com/en-US/os-theme/components/empty>) when the view is empty.

### Tasks

Each task is displayed as an `NCard` (<https://www.naiveui.com/en-US/os-theme/components/card>) wrapping an `NThing` (<https://www.naiveui.com/en-US/os-theme/components/thing>) with the task name as the title and the description as the content.  The card should have actions to pin/unpin, edit, and delete the task.

Use the `indented` mode with `NThing`.  Use the `#avatar` slot to show pinned and the color.  Here, we cannot use attibutity; just use `bg={color}-500` from the color.

- Size small
- Use `#header-extra` slot to add actions on the top right of the card.
- Use an `NButtonGroup` with buttons for pin/unpin, edit, and delete.
- When editing, create a closable tab that contains the edit form (can use the composable from the create form).  Cancel closes the tab
  - When editing, use a small `NBadge` to indicate a "dirty" form state.
- `NDatePicker` to select the due date when creating or editing a task.
- Use Tabler `Pinned`, `Pin`, and `PinnedOff` icons for managing pins
- Use Tabler `Trash` icon for delete; always use `NPopConfirm` to confirm delete actions
- Use the footer to show details like the due date and creation date.
- Add `dayjs` and `RelativeTime` plugin to show relative time like "added 5 minutes ago" in the footer.
- Prefer default `NFlex` (<https://www.naiveui.com/en-US/os-theme/components/flex>) `vertical` for layout; falling back to UnoCSS Wind4 Attributify for custom styles as needed.
- Main card on `Home.vue` should be full height; tabs will go into the card.
- Tab panes should scroll if content overflows; ensure scrollbar is on the content, not the window

## Execution Plan

1. Move the files first
2. Implement the scaffolding of the tabs and ensure that behavior is correct; do not add the form or any cards
3. Implement an example of 3 tasks using fake tasklets; ⚠️ we will stop and iterate here until it looks right.  Do not be concerned with binding or the real data; only the visual layout first before moving on.
4. Implement the create and edit form; ⚠️ we will stop and iterate here until it looks right.  Do not be concerned with binding or the real data; only the visual layout first before moving on.
5. Implement the API calls and wire everything up only after all designs have been iterated.
