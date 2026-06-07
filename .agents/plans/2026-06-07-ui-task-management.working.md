# Task Management UI Working Plan

## Source Plan

- Draft: `.agents/plans/2026-06-07-ui-task-management.draft.md`
- Objective: build the authenticated Tasklet management UI with Vue 3, Naive UI, generated Kubb clients, and Playwright verification.
- Frontend URL for verification: `http://tasklet.localhost:8089`
- Backend API URL for verification: `http://api.localhost:8089`
- Required checkpoint rule from the draft: stop and iterate after the fake-task visual layout, then stop and iterate again after the create/edit form visual layout before wiring real API data.

## Project Guidance Reviewed

- `README.md`
- `.agents/context/tasklet-architecture.md`
- `.agents/context/tasklet-frontend-vue-naiveui.md`
- Draft plan: `.agents/plans/2026-06-07-ui-task-management.draft.md`
- Prior implementation log: `.agents/plans/2026-06-07-sqlite-storage.log.md`
- Prior auth plan: `.agents/plans/2026-06-07-implement-firebase-auth.working.md`

## Pre-Flight State

- Current branch: `main`
- Current uncommitted state: only `.agents/plans/2026-06-07-ui-task-management.draft.md` is untracked.
- This working plan intentionally stays on the current branch because this task only creates the plan file and does not touch implementation files.

## Current Codebase Findings

- The actual frontend path is `src/web`, not `src/frontend`.
- `src/web/src/views/Home.vue` is currently a small authenticated home card showing `displayName`.
- `src/web/src/layouts/AppLayout.vue` owns the sidebar/header chrome and provides the content area under a 60px header.
- `src/web/src/stores/app-store.ts` owns app/auth state and should stay focused on auth, theme, and menu state.
- There is no Tasklet-specific Pinia store yet. The frontend guidance says Pinia stores roughly map to API tags, so the Tasklet UI should add `src/web/src/stores/tasklet-store.ts`.
- Generated Tasklet API clients already exist and must be used instead of direct `fetch`:
  - `Tasklet.listTasklets(...)`
  - `Tasklet.listPinnedTasklets(...)`
  - `Tasklet.createTasklet(...)`
  - `Tasklet.updateTasklet(...)`
  - `Tasklet.deleteTasklet(...)`
  - `Tasklet.getTasklet(...)`
- Generated Tasklet types already exist under `src/web/src/api/generated/types/**` and must not be edited manually.
- Generated model details relevant to the UI:
  - `TaskletResponse` has `id`, `title`, `body`, `status`, `priority`, `explicitOrder`, `pinned`, `color`, `createdAtUtc`, `completedAtUtc`, and `dueAtUtc`.
  - `CreateTaskletRequest` allows nullable `status`, `priority`, `pinned`, `color`, `completedAtUtc`, and `dueAtUtc`.
  - `UpdateTaskletRequest` requires non-null `status`, `priority`, `pinned`, and `color`.
  - `Color` values are `Amber`, `Lime`, `Emerald`, `Cyan`, `Blue`, `Indigo`, `Purple`, `Pink`, and `Rose`.
  - `Priority` values are `Eventually`, `Low`, `Medium`, `High`, and `Critical`.
  - `Status` values are `NotStarted`, `InProgress`, `Completed`, and `Blocked`.
- The generated client runtime in `src/web/src/api/tasklet-api-client.ts` attaches Firebase bearer tokens automatically, so Tasklet store actions do not need to handle auth headers.
- `src/web/package.json` does not currently include `dayjs`; this plan must add it before using relative dates.
- Naive UI, UnoCSS, `@vicons/tabler`, Pinia, VueUse, and generated API imports are already configured.
- There are no frontend unit tests or Storybook in this project. The README and project guidance expect Playwright for UI verification.

## Decisions For This Plan

### Store Boundary

`Home.vue` is the root view for this feature and is the only view-layer file allowed to read/write the Tasklet Pinia store directly.

Implementation consequence:

