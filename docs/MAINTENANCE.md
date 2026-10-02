# Maintenance and repository hygiene

## Source versus generated data

Keep `Assets`, all their `.meta` files, `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings`, source tooling, tests, and docs in Git.

Do not add Unity `Library`, `Temp`, `Logs`, `UserSettings`, IDE state, generated root `.csproj`/`.sln` files, compiled players, or signing material. `.gitignore` now covers these paths while deliberately retaining the real `Tools/Tests/Runner.Core.Tests.csproj` source project.

This review removed the generated package cache and script assemblies, stale APK, generated IDE projects/user state, OS debris, and dormant IAP BillingMode resource. Source art, sounds, prefabs, animations, scenes, project settings, and LICENSE were retained. The title font was moved with its metadata rather than reimported under a new GUID.

## Remaining historical cache cleanup

The original repository tracked more than 17,000 files, mostly generated Unity data. Removing every cache in the same change would exceed this delivery's file-change budget. **8,985 historical generated entries remain tracked in Library** after the first cleanup phase. They are not runtime source, and a clean-source release should untrack them in a separate cleanup.

Preview safely from the repository root:

```sh
python3 Tools/cleanup_generated.py
```

To finish untracking them locally:

```sh
python3 Tools/cleanup_generated.py --apply
git status --short
```

The helper stages only known generated paths/root build/IDE artifacts and uses `git rm --cached`. It **does not delete local working files**, source assets, settings, or `.git`, and it does not rewrite history. Review the staged deletion before committing it. Unity can regenerate caches; already tracked paths are not fixed merely by adding `.gitignore`.

After committing source cleanup, a fresh clone still carries old binaries in Git history. History rewriting/LFS migration is a separate owner-coordinated operation and was not performed here. Never remove source or rewrite shared history just to reduce a checkout size.

## Asset and scene conventions

- Commit every new `.meta` with its asset/directory. Preserve the GUID on moves/renames.
- Do not hand-regenerate scene/prefab IDs or change clip event names without checking their references.
- Run `Tools/validate_project.py` after moving assets or changing package versions.
- Keep the uGUI external-GUID mapping synchronized if upgrading that package; do not broadly ignore unresolved GUIDs.
- New runtime UI is created by the controllers. The hidden legacy canvas can be retired only after native acceptance tests, preferably with an editor-assisted migration.
- Use a prefab/pattern for hazards/pickups and preserve tag/sorting-layer contracts. Avoid allocating new GameObjects inside steady gameplay loops.

## Packages and offline behavior

Keep manifest/lock changes together and let the actual editor confirm package resolution. Unused Ads/Analytics/IAP/Collab/TMP/Timeline/XR-helper/VSCode/Tilemap packages were removed; existing required sprite/uGUI/test and IDE support remain pinned. Built-in engine modules are not broadly pruned without a real import/build because editor/native dependencies need verification.

Online-service startup flags are disabled. If a new network/monetization feature is added later, it needs explicit initialization, failure behavior, policy/privacy review, and tests; do not silently add a package and assume it is unused.

## Code conventions

- Four-space C# indentation, braces on their own lines, LF endings; see `.editorconfig`/`.gitattributes`.
- Put testable rules/data in the engine-free core; inject serialization into IO rather than hiding it behind UI.
- Prefer direct/cached references. One-time compatibility lookup is acceptable; repeated `Find`/component-array creation inside Update is not.
- Treat run state as authoritative; do not restore timescale from animation callbacks.
- Guard duplicate singleton Awake work and reset static references for domain-reload-disabled editor sessions.
- Subscribe/unsubscribe persistent-service events symmetrically.
- Keep pool searches bounded and arithmetic/save inputs validated.
- Handle/report failures; only best-effort temporary cleanup may suppress expected IO cleanup exceptions.
- Do not upgrade engine/packages, rebalance economy, or claim performance/store certification without evidence.

## Adding tests or a feature

1. Add a rule-level regression to `CoreContractCases` when possible.
2. Run the standalone harness and asset validator.
3. Run EditMode with the real JsonUtility codec in Unity.
4. Add/re-run the relevant native acceptance cases, including UI/audio/lifecycle.
5. Update gameplay/schema/style/release docs and change log.

The test harness output, SDK binaries, screenshots, profiler captures, and build artifacts are validation products, not source. Keep them out of Git unless a small curated reference is explicitly needed.
