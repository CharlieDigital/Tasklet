# Tasklet Backend .NET 10 and C# 14

- Use modern .NET 10, C# 14 (NOV25) language features: `var`, switch expressions, pattern matching, collection initializers, record types, tuples, ranges, and so on.
- Make use of pattern matching for terseness and expressive code
- Use named parameters and named tuples for readability and safety
- Keep performance in mind and use appropriate data types
- Prefer C# 14 `extension` blocks for extension members instead of static helper and static util methods and classes
  - Extend collections like `IReadOnlyList<SomeType>` instead of writing `SomeStaticMethod(IReadOnlyList<SomeType>)`
  - **AVOID** extending primitive types (string, int, etc.) unless it is globally applicable
- Sqlite for data storage and full-text search capabilities

<csharp_14_extension_block>

```cs
public static class SomeExtensions
{
  extension(SomeType instance)
  {
    // Extension method
    public string SomeMethod()
      => $"SomeMethod called on {instance.Name}";

    // Extension property
    public string SomeProperty
      => $"SomeProperty called on {instance.Name}";
  }
}
```

</csharp_14_extension_block>

## Language Rules and Formatting

- Use Allman style braces; always brace statements
  - Exception for `using var` and `await using var` for disposables; these do not need to be braced except where necessary
- 4 space tabs
- Follow idiomatic C# coding style from learn.microsoft.com
- When a variable initializer declares a type, use the `new()` expression or a collection initializer `[]` to make the code more terse
- When type can be inferred, omit the type declaration for terseness
- Use `dotnet csharpier format <DIR_OR_FILES>` to format files as you go along to avoid errors

## Logging and OpenTelemetry (OTEL) Tracing

- Use the `ILogger` interface for logging in web endpoints and use Aspire MCP or CLI to check logs
- Follow the high-performance logging in .NET guidelines
  - Use `LoggerMessageAttribute` for compile time log generation on `private partial void LogSomething();`
  - Put these at the bottom of the file
  - This requires that classes are `partial` and these methods are `partial`
- Logging is visible in Aspire MCP for `tasklet-api`
- Logging is connected to OTEL traces and spans as well; use both together
- Use traces, spans, and events where it is improves the visibility of the call flow and for troubleshooting (see `TaskletTelemetry.cs` for example)

<high_performance_logging>

```cs
public partial class SomeService(ILogger<SomeService> log)
{
    // Other code

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Error,
        Message = "❌  Error synchronizing repository {RepoName} from {RepoUrl}."
    )]
    private partial void LogSyncError(string repoName, string repoUrl);
}
```

</high_performance_logging>

## API Development

- Use ASP.NET Core Minimal APIs for web endpoints in `src/backend/runtime/Endpoints`
- An endpoint like `src/backend/runtime/Endpoints/User/UserEndpoints.cs` isolates the DI boundary and HTTP handling ONLY
- An `IEndpointHandler` like `src/backend/runtime/Endpoints/User/UserEndpoints.Handler.Me.cs` encapsulates the domain logic; this is our test surface
  - Handlers are 1:1 with routes in endpoints.  An endpoint class can map many routes to different handlers
  - Repeated logic should be moved to a `*Service` class that can be injected into handlers; avoid duplicating existing logic (DRY) to avoid flaky behaviors
  - Tests should operate against the handler or the service, not the endpoint; the endpoint is just the HTTP entry point
- Boundary models go into a file like `src/backend/runtime/Endpoints/User/UserEndpoints.Models.cs` which can hold many models for the API

## Unit and Integration Tests with TUnit

- The application uses TUnit for unit and integration tests
- These are in the `tests` folder
- Check `.agents/context/tasklet-tunit-testing.md` for more details

## Functional Programming, `Action`, `Func<T>`

- Prefer functional approaches when possible
- Write side-effect free code by moving I/O out into a separate call or layer except where the behavior is explicitly mutating state
- This makes code more testable by allow unit tests to pass in `Action` or `Func` instead of mocks

<example_side_effect_free>