- Add `src/web/src/stores/tasklet-store.ts`.
- `src/web/src/views/home/Home.vue` imports the Tasklet store and passes state/actions down as props and emitted callbacks.
- Child components do not import Pinia stores or generated API clients.

### View Folder

Move the home surface into a folder so feature-specific components and composables stay close to the root view.

Implementation consequence:

- Move `src/web/src/views/Home.vue` to `src/web/src/views/home/Home.vue`.
- Update `src/web/src/router/routes.ts` to import `@/views/home/Home.vue`.
- Put feature components under `src/web/src/views/home/components/**`.
- Put form/view composables under `src/web/src/views/home/composables/**`.

### Checkpointed Build Order

Follow the draft's visual-first sequence.

Implementation consequence:

1. Move files and create tab scaffolding.
2. Render static tabs and scroll layout without cards/forms.
3. Add three fake Tasklets and stop for visual iteration.
4. Add create/edit form visuals and stop for visual iteration.
5. Wire the generated API client only after visual checkpoints are accepted.

### Fake Data Shape

Use a small local helper in the home view area for fake data during visual phases.

Implementation consequence:

- Add `src/web/src/views/home/tasklet-fixtures.ts` only for the visual checkpoint.
- Use generated `TaskletResponse` and enum literal values so the visual implementation already matches API data.
- Remove the fixture dependency when real API wiring is implemented unless a small local development fallback is explicitly approved.

### Date Handling

Use `dayjs` plus the `relativeTime` plugin for footer text.

Implementation consequence:

- Add `dayjs` to `src/web/package.json` with Yarn.
- Add a small formatting helper or composable in the home view folder so date rendering is consistent between cards and forms.
- Convert Naive UI `NDatePicker` millisecond values to `Date | null` for generated request types.

### Edit Tabs

Editing a Tasklet creates a closable dynamic tab beside the pinned, all, and create tabs.

Implementation consequence:

- Home root owns `activeTab` and `editingTaskletIds`.
- Opening edit for an existing Tasklet appends an `edit:<taskletId>` tab when absent and activates it.
- Cancel closes that edit tab.
- Successful save closes the edit tab and refreshes affected lists.
- Dirty state is local to the edit form, surfaced via an emitted/model value, and displayed as a small `NBadge` in the tab label.

### Pinning

Pin/unpin uses the update endpoint with the current Tasklet values and only flips `pinned`.

Implementation consequence:

- Add a store action that maps `TaskletResponse` to `UpdateTaskletRequest`.
- Preserve all mutable fields when toggling `pinned` so the backend update contract receives a complete update payload.
- After toggle, update local list state optimistically or reload both lists. Prefer reload first for correctness; optimize later if needed.

### Delete

Delete is always confirmed with `NPopconfirm`.

Implementation consequence:

- Task card emits `delete` only after `NPopconfirm` confirmation.
- Store action calls `Tasklet.deleteTasklet(id)` and refreshes both pinned and all lists.
- If an edit tab is open for the deleted Tasklet, Home closes it.

### Layout And Scrolling

The Home card should consume the available layout height and the tab pane content should scroll, not the whole window.

Implementation consequence:

- Adjust `Home.vue` and local scoped styles to use the existing app content area height from `AppLayout.vue`.
- Keep `NCard` full height inside the page content area.
- Use a flex column inside the card and give the tab content area `min-height: 0` plus overflow scrolling.
- Avoid nested UI cards. Task items may be `NCard`s, but page sections should be flex layouts rather than additional container cards.

## Non-Goals

- Do not edit files under `src/web/src/api/generated/**` manually.
- Do not change backend endpoint behavior unless API wiring reveals a blocking issue.
- Do not stop or restart the whole Aspire AppHost because this plan does not change `host/Tasklet.AppHost.cs`.
- Do not add frontend unit test infrastructure.
- Do not implement search, heatmaps, timeline, drag/drop ordering, AI summaries, or semantic search.
- Do not add a landing page or marketing hero.
- Do not put store access or generated API calls inside child components.

## Phase 1: Move Home View And Add Tab Shell

