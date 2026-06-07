# Tasklet Architecture

## Key Components

- `src/backend/core` ("Core") contains the shared core library that is used by the runtime and storage providers.  This allows extension to add different storage providers (whether backed by EF or not (e.g. Firebase, etc.))
- `src/backend/runtime` ("Runtime") contains the runtime implementation and API routes; it depends on the core library abstractions and does not directly depend on the storage provider implementation.
- `src/backend/sqlite` contains a SQLite implementation of the storage provider; it implements the core library abstractions and is used by the runtime.
- The Runtime build process will produce an OpenAPI spec when the `GEN=true` environment variable is set.  The file is output to `src/web/api` which then uses Kubb to generate the API client in TypeScript for the frontend
- The Vue frontend API uses Vite and the generated API to interact with the backend runtime.
