# Implement Firebase Authentication Working Plan

## Source Plan

- Draft: `.agents/plans/2026-06-07-implement-firebase-auth.draft.md`
- Frontend framework: Vue 3 with Composition API, Pinia, Vue Router, Naive UI, UnoCSS
- Frontend URL for verification: `http://tasklet.localhost:8089`
- Backend API URL for verification: `http://api.localhost:8089`
- Firebase Auth emulator URL: `http://localhost:9099`

## External Documentation Checked

- Firebase Google sign-in for Web uses the modular API imports from `firebase/auth`, including `getAuth`, `GoogleAuthProvider`, and `signInWithPopup`.
- Firebase Auth emulator wiring for Web uses `connectAuthEmulator(auth, "http://127.0.0.1:9099")`.
- Backend Admin SDK emulator support depends on `FIREBASE_AUTH_EMULATOR_HOST`, which is already set in `host/AppHost.cs`.

## Current Codebase Findings

- `src/web/package.json` already includes `firebase`.
- Aspire is already running for this work. Do not stop or start the Aspire AppHost process during implementation or verification; use the existing stack at `http://tasklet.localhost:8089` and `http://api.localhost:8089`.
- Resource-level Aspire actions are allowed. It is acceptable to stop, start, restart, or rebuild individual resources through the `aspire` CLI or Aspire MCP when needed.
- `src/web/src/views/Login.vue` already renders a Vue/Naive UI login page and calls `appStore.login()`.
- `src/web/src/stores/app-store.ts` already owns `currentUser`, `loginLoading`, `loginError`, `login()`, and `logout()` placeholders. Keep authentication state here.
- `src/web/src/router/index.ts` already redirects unauthenticated users to `Login` with `query.redirect = to.fullPath`, but it has a placeholder for Firebase session checks.
- `src/web/src/router/routes.ts` currently has `Home` and `Login` only. It does not define a `Logout` route even though the router guard checks `to.name === "Logout"`.
- `src/web/src/layouts/AppLayout.vue` handles `/logout` directly in the side menu, so logout currently bypasses the missing router route.
- `src/web/src/views/Home.vue` currently only renders `HOME`; it needs to show the logged-in user's name/email for Phase 1 verification.
- Runtime API base URL is `src/web/src/runtime-env.ts` defaulting to `http://api.localhost:8089/api`.
- The actual backend profile route is `GET http://api.localhost:8089/api/v1/me`:
  - `src/backend/runtime/Config/SetupAppExtensions.cs` applies `UsePathBase("/api")`.
  - The endpoint group is `app.MapGroup("v1").RequireAuthorization()`.
  - `src/backend/runtime/Endpoints/User/UserEndpoints.cs` maps `/me`.
- `src/backend/runtime/Middleware/FirebaseAuthorization.cs` expects an `Authorization: Bearer <Firebase ID token>` header and reads Firebase claims.
- The generated frontend client in `src/web/src/api/generated/clients/User.ts` hardcodes `baseURL: "http://app.localhost:8085/api"`. Phase 2 should not rely on that as-is.
- `src/backend/runtime/appsettings.json` uses Firebase project ID `tasklet-app`. Treat this as the source of truth for both the frontend Firebase config and backend token validation.
- `host/Dockerfile.firebase` currently starts the emulator with project `demo-tasklet`. If frontend/backend token validation fails locally, update the emulator startup project to `tasklet-app` rather than changing the backend app setting away from `tasklet-app`.

## Non-Goals

- Do not introduce React files, React hooks, JSX, or React Router patterns.
- Do not move auth state into views, layout components, or router-local module state.
- Do not edit generated API files under `src/web/src/api/generated/**` manually.
- Do not add a separate frontend state library.
- Do isolate Firebase SDK behavior in `src/web/src/stores/services/firebase-auth-service.ts`; Vue components and routing should still interact primarily with `src/web/src/stores/app-store.ts`.
- Do not implement task CRUD or unrelated UI polish.

## Phase 1: Frontend Login And Logout

### Objective

Implement a complete local Firebase Auth emulator login/logout flow in Vue. The app should:

