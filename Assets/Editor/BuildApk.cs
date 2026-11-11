using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildApk
{
    [MenuItem("Tools/Zombie VR/Build APK")]
    public static void Build()
    {
        string[] scenes = new[] { "Assets/Scenes/Zombie.unity" };
        string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
        Directory.CreateDirectory(outDir);
        string outPath = Path.Combine(outDir, "ZombieVR.apk");
        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("BUILD SUCCEEDED: " + outPath);
        else
            Debug.LogError("BUILD FAILED: " + report.summary.result);
    }
}
