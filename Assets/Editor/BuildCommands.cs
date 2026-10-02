using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Reproducible development builds. Release signing and store compliance are manual gates.</summary>
public static class BuildCommands
{
    [MenuItem("Tools/Let's Run/Build/Linux Development")]
    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64, "Builds/Linux/LetsRun.x86_64");
    }

    [MenuItem("Tools/Let's Run/Build/Android Development APK")]
    public static void BuildAndroid()
    {
        bool previousBundleSetting = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/LetsRun-development.apk");
        }
        finally { EditorUserBuildSettings.buildAppBundle = previousBundleSetting; }
    }

    private static void Build(BuildTarget target, string defaultPath)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
            throw new BuildFailedException("Install the Unity Hub build-support module for " + target + ".");
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled) scenes.Add(scene.path);
        string destination = Environment.GetEnvironmentVariable("LETSRUN_BUILD_PATH");
        if (string.IsNullOrEmpty(destination)) destination = defaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes.ToArray(), target = target, locationPathName = destination,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Build failed: " + report.summary.result);
        Debug.Log("Development build created: " + destination);
    }
}
