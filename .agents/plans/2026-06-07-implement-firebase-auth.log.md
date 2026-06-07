# Firebase Auth Implementation Work Log

## 2026-06-07 11:13 EDT

- Confirmed Playwright access to `http://tasklet.localhost:8089`.
- Captured current unauthenticated state: `/` redirects to `/login?redirect=/`, and the login page renders `Login with Google`.
- Read the working plan, README, backend guidance, TUnit guidance, and Vue/Naive UI guidance.
- Verified the running Aspire resources with Aspire MCP; AppHost is already running, so implementation will use resource-level actions only if needed.
- Checked current Firebase documentation for modular Google sign-in and Auth emulator wiring. The plan's API shape still matches the official docs, and the project ID alignment risk is confirmed.

## 2026-06-07 11:21 EDT

- Added `src/web/src/stores/services/firebase-auth-service.ts` to isolate Firebase SDK setup, Auth emulator connection, Google popup sign-in, sign-out, auth-state subscription, current user access, and token retrieval.
- Added Firebase runtime defaults in `src/web/src/runtime-env.ts`, using `tasklet-app` as the local project ID.
- Replaced placeholder auth state/actions in `src/web/src/stores/app-store.ts` with Firebase-backed state, `ensureAuthReady()`, login/logout, and authenticated menu visibility.
- Updated router guard, logout route, login redirect handling, Home signed-in display, and layout menu handling for a single router-owned logout flow.
- Ran `yarn format` after `yarn format:check` flagged `Login.vue`.
- Ran `yarn build`; build passed. Vite reported existing third-party pure annotation/chunk-size warnings but exited successfully.
- Playwright Phase 1 unauthenticated redirect passed: `/` redirects to `/login?redirect=/`.
- Playwright opened the Firebase Auth emulator popup and created `tasklet.user@example.com` through the emulator provider UI.
- Playwright login pass succeeded: app returned to `/` and rendered `Signed in as tasklet.user@example.com`.
- Playwright reload persistence pass succeeded before logout testing.
- Playwright found a logout route bug: naming the parent `/logout` layout route rendered Login at `/logout` without triggering the guard. Fixed by moving `name: "Logout"` to the empty child route.
- Re-ran `yarn format:check` and `yarn build`; both passed.
- Playwright logout/access revocation passed after the route fix: logout clears the session and direct `/` access redirects to `/login?redirect=/`.

## 2026-06-07 11:31 EDT

- Added `src/web/src/api/user-profile.ts` to call `${apiBaseUrl}/v1/me` with `Authorization: Bearer <Firebase ID token>`.
- Updated `app-store.ts` so login and persisted-session hydration load the backend profile before app entry.
- Changed `isAuthenticated` to require both a Firebase user and backend profile state.
- Updated `host/Dockerfile.firebase` to start the Auth emulator with `--project tasklet-app`.
- Updated `src/web/src/api/user-profile.ts` to use the generated `User.me(config)` call signature with a custom Kubb client. This passes the bearer header without modifying generated files and overrides the generated stale base URL at the wrapper boundary.
- Added a backend development fallback for `FIREBASE_AUTH_EMULATOR_HOST=localhost:9099` before Firebase Admin initializes, because the live Aspire API resource did not expose the AppHost environment variable and rejected emulator tokens with `Firebase ID token has no 'kid' claim.`
- Moved backend `UseCors("api-cors-policy")` before auth and endpoint mapping so browser profile requests from `tasklet.localhost` receive CORS headers.
- Rebuilt only `tasklet-api` through Aspire; build succeeded with 0 warnings and 0 errors.
- Rebuilt the existing local Firebase emulator image tag and restarted only the `firebase-emulator` Aspire resource so it uses `--project tasklet-app`.
- Ran `yarn format:check`, `yarn vue-tsc -b`, and `yarn vite build`; all passed. `vite build` still reports third-party Rolldown pure annotation/chunk-size warnings but exits successfully.
- Ran `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo`; 2/2 tests passed.
- Verified no diffs remain under `src/web/src/api/generated/**` or `src/web/components.d.ts`.
- Playwright Phase 2 success pass: login sends `Authorization: Bearer <token>` to `http://api.localhost:8089/api/v1/me`, receives 200, redirects to `/`, and displays `tasklet.user@example.com` from the backend response.
- Playwright reload hydration pass: reloading `/` issues a fresh `/api/v1/me` request with 200 and stays on Home with the backend email visible.
- Playwright backend failure pass: forced `/api/v1/me` 500 keeps the app on `/login?redirect=/` and renders `Unable to load the current user profile. The server returned 500.`
- Playwright logout/access revocation pass: logout returns to `/login`, clears the email, and direct `/` access redirects to `/login?redirect=/`.

## 2026-06-07 11:50 EDT

- Reworked the profile API integration to use Kubb's supported custom HTTP client path instead of the handwritten `src/web/src/api/user-profile.ts` wrapper.
- Added `src/web/src/api/tasklet-api-client.ts` with the generated-client runtime contract: default client, `RequestConfig`, `ResponseErrorConfig`, `Client`, and `mergeConfig`.
- Updated `src/web/kubb.config.ts` with `pluginClient({ importPath: "../../tasklet-api-client" })` and removed the generated hardcoded base URL.
- Regenerated Kubb output with `yarn generate`; generated clients now import `../../tasklet-api-client` and call `/v1/me` without embedding `http://app.localhost:8085/api`.
- Updated `app-store.ts` to call generated `User.me({ headers: { Authorization: ... } })` directly.
- Removed `src/web/src/api/user-profile.ts`.
- Verified with `yarn format:check`, `yarn vue-tsc -b`, `yarn vite build`, and `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo`; all passed. `vite build` still reports the existing third-party Rolldown/chunk warnings.
- Playwright verified login still sends `Authorization: Bearer <token>` to `http://api.localhost:8089/api/v1/me`, receives 200, redirects to `/`, and displays `tasklet.user@example.com`.

## 2026-06-07 11:58 EDT

- Added concise JSDoc-style comments to the handwritten TypeScript auth and API boundaries:
  - `src/web/src/api/tasklet-api-client.ts`
  - `src/web/src/stores/services/firebase-auth-service.ts`
  - `src/web/src/stores/app-store.ts`
  - `src/web/src/runtime-env.ts`
  - `src/web/src/router/index.ts`
  - `src/web/src/router/routes.ts`
  - `src/web/kubb.config.ts`
- Kept comments focused on why each boundary exists and what it owns.
- Re-ran `yarn format:check` and `yarn vue-tsc -b`; both passed.

## 2026-06-07 12:06 EDT

- Moved Firebase bearer-token attachment into `src/web/src/api/tasklet-api-client.ts`.
- Simplified `app-store.ts` profile loading to call `User.me()` without per-call headers.
- Added comments in both `tasklet-api-client.ts` and `firebase-auth-service.ts` documenting the required one-way dependency direction: API client may import Firebase token helpers, but Firebase auth service must not import generated clients or the API client.
- Re-ran `yarn format:check` and `yarn vue-tsc -b`; both passed.
- Playwright verified the shared client still sends `Authorization: Bearer <token>` to `/api/v1/me`, receives 200, redirects to `/`, and displays `tasklet.user@example.com`.
