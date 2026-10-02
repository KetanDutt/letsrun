# Let's Run

A small, offline **two-lane endless runner**, built in Unity with 2D physics and isometric sprite art. Switch lanes, time your jumps, collect stars, and turn into a T-Rex to smash through obstacles. Spend your stars on nine different runners.

This revision improves the existing Unity game; it is not a web remake. It keeps the authored track, sprites, obstacle patterns, animation clips, and sound assets.

## What's improved

- **Reliable controls:** fixed mobile swipes, UI-aware gesture detection, keyboard controls, on-screen lane/jump buttons, eased lane changes, and a short jump-input buffer.
- **A complete game flow:** first-run instructions, a ready countdown, pause/resume countdowns, automatic pause on focus loss, guarded async scene changes, and retry/home actions.
- **Safer progress:** versioned JSON, last-known-good backups, recovery notices, bounded validation, and a restricted data-only importer for the original save. No runtime `BinaryFormatter` deserialization.
- **Correct economy:** new players start with zero stars; pickups are banked as they happen; repeated death callbacks cannot award them twice; purchases validate balance and ownership.
- **A cohesive UI:** safe-area-aware menu, runner shop, settings, HUD, pace progress, visible T-Rex countdown, results, personal-best celebration, and save-error feedback.
- **Game feel:** unscaled UI tweens, button feedback, a gently floating menu illustration, pooled dust/sparkles/impact particles, restrained screen shake, confetti, and music fades/ducking.
- **Persistent preferences:** separate music and SFX toggles plus reduced decorative motion. The game-over cue respects the SFX setting.
- **Less runtime overhead:** one shared non-allocating frustum query, cached tile bounds, a bounded obstacle-pool search, integer-only score-label updates, and origin/sorting-order rebasing for long runs.
- **Maintainability:** runtime/test assembly definitions, 33 shared core contracts, asset checks, a CI workflow, editor pre-build validation, and development-build commands.

## Open and run

1. Install Unity Hub and **Unity 2020.3.26f1**, the version in `ProjectSettings/ProjectVersion.txt`.
2. Add the repository root as a Unity project. Let Unity restore the pinned packages and reimport assets. Do not open only `Assets/`.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**. Opening `Gameplay` directly also bootstraps the profile manager.
4. The first run shows instructions. Choose **Let's go** to start the countdown.

The shipped presentation is portrait. The desktop default window is 720 × 1280; Android orientation is portrait. Install the corresponding Unity Hub platform module before making a player build.

> **Release status:** a production-oriented candidate, not a certified store release. Unity Editor/device execution was unavailable during this review. Native play-testing, actual JsonUtility EditMode execution, performance measurement, signing, asset-rights review, and a current Android/Unity toolchain compliance check remain release gates. See [the release checklist](docs/RELEASE.md).

## Controls

| Action | Keyboard | Touch / mouse |
| --- | --- | --- |
| Upper lane | D / W / Right / Up | Swipe right or tap **UPPER** |
| Lower lane | A / S / Left / Down | Swipe left/down or tap **LOWER** |
| Jump | Space | Swipe up or tap **JUMP** |
| Pause / resume | P or Escape | Pause button / **Keep running** |
| Menu/dialog navigation | Arrows, Enter/Space, Escape | Tap/click buttons |

Gameplay submit/navigation events are disabled while running, so Space does not also activate a previously focused UI button. Escape closes nested dialogs before exiting the menu; from results it returns home.

## Validation

Requires Python 3.10+ and the .NET 8 SDK for the engine-free tools:

```sh
python3 Tools/validate_project.py
dotnet run --project Tools/Tests/Runner.Core.Tests.csproj --configuration Release
```

Inside Unity, run **Window → General → Test Runner → EditMode → Run All**, then **Tools → Let's Run → Validate Project**. The Unity tests use the same contracts with the real `JsonUtility` adapter. CI checks the engine-independent contracts and serialized asset/package wiring; it does not claim to build Unity players.

See [testing instructions](docs/TESTING.md) for the essential manual collision, input, audio, save, and lifecycle checks.

## Build

- Keep **MainMenu**, then **Gameplay**, enabled in Build Settings.
- Use **Tools → Let's Run → Build → Linux Development** or **Android Development APK** for repeatable development builds.
- Generated output goes into ignored `Builds/` (or `LETSRUN_BUILD_PATH`). The old checked-in APK was removed because it did not contain these changes.
- Android defaults now include ARMv7 + ARM64 and IL2CPP. This alone does **not** establish current store compliance.

[Build/deployment details](docs/RELEASE.md) cover CLI commands and the release gates. Signing material must remain outside source control.

## Project layout

```text
Assets/
  Scenes/          MainMenu and Gameplay
  Prefabs/         Original player, track, hazards, pickups, explosion
  Animations/      Original movement and effect clips
  Resources/       Runtime sprite/font lookups
  Sounds/          Referenced music and SFX
  Scripts/
    Core/          Engine-independent profile, economy, state, persistence
    Helper Scripts/ Scene coordination, profile/audio services, tile recycling
    Player Scripts/ Player actions, gestures, authored animation events
    Obstacle Scripts/ Reused obstacle patterns
    UI/            Runtime uGUI, safe areas, tweens, feedback
    Effects/       Fixed-size world feedback pool
  Editor/          Validation and development build entry points
  Tests/EditMode/  Shared contracts + Unity/NUnit adapter
docs/              Architecture, gameplay, saves, UI, testing, performance, release
Tools/             Asset validator, core test harness, staged cleanup helper
```

## Documentation

Start with [the documentation index](docs/README.md), or go directly to:

- [Setup](docs/GETTING_STARTED.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Gameplay and balance](docs/GAMEPLAY.md)
- [Save format and recovery](docs/SAVE_FORMAT.md)
- [UI and accessibility](docs/UI_AND_FEEDBACK.md)
- [Performance](docs/PERFORMANCE.md)
- [Testing](docs/TESTING.md)
- [Release checklist](docs/RELEASE.md)
- [Maintenance and remaining cache cleanup](docs/MAINTENANCE.md)
- [Review findings, limitations, and suggested improvements](docs/PROJECT_REVIEW.md)
- [Change log](CHANGELOG.md)

Historical generated caches still tracked under `Library/` are a known repository-maintenance limitation. The largest source-package cache, stale script assemblies, APK, and IDE artifacts were removed in this change set. `Tools/cleanup_generated.py` previews the remaining cleanup; `--apply` untracks it without deleting local working files. See the maintenance guide before using it.

## License

**All rights reserved.** The actual terms are in [LICENSE](LICENSE); this repository is not MIT-licensed. Confirm permission and third-party asset/font/audio provenance before redistribution or commercial release.
