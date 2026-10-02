# Architecture

## Overview

This is a Unity 2D game with isometric sprites, the built-in render pipeline, the legacy Input API, Animator clips, uGUI, and Physics2D. There is no backend or required network service.

```text
MainMenu scene                          Gameplay scene
  MainMenuController                     GameplayController → RunSession
  CharacterSelectScript                  PlayerController / SwipeManager
  RunnerUI (runtime)                     MapGenerator / OffScreen
  SoundManager (runtime menu audio)       ObstacleHolder pattern pool
             \                           RunnerUI / RunnerFeedback / SoundManager
              → GameManager (persistent) → GameData / RunnerRules
                                          SaveStore → UnityProfileCodec → profile.json
```

`LetsRun.Runtime.asmdef` contains the game code and references `Unity.ugui`. The pure core and `GameData` have no Unity dependency. The EditMode assembly references the runtime and Unity Test Framework. Editor build/validation utilities remain in `Assets/Editor`.

## Responsibilities

| Type | Owns |
| --- | --- |
| `GameManager` | One persistent profile, validated purchases, banked stars, settings/events, throttled saves, lifecycle flushes |
| `GameData` | Versioned serialized fields and normalization; no engine behavior |
| `RunnerRules` | Balance constants, saturated arithmetic, easing, bounded pool selection |
| `SaveStore` | Size/version checks, last-good backup, replacement writes, quarantine, visible error result |
| `UnityProfileCodec` | Unity field-based JSON conversion and required-header validation |
| `LegacySaveReader` | Restricted reading of the original four-field NRBF record; no type activation |
| `RunSession` | Ready/countdown/running/paused/game-over state, elapsed score, run star count |
| `GameplayController` | State/timescale transitions, camera travel, spawning, UI coordination, once-only run commit |
| `PlayerController` | Buffered jump, lane tween, trigger handling, seven-second refreshed power timer, once-only death |
| `SwipeManager` | One-frame gesture polling, finger tracking, pixel-normalized threshold, UI exclusion |
| `MapGenerator` / `OffScreen` | Initial tile pool, shared frustum planes, cached bounds, recycling, origin/order rebasing |
| `ObstacleHolder` | Re-enable/reset authored children and scroll one pooled pattern |
| `RunnerUI` / UI helpers | Menu, HUD, modal flow, feedback, safe areas, navigation, unscaled cosmetic tweens |
| `SoundManager` | Music fade/ducking and independent one-shot SFX honoring profile preferences |
| `SceneNavigator` | Guarded async scene transitions and a loading overlay |
| `AnimationEvents` | Authored clip receivers; visual completion cannot change global game state |

## State and timing

```text
Ready → Countdown → Running → GameOver
            ↓          ↓
          Paused ←──────┘
            └→ Countdown → Running
```

- Only `Running` accepts movement/pickups or advances score/spawns.
- Ready, countdown, pause, and game over use `Time.timeScale = 0`.
- Countdown, UI, short feedback particles, and effect Animator clips use **unscaled time**. They still finish while the world is paused.
- Authored player movement clips and the T-Rex duration use scaled gameplay time.
- Background/focus loss pauses a running run or its countdown, checkpoints the best score, and flushes progress. Returning to the app does not automatically resume.
- Resume goes through a fresh countdown. Visual animation events cannot resume a dead run.
- Scene changes restore timescale and prevent repeated taps while loading.

The exact run state lives in `RunSession`, not in panel visibility or Animator state.

## Progress flow

A star pickup increments the run counter and is immediately banked in `GameManager`'s profile. Disk writes are throttled, with immediate flushes at pause, purchases, settings changes, scene exit, results, and quit. Game over **does not add the run's stars again**.

`GameplayController.CommitRun` and `PlayerController.player_Died` guard repeated trigger/lifecycle callbacks. The best score can be checkpointed without counting a new run. A started run is counted once when finished, retried, abandoned, or normally quit.

`ProgressChanged`, `PreferencesChanged`, and `SaveProblem` update UI/audio. Scene-owned listeners unsubscribe on destruction so the persistent manager does not retain old canvases/controllers.

## Scene and asset contracts

- Scene names: `MainMenu`, `Gameplay`; MainMenu is first in Build Settings.
- Main camera tag: `MainCamera`.
- Custom tags/sorting layers: constants in `MyTags`, declared in `TagManager.asset`.
- Root Player retains its referenced Player, shadow, explosion, sprites, Collider2D, Rigidbody2D, and Animator.
- Movement state names: `PlayerWalk`, `PlayerJump`, `ChangeLine`.
- Authored event names: `PlayerWalkAnimation`, `AnimationEnded`, `PausePanelClose`.
- Runtime resource paths: `Sprites/Player/hero0_big` through `hero8_big`, `Sprites/Player/trex`, the two menu-background images, and `Fonts/LuckiestGuy`.
- New HUD Text references and audio clips are explicitly serialized in the scenes. Name-based Text lookup is a one-time compatibility fallback.
- Existing script/prefab/animation GUIDs are retained. Never regenerate metadata on a rename.

The old canvas remains as an authoring compatibility layer. After native acceptance tests, a dedicated migration can remove it and its unused UI assets; this review does not destructively rewrite scene hierarchies without an editor.

## Update ordering and long runs

- GameManager bootstraps early (`-1000`).
- Swipes poll before gameplay/player consumption (`-200`). UI raycasts are explicit, so they do not depend on EventSystem's update order.
- Gameplay moves the camera (`-100`).
- MapGenerator updates shared visibility in LateUpdate (`100`); tiles recycle after that (`200`).

The camera owns player/pattern transforms. Moving the camera forward while scrolling patterns back keeps obstacles in track/world space. Impact shake modifies only the projection matrix, not those physics-bearing transforms.

Every 2,048 world units, the camera, tiles, tail positions, active feedback, and star effects are translated together. Tile holders remain near the origin, and tile/decoration sorting orders are renormalized before reaching Unity's signed sorting-order limit. These paths require native soak testing; see [Testing](TESTING.md).

## Extension points

Add balance rules to the pure core and test them first. Add a hazard/pickup through a prefab plus an authored pattern and its tag contract. Add panels through the UI helpers, not by coupling file IO to UI buttons. Change save versions only with an explicit migration and forward-version protection. For larger scope, move balance/catalog data into ScriptableObjects and split panel presenters after native behavior is stable.
