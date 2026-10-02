# UI, accessibility, and feedback

## Presentation

`RunnerUI` builds a consistent runtime uGUI canvas above the original scenes. The original UI remains hidden for serialized-reference compatibility; new panels do not require hand wiring. No paid tween library, render-pipeline migration, or newly sourced artwork/audio is required.

The menu uses the existing sky gradient and isometric illustration. The selected-runner label, shop preview, and actual gameplay sprite are driven by the saved selection; the original menu illustration is decorative brand art.

### Visual language

| Token | Use |
| --- | --- |
| Dark ink | Headlines, readable button labels, dark HUD/control surfaces |
| Warm paper | Cards and modal surfaces |
| Coral | Primary run/jump/retry actions and impact feedback |
| Teal | Secondary actions and T-Rex state |
| Gold | Stars, wallet, and personal-best celebration |
| Muted ink | Secondary explanatory text |

Title/numeric display text uses the original LuckiestGuy font, now loaded from Resources with its GUID preserved. Body/action labels use Unity's built-in Arial with a 22-reference-pixel minimum. Help descriptions and close/pause targets were enlarged for portrait-phone readability; actual device sizing remains an acceptance test. Cards use a shared small generated nine-slice rounded sprite and subtle shadows rather than expensive blur/postprocessing.

## Screens and interaction

- **Menu:** title, best score, wallet, selected-runner state, run/shop/settings actions.
- **Shop:** wraparound browsing, locked/owned/selected states, preview, current wallet, valid purchases, missing-star feedback.
- **Settings:** independent music/SFX toggles, reduced motion, instructions, all-time stats.
- **Onboarding:** numbered lane/jump/pickup instructions and an explicit start action.
- **HUD:** survival score, run stars, pause, pace progress/milestones, T-Rex label/fill, touch controls, fading input hint.
- **Pause:** current run status, resume countdown, retry/home/settings.
- **Results:** animated score, banked stars, best/total runs, retry/home/settings, once-only personal-best confetti.
- **Loading/error:** guarded scene transition state, missing-track message, visible save-error/protected-version feedback.

Modal backdrops block pointer input. The underlying content stops receiving navigation/raycasts while a modal is open. Escape closes a nested dialog before leaving the menu; paused gameplay uses Escape/P to resume through a countdown.

During Running, EventSystem navigation/submit is disabled and touch-control buttons use no navigation selection. This prevents Space from also submitting a previously focused jump/lane/pause button. Menus and modals restore keyboard focus/navigation.

## Motion and VFX

| Feedback | Implementation / budget |
| --- | --- |
| Lane movement | Cubic ease-out of the root position, default 0.16 s; authored movement clips preserved |
| Button hover/press/focus | Unscaled smoothing to subtle scale targets |
| Panel entrance | Unscaled 0.28 s scale/opacity tween with an exact final state |
| Star/countdown response | Short scale pulse; counters updated only when values change |
| Menu illustration | Slow 7-pixel idle bob |
| Results score | 0.7 s eased numeric count-up, once per result |
| Dust, stars, power, impacts | Reused 48-sprite world pool, single updater; exhausted pool drops optional particles |
| Personal-best confetti | Reused 20-piece uGUI pool, rendered above the results overlay |
| Impact shake | Brief projection-matrix offset; no camera/player/pattern physics transform is shaken |
| Explosions/star clips | Existing Animator assets, unscaled so they finish after death/pause |

Power feedback uses a steady tint and timer rather than rapid flashing. Authored player jump/lane squash clips continue to provide necessary action feedback.

## SFX and music

The original clips are reused and scene-referenced. The previously unused menu music/click/purchase clips are now wired. Move/jump have their own sources; pickup/impact/UI cues use `PlayOneShot`, rather than replacing the source's clip and cutting off the previous cue.

- Music fades in/out using unscaled time; pausing/counting down ducks it.
- Music and SFX settings are independent and persisted.
- The game-over cue is SFX, not background music, and respects SFX mute.
- Coin pitch varies slightly, avoiding identical repeated cues.
- Music import is streaming; short effects retain preloaded/decompressed playback and mono import.

Actual volume balance, latency, clipping, interrupted streaming startup, headphones, and silent/background behavior still require target-device listening tests.

## Safe areas and accessibility scope

The canvas targets 720 × 1280, with normalized layouts and safe-area fitters for content, modal, and feedback layers. The primary supported view is portrait. Check notches, gesture insets, tall displays, desktop resizing, and any proposed tablet/landscape support in Unity before claiming certification.

Reduced Motion is saved and disables/reduces decorative bobbing, button/panel pulses, score count-up, shake, and confetti; world pickup feedback becomes a small steady fading cue. Essential player jump/lane movement remains so gameplay is legible.

Labels explain toggles and ownership in text, not color alone. Dark-on-light actions and larger touch targets improve readability. This is **not** a certified accessibility or screen-reader implementation. Localization, remappable controls, scalable text, controller-specific hints, and platform screen-reader integration remain future work.