```cs
// No side effect here! (unit test this)
public async Task<string> DoSomethingAsync(Func<SomeState, Task<string>> sideEffectFn)
{
    var state = new SomeState(); // Build this up
    var result = await sideEffectFn(state);
    return result;
}

// Side effect isolated here (integration test this)
public async Task<string> DoSomethingAsync()
    => DoSomethingAsync(someState => {
        // Side effect isolated here: write to file system, make external API call, etc.
    });
```

</example_side_effect_free>

## Sqlite and Entity Framework (EF) Core

### EF Core Provider Limitations

- SQLite doesn't natively support the following data types. EF Core can read and write values of these types, and querying for equality (where e.Property == value) is also supported. Other operations, however, like comparison and ordering will require evaluation on the client.
  - `DateTimeOffset`
  - `decimal`
  - `TimeSpan`
  - `ulong`
- Instead of `DateTimeOffset`, use `DateTime` values. When handling multiple time zones, convert the values to UTC before saving and then convert back to the appropriate time zone.
- Use `double` instead of `decimal`
- See: <https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations>

### FTS5

See: <https://sqlite.org/fts5.html>
See: <https://www.bricelam.net/2020/08/08/sqlite-fts-and-efcore.html>

### Domain Modeling

- Leverage EF Core field mapping (See: <https://learn.microsoft.com/en-us/ef/core/modeling/backing-field?tabs=data-annotations>) to property encapsulate domain behaviors
  - This avoids "bag of properties" anemic domain models that can be mutated from anywhere in the code
  - Use private fields for storage where business rules should encapsulate logic (e.g. changing state should not be allowed directly on the property; use a backing field and a method like `ChangeState(NewState newState)` that encapsulates the logic and rules around state changes)
  - Encapsulation allows us to keep validation and business rules in one place

## Good Practices

- Prefer `async/await` whenever possible and concurrent code is necessary
- Where it benefits, use the Task Parallel Library (TPL) `Parallel.ForEachAsync` (remember to use concurrent data structures like `ConcurrentDictionary`, `ConcurrentBag`
- Commeting thoroughly will make it easier to read the code when refactoring
- Apply comments in code to all public members (private members in complex cases):
  - `<summary>` should be brief, concise, to-the-point
  - `<remarks>` should add details and explain "why"; document reasoning and chain of thought, related files, business context, etc.  Explain key decisions
  - `<params>` should describe the parameter, constraints, and notes where applicable
  - `<return>` documents what is returned from the call
- Classes, records, etc. should always have a comment that describes the purpose of the type.  Follow the same rules and use `<remarks>` to expand business context and reasoning, related files, and **flow** (how does this method fit into the larger process?)
  - Include commends on `private` methods as well; be concise according to the complexity of method.
  - For methods that build strings, include examples of the constructed value.
- Exit early in functions to reduce nesting and make code clear, concise, and easy to read
- Prefer builder pattern for complex object creation; combine with functional practices and `extension()` blocks and extension members to make fluent, composable code.
- Use parameter named parameters to make code easier to read
- Use C# named tuples to make tuples easier to use safely
- Vertical whitespace (newline) free; make code easy read for human by separating ideas
  - Single line declarations: can be dense; no vertical whitespace
  - Multi-line declaration: vertical whitespace probably good!
  - Variable declaration transition to function call or conditional logic: newline good!
  - Function call transition to function call: newline good!

<csharp_vertical_whitespace_usage>

```csharp
// Single line declaration; same concerns no vertical whitespace:
var identity = ...;
var logger = ...;
var somethingElse = ...

// Different concerns; add vertical whitespace:
log.LogInformation("...");

var ttl = TimeSpan.FromMinutes(1);

var localFn = () => { ... };

var result = await DoSomethingAsync(...)

// Multi-line declaration; add vertical whitespace:
var oneThing = new Thing()
{
  ...
  ...
};

var otherThing = new Thing()
{
  ...
};
```

</csharp_vertical_whitespace_usage>

## Bad Practices to Avoid

- Avoid `Task.Run`.  This can break the `async` `ExecutionContext` and should be avoided
- Avoid using full namespaces in code; `using Some.Name.Space;` to import namespaces
