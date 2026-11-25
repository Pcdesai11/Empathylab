#nullable enable
using EmpathyLab.Core;
using UnityEditor;
using UnityEngine;

namespace EmpathyLab.EditorTools
{
    /// <summary>
    /// Editor window that checks scenario files for dead ends, missing speaker text, and unreachable nodes.
    /// </summary>
    public sealed class ScenarioValidatorWindow : EditorWindow
    {
        ScenarioValidationReport? _report;
        Vector2 _scroll;

        /// <summary>Opens the validator window.</summary>
        [MenuItem("EmpathyLab/Scenario Validator")]
        public static void Open()
        {
            var window = GetWindow<ScenarioValidatorWindow>("Scenario Validator");
            window.minSize = new Vector2(460f, 280f);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Scenario files", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ScenarioValidationExport.ScenarioDirectory(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Validate"))
                _report = ScenarioValidator.ValidateDirectory(ScenarioValidationExport.ScenarioDirectory());

            if (GUILayout.Button("Validate and write JSON report"))
            {
                _report = ScenarioValidationExport.Write();
                Debug.Log("Wrote " + ScenarioValidationExport.AbsolutePath());
            }

            if (_report == null)
            {
                EditorGUILayout.HelpBox("Run Validate to check the scenario files.", MessageType.Info);
                return;
            }

            var summary = _report.Passed
                ? _report.ErrorCount + " errors, " + _report.WarningCount + " warnings."
                : _report.ErrorCount + " errors, " + _report.WarningCount + " warnings. Errors fail the build.";
            EditorGUILayout.HelpBox(summary, _report.Passed ? MessageType.Info : MessageType.Error);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var issue in _report.Issues)
            {
                var prefix = issue.Severity == ValidationSeverity.Error ? "Error" : "Warning";
                EditorGUILayout.LabelField(prefix + "  " + issue.Code, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(issue.Source + "  " + issue.NodeId + "  " + issue.Message, EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
