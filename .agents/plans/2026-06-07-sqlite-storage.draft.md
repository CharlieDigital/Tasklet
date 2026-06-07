# Sqlite Storage Provider Implementation

## Overview

The task is to implement a Tasklet storage provider for Sqlite using EF Core.

The storage provider interface allows different backing stores to provide the same implementation for storage whether EF Core backed or not.

## Implementation Details

`src/backend/sqlite` will contain the implementation fo the Sqlite storage provider which implements the `src/backend/core/Model/ITaskletStorage.cs` interface.

The model is `src/backend/core/Model/Tasklet.cs` which is agnostic of storage level mapping details.  The `DbContext` setup for this must perform the mapping of the properties and creation of the key artifacts like indices.

For Sqlite, target the `.data/` directory.  Add an entry in the `.gitignore` to exclude the create Sqlite database file.

See the stubs:

- `src/backend/sqlite/SqliteStorageProvider.cs` for the implementation of the storage provider.
- `src/backend/sqlite/SqliteContext.cs` for the EF Core `DbContext` implementation.

The test fixture is located at: `src/tests/Fixtures/SqliteDatabaseFixture.cs`.

A base class is provided using TUnits before/after pair to handle starting and closing a transaction `src/tests/Fixtures/SqliteTransactionalTestBase.cs`.

## Execution Plan

Execute in three phases:

1. Implement the `SqliteStorageProvider` and `SqliteContext` with basic CRUD operations.
2. Implement the test cases and ensure all test cases pass; modifying implementation details as necessary.
3. Implement the API surface area following the pattern for `src/backend/runtime/Endpoints/User` and add test cases; wire the storage into DI in `src/backend/runtime/Config`

Stop between each phase and ask for a checkpoint.
