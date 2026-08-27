# Verification report

Last full clean-install run: **2026-08-27**  
Toolchain: **.NET SDK 10.0.400 / runtime 10.0.11**  
Commands:

- `pwsh ./scripts/verify-checkpoints.ps1`
- `pwsh ./scripts/verify-all.ps1`

## Result

`ORDERFLOW END-TO-END PASS — 19 checks completed`

`ALL 8 GIT CHECKPOINTS PASS`

Each annotated `stage-01` through `stage-08` tag was exported into a clean
temporary directory before its dependencies were restored and its
stage-specific command was executed. This verifies the exact code reached by
the tutorial links, not merely the current working tree.

- Stage 01 restored and executed with its expected pass marker.
- Stage 02 restored and executed with its expected pass marker.
- Stage 03 restored and executed with its expected pass marker.
- All nine projects restored and built with **0 warnings and 0 errors**.
- Unit tests: **10 passed, 0 failed**.
- API integration tests: **3 passed, 0 failed**.
- EF Core reported no pending model changes.
- The single clean migration applied to a brand-new SQLite database.
- No vulnerable direct or transitive NuGet packages were reported.
- Live health endpoint returned `200`.
- Unauthenticated create returned `401`.
- Authenticated user with the wrong role returned `403`.
- Invalid model returned `400`.
- Missing order returned `404`.
- OrderManager create returned `201` and an £80 total.
- Reading the created order returned `200`.
- First payment transition returned `200`.
- Repeating the payment transition returned `200` without a second domain transition.

The same run was repeated from a freshly extracted release archive rather than
only from the authoring directory. Short-lived development JWTs and the
temporary SQLite database were removed at the end of the run.

The repository workflow repeats this verification on every push and pull
request. The published site also has a separate structural audit covering all
176 lesson cards, bottom completion controls, interactive knowledge checks,
their eight code checkpoints, duplicate IDs, inline script syntax, and the
downloadable archive.
