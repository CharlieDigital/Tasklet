# Tasklet

A v1, production read TODO list!

## Pre-requisites

|Component|Version|Install|
|---|---|---|
|.NET SDK|10|[Download](https://dotnet.microsoft.com/en-us/download)|
|Node|24|[Download](https://nodejs.org/en/download)|
|Yarn|4|[Download](https://yarnpkg.com/getting-started/install)|

## How to Run

The application uses Aspire to orchestrate.

For local development:

```shell
# Using standard `dotnet` CLI
dotnet run --project host

# With hot reload
dotnet watch run --project host

# With Aspire CLI (watch is on by default)
aspire run
```

This will bring up Aspire with the following key components

## Features

- Basic task management: create, read, update, delete tasks
- Full-text search on tasks via Sqlite
- Views by day, priority, color
- Heatmap visualization
- Timeline visualization
- Optional AI features:
  - Semantic search with embeddings
  - AI summaries

## Key Decisions

### Local Development

- Use Aspire to orchestrate the runtime dependencies
- Allows adding in more infrastructure in the future (e.g. to support Redis container, Postgres, etc.)
- Dashboard to make it easy to see logs, metrics, traces.
- Single command to bring the stack up
- Programmable to build a better DX for future VC-funded dev team

### Backend

- Sqlite is chosen as it provides more capabilities (e.g. full-text search); thought Postgres would have been preferred
- .NET minimal web APIs is suitable for this app due to the small surface area
- Firebase emulator is used for auth as it provides a simple DX for local development and ease of use upstream
- OpenTelemetry is used to provide observability and insights we will need in production
  - On localhost, this goes to the Aspire dashboard
- No SignalR for this app as it would require backplane in multi-instance scenarios or use of Azure SignalR which adds runtime cost
  - OpenAPI spec to keep it simple and allow for ease of local testing via Scalar UI
  - No multi-player support!
- No Redis or caching for the v0, but possible to add to scale app

### Frontend

- Vue is chosen for its simplicity
- NaiveUI is selected as the component library for its rich components, clean design, stylable theme
- UnoCSS with the Windy4 CSS preset and attributity as this provides front-end teams with familiarity and flexibility in making it prettier 😅
- No Quasar: I like it better for possible responsive design and mobile support, but that can be a future refinement
- No FE tests for now; we keep it simple and rely on Playwright to verify during dev

## Deployment

This project includes a `Dockerfile` which packages the application for deployment.

A good target for this is Google Cloud Run as this is capable of scaling to 0 which is a great way to run this economically.

The container will package the Vue app into the .NET app's `wwwroot` and serve it with cache headers through Google's CDNs, allowing it to scale well.
