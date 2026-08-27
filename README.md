# OrderFlow C#/.NET Interview Reactivation

OrderFlow is a six-hour C# and .NET interview refresher built around one
program that grows from its first variables into a tested ASP.NET Core Web API
with EF Core, SQLite, JWT authorization, clean boundaries, and algorithms.

**Read the tutorial:**
[michaelegbo.github.io/orderflow-dotnet-interview-tutorial](https://michaelegbo.github.io/orderflow-dotnet-interview-tutorial/)

**Need only the interview essentials?**
[Open the separate 30-minute refresher](https://michaelegbo.github.io/orderflow-dotnet-interview-tutorial/orderflow-30-minute-interview-crash-course.html)

## What is actually executable?

Every one of the 176 lesson cards contains a focused practical exercise:
build, experiment, trace/debug, design, or rehearse. Before asking the learner
to act, every exercise now supplies its input values or a short response frame
and breaks the task into small ordered steps. Each exercise also names the
starting state, learner-owned file, action, command, concrete expected proof,
hint, focused answer, and the state from which the next lesson continues. Chapters
1–3 grow `OrderFlow.Learning`; the finished repository projects are comparison
checkpoints, not files the learner must open during each lesson.

Teaching remains separate from practice. All 128 concept lessons from Setup
through the algorithm patterns now show a visible mastery explanation before
the exercise. Each one now starts with the everyday idea, translates three or
four necessary technical words into plain English, and only then gives the
formal explanation, four short mechanism steps, OrderFlow connection, reason
it matters, common boundary, and interview-ready answer. A simple opening is
limited to 30 words and each mechanism step to 30 words, while the technical
depth remains available immediately below it. The other 48 orientation,
combined-practice, and rapid-recall
cards identify their role explicitly, so an assessment cannot masquerade as
an unexplained lesson.

The standalone tutorial embeds a deduplicated full-file browser, so learners
can inspect the verified cumulative checkpoint for each chapter without opening GitHub. GitHub remains
an independent reference: the `lesson-history` branch contains 176 ordered
commits, one per lesson. Conceptual lessons deliberately keep the production
tree unchanged; every production tree maps exactly to one of the eight tested
stage tags.

The same standalone page is designed for phone study. At 600 px and below it
uses a four-part header (chapters, current chapter, search, study tools), a
scroll-safe chapter drawer, labelled tool panel, single-column teaching and
exercise sections, swipeable code and tables, safe-area padding, and 44–48 px
primary touch targets. A separate 360 px rule keeps the course usable on 320 px
screens without removing lesson content.

Every lesson follows the same learning loop: learn, review the supplied inputs,
follow the small steps using only the current concept in the learner copy,
predict the result, use a hint if blocked, reveal the focused answer,
answer the knowledge check, rate confidence, and mark completion at the bottom.
The cumulative source browser is deliberately after the chapter's final lesson,
where later syntax can no longer leak into an earlier exercise. Exercise and
course progress remain saved in the browser between study sessions.

| Stage | Learning checkpoint | Direct command |
|---|---|---|
| `stage-01` | Syntax, control flow, methods | `dotnet run --project tutorial-snapshots/01-console` |
| `stage-02` | OOP, interfaces, collections, LINQ | `dotnet run --project tutorial-snapshots/02-domain-linq` |
| `stage-03` | Async service, cancellation, composition | `dotnet run --project tutorial-snapshots/03-async-service` |
| `stage-04` | ASP.NET Core, EF Core, SQLite, JWT | `dotnet run --project src/OrderFlow.Api` |
| `stage-05` | SOLID, DI, clean boundaries, tests | `dotnet test OrderFlow.sln` |
| `stage-06` | Measured production evolution | Read [`docs/architecture.md`](docs/architecture.md) and run the API |
| `stage-07` | Six OrderFlow algorithm patterns | `dotnet test --filter OrderAlgorithmsTests` |
| `stage-08` | Complete release candidate | `pwsh ./scripts/verify-all.ps1` |

## One-command checkpoint proof

From PowerShell 7, export and verify the exact code stored at all eight Git
tags:

```powershell
pwsh ./scripts/verify-checkpoints.ps1
```

Verify the 176 lesson commits, their metadata, ordering, tree hashes, and exact
production-source mapping to the eight compiled/tested checkpoints (generated
site files are excluded to avoid a manifest/history hash cycle):

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
their practical flow, chapter-end snapshot controls, valid history references,
stage links, downloadable solution, and a zero-finding pedagogical dependency
audit across 45 concept-introduction boundaries. That dependency audit covers
lesson examples, supplied exercise starters, and focused answers. CI also
rejects generic code-exercise starters, generic completion proofs, thin
teaching, and any lesson that places practice before teaching. See the latest checked-in
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
