# Testing and acceptance

## Automated checks

### Engine-free asset validation

```sh
python3 Tools/validate_project.py
```

Checks metadata presence/uniqueness/orphans, serialized GUIDs/local IDs, scene button callback names, required resource/clip/HUD references, build scene order, package lock closure, assembly-definition JSON, Android architecture/backend defaults, and README links/license consistency. It also reports remaining tracked generated files.

It does not load assets through Unity, execute Animator clips, measure physics, or render the UI. The uGUI GUID allowlist is tied to the pinned `com.unity.ugui@1.0.0`, not a general exemption for missing assets.

### Core contracts

```sh
dotnet run --project Tools/Tests/Runner.Core.Tests.csproj --configuration Release
```

The console program returns a nonzero exit code on any failed contract. No NuGet test packages are required. The 33 contracts cover:

- Profile defaults/repair and saturated counters.
- Purchase validation, exact-price charging, owned selection, invalid indices, and insufficient balance.
- Pace thresholds and exact/monotonic/clamped easing.
- Empty/exhausted/wrapping pool searches.
- State transitions, pause/countdown, score flooring, long-run clock precision/saturation, invalid deltas, pickup gating, and repeated finish.
- Save/settings round trips, backup recovery, corruption quarantine, required JSON fields, size/UTF-8 limits, write failures, interrupted temp files, changed future schemas, and future-backup protection.
- Restricted legacy import, inline/referenced Boolean arrays, unexpected types/trailing data, truncation, and random streams.

`Assets/Tests/EditMode/CoreContractCases.cs` is shared with the Unity tests; the console adapter uses System.Text.Json. It intentionally does not attempt to run Unity native internal calls.

### Unity EditMode

Open **Window → General → Test Runner → EditMode → Run All**. `CoreEditModeTests` runs the same contracts with `UnityProfileCodec`/JsonUtility. These tests use unique temporary directories, not a player's real save path.

For a licensed editor in CI/local automation:

```sh
Unity -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults Artifacts/editmode-results.xml \
  -logFile Artifacts/editmode.log
```

Use the actual platform-specific editor executable/absolute path. Do not assume the sandbox compilation check replaces these tests. The included GitHub workflow runs only the engine-independent validations; there is no embedded Unity license/signing secret.

## Verification performed for this revision

- **33 core contracts passed**, using the shared source in a temporary portable .NET host.
- **Runtime scripts compile-checked** against the repository's cached Unity player API assemblies.
- **EditMode test assembly compile-checked** against cached NUnit/runtime assemblies; actual native JsonUtility execution was unavailable.
- Safe migration also checked against a locally generated record of the original private-field DTO, without runtime binary deserialization.
- All C# source syntax and the test-project XML checked.
- Asset/package/scene checks run; the only expected repository warning is historical generated `Library` data still tracked.

The pinned Unity editor, graphics/audio/physics runtime, Android hardware, and signed-player build were not installed/available. The following native acceptance checks are **pending**, not implicitly passed.

## Manual native checklist

Use copied/temporary test profiles. Never delete the only valid primary/backup/legacy save to test recovery.

### Startup and lifecycle

- [ ] Fresh profile has 0 stars and only runner 01; onboarding appears once after choosing to start.
- [ ] Direct Gameplay entry works without first entering MainMenu.
- [ ] 3–2–1–GO is readable; no score, spawns, movement, or pickup during the countdown.
- [ ] Pause during Running and during the initial/resume countdown. All world state freezes; UI and effects remain responsive.
- [ ] Resume goes through a countdown and clears buffered input.
- [ ] Background/foreground, focus loss, Android Back, and desktop Escape behave consistently; app return does not auto-resume.
- [ ] Rapid retry/home/run taps cannot create overlapping scene loads or duplicate managers.
- [ ] Exit/retry/home after collecting stars retains the banked stars and records one started run.
- [ ] Normal desktop/app quit commits once and does not throw during object teardown.

### Movement, collisions, and fairness

- [ ] Mouse, touch, keyboard, and on-screen buttons all perform the documented action once.
- [ ] Gestures beginning on pause/touch controls/modals do not become swipes; canceled/second touches do not create phantom actions.
- [ ] Space after clicking a touch-control button does not submit that UI button again.
- [ ] Lane changes ease cleanly, including quick reversal and mid-jump movement.
- [ ] Buffered jumps near landing work, but holding a key does not automatically repeat jumps.
- [ ] Every hazard pattern can be encountered/avoided at 12/14/16 pace. Test spikes, cars, TNT, and mixed pickups specifically.
- [ ] The authored collider-disable jump window works with the kinematic Rigidbody/full contacts; landing restores collisions.
- [ ] Multiple triggers in one physics step produce one death, one result, and no duplicate stars/run count.
- [ ] T-Rex destroys obstacles; a second pickup refreshes a full seven seconds; pause does not consume power.
- [ ] Inactive star/explosion effects replay and reset correctly after repeated pattern reuse.

### Economy and saves

- [ ] Browsing wraps nine runners; selected/owned/locked state is correct immediately on opening.
- [ ] 999 stars cannot buy; 1,000 can; repeated/owned selections do not charge again.
- [ ] Purchased/selected runner is the actual gameplay sprite after restart/relaunch.
- [ ] Music/SFX/reduced-motion preferences survive relaunch and show the correct state.
- [ ] A copied corrupt primary recovers a copied valid backup without destroying it.
- [ ] Both historical binary-file locations import supported test data, preserving the original file.
- [ ] Unsupported legacy data is retained and reported, not deserialized.
- [ ] Future-version JSON is protected/read-only and the banner explains upgrading.
- [ ] Simulated unwritable/full storage produces visible feedback and a later successful retry clears it.

### Presentation/audio/accessibility

- [ ] All menu/shop/help/settings/pause/results/loading text fits and can be navigated with keyboard and touch.
- [ ] Check 9:16, tall/notched phone sizes, small portrait window, and proposed tablet/wide-window layouts.
- [ ] Save/power/toast overlays layer correctly; no invisible graphic intercepts gameplay gestures.
- [ ] Music fades/ducks; toggles work immediately; game-over SFX does not play with SFX muted.
- [ ] Fast pickup/impact sounds overlap without objectionable clipping or latency on speakers/headphones.
- [ ] Explosions/results continue after death; shake resets and never moves physics-bearing camera children.
- [ ] Reduced motion suppresses decorative bob/pulse/shake/confetti/count-up without hiding essential gameplay state.

### Soak and performance

- [ ] Cross many 2,048-unit origin shifts and >4,096 row recycling orders; no gaps, order jumps, jitter, or missed collisions.
- [ ] Repeat at least 50 menu/gameplay/retry transitions; singleton/listener/native texture counts stabilize.
- [ ] Measure frame time, allocations, draw calls, memory, audio, save spikes, and thermal/battery behavior on chosen baseline devices.
- [ ] Confirm ARM64 IL2CPP build, current native-library/page-size/platform requirements, and a non-Development build before release.

Record device, OS, editor/package versions, build mode, resolution, profile/settings, and reproduction steps for failures. Add pure-rule regressions to the shared contracts and native scene regressions once an editor is available.
