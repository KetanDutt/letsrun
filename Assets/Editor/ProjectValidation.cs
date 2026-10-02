using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Fast pre-build checks, without opening/replacing any of the developer's scenes.</summary>
public sealed class ProjectValidation : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    [MenuItem("Tools/Let's Run/Validate Project")]
    public static void ValidateMenu()
    {
        List<string> errors = FindErrors();
        if (errors.Count == 0) Debug.Log("Let's Run: project contracts are valid. Complete the device checklist in docs/TESTING.md before release.");
        else Debug.LogError("Let's Run validation failed:\n- " + string.Join("\n- ", errors));
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        List<string> errors = FindErrors();
        if (report.summary.platform == BuildTarget.Android)
        {
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                errors.Add("Enable Android ARM64 in Player Settings.");
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
                errors.Add("Use IL2CPP for the Android ARM64 build.");
        }
        if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));
    }

    private static List<string> FindErrors()
    {
        var errors = new List<string>();
        var scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);
        if (scenes.Count == 0 || scenes[0] != "Assets/Scenes/MainMenu.unity")
            errors.Add("MainMenu must be the first enabled build scene.");
        if (!scenes.Contains("Assets/Scenes/Gameplay.unity")) errors.Add("Enable Gameplay in Build Settings.");
        foreach (string path in scenes) if (!File.Exists(path)) errors.Add("Missing scene: " + path);

        for (int i = 0; i < RunnerRules.HeroCount; i++)
            RequireAsset<Sprite>("Assets/Resources/Sprites/Player/hero" + i + "_big.png", errors);
        RequireAsset<Sprite>("Assets/Resources/Sprites/Player/trex.png", errors);
        RequireAsset<Sprite>("Assets/Resources/Sprites/UI/BG/Background.png", errors);
        RequireAsset<Sprite>("Assets/Resources/Sprites/UI/BG/Menu Scene 1.png", errors);
        RequireAsset<Font>("Assets/Resources/Fonts/LuckiestGuy.ttf", errors);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player Prefabs/Root Player.prefab");
        PlayerController player = prefab == null ? null : prefab.GetComponent<PlayerController>();
        if (player == null || player.player == null || player.shadow == null || player.explosion == null)
            errors.Add("Root Player prefab needs its controller, player, shadow, and explosion references.");
        else
        {
            Animator animator = player.player.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) errors.Add("Player needs the authored animation controller.");
            if (player.player.GetComponent<Collider2D>() == null || prefab.GetComponent<Rigidbody2D>() == null)
                errors.Add("Player needs its 2D collider and root Rigidbody2D.");
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { errors.Add("Could not import prefab: " + path); continue; }
            foreach (MonoBehaviour script in asset.GetComponentsInChildren<MonoBehaviour>(true))
                if (script == null) errors.Add("Missing script on prefab: " + path);
        }
        CheckSceneReference("Assets/Scenes/MainMenu.unity", "menuMusic", errors);
        CheckSceneReference("Assets/Scenes/MainMenu.unity", "uiClick", errors);
        CheckSceneReference("Assets/Scenes/MainMenu.unity", "purchaseSound", errors);
        CheckSceneReference("Assets/Scenes/Gameplay.unity", "score_Text", errors);
        CheckSceneReference("Assets/Scenes/Gameplay.unity", "star_Score_Text", errors);
        CheckSceneReference("Assets/Scenes/Gameplay.unity", "ui_Click", errors);
        CheckSceneReference("Assets/Scenes/Gameplay.unity", "crash_Clip", errors);
        return errors;
    }

    private static void RequireAsset<T>(string path, List<string> errors) where T : Object
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) == null) errors.Add("Missing or incorrectly imported asset: " + path);
    }

    private static void CheckSceneReference(string path, string field, List<string> errors)
    {
        if (!File.Exists(path)) return;
        if (!Regex.IsMatch(File.ReadAllText(path), @"(?m)^  " + field + @": \{fileID: (?!0\b)\d+"))
            errors.Add("Assign " + field + " in " + path);
    }
}
