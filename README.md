# TaskTracker

TaskTracker is a .NET 9 REST API for managing tasks with a clean architecture style, CQRS, MediatR-based request handling, FluentValidation, AutoMapper, EF Core, PostgreSQL, and Serilog logging.

## Background

This solution is organized as a layered task-management backend. The API exposes task operations over HTTP, the Application layer contains the use cases, the Domain layer holds the core task model and repository contract, and the Infrastructure layer provides PostgreSQL persistence through Entity Framework Core.

The application is designed to keep business logic out of controllers. Requests enter through the API, are translated into commands or queries, then handled in the Application layer and persisted through an abstraction defined in the Domain layer.

## Clean Architecture

The solution follows a dependency flow from outer layers toward the core:

- `TaskTracker.API` handles HTTP endpoints, middleware, Swagger, and composition root wiring.
- `TaskTracker.Application` contains DTOs, commands, queries, handlers, validators, mappings, and pipeline behaviors.
- `TaskTracker.Domain` contains the task entity, task status enum, and repository interface.
- `TaskTracker.Infrastructure` contains the EF Core `DbContext`, repository implementation, and migrations.

This separation keeps the core task rules independent from web and database concerns.

## CQRS

The project uses CQRS to split reads from writes:

- Commands handle mutations such as create, update, and delete.
- Queries handle reads such as list and get-by-id.

Each request type has its own handler, and validation is applied through FluentValidation before the handler runs. Logging is handled through MediatR pipeline behaviors so request tracing stays consistent across the application.

## Overall Working

1. A client calls a task endpoint in `TaskTracker.API`.
2. The controller maps request DTOs to MediatR commands or queries.
3. MediatR sends the request through logging and validation behaviors.
4. The handler uses the `ITaskRepository` abstraction.
5. The Infrastructure repository reads or writes PostgreSQL through EF Core.
6. Results are mapped back to `TaskDto` and returned to the client.

The API also uses centralized exception handling middleware to return consistent problem details for validation errors, missing resources, database errors, and unexpected failures.

## Features

### Task management

- Create a task.
- Get a task by id.
- List tasks.
- Update a task.
- Delete a task.

### Filtering and sorting

- Filter the task list by partial title match.
- Filter the task list by status.
- Return tasks ordered by newest first.

### Task status model

The domain status enum currently supports:

- `NotStarted`
- `InProgress`
- `Completed`
- `OnHold`
- `Cancelled`

### Validation rules

- Task title is required.
- Task title is limited to 100 characters.
- Task description is limited to 500 characters.
- Task id must be greater than 0 for id-based operations.
- Task status must be a valid enum value on update.

### Logging and error handling

- Serilog console and file logging.
- Request/response timing through a MediatR logging behavior.
- Validation short-circuiting through a MediatR validation behavior.
- Centralized problem-details responses from middleware.

### API tooling

- Swagger/OpenAPI enabled.
- HTTPS redirection enabled.
- Controller-based REST endpoints.

## API Endpoints

Base route: `api/tasks`

- `GET /api/tasks` - list tasks, optionally filtered by title and status.
- `GET /api/tasks/{id}` - get a single task by id.
- `POST /api/tasks` - create a task.
- `PUT /api/tasks/{id}` - update a task.
- `DELETE /api/tasks/{id}` - delete a task.

## Project Structure

- `TaskTracker.API` - controllers, middleware, startup, Swagger.
- `TaskTracker.Application` - DTOs, MediatR commands and queries, validators, AutoMapper profile, pipeline behaviors.
- `TaskTracker.Domain` - `TaskItem`, `Status`, repository contract.
- `TaskTracker.Infrastructure` - `AppDbContext`, repository implementation, migrations.

## Data Model

The current task entity contains:

- `Id`
- `Title`
- `Description`
- `Status`
- `CreatedAt`

## Configuration Notes

- The application uses a PostgreSQL connection string named `DefaultConnection`.
- Development logging writes rolling files under `logs/`.
- `appsettings.Development.json` is intended for local-only settings and should not be shared with secrets committed to the repository.

## Getting Started

1. Restore packages with `dotnet restore`.
2. Apply migrations with `dotnet ef database update` from the infrastructure-aware startup context.
3. Run the API with `dotnet run --project TaskTracker.API`.
4. Open Swagger to explore the endpoints.

## Notes

- The current implementation uses MediatR for all task use cases.
- Queries are read-only and use `AsNoTracking` where appropriate.
- The repository uses PostgreSQL-specific `ILIKE` for case-insensitive title filtering.
