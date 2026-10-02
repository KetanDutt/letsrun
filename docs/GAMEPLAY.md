# Gameplay and balance

## Objective

Stay alive as long as possible. The track scrolls continuously; switching lanes and jumping avoid obstacles. Collect stars to unlock cosmetic runners. The T-Rex pickup provides temporary obstacle-smashing power.

## Input

| Action | Keyboard | Gesture | Touch controls |
| --- | --- | --- | --- |
| Upper lane | D, W, Right, Up | Right | UPPER |
| Lower lane | A, S, Left, Down | Left or down | LOWER |
| Jump | Space | Up | JUMP |
| Pause/resume | P or Escape | — | Pause / Keep running |

Mouse drags can emulate gestures in the editor/desktop build. Touch polling has precedence over Unity's emulated mouse events. A gesture yields at most one action, tracks its original finger, and does not start over UI. The swipe threshold is 6% of the shorter screen dimension, with a 24-pixel floor.

Actions are ignored during help, countdowns, pause, results, death, and scene loading. Jump buffering is short, not an automatic held-key jump feature. A lane change does not interrupt an active jump clip.

## Score and difficulty

The HUD shows survival seconds with the current scene's scoring factor of **1**. Score floors elapsed gameplay time; countdown/pause time does not count. `distance_Factor` is retained for authored-scene compatibility: keep it at 1 if displaying seconds or comparing existing records.

| Value | Default |
| --- | --- |
| Starting speed | 4 world units/s |
| Acceleration toward target | 5 world units/s² |
| Cruising target | 12 world units/s |
| At 30 survival points | 14 world units/s |
| At 60 survival points | 16 world units/s |
| Initial obstacle-free warm-up | 1.2 gameplay seconds after countdown |
| Countdown | 3, 2, 1 at 0.7-second intervals; brief GO badge |
| Gap after pattern recycling | 0.35 seconds before a new selection |
| Lane tween | 0.16 seconds, cubic ease-out |
| Jump input buffer | 0.15 seconds |
| Lost jump-event watchdog | 0.9 seconds |

Speed changes approach targets rather than jumping abruptly. The HUD shows progress to the next pace and a milestone toast at 30/60.

## Hazards, jumping, and power

- Hazard prefab tags are `Obstacle`. A normal collision ends the run once.
- The existing jump clip moves the sprite and temporarily disables its Collider2D. This authored immunity window is preserved; pickups can also be missed while that collider is disabled. It is not a gravity-based platformer jump.
- The `Trex` pickup refreshes power to **seven seconds**. A second pickup refreshes the same timer, rather than starting a competing expiration coroutine.
- While powered, obstacle contacts produce an explosion/impact effect and crash SFX without ending the run.
- Power duration freezes during pause/countdown. A steady tint, sprite change, trail, and visible timer communicate the state; there is no rapid flashing.
- The root Rigidbody2D is kinematic with full kinematic contacts, rotation frozen, and continuous detection. The real collision/animation interplay must be checked in Unity on target devices.

## Stars and runners

- New profile: **0 stars**, runner 01 unlocked and selected.
- All eight paid runners cost **1,000 stars** each.
- Unlocking charges once, marks ownership, selects the runner, and saves.
- Selecting an owned runner is free. Re-selecting the selected runner does nothing.
- Insufficient funds shows how many more stars are needed, rather than only printing to the Console.
- Browsing wraps across all nine runners; the UI refreshes from the validated profile.
- Stars are banked into the profile when collected. Results display the amount earned this run, not another currency award.
- Wallet additions and statistics use saturated arithmetic, avoiding integer overflow into negative balances.

The original debug starter grant is removed for **new profiles**. Safe legacy migration preserves an existing player's recorded balance/ownership; it does not retroactively confiscate historical balances.

## Run lifecycle

First-run help is saved only when the player chooses to start. The pause menu offers resume, retry, home, and settings. Focus/background loss pauses automatically. Resume counts down before re-enabling the world.

A run that reached Running increments total runs once on game over, retry, home, or normal quit. Exiting from the initial tutorial/countdown before Running does not count. Best score can be checkpointed on pause without finishing the run. Settings display all-time run and collected-star totals.

## Balance work still recommended

Keep the original prices/pattern difficulty until real play-testing is available. Measure star earn rate, time to first unlock, jump readability, reaction time at max pace, and pattern fairness. Possible follow-ups: an easier onboarding pattern set, shorter early unlock progression, challenge goals, and a deterministic seed for reproducing difficult runs. None of those untested rebalances are silently applied here.
