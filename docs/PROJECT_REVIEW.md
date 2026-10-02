# Project review, limitations, and improvement plan

Review date: **2026-10-02**.

## Original project

Unity 2020.3.26f1 project with two scenes, nine player sprites, a reusable track/decor tile system, 21 authored obstacle/pickup patterns, jump/lane/effect Animator clips, a star-funded runner shop, and existing MP3 music/SFX. The visuals are isometric sprites using 2D physics, not a realistic 3D runner as the old README implied.

The review covered all original gameplay scripts, both scenes and their serialized callbacks, referenced prefabs/animation behavior, resources/audio import settings, package/configuration files, save/economy/lifecycle flow, and generated repository contents. Generated package/cache files are not authored game source and were not treated as features to preserve.

## Findings and changes

| Finding | Resolution |
| --- | --- |
| Touch movement reads only when `touches.Length < 0`, an impossible condition | Correct touch polling/finger tracking, normalized threshold, one action per gesture, UI exclusion |
| Input continues during pause/death and execution order is unreliable | Authoritative run-state gates, ordered gesture polling, cleared jump buffer, live-game submit/nav exclusion |
| Instant lane teleport and lane clip can interrupt a jump | Eased root transition; keep an active jump clip; short input buffer/watchdog |
| Duplicate manager Awake still initializes/saves data | Early duplicate return/disable; one persistent instance; safe direct Gameplay bootstrap; static reset |
| Unsafe binary deserialization, missing path separator, swallowed IO errors | Versioned bounded JSON, backup/recovery/quarantine, restricted legacy reader, visible error/retry and correct paths |
| Debug-only 9,000-star starter balance | New profiles start at zero; existing migrated balance remains intact |
| Repeated death callbacks can double-credit stars/results | Once-only death/finish/commit; stars banked on pickup, never re-awarded at results |
| Power-up coroutines overlap and an older timer expires newer power | One refreshed gameplay-time countdown; visible timer |
| Inactive star effects are missed by tag search | Discover/cache inactive children from the explicit pool holder |
| Death freezes explosion/effect animations | Separate scaled player/world time from unscaled short effects/UI |
| Pause animation event changes global timescale | Visual callback only; controller/state decides when to resume |
| Obstacle enumeration repeats component-array fetches; do/while can spin | One enumeration plus bounded candidate selection |
| Per-tile frustum allocation/camera lookup; decoration/order drift | Shared plane buffer/camera, cached composite bounds/renderers, child sorting offsets |
| Endless coordinates, sorting orders, and float-only survival clock eventually lose precision | Origin shifts, order normalization, double-precision score clock; native soak test required |
| Music setting neither persists nor fully controls the audio presentation | Saved independent music/SFX/reduced-motion settings and reactive audio/UI |
| Clip replacement interrupts pickup/impact SFX | One-shot sources, streamed music, fades/ducking, proper game-over SFX semantics |
| No onboarding/touch controls/power HUD/save error UI; incomplete menu feedback | Full runtime menu/shop/settings/HUD/pause/results/help/loading flow with safe areas and clear states |
| No code/asset regression checks or build docs | Pure contracts, NUnit adapter, asset validator, CI, Editor validation/build entry points, documentation |
| Generated files dominate the repository and README states the wrong license | First cleanup phase + ignore rules/helper; accurate README and all-rights-reserved license link |

## Verification record

Completed in the sandbox:

- Runtime C# compilation against cached Unity player API assemblies, without warnings.
- EditMode test-assembly compilation against cached NUnit/runtime references.
- 33 shared engine-independent contracts passed, including save IO/recovery, purchase/state/pool rules, and legacy parsing/fuzz/truncation cases.
- A restricted importer check using locally generated original private-field save output.
- C# source syntax, test-project XML, GUID/metadata/scene/package contracts checked.

The temporary compiler/runtime tools and trusted fixture were outside the repository and are not shipped. The console harness's .NET/JSON adapter exercises pure code; it is not native Unity execution. The pinned editor, JsonUtility/native EditMode run, rendered UI review, physics/Animator integration, audio listening, profiling, and signed players remain unverified here.

## Known limitations and release blockers

1. **Native validation is mandatory.** Execute the acceptance list, especially kinematic trigger/jump clips, pause/focus/countdown, repeated scene loading, origin/sorting rebasing, and audio streaming/preferences.
2. **Toolchain/store readiness is not certified.** Keep the pinned editor for initial compatibility verification, then review a supported Unity/Android toolchain and current native/page-size/store requirements before release.
3. **Historical generated cache cleanup is partial.** 8,985 generated Library entries remain tracked to keep this change set within its file-change budget. The untracking helper and maintenance guide complete that locally; no history rewrite was attempted.
4. **Old canvas/assets remain for compatibility.** Runtime UI hides them. Removing them safely is a later editor-assisted migration after tests, not an unchecked scene-YAML deletion.
5. **Assets require a provenance audit.** Repository LICENSE is all rights reserved; original font/art/audio redistribution rights are not established by this code review.
6. **Save durability is local/best effort.** Abrupt termination can lose the latest unflushed two seconds; no cloud sync or multi-writer locking. Unsupported/newer versions are protected. Legacy import supports only the known data-only record shape.
7. **Portrait is the supported presentation.** Relative layout/safe areas are implemented, but tablet/landscape/screen-reader/localization/controller certification is not claimed.
8. **Authored jump semantics are retained.** The clip disables its collider during its immunity window, so some airborne pickups are intentionally missed; a separate pickup sensor would require native collision/layer tests.
9. **Economy/patterns are not silently rebalanced.** The first paid runner still costs 1,000 stars. Real earn-rate/fairness testing should inform changes.
10. **No new release binary/screenshots were made.** The old APK/screenshots were misleading/outdated and removed from the current README/deliverables.

## Suggested improvements, in order

### Before public release

- Complete native/device tests and record evidence; fix any resulting regressions.
- Finish cache untracking, confirm package restoration from a clean clone, and review asset rights/branding.
- Resolve toolchain/platform/signing requirements; build/test the exact signed non-Development artifact.
- Measure a lower-end phone and review rendering quality/audio balance/thermal behavior.

### Next gameplay/support iteration

- Configurable balance/catalog ScriptableObjects with regression-tested prices and progression.
- Fair/deterministic pattern seeds and an easier initial pattern set; daily/local challenge goals if desired.
- Save export/import and an explicit double-confirmed reset option, with strict validation and recovery.
- Remappable/controller inputs, larger-text/localized layouts, and accessibility testing.
- A separately tested always-on pickup sensor if airborne collectible behavior is redesigned.

### After profiling demonstrates need

- Sprite atlases and legacy-resource trimming with measured memory/draw-call comparisons.
- Tunable FX/quality tiers; centralized tile dispatch only if per-tile updates are a bottleneck.
- Split larger UI panel presenters/catalog data as scope grows rather than importing a heavy UI/tween framework now.

Ads, cloud leaderboards, real-money purchases, accounts, and external services were deliberately **not** added to this offline game. They would add policy/security/network dependencies and require an explicit product decision.
