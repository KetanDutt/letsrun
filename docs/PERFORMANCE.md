# Performance and long-run stability

## Implemented changes

| Original issue | Change |
| --- | --- |
| Every tile allocates six frustum planes and repeatedly looks up Camera.main each frame | MapGenerator owns one camera and one preallocated plane array; OffScreen uses the shared query in LateUpdate |
| Visibility only considers the parent sprite, not its decoration | Composite static bounds are calculated once and translated on reuse |
| Recycling updates root order but leaves trees/grass with stale orders | All cached descendant renderers receive the same sorting-order offset |
| Unbounded sorting-order growth in an endless run | Row orders are renormalized before the signed sorting limit |
| Camera/world coordinates grow forever | Translate camera, tiles/tail positions, effects, and patterns together every 2,048 units; keep holders near origin |
| GetComponentsInChildren is repeated inside an obstacle enumeration loop | Fetch holder components once and retain the pool |
| Random do/while can spin forever when all patterns are active | Bounded wraparound search returns no candidate when empty/exhausted |
| Touch-array polling allocates and the length condition is impossible | Use touchCount/GetTouch, original-finger tracking, and squared thresholds |
| Score string and Text are assigned every frame | Refresh at integer score/star changes; power label at tenths of a second |
| Effects need frequent Instantiate/Destroy or are not found while inactive | Fixed world/UI pools and explicit inactive-child effect discovery |
| Audio clips replace/cut each other on one source | One-shot playback on cached 2D sources; background music streams |
| Save writes are unrestricted/silent/unsafe | Small bounded JSON and throttled writes with immediate lifecycle flushes and visible failure/retry |

The authored initialization uses 126 recycling root tiles. The original implementation therefore performed about 126 plane-array creations per frame. This revision shares one persistent array. That is a structural allocation improvement, **not a measured FPS claim**.

## Budgets

- Target frame rate: 60. Mobile disables vSync; desktop uses vSync.
- One active authored obstacle pattern at a time. No new patterns are instantiated during the run.
- 48 world feedback sprites, one updater. Optional excess effects are dropped rather than allocating.
- 20 uGUI confetti pieces, reused; updater disables itself when idle.
- Tile/decor bounds/renderers are cached; startup allocation is intentional.
- Profile size: 64 KiB maximum; normal dirty-profile saves at most once per two seconds; failed writes retry after ten seconds.
- Resource sprite/font lookups occur on initialization/browsing setup, not inside gameplay frame loops.

## Profile in Unity before shipping

Use a Development Build with the Unity Profiler, then repeat key checks in a non-Development build. The sandbox could not execute native Unity rendering, physics, or audio.

1. Capture idle menu, initial run, busy star/power patterns, max pace, death/results, and repeated retry/return-home.
2. Check CPU Timeline and GC Alloc for steady running, UI repaint, physics, audio decode, and save spikes.
3. Use Memory Profiler/native memory snapshots before/after 50 scene changes. Persistent GameManager must not retain old canvases/listeners/particle textures.
4. Inspect Frame Debugger draw calls/overdraw for sprite textures, UI cards, and overlapping translucent effects.
5. Use a long soak run or debug invulnerability to cross many origin and sorting-order rebases. Look for tile gaps, decoration order jumps, missed triggers, camera shake reset issues, and float jitter.
6. Profile at least one lower-end supported Android phone, one ARM64 phone, and the desktop development target.
7. Capture median/p95 frame time and allocation rate with device/model, OS, build mode, editor/package versions, resolution, and effect/settings state. Do not publish unmeasured performance numbers.

Suggested target for a chosen 60 Hz baseline device is a 16.7 ms frame budget, with no avoidable per-frame managed allocation during steady gameplay. Treat that as an acceptance target, not a verified result.

## Next optimization candidates

Prioritize evidence over additional framework complexity:

- Add measured sprite atlases for world/UI assets after validating texture limits and packing in the actual editor.
- Reduce unnecessary legacy canvas/resources after the runtime UI's native acceptance tests.
- Review render settings (HDR/MSAA/shadows) for this unlit sprite game and profile a lightweight mobile quality tier.
- Add a tunable FX budget/quality preference if target-device captures show fill-rate pressure.
- Consider a ring-buffer/centralized recycler only if per-tile MonoBehaviour dispatch is a measured bottleneck.
- Add deterministic patterns/seeds for reproducible profiling and fairness testing.

Do not assume that a 64-bit build, a pool, or a newer engine automatically makes the game faster. Record comparisons on the same device and workload.