- Redirect unauthenticated users to `/login`.
- Preserve the originally requested path in the `redirect` query string.
- Log in with the Firebase Auth emulator.
- Redirect to the intended route after successful login, falling back to `/`.
- Show the logged-in user's display name or email on the home view.
- Log out through Firebase Auth.
- Clear app auth state on logout.
- Redirect to `/login` after logout.
- Prevent access to `/` after logout.
- Keep all user/session state in `src/web/src/stores/app-store.ts`.
- Keep Firebase SDK calls in `src/web/src/stores/services/firebase-auth-service.ts`.
- Leave a clear Phase 2 TODO where backend profile retrieval will occur after Firebase login and before redirect.

### Files To Create

#### `src/web/src/stores/services/firebase-auth-service.ts`

Create a Firebase Auth service module that owns SDK initialization and Firebase-specific behavior.

Edits:

- Import from Firebase modular SDK:
  - `initializeApp` from `firebase/app`
  - `connectAuthEmulator`, `getAuth`, `GoogleAuthProvider`, `onAuthStateChanged`, `signInWithPopup`, `signOut`, `type User` from `firebase/auth`
- Read Firebase config from `src/web/src/runtime-env.ts`.
- Initialize the Firebase app once.
- Connect to the Auth emulator only when development mode is true.
- Guard emulator connection so it runs once even if the module is evaluated during HMR.
- Use emulator URL `http://127.0.0.1:9099` or `http://localhost:9099`. Prefer `127.0.0.1` to match Firebase docs and avoid host resolution surprises.
- Export service-level functions/types for the store:
  - `type FirebaseAuthUser = User`
  - `onFirebaseAuthChanged(callback: (user: FirebaseAuthUser | null) => void): () => void`
  - `signInWithGoogle(): Promise<FirebaseAuthUser>`
  - `signOutOfFirebase(): Promise<void>`
  - `getCurrentFirebaseUser(): FirebaseAuthUser | null`
  - Phase 2: `getCurrentFirebaseIdToken(): Promise<string | null>`

Reason:

- `app-store.ts` remains the Pinia state boundary for routing and Vue components.
- `firebase-auth-service.ts` isolates Firebase SDK initialization, emulator connection, provider setup, popup login, logout, auth-state subscription, and token retrieval.
- This keeps Firebase implementation details out of views, layout, router, and most store logic.

Expected shape:

```ts
import { initializeApp } from "firebase/app";
import {
  connectAuthEmulator,
  getAuth,
  GoogleAuthProvider,
  onAuthStateChanged,
  signInWithPopup,
  signOut,
  type User,
} from "firebase/auth";
import {
  firebaseConfig,
  firebaseAuthEmulatorUrl,
  isDevelopment,
} from "@/runtime-env";

const firebaseApp = initializeApp(firebaseConfig);
const firebaseAuth = getAuth(firebaseApp);
const googleAuthProvider = new GoogleAuthProvider();

if (isDevelopment) {
  connectAuthEmulator(firebaseAuth, firebaseAuthEmulatorUrl);
}

export type FirebaseAuthUser = User;

export function onFirebaseAuthChanged(callback: (user: FirebaseAuthUser | null) => void) {
  return onAuthStateChanged(firebaseAuth, callback);
}

export async function signInWithGoogle() {
  const result = await signInWithPopup(firebaseAuth, googleAuthProvider);
  return result.user;
}

export async function signOutOfFirebase() {
  await signOut(firebaseAuth);
}
```

Add a module-level guard around `connectAuthEmulator()` in the actual implementation so Vite HMR does not attempt duplicate emulator wiring.

### Files To Update

#### `src/web/src/runtime-env.ts`

Add Firebase runtime defaults beside `apiBaseUrl`.

Edits:

- Keep `apiBaseUrl` default as `http://api.localhost:8089/api`.
- Add `firebaseAuthEmulatorUrl`, defaulting to `http://127.0.0.1:9099`.
- Add `firebaseConfig` with Vite env overrides and local emulator-safe defaults:
  - `apiKey`: `import.meta.env.VITE_FIREBASE_API_KEY || "demo-api-key"`
  - `authDomain`: `import.meta.env.VITE_FIREBASE_AUTH_DOMAIN || "tasklet-app.firebaseapp.com"`
  - `projectId`: `import.meta.env.VITE_FIREBASE_PROJECT_ID || "tasklet-app"`
