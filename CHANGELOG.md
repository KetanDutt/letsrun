# Change log

## 1.1.0 candidate — 2026-10-02

### Fixed

- Broken touch swipe detection, frame-order/UI gesture interference, and gameplay actions while paused/dead.
- Duplicate manager initialization, unsafe binary saves, missing save-path separator, silently swallowed save failures, invalid/locked selections, and the debug starter balance.
- Repeated game-over currency/results, competing power timers, inactive star-effect discovery, frozen post-death effects, animation-driven global resume, and interrupted SFX playback.
- Unbounded obstacle selection, repeated holder enumeration, per-tile frustum allocations, stale decoration sorting, and long-run coordinate/order growth.
- Incorrect README descriptions/license and stale generated/build artifacts in the current source checkout.

### Added

- Pure run/economy/profile/persistence core, restricted data-only legacy migration, JSON backup/recovery/quarantine, settings/statistics persistence, and save status UI.
- First-run help, countdown/resume countdown, focus-loss pause, async guarded navigation, smooth lanes, jump buffer/watchdog, and touch controls.
- Cohesive safe-area-aware runtime menu/shop/settings/HUD/pause/results/loading UI; power/pace feedback; reduced motion.
- Unscaled UI tweens, button/idle/pickup feedback, bounded world particles, projection-only impact shake, reusable best-score confetti, music fades/ducking, and proper mute semantics.
- Runtime/test assembly definitions, 33 shared core contracts, asset validator, CI, editor pre-build validation, development build entry points, source hygiene rules, cleanup helper, and project documentation.

### Changed

- New profiles start at zero stars; stars bank on pickup rather than being re-awarded at results.
- Title font moved into Resources with its GUID preserved; existing audio reused/wired for the menu and UI.
- Portrait orientation/default desktop window; Android ARMv7+ARM64/IL2CPP and minimum API 23 defaults; source version 1.1.0/build code 2.
- Background audio imports stream; SFX import as mono. Unused service/editor packages removed.

### Release notes / pending

- No native Unity/device test, measured performance claim, signed release artifact, or current store-compliance certification yet.
- Hidden legacy UI remains for authoring compatibility. Remaining historical Library files have a documented separate cleanup.
- Current LICENSE is all rights reserved; original assets still need a rights/provenance audit before redistribution.