### Objective

Create the home feature folder, update routing, and render the main Naive UI tab surface with stable full-height layout and empty content placeholders.

### Files To Update

#### `src/web/src/views/Home.vue`

Move to:

```text
src/web/src/views/home/Home.vue
```

Initial edits:

- Keep the signed-in display label available near the top of the task surface, but avoid making it the main content.
- Replace the small existing card with a full-height task-management card.
- Add `NTabs` with these fixed panes:
  - Pinned
  - All
  - Create
- Keep `activeTab` in the root view.
- Add comments that document why the root view owns store and tab orchestration.

#### `src/web/src/router/routes.ts`

Update the Home route component import:

```ts
component: () => import("@/views/home/Home.vue")
```

### Files To Create

#### `src/web/src/views/home/components/PinnedTaskletsTab.vue`

Render an empty-state placeholder for the pinned pane.

Inputs:

- `tasklets`
- `loading`
- `error`

Outputs:

- `pin`
- `edit`
- `delete`

Phase 1 may keep these props/events stubbed for the shell; the signatures should match the later real component surface.

#### `src/web/src/views/home/components/AllTaskletsTab.vue`

Render an empty-state placeholder for the all-tasks pane.

Inputs/outputs match `PinnedTaskletsTab.vue`.

#### `src/web/src/views/home/components/CreateTaskletTab.vue`

Render an empty-state placeholder where the form will go in Phase 4.

#### `src/web/src/views/home/components/EditTaskletTab.vue`

Prepare the edit-tab component boundary, but do not render it until dynamic edit tabs are added in Phase 4.

### Verification

- `yarn --cwd src/web format:check`
- `yarn --cwd src/web vue-tsc -b`
- `yarn --cwd src/web vite build`
- Playwright:
  - Open `http://tasklet.localhost:8089`.
  - Sign in through the Firebase emulator if needed.
  - Verify the Home route renders the full-height task card.
  - Verify the Pinned, All, and Create tabs switch without console errors.
  - Verify the page content area does not cause window-level scrolling at desktop size.

## Phase 2: Static Visual Task Cards

### Objective

Use three fake Tasklets to build and verify the card/list visual design before binding data or forms.

### Files To Create

#### `src/web/src/views/home/tasklet-fixtures.ts`

Add three fake `TaskletResponse` values:

- One pinned high-priority task with a due date.
- One unpinned in-progress task.
- One completed task with `completedAtUtc`.

Use generated types and enum literal values.

#### `src/web/src/views/home/components/TaskletCard.vue`

Render a single Tasklet as an `NCard` containing an indented `NThing`.

Required UI:

- `NThing` title uses `tasklet.title`.
- `NThing` content uses `tasklet.body`; show compact fallback text only when empty.
- `#avatar` shows a small color swatch and pinned signal.
- `#header-extra` has an `NButtonGroup`.
- Pin button uses Tabler `Pinned`, `Pin`, or `PinnedOff` icon as appropriate.
- Edit button uses a Tabler edit icon.
- Delete button uses Tabler `Trash` icon inside `NPopconfirm`.
- Footer shows due date, created date, and relative created text.
- Use small buttons and tooltips/accessible labels for icon-only actions.

Important styling detail:

- Map generated `Color` values to explicit UnoCSS classes or local CSS classes.
- Do not dynamically construct UnoCSS class names that the build cannot see.

#### `src/web/src/views/home/components/TaskletList.vue`

Render a list of `TaskletCard` components with `NFlex` and `NEmpty`.

Inputs:

- `tasklets`
- `loading`
- `emptyTitle`
- `emptyDescription`

Outputs:

- `pin`
- `edit`
- `delete`

### Files To Update

#### `src/web/src/views/home/components/PinnedTaskletsTab.vue`

Use `TaskletList` and filter fake data to pinned tasks for the checkpoint.

#### `src/web/src/views/home/components/AllTaskletsTab.vue`

Use `TaskletList` with all fake data for the checkpoint.

#### `src/web/src/views/home/Home.vue`

