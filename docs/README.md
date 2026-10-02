# Documentation

These guides describe the actual Unity implementation and distinguish delivered changes from release work that still needs an editor/device.

| Guide | Contents |
| --- | --- |
| [Getting started](GETTING_STARTED.md) | Editor version, first import, local validation, development builds |
| [Architecture](ARCHITECTURE.md) | Scene contracts, state machine, ownership, timing, extension points |
| [Gameplay](GAMEPLAY.md) | Controls, scoring, economy, power-up, balance values |
| [Save format](SAVE_FORMAT.md) | JSON schema, backup/recovery, legacy migration, error behavior |
| [UI and feedback](UI_AND_FEEDBACK.md) | Layout, visual language, tweens, SFX/VFX, reduced motion |
| [Performance](PERFORMANCE.md) | Allocation fixes, bounded pools, long-run stability, profiling plan |
| [Testing](TESTING.md) | Automated commands, shared contracts, manual acceptance tests |
| [Release](RELEASE.md) | Build entry points, platform/signing/compliance gates, release checklist |
| [Maintenance](MAINTENANCE.md) | Metadata, package hygiene, staged cache cleanup, contributor conventions |
| [Project review](PROJECT_REVIEW.md) | Audit findings, verification record, known limitations, roadmap |

Also see the root [README](../README.md), [change log](../CHANGELOG.md), and [license](../LICENSE).

## Status at a glance

- 33 engine-independent contracts passed in the sandbox using the shared source and a System.Text.Json adapter.
- Runtime scripts and the EditMode test assembly were compile-checked against cached Unity/NUnit assemblies.
- Source syntax, XML, package graph, metadata, scene callbacks, and GUID references were checked.
- Unity Editor, native physics/render/audio execution, device profiling, and signed release builds were **not** available here.
- Remaining historical `Library` files are explicitly documented; there is a safe untracking helper.

Treat this revision as a release candidate until the native acceptance and release checklists are complete.
