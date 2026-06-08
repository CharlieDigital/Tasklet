# Tasklet

A v1, production read TODO list!

## Pre-requisites

|Component|Version|Install|
|---|---|---|
|.NET SDK|10|[Download](https://dotnet.microsoft.com/en-us/download)|
|Node|24|[Download](https://nodejs.org/en/download)|
|Yarn|1.x|[Download](https://yarnpkg.com/getting-started/install)|
|Docker|(latest)|[Download](https://www.docker.com/get-started/)|

> I debated on whether to use the Firebase docker container or `npx`, but `npx` run version does not shut down cleanly and will lose the state for stop/restart cycles.`

## How to Run

The application uses Aspire to orchestrate.

For local development:

```shell
# Install tools (Glider MCP)
dotnet tool restore

# Using standard `dotnet` CLI
dotnet run --project host

# With hot reload
dotnet watch run --project host
dotnet watch run --project host --non-interactive

# With Aspire CLI (watch is on by default)
aspire run
```

> Quick sanity check: `curl http://api.localhost:8089/health` should return `{"status":"Healthy","checkedAt":"<time>"}`

This will bring up Aspire with the following key components:

|Component|Description|
|---|---|
|`tasklet-api`|The .NET backend API, running on `http://api.localhost:8089`|
|`tasklet-web`|The Vue frontend, running on `http://tasklet.localhost:8089`|
|`firebase-emulator`|Firebase emulator for auth, running on `http://localhost:9099`|
|`glider-mcp`|Glider MCP which provides the agent a Roslyn analyzer|

## Prologue

Actually, the hardest part of this exercise was determining what "production ready" means.  The scope of that is really difficult to encapsulate in a weekend project 😅.

(I spent waaay more time on this because of the qualifier...)

For me, it means:

- Operational telemetry and visibility into what the code is doing
- Efficiency and scalability without complexity (e.g. using cache headers on the static assets to ensure low server load)
- Relatively good separation and a sensible way to continue to break apart the app (e.g. use of `IEndpoint` and `IEndpointHandler` abstraction to make it easy to separate endpoints out as the application grows
- Decent abstractions and baselines to make it easy for other developers to work on top of it
  - Nowadays, this also includes artifacts for AI agents like skills and docs
- Considerations like security, authentication.
- A deployment and rollout plan that ensures users can stay online while the application upgrades (for a v1, I like Cloud Run for this because it automates a lot of work like blue/green deploys, gradual rollouts, rollbacks, etc. while still having a good strategy to move to GKE Autopilot or full GKE if needed))
- Ability to bring the system back online rapidly in case of failures
- Ability to deploy more instances of the same thing to scale the app.
  - One downside to using Sqlite in this case is that in a deployed environment, it is not possible to scale this out to multiple server instances
  - The HA/DR strategy is reliant on file backups or otherwise synchronizing the data out and then back; possibly using a third party vendor with a Sqlite compatible wire protocol that handles this
  - Using Postgres would have been preferred, but I'm not sure if the exercise specifically wants Sqlite or if it is a constraint of ensuring that the application is runnable without infrastructure setup (but Aspire handles that well)

A simple TODO app can probably be implemented with far less infrastructure and much, much less code, but would also not really meet the criteria of "production ready" as a product without these characteristics.

I feel like the "production" qualifier ended up pushing this from a 2 hour coding task to a 2 day engineering effort 😅

## Features

- Basic task management: create, read, update, delete tasks
- Quick add tasks with minimal required fields (e.g. just title)
- View tasks by pinned, all, and done
- Dense view and normal view toggle
- Log in/log out

## Key Decisions

### Local Development

- Use Aspire to orchestrate the runtime dependencies
- Allows adding in more infrastructure in the future (e.g. to support Redis container, Postgres, etc.)
- Dashboard to make it easy to see logs, metrics, traces.
- Single command to bring the stack up
- Programmable to build a better DX for future VC-funded dev team

### Backend

- Sqlite is chosen as it provides more capabilities (e.g. full-text search); though Postgres would have been preferred personally and feels a better fit for a production app supporting multiple instances.
  - Use a storage interface to allow for swapping out the underlying storage engine in the future (e.g. Postgres (EF Core backed), Firebase (not EF Core backed), etc.)
- .NET minimal web APIs is suitable for this app due to the small surface area
- Firebase emulator is used for auth as it provides a simple DX for local development and ease of use upstream
- Global exception handler for the API surface area that will update to `Activity.Current` with exception details and also log the exception with `ILogger`
- OpenTelemetry is used to provide observability and insights we will need in production
  - On localhost, this goes to the Aspire dashboard
- Use standard `ILogger` for this with `Serilog` injected in place (so we can configure it for OTEL sink).
- No SignalR for this app as it would require backplane in multi-instance scenarios or use of Azure SignalR which adds runtime cost
  - OpenAPI spec to keep it simple and allow for ease of local testing via Scalar UI
  - No multi-player support!
- No Redis or caching for the v0, but possible to add to scale app

### Frontend

- Vue is chosen for its simplicity and familiarity
- NaiveUI is selected as the component library for its rich components, clean design, stylable theme
- UnoCSS with the Wind4 CSS preset and attributity as this provides front-end teams with familiarity and flexibility in making it prettier 😅
- Key packages
  - `unplugin-auto-import`: Automatically imports APIs on demand as you use them in templates and scripts, reducing boilerplate and improving DX
  - `unplugin-vue-components`: Automatically imports Vue components as you use them in templates
  - `unplugin-vue-router`: Automatically generates Vue Router routes based on your file system, simplifying navigation setup
- Firebase is used for auth because it is simple to start, scalable, and comes with a nice emulator for local development.
  - Other options: OpenIddict, IdentityServer if that needs to be owned infrastructure
  - Cognito if on AWS (but it is not very ergonomic and heavy to configure)
  - Entra ID if on Azure (but also heavy to configure and not great for B2C scenarios)
- No Quasar: I like it better for possible responsive design and mobile support, but that can be a future refinement
- No FE tests for now; we keep it simple and rely on Playwright to verify during dev
- No StoryBook; we don't have that many components

## Deployment

This project includes a `Dockerfile` which packages the application for deployment.

```shell
# From root:
docker build -t tasklet:local .
```

A good target for this is Google Cloud Run as this is capable of scaling to 0 which is a great way to run this economically.

The container will package the Vue app into the .NET app's `wwwroot` and serve it with cache headers through Google's CDNs, allowing it to scale well.

If I have time, I will deploy this to Cloud Run!

## TODOs for TODO App

Things I did not get done...

- [ ] Fully test the container build and deployment to Cloud Run (might need another 2-3 hours)
- [ ] Make the search and filtering work on the backend
- [ ] Make use of the pagination; added to the API, but the demo dataset will be small
- [ ] UX tweaks to make it easier to quickly update individual fields like color, status, etc. without going to edit tab
- [ ] FTS5 Sqlite integration for better backend search capabilities
- [ ] AI features like "agenda" and sorting by impact (due date and priority, urgency from the title, etc.)
