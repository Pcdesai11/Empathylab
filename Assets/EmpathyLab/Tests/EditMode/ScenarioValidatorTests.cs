#nullable enable
using System.IO;
using EmpathyLab.Core;
using NUnit.Framework;

namespace EmpathyLab.Tests.EditMode
{
    public sealed class ScenarioValidatorTests
    {
        [Test]
        public void ShippedScenarioFilesHaveNoBrokenLinksOrUnreachableNodes()
        {
            var report = ScenarioValidator.ValidateDirectory(ScenarioTestFiles.DirectoryPath());

            Assert.AreEqual(0, report.ErrorCount, report.ToJson());
            Assert.IsTrue(report.Passed);
            Assert.GreaterOrEqual(Directory.GetFiles(ScenarioTestFiles.DirectoryPath(), "*.json").Length, 2);
        }

        [Test]
        public void BrokenLinkIsReported()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""broken"",
                ""title"": ""Broken"",
                ""initialTension"": 0.5,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    {
                        ""id"": ""a"",
                        ""speaker"": ""Avery"",
                        ""text"": ""Hello."",
                        ""choices"": [
                            { ""id"": ""go"", ""text"": ""Hi."", ""nextNodeId"": ""missing"", ""rationale"": ""A greeting."" },
                            { ""id"": ""stay"", ""text"": ""Okay."", ""nextNodeId"": ""b"", ""rationale"": ""A stop."" }
                        ]
                    },
                    { ""id"": ""b"", ""speaker"": ""Avery"", ""text"": ""Bye."", ""ending"": true, ""choices"": [] }
                ]
            }", "broken.json");

            Assert.IsFalse(report.Passed);
            AssertIssue(report, "broken-link", "a");
        }

        [Test]
        public void UnreachableNodeIsReported()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""island"",
                ""title"": ""Island"",
                ""initialTension"": 0.5,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    {
                        ""id"": ""a"",
                        ""speaker"": ""Rio"",
                        ""text"": ""Hello."",
                        ""choices"": [
                            { ""id"": ""one"", ""text"": ""Hi."", ""nextNodeId"": ""b"", ""rationale"": ""Opens."" },
                            { ""id"": ""two"", ""text"": ""Hey."", ""nextNodeId"": ""b"", ""rationale"": ""Also opens."" }
                        ]
                    },
                    { ""id"": ""b"", ""speaker"": ""Rio"", ""text"": ""Bye."", ""ending"": true, ""choices"": [] },
                    { ""id"": ""orphan"", ""speaker"": ""Rio"", ""text"": ""Nobody reaches me."", ""ending"": true, ""choices"": [] }
                ]
            }", "island.json");

            AssertIssue(report, "unreachable", "orphan");
        }

        [Test]
        public void MissingSpeakerTextIsReported()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""quiet"",
                ""title"": ""Quiet"",
                ""initialTension"": 0.4,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    { ""id"": ""a"", ""speaker"": ""  "", ""text"": ""Hello."", ""ending"": true, ""choices"": [] }
                ]
            }", "quiet.json");

            AssertIssue(report, "missing-speaker", "a");
        }

        [Test]
        public void DeadEndIsReported()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""stuck"",
                ""title"": ""Stuck"",
                ""initialTension"": 0.4,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    { ""id"": ""a"", ""speaker"": ""Avery"", ""text"": ""And then nothing."", ""ending"": false, ""choices"": [] }
                ]
            }", "stuck.json");

            AssertIssue(report, "dead-end", "a");
        }

        [Test]
        public void EmptyChoiceTextIsReported()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""blank"",
                ""title"": ""Blank"",
                ""initialTension"": 0.4,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    {
                        ""id"": ""a"",
                        ""speaker"": ""Avery"",
                        ""text"": ""Well?"",
                        ""choices"": [
                            { ""id"": ""empty"", ""text"": ""  "", ""nextNodeId"": ""b"", ""rationale"": ""Blank."" },
                            { ""id"": ""real"", ""text"": ""I'm listening."", ""nextNodeId"": ""b"", ""rationale"": ""Real."" }
                        ]
                    },
                    { ""id"": ""b"", ""speaker"": ""Avery"", ""text"": ""Okay."", ""ending"": true, ""choices"": [] }
                ]
            }", "blank.json");

            AssertIssue(report, "empty-choice", "a");
        }

        [Test]
        public void TensionOutOfRangeIsReported()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""hot"",
                ""title"": ""Hot"",
                ""initialTension"": 2.5,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    { ""id"": ""a"", ""speaker"": ""Rio"", ""text"": ""Too much."", ""ending"": true, ""choices"": [] }
                ]
            }", "hot.json");

            AssertIssue(report, "tension-out-of-range", string.Empty);
        }

        [Test]
        public void SingleChoiceIsAWarningAndDoesNotFailTheReport()
        {
            var report = ScenarioValidator.ValidateJson(@"{
                ""id"": ""rail"",
                ""title"": ""Rail"",
                ""initialTension"": 0.4,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""a"",
                ""nodes"": [
                    {
                        ""id"": ""a"",
                        ""speaker"": ""Rio"",
                        ""text"": ""Only one way."",
                        ""choices"": [
                            { ""id"": ""only"", ""text"": ""Okay."", ""nextNodeId"": ""b"", ""rationale"": ""Only path."" }
                        ]
                    },
                    { ""id"": ""b"", ""speaker"": ""Rio"", ""text"": ""Done."", ""ending"": true, ""choices"": [] }
                ]
            }", "rail.json");

            Assert.IsTrue(report.Passed);
            Assert.GreaterOrEqual(report.WarningCount, 1);
            AssertIssue(report, "single-choice", "a");
        }

        static void AssertIssue(ScenarioValidationReport report, string code, string nodeId)
        {
            foreach (var issue in report.Issues)
            {
                if (issue.Code == code && issue.NodeId == nodeId)
                    return;
            }

            Assert.Fail($"Missing issue {code} on '{nodeId}'. Report: {report.ToJson()}");
        }
    }
}
