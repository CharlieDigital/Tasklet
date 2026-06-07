# Task Management UI Work Log

## Plan

- Working plan: `.agents/plans/2026-06-07-ui-task-management.working.md`
- Started: 2026-06-07

## Baseline

- Attached Playwright to `http://tasklet.localhost:8089`.
- Current app is signed in and renders the existing minimal Home card.
- Console baseline has no warnings or errors.
- Branch: `main`.
- Existing untracked files before implementation: draft and working plan files.

## Phase 1

- Moving `Home.vue` into the home feature folder.
- Adding tab shell components before task cards or API wiring so layout behavior can be verified first.
- Updated `src/web/src/router/routes.ts` to lazy-load `@/views/home/Home.vue`.
- Added placeholder tab components for pinned, all, create, and edit surfaces.
- Ran `yarn --cwd src/web format:check`; first pass found Prettier wrapping only, then `yarn --cwd src/web format` fixed it.
- Verification passed:
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`

## Direct Pin/Done API Support

- Added first-class storage APIs for common task actions:
  - `GetDoneTaskletsForUserAsync`
  - `SetTaskletPinnedAsync`
  - `CompleteTaskletAsync`
- Implemented the SQLite storage operations with user-scoped updates so pinning and completion cannot mutate another user's Tasklet.
- Added storage tests for Done filtering, scoped pin updates, and scoped completion updates.
- Added direct API endpoints:
  - `GET /tasklets/done`
  - `PUT /tasklets/{id}/pin`
  - `PUT /tasklets/{id}/unpin`
  - `PUT /tasklets/{id}/complete`
- Added endpoint tests for the new handlers and not-found behavior.
- Regenerated the OpenAPI operations and frontend generated client methods:
  - `listDoneTasklets`
  - `pinTasklet`
  - `unpinTasklet`
  - `completeTasklet`
- Verification passed:
  - Focused storage tests for Done, pin, and complete.
  - Focused endpoint tests for Done, pin, unpin, and complete.
  - Full `SqliteStorageProviderTests` collection.
  - Full `TaskletEndpointTests` collection.
  - Full TUnit suite: 52/52 passing.
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`
- `dotnet csharpier format src/backend src/tests` could not run because `dotnet-csharpier` is not installed in the local tool environment.

## Phase 4

- Replaced fake fixture data with `src/web/src/stores/tasklet-store.ts`.
- The Home root now owns the Tasklet store boundary and passes store state/actions into presentational tab/card/form components.
- Removed `src/web/src/views/home/tasklet-fixtures.ts` from runtime data flow.
- Wired generated clients for list, done, create, update, delete, pin, unpin, and complete.
- Deliberate deviation from the original Phase 4 pinning plan:
  - The store uses the direct `pinTasklet`, `unpinTasklet`, and `completeTasklet` operations added in the previous slice.
  - This avoids rebuilding full update payloads for quick card actions and prevents stale card data from overwriting unrelated fields.
- Added store-side date normalization because generated date fields are typed as `Date`, but JSON responses arrive as strings.
  - UTC strings without an explicit offset are normalized with `Z` so relative card dates do not render as future local times.
- Updated the generated client runtime wrapper to send `Content-Type: application/json` for JSON bodies; the backend returned `415 Unsupported Media Type` without it.
- Changed the All tab view model to exclude completed Tasklets; completed work now belongs to the Done tab.
- Simplified the delete card action trigger so `NPopconfirm` receives the button directly; the previous tooltip/popconfirm nesting intercepted clicks.
- Rebuilt only the Aspire `tasklet-api` resource after Playwright found the running backend did not yet expose `/tasklets/done`.
- Verification passed:
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo`
  - `git diff --check`
- Playwright confirmed:
  - Initial all/pinned/done list requests return `200`.
  - Create posts to `/api/v1/tasklets` and refreshes all lists.
  - Pin posts to `/api/v1/tasklets/{id}/pin` and updates the Pinned count.
  - Complete posts to `/api/v1/tasklets/{id}/complete`, updates the Done count, and removes the completed task from All.
  - Edit posts to `/api/v1/tasklets/{id}` and refreshes the card title.
  - Delete opens the confirmation popover, posts `DELETE /api/v1/tasklets/{id}`, and refreshes lists back to zero.
  - Browser console is clean after the final pass.

## Quick Add Refinement

- Added reusable `src/web/src/components/TaskletQuickAdd.vue`.
- Empty Tasklet lists now use `NEmpty`'s `#extra` slot with an `NInputGroup` and ghost Add button.
- Added Home card footer quick-add, shown only when at least one Tasklet exists.
- Footer quick-add uses full width; empty-state quick-add stays compact.
- Quick-add defaults are context-sensitive:
  - Pinned creates pinned Tasklets.
  - All and Done create unpinned Tasklets.
  - Home footer follows the current active tab.
- Playwright confirmed:
  - Empty state shows quick-add while the footer is hidden.
  - Pinned quick-add creates a pinned Tasklet.
  - All/footer quick-add creates an unpinned Tasklet.
  - Footer quick-add fills the card footer content width with equal side inset.
  - Verification Tasklets were deleted and the browser console stayed error-free.

## Component Utility-Class Pass

- Reviewed new `.vue` files under `src/web/src/views/home/components`.
- Replaced simple custom scoped CSS with UnoCSS/Wind4 utilities:
  - Tab pane `min-height` and vertical padding.
  - Tasklet list `min-height` and padding.
  - Form width, date picker width, action margin, and color swatch layout.
  - Card radius, avatar shrink behavior, title typography/wrapping, and footer wrapping/gap.
