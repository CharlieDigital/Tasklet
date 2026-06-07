import { apiBaseUrl } from "@/runtime-env";
import { getCurrentFirebaseIdToken } from "@/stores/services/firebase-auth-service";

/**
 * Fetch credential modes supported by Kubb's generated client runtime.
 */
export type RequestCredentials = "omit" | "same-origin" | "include";

/**
 * Request shape consumed by generated Kubb client methods.
 *
 * The generated methods provide the endpoint path, HTTP method, and any
 * operation-specific headers. This runtime client supplies the app's base URL
 * and shared bearer-token behavior.
 */
export type RequestConfig<TData = unknown> = {
  baseURL?: string;
  url?: string;
  method?: "GET" | "PUT" | "PATCH" | "POST" | "DELETE" | "OPTIONS" | "HEAD";
  params?: unknown;
  data?: TData | FormData;
  responseType?:
    | "arraybuffer"
    | "blob"
    | "document"
    | "json"
    | "text"
    | "stream";
  signal?: AbortSignal;
  headers?: [string, string][] | Record<string, string>;
  credentials?: RequestCredentials;
};

/**
 * Normalized response shape returned to generated Kubb client methods.
 */
export type ResponseConfig<TData = unknown> = {
  data: TData;
  status: number;
  statusText: string;
  headers: Headers;
};

/**
 * Error type placeholder required by Kubb generated imports.
 */
export type ResponseErrorConfig<TError = unknown> = TError;

/**
 * Function contract expected by Kubb static-class clients.
 *
 * The `getConfig` and `setConfig` members mirror Kubb's built-in fetch client
 * so generated code can use this module as a drop-in custom runtime.
 */
export type Client = {
  <TResponseData, _TError = unknown, TRequestData = unknown>(
    config: RequestConfig<TRequestData>,
  ): Promise<ResponseConfig<TResponseData>>;
  getConfig: typeof getConfig;
  setConfig: typeof setConfig;
};

let clientConfig: Partial<RequestConfig> = {};

/**
 * Returns process-wide client defaults for generated API calls.
 */
export function getConfig() {
  return clientConfig;
}

/**
 * Sets process-wide client defaults used by generated API calls.
 */
export function setConfig(config: Partial<RequestConfig>) {
  clientConfig = config;
  return getConfig();
}

/**
 * Merges generated call config with runtime defaults while preserving headers.
 */
export function mergeConfig<T extends RequestConfig>(
  ...configs: Array<Partial<T>>
): Partial<T> {
  return configs.reduce<Partial<T>>((merged, config) => {
    return {
      ...merged,
      ...config,
      headers: {
        ...(Array.isArray(merged.headers)
          ? Object.fromEntries(merged.headers)
          : merged.headers),
        ...(Array.isArray(config.headers)
          ? Object.fromEntries(config.headers)
          : config.headers),
      },
    };
  }, {});
}

/**
 * Builds request headers for generated API calls.
 *
 * Keep this auth dependency one-way: `tasklet-api-client.ts` may import
 * `firebase-auth-service.ts`, but the Firebase service must not import API
 * clients. That direction prevents auth initialization from creating a module
 * cycle through generated Kubb clients.
 */
async function buildHeaders(configHeaders: RequestConfig["headers"]) {
  const idToken = await getCurrentFirebaseIdToken();
  const headers = Array.isArray(configHeaders)
    ? Object.fromEntries(configHeaders)
    : { ...configHeaders };

  if (idToken === null) {
    return headers;
  }

  return {
    ...headers,
    Authorization: `Bearer ${idToken}`,
  };
}

const client = (async <TResponseData, _TError, TRequestData>(
  config: RequestConfig<TRequestData>,
) => {
  const mergedConfig = mergeConfig(getConfig(), config);
  const headers = await buildHeaders(mergedConfig.headers);
  const normalizedParams = new URLSearchParams();

  Object.entries(mergedConfig.params || {}).forEach(([key, value]) => {
    if (value !== undefined) {
      normalizedParams.append(key, value === null ? "null" : value.toString());
    }
  });

  /**
   * Resolve the base URL at runtime so generated clients do not bake local or
   * production hosts into generated source.
   */
  let targetUrl = [apiBaseUrl, mergedConfig.url].filter(Boolean).join("");

  if (mergedConfig.params) {
    targetUrl += `?${normalizedParams}`;
  }

  const response = await fetch(targetUrl, {
    credentials: mergedConfig.credentials || "same-origin",
    method: mergedConfig.method?.toUpperCase(),
    body:
      mergedConfig.data instanceof FormData
        ? mergedConfig.data
        : mergedConfig.data === undefined
          ? undefined
          : JSON.stringify(mergedConfig.data),
    signal: mergedConfig.signal,
    headers,
  });

  if (!response.ok) {
    throw new Error(
      `Unable to complete the API request. The server returned ${response.status}.`,
    );
  }

  const data =
    [204, 205, 304].includes(response.status) || !response.body
      ? {}
      : await response.json();

  return {
    data: data as TResponseData,
    status: response.status,
    statusText: response.statusText,
    headers: response.headers,
  };
}) as Client;

client.getConfig = getConfig;
client.setConfig = setConfig;

export default client;