- Keep `isDevelopment`.

Reason:

- The backend app setting uses `tasklet-app`, so the frontend local default should use the same project ID when creating Firebase Auth users and tokens.

#### `src/web/src/stores/app-store.ts`

Implement Pinia auth state here while delegating Firebase behavior to the service.

Edits:

- Import from `@/stores/services/firebase-auth-service`:
  - `onFirebaseAuthChanged`
  - `signInWithGoogle`
  - `signOutOfFirebase`
  - `type FirebaseAuthUser`
- Add frontend auth state:
  - `firebaseUser = shallowRef<FirebaseAuthUser | null>(null)` or `ref<FirebaseAuthUser | null>(null)`
  - `authReady = ref(false)`
  - `isAuthenticated = computed(() => firebaseUser.value !== null)`
  - `displayName = computed(() => currentUser.value?.email ?? firebaseUser.value?.displayName ?? firebaseUser.value?.email ?? "")`
- Register `onFirebaseAuthChanged(...)` once inside the store setup:
  - Set `firebaseUser`.
  - For Phase 1, set `currentUser` from Firebase user fields only, using the existing generated `UserInfoResponse` type:
    - `userId: user.uid`
    - `email: user.email ?? user.displayName ?? user.uid`
  - Clear `currentUser` when user is null.
  - Set `authReady` true after the first callback.
- Add an `ensureAuthReady()` action that resolves when the first `onFirebaseAuthChanged` callback has run.
- Implement `login()`:
  - Set `loginLoading = true`.
  - Clear `loginError`.
  - Call `signInWithGoogle()`.
  - Update `firebaseUser` and Phase 1 `currentUser` from the returned user.
  - Add `// TODO(phase-2): Retrieve the backend profile before redirecting after login.`
  - Catch Firebase errors and set `loginError` to a concise user-facing message.
  - Re-throw after setting `loginError` so `Login.vue` does not redirect on failure.
  - Set `loginLoading = false` in `finally`.
- Implement `logout()`:
  - Set `loginLoading = true` only if useful for menu/button disabled state.
  - Call `signOutOfFirebase()`.
  - Clear `firebaseUser`, `currentUser`, and `loginError`.
  - Set `loginLoading = false` in `finally`.
- Return `authReady`, `isAuthenticated`, `displayName`, and `ensureAuthReady` from the store.
- Keep `leftMenuOptions` and `bottomMenuOptions` in this store.
- Change `bottomMenuOptions` so logout is only present when authenticated, if the layout should not show logout on `/login`.

Important:

- Use `unknown` narrowing for caught errors instead of `any`.
- Do not put Firebase calls into `Login.vue`, `Home.vue`, `AppLayout.vue`, or the router.
- Do not import `firebase/auth` directly in `app-store.ts`; import the service functions instead.
- Vue components and routing should not import `firebase-auth-service.ts`; they should use `app-store.ts`.

#### `src/web/src/router/index.ts`

Make route protection wait for Firebase auth initialization and use store state.

Edits:

- In `beforeEach`, call `await appStore.ensureAuthReady()` before checking authentication.
- If `to.name === "Logout"`:
  - Call `await appStore.logout()`.
  - Return `{ name: "Login" }`.
- If `!appStore.isAuthenticated && to.name !== "Login"`:
  - Return `{ name: "Login", query: { redirect: to.fullPath } }`.
- If `appStore.isAuthenticated && to.name === "Login"`:
  - Read `to.query.redirect`.
  - Return redirect string if present, otherwise `{ name: "Home" }`.
- Otherwise allow navigation.

Flow:

1. User visits `/`.
2. Router waits for Firebase to report session state.
3. If no session exists, router redirects to `/login?redirect=/`.
4. `Login.vue` calls `appStore.login()`.
5. After successful login, `Login.vue` pushes `redirect` or Home.
6. Router sees `isAuthenticated` and allows the target route.

#### `src/web/src/router/routes.ts`

Add an explicit logout route so router behavior and menu keys align.

Edits:

- Add route:
  - `path: "/logout"`
  - `name: "Logout"`
  - Use a redirect or a tiny no-render route component.
