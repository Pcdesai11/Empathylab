#nullable enable
using System.IO;
using EmpathyLab.Core;
using UnityEngine;

namespace EmpathyLab.EditorTools
{
    /// <summary>
    /// Writes the same validation report the editor window and the batchmode build both use.
    /// </summary>
    public static class ScenarioValidationExport
    {
        /// <summary>Project-relative path of the JSON report.</summary>
        public const string RelativePath = "Logs/scenario-validation.json";

        /// <summary>Validates every scenario file and writes <see cref="RelativePath"/>.</summary>
        public static ScenarioValidationReport Write()
        {
            var report = ScenarioValidator.ValidateDirectory(ScenarioDirectory());
            var path = AbsolutePath();
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            File.WriteAllText(path, report.ToJson());
            return report;
        }

        /// <summary>Absolute path of the report, next to the Assets folder.</summary>
        public static string AbsolutePath()
        {
            var root = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(root, "Logs", "scenario-validation.json");
        }

        /// <summary>Folder of shipped scenario JSON files.</summary>
        public static string ScenarioDirectory()
        {
            return Path.Combine(Application.dataPath, "StreamingAssets", "Scenarios");
        }
    }
}
