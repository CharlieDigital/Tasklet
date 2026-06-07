/**
 * Runtime environment configuration.
 *
 * Defaults target the Aspire local stack. Vite environment variables can
 * override these values for deployed builds.
 */

/**
 * Base URL used by the custom Kubb HTTP client.
 */
export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL || "http://api.localhost:8089/api";

/**
 * Firebase Auth emulator endpoint for local development.
 */
export const firebaseAuthEmulatorUrl =
  import.meta.env.VITE_FIREBASE_AUTH_EMULATOR_URL || "http://127.0.0.1:9099";

/**
 * Firebase Web SDK config.
 *
 * The local project ID must match backend Firebase Admin validation and the
 * Auth emulator project.
 */
export const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY || "demo-api-key",
  authDomain:
    import.meta.env.VITE_FIREBASE_AUTH_DOMAIN || "tasklet-app.firebaseapp.com",
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID || "tasklet-app",
};

/**
 * True when Vite is serving the app in development mode.
 */
export const isDevelopment = import.meta.env.MODE === "development";
