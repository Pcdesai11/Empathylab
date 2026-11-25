#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EmpathyLab.EditorTools
{
    /// <summary>
    /// Batchmode entry point used by GameCI. It validates scenario files before it builds, and exits nonzero on failure.
    /// </summary>
    public static class BuildScript
    {
        /// <summary>
        /// Validates scenarios, then builds the enabled scenes.
        /// GameCI calls <c>EmpathyLab.EditorTools.BuildScript.Build</c>.
        /// </summary>
        public static void Build()
        {
            var code = Run();
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
            else if (code != 0)
                Debug.LogError("EmpathyLab build failed with code " + code + ".");
        }

        /// <summary>Menu wrapper so a developer can run the same path inside the editor.</summary>
        [MenuItem("EmpathyLab/Build Standalone Linux 64")]
        public static void BuildFromMenu() => Build();

        static int Run()
        {
            var report = ScenarioValidationExport.Write();
            Debug.Log("Scenario validation wrote " + ScenarioValidationExport.AbsolutePath()
                + " errors=" + report.ErrorCount + " warnings=" + report.WarningCount);
            if (!report.Passed)
            {
                foreach (var issue in report.Issues)
                {
                    if (issue.Severity == EmpathyLab.Core.ValidationSeverity.Error)
                        Debug.LogError(issue.Source + " " + issue.Code + ": " + issue.Message);
                }

                return 1;
            }

            var targetName = Arg("-customBuildTarget") ?? Arg("-buildTarget") ?? "StandaloneLinux64";
            if (!Enum.TryParse(targetName, true, out BuildTarget target))
            {
                Debug.LogError("Unknown build target '" + targetName + "'.");
                return 1;
            }

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("No enabled scenes in EditorBuildSettings.");
                return 1;
            }

            var location = ResolveLocation(target);
            var parent = Path.GetDirectoryName(location);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.None
            };

            Debug.Log("Building " + target + " to " + location);
            var build = BuildPipeline.BuildPlayer(options);
            if (build.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + location);
                return 0;
            }

            Debug.LogError("Build failed: " + build.summary.result);
            return 1;
        }

        /// <summary>
        /// GameCI v4 passes <c>-customBuildPath</c> as the player file, not a folder.
        /// On StandaloneLinux64 that file has no extension, so a "no dot means directory" check would nest the player.
        /// </summary>
        static string ResolveLocation(BuildTarget target)
        {
            var custom = Arg("-customBuildPath");
            if (!string.IsNullOrEmpty(custom))
                return custom;

            var product = string.IsNullOrWhiteSpace(PlayerSettings.productName)
                ? "EmpathyLabVR"
                : PlayerSettings.productName;
            return Path.Combine("build", target.ToString(), product);
        }

        static string? Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }

            return null;
        }
    }
}
