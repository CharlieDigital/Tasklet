/**
 * Runtime environment configuration.
 */

export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL || "http://api.localhost:8089/api";

export const isDevelopment = import.meta.env.MODE === "development";
