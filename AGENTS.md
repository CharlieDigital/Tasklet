# Tasklet Todo App

## Technologies

- Check the `README.md` for key project insight.
- .NET 10, C#14
- Sqlite for data storage
- Vue 3 for frontend
- Firebase for authentication (with emulator for local development)
- Aspire for orchestration of runtime resources (stop start the backend, rebuild client OpenAPI clients, etc.)

### Backend (`src/backend`)

- Before writing code, review the docs!
- READ: `.agents/context/tasklet-backend-dotnet.md` for important C# and .NET 10 guidance
- READ: `.agents/context/tasklet-tunit-testing.md` for unit testing guidance with TUnit

### Frontend (`src/frontend`)

- READ: `.agents/context/tasklet-frontend-vue.md` for important Vue 3 guidance

## How To Work

### Automated Testing

- Frontend URI: `http://tasklet.localhost:8089`
- Backend API URI: `http://api.localhost:8089`
- Use Playwright to test the frontend
- Run unit tests with `dotnet run` because this is `TUnit`, not `XUnit`
  - `dotnet run --project src/tests/Tasklet.Tests.csproj --output detailed --disable-logo`
  - Use targeted runs with tree-node filter syntax, then run the collection, then run all tests; got step by step to verify the tests are running and passing as expected
- For Firebase, use the "Auto-generate user information" button when creating new accounts to simplify user creation in the emulator.

### Plans

Plans go into `.agents/plans` with the following rules:

- The initial filename will be `{timestamp}-{title}.draft.md`.  Example: `2026-06-07-implement-firebase-auth.draft.md`.  This is a human edited draft; do not modify.
- `{timestamp}-{title}.working.md` will be produced from the draft and can be updated and iterated as needed.  It should be:
  - Detailed and granular; what files will be changed?  What is the nature of the change? How will the code be modified?
  - What is the test and verification procedure?
  - If there are gaps or lack of clarity, they must be called out and answered before proceedings; do not proceed with ambiguity.
- `{timestamp}-{title}.log.md` is the log of the work
  - The running log as key pieces are implemented; standard tracing of the the work performed as planned
  - Ad-hoc decisions and why they were made; what are the tradeoffs?
  - What unexpected issues were encountered?
- Not all work requires plan documents; for small self-contained work, no plan document is needed, but make a running checklist (see "Work Loop" below) and use comments in the code to capture the train of thought and decisions.

### Work Loop

- When working on the UI, start by getting access to Playwright; this will allow work to go smoothly
- Build "bottom up" when working across layers; data models -> data tests -> API routes and endpoints -> handler tests -> produce generated API client -> frontend stores, services -> views and components last
- Use tests to verify te work and expected behavior
- Use Playwright to verify frontend work
- Document with comments as you go along; capture the train of thought and why the code is being written and how it participates in a flow being implemented.
- For complex work, always start from an existing plan or build a plan before starting to write code.
- For simple work, read the code and determine if any clarifying questions are needed to clear up ambiguity before starting
  - Clearly identify the objective and target outcome of the work
  - Determine how the work will be verified whether with a unit test or Playwright or if manual intervention is required to perform some external action
  - Then break down the work into a checklist of action items to perform, step-by-step to reach te objective
  - Use the checklist to guide the work so that every step is completed and nothing is missed
- Use logs and OpenTelemetry traces on the backend to trace the flow of work and troubleshoot.  Leave meaningful log messages and created custom spans as needed; use OTEL events as a mechanism to trace work.
  - You can use `aspire` CLI or the MCP tool to view this as the application is running (use Aspire to rebuild as needed)
  - Use Aspire skills in `.agents/skills` as needed

### Code Comments

Leaving code comments is **very important**.  This is your long term memory store for future travelers; they will see these comments and understand decisions

- Follow the style of the existing comments in the codebase; tone of voice, level of detail, etc.
- Use idiomatic JSDoc comments in `*.ts` and `*.vue` files for TypeScript
- Use idiomatic C# XML comments `/// <summary>` for C# files
- Always keep it terse, concise, and to the point; do not overdo it.
- Focus on "why" and the core interaction that the code facilitates; do not dwell on "what" (we can read the code)
- Use inline comments to call out important details, complexity, tradeoffs and decisions
- If a comment has a link to an external resource, never remove it; these links are where we can find relevant information about some external system, SDK, third party library, etc.

### Key Rules

- Unless working on the Aspire Host, avoid stopping and starting Aspire.
  - Stop and start individual resources as needed; do not stop the entire stack during normal dev
  - Only restart the Aspire stack if we are specifically modifying the `AppHost.cs` (requires a restart)