- Kept `TaskletCard.vue` scoped CSS for Naive UI internals and hover state:
  - `:deep(...)` selectors for generated `NThing` header layout.
  - Action button opacity/pointer-event transitions.
  - Mobile header/action layout.
- Playwright before/after parity:
  - Captured before and after screenshots of the same Pinned view.
  - Verified computed title styles: `font-weight: 600`, `line-height: 21.6px`, `overflow-wrap: anywhere`, `padding-top: 4px`.
  - Verified task card radius remains `8px`.
  - Verified footer still wraps with `row-gap: 6px`.
  - Browser console stayed error-free.

## Phase 3

- Added the shared create/edit form composable and Tasklet form component.
- Wired Create to mutate the fake task list and reset the form after submit.
- Wired Edit card actions to dynamic closable edit tabs with dirty indicators.
- Replaced raw tab-pane overflow with `NScrollbar trigger="hover"` wrappers for Pinned, All, Create, and dynamic edit panes.
- Changed the edit-tab dirty marker to an unlabeled green badge, matching the tab count badge color.
- Verification passed:
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`
- Vite build still emits the existing VueUse/Rolldown pure-annotation warnings and bundle-size warning, but exits successfully.
- Playwright confirmed:
  - Create form renders inside `NScrollbar`.
  - Fake create adds a tasklet and updates the All count.
  - Edit opens a closable dynamic tab.
  - Editing a field shows an unlabeled green marker and `Unsaved changes`.
  - The word `Dirty` is no longer visible.
  - Mobile has no horizontal or document-level overflow at `390px`.
  - No console warnings were introduced.
- Phase 3 screenshots captured:
  - `tasklet-phase3-desktop-form.png`
  - `tasklet-phase3-mobile-form.png`
- Expanded the form to use the available tab width after visual review showed excessive right-side padding from the previous `720px` max width.
- Added a ghost-style Cancel button to create mode; cancel switches back to the Pinned tab while preserving the existing edit cancel behavior.
- Removed one-sided scrollbar content padding so create/edit forms align evenly with the left and right card edges.
- Removed `circle` from the Tasklet avatar so it uses Naive UI's default square avatar shape.
- Added a small `pt-1` attributify adjustment to align tasklet titles better with the square avatar.
- Added a `Checkbox` action to task cards for marking a tasklet complete during the visual/fake-data phase.
- Added a Done tab for completed tasklets; completing a task moves it into the Done list in the fake-data phase.
- Vite build emitted existing Rolldown/VueUse pure-annotation warnings and a chunk-size warning, but exited successfully.
- Playwright confirmed the moved route renders, Pinned/All/Create tabs switch, and the desktop document height matches the viewport with no console warnings.

## Phase 2

- Building static Tasklet card/list visuals with generated model types and local fixtures.
- Keeping fake data in the Home view boundary so it can be removed cleanly before API wiring.
- Adjusted the Home surface to a constrained `960px` max width after visual feedback that the first full-width pass was too wide.
- Added `dayjs` for relative card date text.
- Added fixture tasklets, reusable task cards, and reusable task lists.
- Corrected `NFlex` size from `middle` to Naive UI's supported `medium` value during typecheck.
- Verification passed:
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`
- Vite build still emits the existing VueUse/Rolldown pure-annotation warnings and bundle-size warning, but exits successfully.
- Playwright desktop: Home/card width measures `960px` at a `1440px` viewport, the Pinned tab shows only the pinned fixture, and there are no console warnings.
- Playwright mobile: no horizontal overflow at `390px` viewport; `bodyScrollWidth` equals the viewport width.
- Screenshots captured through Playwright:
  - `tasklet-phase2-desktop.png`
  - `tasklet-phase2-mobile.png`

## Phase 2 Visual Iteration

- Staying in Phase 2 until the static card visuals are clean.
- Replaced tab `NBadge` usage with inline count pills because the badge offset collided with tab labels.
- Reduced the Home surface max width from `960px` to `896px`, matching the earlier constrained Home feel more closely.
- Added responsive `NThing` header styling so mobile card actions stack below the title instead of compressing the title into broken words.
- Moved due/completed dates into a right-aligned `NFlex` footer group, keeping status/priority/created metadata grouped on the left.
- Replaced the hand-built color swatch plus absolutely-positioned pin with `NAvatar circle`; the pin icon now renders inside the avatar default slot, matching Naive UI's avatar composition.
- Made all tab count pills use the same green as the All tab for a consistent badge treatment.
- Used VueUse `useElementHover` on the task card to reveal the header action group only while the card is hovered; actions stay visible on narrow viewports where hover is not a reliable interaction.
- Verified the hover interaction with Playwright:
  - Off-card action group opacity is `0` and pointer events are `none`.
  - On-card-hover action group opacity is `1` and pointer events are `auto`.
  - No console warnings were introduced.
- Verification passed:
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`
- Verification passed after iteration:
  - `yarn --cwd src/web format:check`
  - `yarn --cwd src/web vue-tsc -b`
  - `yarn --cwd src/web vite build`
- Playwright desktop: due date is right-aligned in the card footer and tab counts no longer overlap labels.
- Playwright mobile: no horizontal overflow at `390px`; card actions stack below the title instead of squeezing it.
- Iteration screenshots captured:
  - `tasklet-phase2-desktop-iteration.png`
  - `tasklet-phase2-mobile-iteration.png`