- Prefer letting `router.beforeEach` handle logout rather than rendering a view.
- Keep it under the same root layout only if the brief navigation to `/logout` should preserve layout; otherwise a top-level route is fine.

Expected implementation option:

```ts
{
  path: "/logout",
  name: "Logout",
  component: AppLayout,
  children: [
    {
      path: "",
      component: () => import("@/views/Login.vue"),
    },
  ],
}
```

The guard will intercept before rendering.

#### `src/web/src/views/Login.vue`

Keep this as a Vue component and wire redirects carefully.

Edits:

- Keep `storeToRefs(appStore)` for `loginLoading` and `loginError`.
- Keep `handleLogin()` as the only button action.
- After `await appStore.login()`, resolve redirect:
  - If `route.query.redirect` is a string, use it.
  - Otherwise push `{ name: "Home" }`.
- Use `await router.push(...)` to avoid floating promises.
- Optionally disable the login button when the store is already authenticated.
- Do not import Firebase here.

#### `src/web/src/views/Home.vue`

Replace placeholder content with a minimal authenticated home state.

Edits:

- Import `useAppStore` and `storeToRefs`.
- Read `displayName` and/or `currentUser`.
- Render a Naive UI section that shows:
  - A heading like `Home`.
  - The current user's display name or email.
- Keep it simple; this is verification surface, not a marketing page.

Example UI intent:

```vue
<template>
  <NSpace vertical>
    <NText text-xl>Home</NText>
    <NText v-if="displayName">Signed in as {{ displayName }}</NText>
  </NSpace>
</template>
```

#### `src/web/src/layouts/AppLayout.vue`

Make logout navigation consistent.

Edits:

- Prefer removing the special `/logout` branch in `handleMenuUpdate`.
- Let `router.push(value)` navigate to `/logout`.
- Let `src/web/src/router/index.ts` own the logout side effect.
- Optionally hide sidebar menu entries on the login route or when unauthenticated by using store `isAuthenticated`.

Reason:

- A single logout flow in the router prevents duplicated logout behavior.

### Phase 1 Validation

#### Static Checks

Run from `src/web`:

```shell
yarn format:check
yarn build
```

If formatting fails, run:

```shell
yarn format
```

Then rerun `yarn build`.

#### Aspire Runtime

Aspire is already running. Do not stop or start the Aspire AppHost process for this work.

Allowed Aspire operations:

- Restart an individual resource with Aspire CLI or Aspire MCP when code/config changes require it.
- Rebuild the Firebase emulator resource if `host/Dockerfile.firebase` changes.
- If `host/Dockerfile.firebase` is updated to use `--project tasklet-app`, restart/rebuild only the `firebase-emulator` resource from the running Aspire app.

Verify the basic backend health endpoint:

```shell
curl http://api.localhost:8089/health
```

Expected result: JSON containing `"status":"Healthy"`.

#### Playwright Pass 1: Unauthenticated Redirect

Goal: prove the app blocks Home before login.

Steps:

1. Open `http://tasklet.localhost:8089/`.
2. Expect the browser URL to become `/login?redirect=/`.
3. Expect the login card and `Login with Google` button to be visible.
4. Confirm no Home content is visible.

#### Playwright Pass 2: Login Popup And Redirect

Goal: prove a user can log in through Firebase Auth emulator and land on the intended route.

Steps:

1. Start from `http://tasklet.localhost:8089/login?redirect=/`.
2. Click `Login with Google`.
3. Handle the Firebase emulator popup.
4. In the emulator sign-in UI, create or choose a test user.
5. Complete sign-in.
6. Expect the main page URL to be `http://tasklet.localhost:8089/`.
7. Expect Home content to be visible.
8. Expect the page to show the signed-in user's display name or email.

Notes:

- If Playwright cannot drive the provider popup reliably, use the Auth emulator REST API or Emulator Suite UI to create a test user, then complete the browser sign-in with that user.
- Keep this pass browser-level; do not mark Phase 1 complete using only store-level assertions.

#### Playwright Pass 3: Intended Route Preservation

Goal: prove the router preserves a requested target route.

Steps:

1. Log out or clear browser storage.
2. Open a protected route that exists, currently `/`.
3. Confirm redirect query captures the original target.
4. Log in.
5. Expect navigation to return to the captured target, not always a hardcoded page.

Future extension:

- When additional protected routes are added, repeat with a non-root route such as `/tasks`.

#### Playwright Pass 4: Logout And Access Revocation

Goal: prove logout clears the Firebase session and protects Home again.

Steps:

1. Start authenticated at `/`.
2. Click the sidebar logout menu item.
3. Expect URL to become `/login`.
4. Expect the login card to be visible.
5. Navigate directly to `http://tasklet.localhost:8089/`.
6. Expect redirect back to `/login?redirect=/`.
7. Confirm the previous signed-in user name/email is no longer visible.

#### Playwright Pass 5: Reload Persistence

Goal: prove the service-backed auth-state subscription and `ensureAuthReady()` prevent false redirects during refresh.

Steps:

1. Log in and land on `/`.
2. Reload the page.
3. Expect the app to stay on `/`.
4. Expect the signed-in user text to remain visible.
5. Confirm there is no brief final state on `/login` after reload.

### Phase 1 Completion Criteria

- `src/web/src/stores/services/firebase-auth-service.ts` exists and connects Firebase Auth to the local emulator in development.
- `app-store.ts` delegates Firebase SDK behavior to `firebase-auth-service.ts`.
- `app-store.ts` owns all Firebase user/session state and implements login/logout.
- Router auth guard waits for Firebase readiness and preserves redirect targets.
- Login page redirects only after successful `appStore.login()`.
- Home view displays the signed-in user.
- Logout clears state and prevents Home access.
- `yarn build` succeeds.
- All Phase 1 Playwright passes succeed.

## Phase 2: Backend Profile Retrieval

### Objective

After Firebase login succeeds, call the .NET backend profile endpoint before redirecting. The app should:

- Get the Firebase ID token from the current Firebase user.
- Call `GET http://api.localhost:8089/api/v1/me`.
- Send `Authorization: Bearer <id token>`.
- Store the backend `UserInfoResponse` in `currentUser`.
- Redirect only after the backend profile is successfully retrieved.
- Keep profile retrieval inside `src/web/src/stores/app-store.ts` or a small API helper called only by the store.
- Retrieve Firebase ID tokens through `src/web/src/stores/services/firebase-auth-service.ts`, not directly from Firebase SDK calls in the store.

### Files To Create

#### `src/web/src/api/user-profile.ts`

Create a handwritten API helper rather than editing generated files.

Edits:

- Import `apiBaseUrl` from `@/runtime-env`.
- Import `type UserInfoResponse` from `@/api/generated/types/UserInfoResponse`.
- Export `async function getCurrentUserProfile(idToken: string): Promise<UserInfoResponse>`.
- Fetch `${apiBaseUrl}/v1/me`.
- Send headers:
  - `Authorization: Bearer ${idToken}`
  - `Accept: application/json`
- Throw a typed or descriptive error on non-2xx responses.
- Return parsed JSON as `UserInfoResponse`.

Reason:

- `src/web/src/api/generated/clients/User.ts` is generated and currently hardcodes a stale `http://app.localhost:8085/api` base URL. A small wrapper avoids manual edits to generated files and keeps Phase 2 scoped.

### Files To Update

#### `src/web/src/stores/app-store.ts`

Replace the Phase 1 TODO with backend profile retrieval.

Edits:

- Import `getCurrentUserProfile` from `@/api/user-profile`.
- Import `getCurrentFirebaseIdToken` from `@/stores/services/firebase-auth-service`.
- Add an action `loadCurrentUserProfile()`:
  - Call `await getCurrentFirebaseIdToken()`.
  - If no token is returned, clear `currentUser` and return.
  - Call `await getCurrentUserProfile(token)`.
  - Set `currentUser` to the backend response.
- In `login()`:
  - After `signInWithGoogle()`, call `await loadCurrentUserProfile()`.
  - Only return success after `currentUser` is populated from the backend.
- In the `onFirebaseAuthChanged` store subscription:
  - For an existing session, call `loadCurrentUserProfile()` before setting `authReady = true`, or set a separate `profileLoading` if the UI needs to distinguish Firebase readiness from backend profile readiness.
  - If profile retrieval fails during initial load, sign out or clear session state and set `loginError` depending on desired behavior.