- Use `NBadge` in tab labels for pinned/all counts.
- Wire fake data through props, not through a store.
- Add placeholder handlers for pin/edit/delete that are clearly temporary for the visual phase.

### Verification

- `yarn --cwd src/web format:check`
- `yarn --cwd src/web vue-tsc -b`
- `yarn --cwd src/web vite build`
- Playwright:
  - Verify the Pinned tab shows only the pinned fake task.
  - Verify the All tab shows all three fake tasks.
  - Verify each card action is visible, aligned, and does not overflow at desktop and mobile widths.
  - Verify list overflow scrolls inside the tab content area.

### Required Checkpoint

Stop after Phase 2 and report:

- Files changed.
- Build/typecheck status.
- Playwright findings.
- Screenshots or visual observations for desktop and mobile.
- Any proposed layout adjustments.

Do not proceed to Phase 3 until the visual card layout is accepted.

## Phase 3: Create And Edit Form Visuals

### Objective

Build the create/edit form UI and dynamic edit-tab behavior using fake data, without calling the backend yet.

### Files To Create

#### `src/web/src/views/home/composables/useTaskletForm.ts`

Create a composable for shared create/edit form state.

Responsibilities:

- Initialize defaults for create mode.
- Initialize from `TaskletResponse` for edit mode.
- Track `title`, `body`, `status`, `priority`, `color`, `pinned`, `dueAtUtc`, and `completedAtUtc`.
- Track dirty state.
- Provide lightweight validation for required title.
- Build `CreateTaskletRequest` or `UpdateTaskletRequest` payloads.
- Convert `NDatePicker` millisecond values to `Date | null`.

#### `src/web/src/views/home/components/TaskletForm.vue`

Render the shared form.

Inputs:

- `mode: "create" | "edit"`
- `tasklet?: TaskletResponse`
- `loading`
- `submitLabel`

Outputs:

- `submit`
- `cancel`
- `dirty-change`

Required UI:

- `NForm`, `NFormItem`, `NInput`, `NInput` textarea, `NSelect`, `NSwitch` or `NCheckbox` for pinned, and `NDatePicker`.
- Color selection should use visible swatches.
- Priority/status selection should use generated enum options.
- Submit and cancel buttons should be in predictable form actions.

#### `src/web/src/views/home/components/EditTaskletTab.vue`

Wrap `TaskletForm` in edit mode and emit save/cancel/dirty changes.

### Files To Update

#### `src/web/src/views/home/components/CreateTaskletTab.vue`

Render `TaskletForm` in create mode.

#### `src/web/src/views/home/Home.vue`

- Add dynamic closable tabs for edits.
- Track `editingTaskletIds`.
- Track per-edit dirty state.
- Add a small `NBadge` to dirty edit tab labels.
- Cancel closes the edit tab.
- Fake save updates local fake data only for visual validation, or logs/marks the action as temporary if mutation would make the visual checkpoint confusing.

### Verification

- `yarn --cwd src/web format:check`
- `yarn --cwd src/web vue-tsc -b`
- `yarn --cwd src/web vite build`
- Playwright:
  - Open the Create tab and verify all fields render cleanly.
  - Fill a title/body/date and verify dirty/validation states behave correctly.
  - Open an edit tab from a fake card.
  - Verify the edit tab is closable.
  - Verify the dirty badge appears after changing a field.
  - Verify cancel closes the edit tab.
  - Verify the form does not overflow or overlap at mobile width.

### Required Checkpoint

Stop after Phase 3 and report:

- Files changed.
- Build/typecheck status.
- Playwright findings.
- Form screenshots or visual observations for desktop and mobile.
- Any proposed form layout adjustments.

Do not proceed to Phase 4 until the form layout and edit-tab behavior are accepted.

## Phase 4: Tasklet Store And API Wiring

### Objective

Replace fake data with generated API client calls and make list, create, edit, pin/unpin, and delete work against the authenticated backend.

### Files To Create

#### `src/web/src/stores/tasklet-store.ts`

