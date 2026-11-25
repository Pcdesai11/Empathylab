#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EmpathyLab.Core
{
    /// <summary>How serious a validation finding is. Errors fail the build. Warnings do not.</summary>
    public enum ValidationSeverity
    {
        /// <summary>The file is safe to ship, but an author should look at it.</summary>
        Warning = 0,

        /// <summary>The file must not ship.</summary>
        Error = 1
    }

    /// <summary>One finding from the scenario validator.</summary>
    public sealed class ValidationIssue
    {
        /// <summary>Creates a finding.</summary>
        public ValidationIssue(ValidationSeverity severity, string code, string source, string nodeId, string message)
        {
            Severity = severity;
            Code = code;
            Source = source;
            NodeId = nodeId ?? string.Empty;
            Message = message;
        }

        /// <summary>Warning or error.</summary>
        public ValidationSeverity Severity { get; }

        /// <summary>Stable code, such as <c>dead-end</c> or <c>unreachable</c>.</summary>
        public string Code { get; }

        /// <summary>File name or other label for the document.</summary>
        public string Source { get; }

        /// <summary>Node id when the finding is about one node.</summary>
        public string NodeId { get; }

        /// <summary>Sentence a person can act on.</summary>
        public string Message { get; }
    }

    /// <summary>Every finding from one validation pass.</summary>
    public sealed class ScenarioValidationReport
    {
        readonly List<ValidationIssue> _issues;

        /// <summary>Creates a report.</summary>
        /// <param name="issues">Findings. The report copies the list.</param>
        public ScenarioValidationReport(IEnumerable<ValidationIssue> issues)
        {
            _issues = new List<ValidationIssue>(issues);
        }

        /// <summary>All findings, errors first in the order they were recorded.</summary>
        public IReadOnlyList<ValidationIssue> Issues => _issues;

        /// <summary>Number of errors.</summary>
        public int ErrorCount
        {
            get
            {
                var count = 0;
                foreach (var issue in _issues)
                {
                    if (issue.Severity == ValidationSeverity.Error)
                        count++;
                }

                return count;
            }
        }

        /// <summary>Number of warnings.</summary>
        public int WarningCount
        {
            get
            {
                var count = 0;
                foreach (var issue in _issues)
                {
                    if (issue.Severity == ValidationSeverity.Warning)
                        count++;
                }

                return count;
            }
        }

        /// <summary>True when nothing will fail the build.</summary>
        public bool Passed => ErrorCount == 0;

        /// <summary>Serializes the report for the editor window, the build, and CI.</summary>
        public string ToJson()
        {
            var payload = new JsonReport
            {
                Passed = Passed,
                ErrorCount = ErrorCount,
                WarningCount = WarningCount,
                Issues = new List<JsonIssue>(_issues.Count)
            };

            foreach (var issue in _issues)
            {
                payload.Issues.Add(new JsonIssue
                {
                    Severity = issue.Severity == ValidationSeverity.Error ? "error" : "warning",
                    Code = issue.Code,
                    Source = issue.Source,
                    NodeId = issue.NodeId,
                    Message = issue.Message
                });
            }

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }

        sealed class JsonReport
        {
            public bool Passed { get; set; }
            public int ErrorCount { get; set; }
            public int WarningCount { get; set; }
            public List<JsonIssue> Issues { get; set; } = new List<JsonIssue>();
        }

        sealed class JsonIssue
        {
            public string Severity { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string NodeId { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Checks scenario files for dead ends, missing speaker text, broken links, and unreachable nodes.
    /// The editor window and the batchmode build both call this so they cannot drift apart.
    /// </summary>
    public static class ScenarioValidator
    {
        /// <summary>Longest NPC line before a readability warning. World-space text past this is hard to read in VR.</summary>
        public const int LineLengthWarning = 280;

        /// <summary>Longest choice label before a readability warning.</summary>
        public const int ChoiceLengthWarning = 140;

        /// <summary>Validates one JSON document.</summary>
        /// <param name="json">Scenario JSON.</param>
        /// <param name="sourceName">Label stored on each issue, usually the file name.</param>
        public static ScenarioValidationReport ValidateJson(string json, string sourceName)
        {
            DialogueScenario scenario;
            try
            {
                scenario = ScenarioLoader.Load(json);
            }
            catch (ScenarioLoadException ex)
            {
                return new ScenarioValidationReport(new[]
                {
                    new ValidationIssue(ValidationSeverity.Error, "unreadable", sourceName, string.Empty, ex.Message)
                });
            }

            return ValidateScenario(scenario, sourceName);
        }

        /// <summary>Validates a scenario that is already loaded.</summary>
        /// <param name="scenario">Scenario to check.</param>
        /// <param name="sourceName">Label stored on each issue.</param>
        public static ScenarioValidationReport ValidateScenario(DialogueScenario scenario, string sourceName)
        {
            var issues = new List<ValidationIssue>();
            var graph = new DialogueGraph(scenario);

            if (!MoodModel.IsInRange(scenario.InitialTension))
            {
                issues.Add(Error(sourceName, string.Empty, "tension-out-of-range",
                    $"Initial tension {scenario.InitialTension} is outside 0–1."));
            }

            if (scenario.ScoreBaseline < ScoringEngine.Min || scenario.ScoreBaseline > ScoringEngine.Max)
            {
                issues.Add(Error(sourceName, string.Empty, "baseline-out-of-range",
                    $"Score baseline {scenario.ScoreBaseline} is outside 0–100."));
            }

            var reachable = Reachable(graph);
            foreach (var node in scenario.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Speaker))
                {
                    issues.Add(Error(sourceName, node.Id, "missing-speaker",
                        $"Node '{node.Id}' has no speaker text."));
                }

                if (string.IsNullOrWhiteSpace(node.Text))
                {
                    issues.Add(Error(sourceName, node.Id, "missing-line",
                        $"Node '{node.Id}' has no spoken line."));
                }
                else if (node.Text.Length > LineLengthWarning)
                {
                    issues.Add(Warning(sourceName, node.Id, "long-line",
                        $"Node '{node.Id}' is {node.Text.Length} characters. Keep VR lines under {LineLengthWarning}."));
                }

                if (!node.IsEnding && node.Choices.Count == 0)
                {
                    issues.Add(Error(sourceName, node.Id, "dead-end",
                        $"Node '{node.Id}' has no choices and is not marked as an ending."));
                }

                if (node.IsEnding && node.Choices.Count > 0)
                {
                    issues.Add(Warning(sourceName, node.Id, "ending-has-choices",
                        $"Node '{node.Id}' is an ending and also has choices. The session will stop here."));
                }

                if (!node.IsEnding && node.Choices.Count == 1)
                {
                    issues.Add(Warning(sourceName, node.Id, "single-choice",
                        $"Node '{node.Id}' has only one choice, so the player cannot steer the conversation."));
                }

                if (!reachable.Contains(node.Id))
                {
                    issues.Add(Error(sourceName, node.Id, "unreachable",
                        $"Node '{node.Id}' cannot be reached from '{scenario.StartNodeId}'."));
                }

                foreach (var choice in node.Choices)
                {
                    if (string.IsNullOrWhiteSpace(choice.Text))
                    {
                        issues.Add(Error(sourceName, node.Id, "empty-choice",
                            $"Choice '{choice.Id}' on node '{node.Id}' has no player text."));
                    }
                    else if (choice.Text.Length > ChoiceLengthWarning)
                    {
                        issues.Add(Warning(sourceName, node.Id, "long-choice",
                            $"Choice '{choice.Id}' is {choice.Text.Length} characters. Keep labels under {ChoiceLengthWarning}."));
                    }

                    if (string.IsNullOrWhiteSpace(choice.Rationale))
                    {
                        issues.Add(Warning(sourceName, node.Id, "missing-rationale",
                            $"Choice '{choice.Id}' has no rationale, so the debrief cannot explain it."));
                    }

                    if (choice.NextNodeId != null && !graph.TryGetNode(choice.NextNodeId, out _))
                    {
                        issues.Add(Error(sourceName, node.Id, "broken-link",
                            $"Choice '{choice.Id}' on node '{node.Id}' points at missing node '{choice.NextNodeId}'."));
                    }
                }
            }

            return new ScenarioValidationReport(issues);
        }

        /// <summary>Validates every <c>*.json</c> file in a directory, plus duplicate scenario ids across files.</summary>
        /// <param name="directory">Folder of scenario files.</param>
        public static ScenarioValidationReport ValidateDirectory(string directory)
        {
            var issues = new List<ValidationIssue>();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                issues.Add(Error(directory ?? string.Empty, string.Empty, "missing-directory",
                    "Scenario directory was not found."));
                return new ScenarioValidationReport(issues);
            }

            var files = Directory.GetFiles(directory, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            if (files.Length == 0)
            {
                issues.Add(Error(directory, string.Empty, "no-scenarios", "No scenario JSON files were found."));
                return new ScenarioValidationReport(issues);
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var json = File.ReadAllText(file);
                DialogueScenario? scenario = null;
                try
                {
                    scenario = ScenarioLoader.Load(json);
                }
                catch (ScenarioLoadException ex)
                {
                    issues.Add(Error(name, string.Empty, "unreadable", ex.Message));
                }

                if (scenario == null)
                    continue;

                if (!seenIds.Add(scenario.Id))
                {
                    issues.Add(Error(name, string.Empty, "duplicate-scenario",
                        $"Scenario id '{scenario.Id}' is already used by another file."));
                }

                foreach (var issue in ValidateScenario(scenario, name).Issues)
                    issues.Add(issue);
            }

            return new ScenarioValidationReport(issues);
        }

        static HashSet<string> Reachable(DialogueGraph graph)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(graph.StartNodeId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!seen.Add(id))
                    continue;
                if (!graph.TryGetNode(id, out var node) || node == null)
                    continue;
                foreach (var choice in node.Choices)
                {
                    if (!string.IsNullOrEmpty(choice.NextNodeId))
                        queue.Enqueue(choice.NextNodeId);
                }
            }

            return seen;
        }

        static ValidationIssue Error(string source, string nodeId, string code, string message) =>
            new ValidationIssue(ValidationSeverity.Error, code, source, nodeId, message);

        static ValidationIssue Warning(string source, string nodeId, string code, string message) =>
            new ValidationIssue(ValidationSeverity.Warning, code, source, nodeId, message);
    }
}
