# OrderFlow architecture evolution

OrderFlow deliberately begins as a modular monolith. The first production
shape is one deployable ASP.NET Core API with explicit Domain, Application,
Infrastructure, and API boundaries.

```text
Client
  |
load balancer
  |
stateless OrderFlow API instances
  |
managed relational database
  |                    \
read cache              transactional outbox
                              |
                         durable queue
                              |
                     idempotent receipt worker
```

Complexity is earned in this order:

1. Measure traffic, latency, failures, and database queries.
2. Scale stateless API instances behind health-aware load balancing.
3. Fix queries and indexes before adding a cache.
4. Cache only reads with understood freshness requirements.
5. Use a transactional outbox when database state and queued side effects
   must not drift apart.
6. Make consumers idempotent because durable queues normally provide
   at-least-once delivery.
7. Add bounded retries, timeouts, circuit breaking, and observability based on
   the failure semantics of each dependency.

The tutorial's in-process receipt sender is intentionally a learning boundary,
not an exactly-once production-delivery claim.