- Add `profileLoading = ref(false)` if the UI needs a visible loading state on refresh.
- Keep Phase 2 profile data in `currentUser`; do not add a second profile store.

#### `src/web/src/stores/services/firebase-auth-service.ts`

Extend the Firebase Auth service with token retrieval for backend API calls.

Edits:

- Export `async function getCurrentFirebaseIdToken(): Promise<string | null>`.
- Inside the service, read `firebaseAuth.currentUser`.
- If there is no current Firebase user, return `null`.
- Otherwise call `await firebaseAuth.currentUser.getIdToken()`.
- Keep token retrieval out of `app-store.ts`, Vue components, and router files.

#### `host/Dockerfile.firebase`

Align the Firebase Auth emulator project with the app's configured Firebase project ID if local token validation requires it.

Edits:

- Change the emulator command project from `demo-tasklet` to `tasklet-app`:

```dockerfile
ENTRYPOINT ["firebase", "emulators:start", "--only", "auth", "--project", "tasklet-app"]
```

Reason:

- `src/backend/runtime/appsettings.json` sets `AppSettings.Firebase.ProjectId` to `tasklet-app`.
- The frontend Firebase config should also use `projectId: "tasklet-app"`.
- Matching the emulator project ID keeps the Auth emulator, frontend ID tokens, and backend Firebase Admin validation on the same local project identity.

#### Optional: `src/web/kubb.config.ts`

Only update this if the implementation chooses to fix generated clients instead of adding `src/web/src/api/user-profile.ts`.

Potential edits:

- Use a runtime-compatible custom Kubb client or avoid embedding stale base URL into generated output.
- Regenerate with `yarn generate`.

Recommendation:

- Do not do this in Phase 2 unless the generated client is needed more broadly. The handwritten helper is lower risk for this feature.

### Backend Code Changes

No backend endpoint code should be required for Phase 2 if local Firebase project IDs are aligned.

Confirm these existing files are sufficient:

- `src/backend/runtime/Middleware/FirebaseAuthorization.cs` validates the bearer token.
- `src/backend/runtime/Endpoints/User/UserEndpoints.Handler.Me.cs` reads `user_id` and `email` claims.
- `src/backend/runtime/Endpoints/User/UserEndpoints.cs` maps `/api/v1/me` through path base and version group.

Potential backend issue to watch:

- If `/api/v1/me` returns unauthorized even with a Firebase ID token, inspect backend logs for Firebase project ID or emulator host mismatch. The likely fix is aligning the frontend config and emulator startup project to `tasklet-app`, not changing endpoint code.

### Phase 2 Validation

#### Static Checks

Run frontend checks:

```shell
cd src/web
yarn format:check
yarn build
```

Run backend tests:

```shell
dotnet run --project src/tests/Tasklet.Tests --output detailed --disable-logo
```

#### Manual API Check With Token

Goal: prove backend accepts the Firebase emulator token.

Steps:

1. Log in through the frontend.
2. In Playwright, inspect network requests rather than reaching directly into Firebase SDK state.
3. Call:

```shell
curl -H "Authorization: Bearer <token>" http://api.localhost:8089/api/v1/me
```

Expected:

```json
{
  "userId": "...",
  "email": "..."
}
```

#### Playwright Pass 1: Profile Request Happens Before Redirect

Goal: prove login waits for `/api/v1/me`.

Steps:

1. Intercept or observe network requests for `**/api/v1/me`.
2. Start from `/login?redirect=/`.
3. Click `Login with Google`.
4. Complete emulator sign-in.
5. Wait for the `/api/v1/me` response.
6. Assert the response status is `200`.
7. Assert the app navigates to `/` only after the profile response completes.
8. Assert Home displays the email from the backend response.

#### Playwright Pass 2: Authorization Header

Goal: prove the frontend sends the Firebase ID token to the backend.

Steps:

1. Capture the `/api/v1/me` request.
2. Assert the request has an `Authorization` header.
3. Assert the header starts with `Bearer `.
4. Assert the token value is non-empty.

Do not log the full token in test output.

#### Playwright Pass 3: Backend Failure Blocks Redirect

