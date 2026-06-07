# Implement Firebase Authentication in Frontend

## Objective

Implement frontend authentication using Firebase connected to the local emulator.

## Background

This is a Vue 3 front-end application that uses NaiveUI (read `./agents/context/tasklet-frontend-vue-naiveui.md` for more details on working on the frontend).

The application uses `vue-router` (see: `src/web/src/router/*.ts`) for routing and navigation guards.

The application state of whether the user is logged in or not exists in the main `src/web/src/stores/app-store.ts` which can be accessed by the application top-level views and the router to check the state of the user authentication on the front-end.

The `login` and `logout` functions in the `app-store.ts` will be responsible for interacting with Firebase Authentication to manage the user's session.

### Phase 1

Implement the front-end login flow using the Firebase auth emulator as the target.

Use the Firebase Modular API; see docs: <https://firebase.google.com/docs/auth/web/google-signin>

Once the user successfully logs in, the application should redirect the user to the intended target.  For example, if the user intended to visit the route `/some/route` and is redirected at the router guard in `src/web/src/router/index.ts`, the user must be redirected to `/some/route` after successful login instead of the default home page.

User information and login state should only be managed inside of the `apps-store.ts`; do not leak this to the rest of the application so we can isolate this.

Key objectives:

- User is able to log in via Firebase Authentication using the emulator UI
- User is redirected to the home page (`/`) and user name is printed
- User is able to log out via Firebase logout function
- User is correctly redirected to the Home page (`/`) after login
- User is correctly redirected to the Login page after a successful logout and cannot access the Home view

### Phase 2

Upon successful login, the user must also make an API call to the .NET backend via the `src/backend/runtime/Endpoints/User/UserEndpoints.Handler.Me.cs` route which will retrieve the user profile (in the real app, this will include user specific settings and other configuration information which is not here at the present).

This should occur before the redirect after the login.  When implementing Phase 1, use a placeholder TODO to mark this as a Phase 2 activity.

## Approach

- Implement the front-end login flow and logout flow
- Use Playwright to verify behavior; this is using the emulator so you can automatically generate users
- Ensure login and logout work correctly
- Keep code isolated in `app-store.ts`; minimally update the related TS files like the router, `.vue` files to wire up the behavior
- Use simple comments to mark key points in the implementation `//` when inline and idiomatic `/** */` JSDoc style comments for functions; don't overdo it -- keep it simple, terse, concise, and to the point
