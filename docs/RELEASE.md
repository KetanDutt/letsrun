# Builds and release readiness

## Current status

This is a production-oriented **release candidate**, not a certified production/store build. The project remains pinned to Unity 2020.3.26f1. No signed APK/AAB, native play-mode execution, device profiling, or store compliance certification was produced in the sandbox.

The checked-in legacy APK was removed: it was an outdated compiled artifact, not a build of the revised source. Build outputs belong in `Builds/`/release artifact storage, not Git.

## Required configuration

- Build scenes: `Assets/Scenes/MainMenu.unity`, then `Assets/Scenes/Gameplay.unity`.
- Desktop default: portrait 720 × 1280.
- Android orientation: portrait; minimum API configured as 23.
- Android backend: IL2CPP; architectures: ARMv7 + ARM64.
- Source version: 1.1.0; Android build code: 2.
- Package versions: pinned by manifest and lock; unused Ads/Analytics/IAP/Collab packages removed.
- No new remote runtime service or monetization integration.

Company/product/application identifiers remain unchanged to avoid silently relocating old saves or breaking existing installs. They must be reviewed for branding/ownership before a public release. If changing identity, plan an explicit progress-migration strategy rather than changing the bundle ID casually.

## Editor build validation

**Tools → Let's Run → Validate Project** checks required resources, player references, enabled scenes, prefab missing scripts, and new clip/HUD bindings without replacing the developer's open scenes.

`ProjectValidation.OnPreprocessBuild` runs automatically, blocking broken scene/resource contracts and Android builds missing ARM64/IL2CPP. It is a project-integrity gate, **not** a complete platform compliance validator.

## Development builds

Editor menus:

- **Tools → Let's Run → Build → Linux Development**
- **Tools → Let's Run → Build → Android Development APK**

The matching modules must be installed. These builds enable Development/Allow Debugging and are not store-release output.

Batch examples:

```sh
Unity -batchmode -quit -projectPath "$PWD" \
  -executeMethod BuildCommands.BuildLinux \
  -logFile Artifacts/build-linux.log

Unity -batchmode -quit -projectPath "$PWD" \
  -executeMethod BuildCommands.BuildAndroid \
  -logFile Artifacts/build-android.log
```

Use the actual editor executable path for your OS. `LETSRUN_BUILD_PATH` can override the default output path. Failures throw `BuildFailedException` so automation can detect them.

## Public-release gates

### Engine/toolchain

- [ ] Import/compile with the pinned editor and run the real JsonUtility EditMode suite.
- [ ] Complete native acceptance tests in [Testing](TESTING.md).
- [ ] Evaluate a supported Unity editor/package/toolchain migration in a separate reviewed step.
- [ ] Verify current store target-API, architecture, native-library/page-size (including 16 KiB devices), SDK/NDK, and platform policy requirements using the actual release toolchain. ARM64 alone does not establish compliance.
- [ ] Validate Android API 23 support if keeping that minimum; test lower-end and modern devices rather than assuming compatibility.
- [ ] Profile a Release/non-Development player and record results; verify cold startup, resume, audio latency, save durability, and long-run stability.

### Identity, licensing, and privacy

- [ ] Confirm authority to distribute this all-rights-reserved project under [LICENSE](../LICENSE).
- [ ] Audit the provenance/redistribution rights of original sprites, music/SFX, LuckiestGuy, and HelveticaNeue. Retaining an asset does not prove its license.
- [ ] Review original app/company IDs, icon/splash/store art, version/build numbers, contact details, and platform branding.
- [ ] Confirm the offline build performs no unexpected service initialization/network access and request only necessary permissions.
- [ ] Prepare accurate privacy/store disclosures. This revision adds no accounts, ads, analytics, cloud sync, or real-money purchases.

### Packaging/signing

- [ ] Disable Development Build, script debugging, and profiler attachment for release output.
- [ ] Configure an appropriate Android AAB/signed player or desktop package in Build Settings.
- [ ] Use secured local/CI signing material; never add keystores, passwords, service credentials, or certificates to the repository/logs.
- [ ] Test install/update/uninstall behavior and profile preservation on physical hardware.
- [ ] Test legacy import using copied data from an original installation.
- [ ] Finish remaining generated-file untracking and review clean-source CI.
- [ ] Generate accurate screenshots from the actual revised game; do not reuse the README's removed old external screenshots as proof of current UI.

## Release procedure

1. Review source/asset diff and version/configuration changes.
2. Run asset checks, core harness, real EditMode tests, Editor validation, and native device acceptance.
3. Resolve all blockers above, with recorded editor/device evidence.
4. Build with the reviewed release toolchain, signing configuration, and non-Development settings.
5. Install/test the exact output artifact, not only an editor session.
6. Record artifact hashes, version, target architecture, toolchain, test devices/results, and known issues.
7. Publish the binary through release/artifact storage under the correct license, not as a tracked root APK.

A future licensed Unity CI build can be added after toolchain and signing ownership are confirmed. The included GitHub workflow intentionally uses no Unity/signing secrets and only validates the pure source/asset contracts.
