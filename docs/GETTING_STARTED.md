# Getting started

## Requirements

### To play or build the Unity game

- Unity Hub with **2020.3.26f1**, matching `ProjectSettings/ProjectVersion.txt`.
- A valid Unity license for your use case.
- Android Build Support, including the compatible SDK/NDK/OpenJDK, for Android builds; Linux Build Support for the provided Linux build command.
- Enough disk space for Unity's regenerated `Library/` cache.

The original editor version is intentionally retained to avoid an untested engine migration. Before public/store distribution, evaluate a supported Unity toolchain and current platform requirements; see [Release](RELEASE.md).

### To run engine-independent checks

- Python 3.10 or newer. The asset validator uses only the standard library.
- .NET 8 SDK. The console test harness has no NuGet test-framework dependencies.
- Git for the optional generated-file inventory/cleanup.

These tools are not required by players and do not change Unity's scripting runtime.

## First import

1. Add the **repository root** to Unity Hub.
2. If an old local cache exists from another editor version, close Unity and remove the local `Library` folder before importing. It is generated, not source. Do not remove `Assets`, `Packages`, or `ProjectSettings`.
3. Allow Package Manager to restore `Packages/manifest.json` and `packages-lock.json`.
4. Wait for asset import and compilation; resolve any Console errors before Play mode.
5. Open `Assets/Scenes/MainMenu.unity`.
6. Confirm **MainMenu → Gameplay** are the two enabled scenes in Build Settings.
7. Run **Tools → Let's Run → Validate Project**.
8. Press Play. A first-run help panel precedes the countdown.

`Gameplay` can also be opened directly: `GameManager.EnsureInstance()` supplies the persistent profile service without requiring the menu scene first.

## Where the UI comes from

`MainMenuController` and `GameplayController` create `RunnerUI` at startup. The original `UI Canvas` is retained but hidden, so existing serialized references and button callbacks are not broken. The new UI needs no manual scene setup.

The title font moved to `Assets/Resources/Fonts/LuckiestGuy.ttf` **with its original GUID**. Existing font references continue to resolve. New menu/click/purchase/crash audio references are already wired in the two scenes.

## Local checks

From the repository root:

```sh
python3 Tools/validate_project.py
dotnet run --project Tools/Tests/Runner.Core.Tests.csproj --configuration Release
```

In Unity, use **Window → General → Test Runner → EditMode → Run All**. The console harness checks the pure core with System.Text.Json; the EditMode suite checks the same contracts with Unity's field-based JsonUtility codec.

A generated-file warning is expected until the remaining historical cache cleanup is completed. It is a repository warning, not a failed gameplay contract.

## Development builds

Use the Editor menus under **Tools → Let's Run → Build**, or the batch entry points in [Release](RELEASE.md). Output is ignored under `Builds/`. Do not add APKs, AABs, build logs, or signing files to Git.

The default desktop view and mobile orientation are portrait. Test with a 720 × 1280 Game view and at least one notched/tall mobile aspect ratio. The safe-area fitter handles screen changes, but landscape/tablet certification is not claimed.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Missing scripts or compile errors on first import | Wait for package restoration; import with the pinned editor; delete stale local Library data if necessary. |
| Scene unavailable toast | Add both scenes to Build Settings in the documented order. |
| No menu audio | Check the three new MainMenuController clip references and music/SFX settings. |
| No new UI | Start Play mode, not just Scene view; confirm the scene controller is enabled. |
| Shop starts with zero stars | Intended for new profiles. The old 9,000-star debug grant was removed. |
| Save pending banner | Check writable storage and available space. See the Console and save guide; do not delete a valid backup. |
| Protected save banner | The profile version is newer than the app. Use the newer app rather than overwriting it. |
| Android ARM64 build fails | Install the matching Android/IL2CPP modules and review SDK/NDK compatibility. |