Goal: prove failed profile retrieval does not silently enter the app with incomplete state.

Steps:

1. Route/intercept `**/api/v1/me` and fulfill with `401` or `500`.
2. Attempt login.
3. Expect the app to remain on `/login`.
4. Expect `loginError` to render in the `NAlert`.
5. Confirm Home content is not visible.

Recommended behavior:

- Treat profile retrieval failure as login failure for this app flow because the profile/settings payload is required before app entry.

#### Playwright Pass 4: Reload Hydrates Backend Profile

Goal: prove persisted Firebase sessions rehydrate backend profile state.

Steps:

1. Log in successfully.
2. Reload `/`.
3. Observe a fresh `/api/v1/me` request.
4. Expect status `200`.
5. Expect the app to stay on `/`.
6. Expect Home to show backend profile data.

#### Playwright Pass 5: Logout Clears Backend Profile State

Goal: prove profile data is cleared after logout.

Steps:

1. Log in and wait for `/api/v1/me`.
2. Confirm Home displays backend email.
3. Click logout.
4. Expect `/login`.
5. Confirm the previous email is not visible.
6. Navigate to `/`.
7. Expect redirect to `/login?redirect=/`.

### Phase 2 Completion Criteria

- `src/web/src/api/user-profile.ts` exists and calls `${apiBaseUrl}/v1/me` with a Firebase bearer token.
- `src/web/src/stores/services/firebase-auth-service.ts` owns Firebase ID token retrieval.
- `app-store.ts` retrieves backend profile data after login before redirect.
- Existing sessions reload and hydrate `currentUser` from the backend.
- Profile retrieval failure prevents app entry and shows a login error.
- Logout clears Firebase user and backend profile state.
- Frontend Firebase config, Firebase Auth emulator startup, and backend `AppSettings.Firebase.ProjectId` all use `tasklet-app`.
- `yarn build` succeeds.
- TUnit backend tests pass.
- All Phase 2 Playwright passes succeed.

## Implementation Order

1. Phase 1 create `src/web/src/stores/services/firebase-auth-service.ts`.
2. Phase 1 update `src/web/src/runtime-env.ts`.
3. Phase 1 implement Firebase state/actions in `src/web/src/stores/app-store.ts`.
4. Phase 1 update `src/web/src/router/routes.ts` with `Logout`.
5. Phase 1 update `src/web/src/router/index.ts` auth guard.
6. Phase 1 update `src/web/src/views/Login.vue` redirect push handling if needed.
7. Phase 1 update `src/web/src/views/Home.vue` signed-in user display.
8. Phase 1 update `src/web/src/layouts/AppLayout.vue` to route logout through the guard.
9. Run Phase 1 static checks and Playwright passes.
10. Phase 2 create `src/web/src/api/user-profile.ts`.
11. Phase 2 extend `src/web/src/stores/services/firebase-auth-service.ts` with Firebase ID token retrieval.
12. Phase 2 update `src/web/src/stores/app-store.ts` to load backend profile through the service/helper boundary.
13. Phase 2 update `host/Dockerfile.firebase` only if needed so the local Auth emulator starts with `--project tasklet-app`.
14. Run Phase 2 static checks, backend tests, API token check, and Playwright passes.

## Risk Register

- Firebase emulator popup automation may be brittle in Playwright. Mitigation: prefer browser-level flow first; if provider UI cannot be driven reliably, create emulator users through the emulator UI or REST API and document the stable login path used.
- Duplicate `connectAuthEmulator()` calls can fail during Vite HMR. Mitigation: guard emulator connection at module scope in `src/web/src/stores/services/firebase-auth-service.ts`.
- The generated Kubb client has a stale base URL. Mitigation: create `src/web/src/api/user-profile.ts` and do not manually edit generated files.
- Firebase project ID mismatch can cause backend token validation failure. Mitigation: keep `tasklet-app` as the shared project ID across frontend config, emulator startup, and backend settings.
- Router may briefly redirect before Firebase restores a persisted session. Mitigation: add and await `ensureAuthReady()` before route decisions.
- Logout flow currently exists in both layout and router intent. Mitigation: route `/logout` through the router guard and remove duplicated layout-side logout branching.