Add a Tasklet Pinia store that owns API interaction and Tasklet state.

State:

- `allTasklets`
- `pinnedTasklets`
- `allLoading`
- `pinnedLoading`
- `saving`
- `deletingIds`
- `taskletError`

Getters:

- `allCount`
- `pinnedCount`

Actions:

- `loadAllTasklets()`
- `loadPinnedTasklets()`
- `loadTasklets()`
- `createTasklet(request: CreateTaskletRequest)`
- `updateTasklet(id: string, request: UpdateTaskletRequest)`
- `togglePinned(tasklet: TaskletResponse)`
- `deleteTasklet(id: string)`
- `clearError()`

Implementation details:

- Call `Tasklet.listTasklets({ sort: "CreatedAtUtc", sortDirection: "Descending" })`.
- Call `Tasklet.listPinnedTasklets({ sort: "CreatedAtUtc", sortDirection: "Descending" })`.
- Use the generated response `items` arrays.
- Refresh both all and pinned lists after create, update, pin toggle, and delete.
- Keep thrown errors user-readable without exposing raw stack traces.
- Include JSDoc comments explaining why complete update payloads are rebuilt from `TaskletResponse`.

### Files To Update

#### `src/web/src/views/home/Home.vue`

- Replace fixture state with `useTaskletStore()`.
- Call `loadTasklets()` on mount.
- Pass store state to child components as props.
- Wire emitted actions to store actions.
- Close edit tabs after save/delete.
- Show `NAlert` or equivalent for `taskletError`.

#### `src/web/src/views/home/components/*.vue`

- Replace temporary fake handlers with emitted events used by Home.
- Ensure child components remain store-free.
- Ensure loading and empty states use real store props.

#### `src/web/src/views/home/tasklet-fixtures.ts`

- Remove this file if no longer used.
- If kept for development-only previews, document why and ensure it is not part of runtime data flow.

### Verification

- `yarn --cwd src/web format:check`
- `yarn --cwd src/web vue-tsc -b`
- `yarn --cwd src/web vite build`
- `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo`
- Playwright:
  - Open `http://tasklet.localhost:8089`.
  - Sign in through Firebase emulator if needed.
  - Verify initial all/pinned lists load without console or network errors.
  - Create a Tasklet and verify it appears in All.
  - Create or edit a pinned Tasklet and verify it appears in Pinned.
  - Toggle pin and verify counts/lists update.
  - Edit title/body/due date and verify the card updates after save.
  - Delete a Tasklet and verify it disappears after confirmation.
  - Confirm unauthenticated API calls are not introduced; generated clients should continue sending bearer tokens through `tasklet-api-client.ts`.

## Phase 5: Final Polish And Regression Pass

### Objective

Clean up rough edges after real API wiring and verify the task-management UI is stable across viewport sizes.

### Files To Update

Only update files touched in earlier phases unless a blocking layout issue requires a small shared-style change.

Polish checklist:

- Confirm text does not overflow buttons, tab labels, cards, forms, or badges.
- Confirm icon-only buttons have useful labels/tooltips.
- Confirm empty states render for all and pinned lists.
- Confirm loading states are visible but do not shift layout.
- Confirm error states can be dismissed or are replaced after a successful retry.
- Confirm the app does not use a one-note purple/blue palette; color swatches should reflect Tasklet colors while the shell remains restrained.
- Confirm comments are concise and explain boundaries/decisions rather than restating implementation.

### Verification

- `yarn --cwd src/web format:check`
- `yarn --cwd src/web vue-tsc -b`
- `yarn --cwd src/web vite build`
- `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo`
- `git diff --check`
- Playwright desktop and mobile smoke pass against `http://tasklet.localhost:8089`.

## Implementation Log

Create `.agents/plans/2026-06-07-ui-task-management.log.md` when implementation begins.

The log should record:

- Phase start/end checkpoints.
- Files changed in each phase.
- Verification commands and results.
- Playwright observations and screenshots where useful.
- Any deviations from this working plan and why.
- Any Aspire resource-level actions taken.
