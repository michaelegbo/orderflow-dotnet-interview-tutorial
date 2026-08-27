# OrderFlow C#/.NET Interview Reactivation

OrderFlow is a six-hour C# and .NET interview refresher built around one
program that grows from its first variables into a tested ASP.NET Core Web API
with EF Core, SQLite, JWT authorization, clean boundaries, and algorithms.

**Read the tutorial:**
[michaelegbo.github.io/orderflow-dotnet-interview-tutorial](https://michaelegbo.github.io/orderflow-dotnet-interview-tutorial/)

## What is actually executable?

Every one of the 176 lesson cards now contains a focused practical exercise:
build, experiment, trace/debug, design, or rehearse. Each exercise names the
starting state, exact file, action, command, expected proof, hint, focused
answer, and the state from which the next lesson continues.

The standalone tutorial embeds a deduplicated full-file browser, so learners
can inspect the complete project state without opening GitHub. GitHub remains
an independent reference: the `lesson-history` branch contains 176 ordered
commits, one per lesson. Conceptual lessons deliberately keep the production
tree unchanged; every production tree maps exactly to one of the eight tested
stage tags.

Every lesson now follows the same learning loop: learn, attempt the practical
task, use a hint if blocked, reveal the focused answer, inspect the full state,
answer the knowledge check, rate confidence, and mark completion at the bottom.
Exercise and course progress remain saved in the browser between study sessions.

| Stage | Learning checkpoint | Direct command |
|---|---|---|
| `stage-01` | Syntax, control flow, methods | `dotnet run --project tutorial-snapshots/01-console` |
| `stage-02` | OOP, interfaces, collections, LINQ | `dotnet run --project tutorial-snapshots/02-domain-linq` |
| `stage-03` | Async service, cancellation, composition | `dotnet run --project tutorial-snapshots/03-async-service` |
| `stage-04` | ASP.NET Core, EF Core, SQLite, JWT | `dotnet run --project src/OrderFlow.Api` |
| `stage-05` | SOLID, DI, clean boundaries, tests | `dotnet test OrderFlow.sln` |
| `stage-06` | Measured production evolution | Read [`docs/architecture.md`](docs/architecture.md) and run the API |
| `stage-07` | Two Sum and binary search | `dotnet test --filter OrderAlgorithmsTests` |
| `stage-08` | Complete release candidate | `pwsh ./scripts/verify-all.ps1` |

## One-command checkpoint proof

From PowerShell 7, export and verify the exact code stored at all eight Git
tags:

```powershell
pwsh ./scripts/verify-checkpoints.ps1
```

Verify the 176 lesson commits, their metadata, ordering, tree hashes, and exact
mapping to the eight compiled/tested checkpoints:

```powershell
pwsh ./scripts/verify-lesson-history.ps1
```

For the faster current-HEAD check, run `pwsh ./scripts/verify-all.ps1`.

The checkpoint verifier exports every `stage-01` through `stage-08` tag into a
fresh temporary directory, restores its dependencies, and executes the command
appropriate to that stage. The final-stage verifier does more than compile. It:

1. Restores and executes Stages 01, 02, and 03 and checks their deterministic
   pass markers.
2. Restores and builds all nine projects with warnings treated as errors.
3. Runs all unit and integration tests.
4. Confirms the EF model has no pending migration changes.
5. Audits direct and transitive NuGet packages for known vulnerabilities.
6. Applies the clean migration to a brand-new temporary SQLite database.
7. Starts the real API and proves health, validation, not-found, `401`, `403`,
   authenticated `201`, read, first payment, and repeated payment paths.
8. Removes its temporary database, processes, and short-lived development
   tokens when it finishes.

The GitHub Actions workflow validates the 176-commit lesson history and runs
the eight-tag matrix on every push and pull request. The Stage 08 pass also
runs `scripts/verify-site.mjs`, which verifies that all lesson cards contain
their practical flow, snapshot controls, valid history references, stage links,
and downloadable solution. See the latest checked-in
[verification report](docs/verification-report.md).

## Run the API manually

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update `
  --project src/OrderFlow.Infrastructure `
  --startup-project src/OrderFlow.Api

dotnet user-jwts create `
  --project src/OrderFlow.Api `
  --role OrderManager

dotnet run --project src/OrderFlow.Api
```

Use the generated value as `Authorization: Bearer <token>` for POST and PUT
requests. `dotnet user-jwts` is development tooling; production identity should
come from a real identity provider.

## Repository shape

```text
docs/                         GitHub Pages tutorial and downloadable solution
scripts/                      end-to-end and static-site verification
tutorial-snapshots/           three independently runnable progressive apps
src/OrderFlow.Domain/         entities and invariants
src/OrderFlow.Application/    use cases and inward-facing contracts
src/OrderFlow.Infrastructure/ EF Core, SQLite, and receipt boundary
src/OrderFlow.Api/            HTTP, validation, errors, JWT policy
tests/                        unit and real-boundary integration tests
```

## Deliberate production boundary

`MarkPaidAsync` makes the paid-state transition idempotent after a fully
successful call. Saving the database state and sending a receipt are still two
operations. A production design should close that failure window with a
transactional outbox, durable delivery, and an idempotent consumer. The
tutorial teaches this boundary instead of claiming exactly-once delivery.
