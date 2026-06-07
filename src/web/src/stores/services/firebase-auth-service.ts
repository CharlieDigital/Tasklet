import { initializeApp, type FirebaseOptions } from "firebase/app";
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
  firebaseAuthEmulatorUrl,
  firebaseConfig,
  isDevelopment,
} from "@/runtime-env";

/**
 * Firebase SDK boundary for the app.
 *
 * Components, routes, and stores should use these functions instead of
 * importing Firebase directly. This keeps emulator wiring and provider setup in
 * one place.
 *
 * Dependency direction matters: this service must not import generated API
 * clients or `tasklet-api-client.ts`. The shared API client imports token
 * helpers from here, and reversing that relationship creates a module cycle
 * during auth initialization.
 */
const firebaseApp = initializeApp(firebaseConfig as FirebaseOptions);
const firebaseAuth = getAuth(firebaseApp);
const googleAuthProvider = new GoogleAuthProvider();
const emulatorConnectionKey = "__taskletFirebaseAuthEmulatorConnected";

/**
 * Global marker used to avoid duplicate emulator connection during Vite HMR.
 */
type FirebaseGlobal = typeof globalThis & {
  [emulatorConnectionKey]?: boolean;
};

const firebaseGlobal = globalThis as FirebaseGlobal;

if (isDevelopment && !firebaseGlobal[emulatorConnectionKey]) {
  connectAuthEmulator(firebaseAuth, firebaseAuthEmulatorUrl);
  firebaseGlobal[emulatorConnectionKey] = true;
}

export type FirebaseAuthUser = User;

/**
 * Subscribes to Firebase auth session changes.
 */
export function onFirebaseAuthChanged(
  callback: (user: FirebaseAuthUser | null) => void,
) {
  return onAuthStateChanged(firebaseAuth, callback);
}

/**
 * Opens the Firebase Google provider popup and returns the signed-in user.
 */
export async function signInWithGoogle() {
  const result = await signInWithPopup(firebaseAuth, googleAuthProvider);
  return result.user;
}

/**
 * Signs out of the active Firebase session.
 */
export async function signOutOfFirebase() {
  await signOut(firebaseAuth);
}

/**
 * Returns Firebase's current user without subscribing to state changes.
 */
export function getCurrentFirebaseUser() {
  return firebaseAuth.currentUser;
}

/**
 * Returns the current user's Firebase ID token for backend bearer auth.
 */
export async function getCurrentFirebaseIdToken() {
  if (firebaseAuth.currentUser === null) {
    return null;
  }

  return await firebaseAuth.currentUser.getIdToken();
}
