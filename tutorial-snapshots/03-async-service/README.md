# Stage 03 — asynchronous application service

This is the independently runnable checkpoint after Chapter 3.

```powershell
dotnet run --project OrderFlow.Stage03.csproj
```

Successful output ends with:

```text
STAGE 03 PASS — async, cancellation and retry-safe state transition
```

The paid-state transition is idempotent after a successful call. Receipt
delivery is still a separate side effect; the main tutorial explains why a
production system uses a transactional outbox and idempotent consumer.
