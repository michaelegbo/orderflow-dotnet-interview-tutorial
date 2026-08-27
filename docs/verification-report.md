# Verification report

Last full clean-install run: **2026-08-27**  
Toolchain: **.NET SDK 10.0.400 / runtime 10.0.11**  
Commands:

- `pwsh ./scripts/verify-checkpoints.ps1`
- `pwsh ./scripts/verify-all.ps1`
- `pwsh ./scripts/verify-lesson-history.ps1`
- `node ./scripts/verify-site.mjs`
- `node ./scripts/verify-pedagogy.mjs`

## Result

`ORDERFLOW END-TO-END PASS — 19 checks completed`

`ALL 8 GIT CHECKPOINTS PASS`

`176 LESSON-HISTORY COMMITS PASS`

Each annotated `stage-01` through `stage-08` tag was exported into a clean
temporary directory before its dependencies were restored and its
stage-specific command was executed. This verifies the exact code reached by
the tutorial links, not merely the current working tree.

- Stage 01 restored and executed with its expected pass marker.
- Stage 02 restored and executed with its expected pass marker.
- Stage 03 restored and executed with its expected pass marker.
- All nine projects restored and built with **0 warnings and 0 errors**.
- Unit tests: **22 passed, 0 failed**.
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
request. The lesson-history verifier additionally checks that all 176 ordered
lesson commits are reachable, match the manifest, contain the expected
metadata, and restore to one of the eight verified executable production-source
checkpoints. Generated site and manifest files are excluded from that tree
comparison because embedding the final history hashes creates a circular hash;
the separate site audit verifies those generated files directly.

The published site has a separate structural, semantic, and pedagogical audit
covering all 176 lesson cards, practical exercise panels, hint and answer
controls, supplied-input or response-frame panels, ordered task steps, eight
chapter-end checkpoint mappings, bottom completion controls,
interactive knowledge checks, duplicate IDs, inline script syntax, and the
downloadable archive. It also requires visible, pre-exercise mastery teaching
for all 128 concept lessons and explicit orientation/practice/recall roles for
the other 48 cards. Every mastery contract follows the enforced order
**simple picture -> words in plain English -> technical version -> four short
steps -> exercise**. The simple opening and each mechanism step are limited to
30 words; all 128 lessons include a three- or four-term plain-English guide,
and the current maximum is 26 words. Every mastery contract also contains a
connection to the same OrderFlow
solution, why-it-matters discussion, common boundary, interview answer frame,
and at least 225 teaching words. Every task has an explicitly matched answer. The 68 C#
focused answers and 142 standalone rendered teaching examples pass Roslyn
syntax parsing; deliberately incomplete signatures are labelled as fragments.
The sequencing audit scans every displayed C# example, supplied C# starter, and focused answer
against 45 concept-introduction boundaries and reports **0 premature-syntax
findings**. One additional focused answer is a valid .NET CLI command rather
than C#.

The exercise-friction audit also requires all 176 lessons to state what the
learner is given, provide concrete starter inputs or a response frame, and
break the work into at least three small ordered steps. All 69 code-oriented
lessons retain their exact answer contract behind the reveal. The Operators
acceptance check specifically proves that quantity, unit price and paid state
are supplied, that the ordered steps do not disclose the implementation, and
that the expected console result is concrete. All 69 code-oriented lessons now
have a task-specific starter and observable expected result; the site verifier
rejects generic “it compiles” proofs and generic blank-file instructions.

The attached source Markdown remains unmodified. The generator applies narrow,
manifest-hashed rendering corrections where the source previously introduced
arithmetic, branching, arrays, async, routing, or other syntax before its
dedicated lesson. A supplied array in Exercise 2 is the only intentional early
scaffold and is labelled explicitly in the page.

Browser acceptance also covered the learner-file/run-command separation,
attempt/hint/answer and chapter snapshot interactions, saved state after reload,
core/full route switching, lesson completion, and responsive layouts at
1280 px, 768 px, 390 px, and 320 px. No horizontal overflow or browser-console
errors were observed.
