#nullable enable
using System;
using System.IO;
using EmpathyLab.Core;
using NUnit.Framework;

namespace EmpathyLab.Tests.EditMode
{
    public sealed class DialogueGraphTests
    {
        [Test]
        public void StartNodeResolves()
        {
            var graph = new DialogueGraph(LoadFrontCounter());

            Assert.IsTrue(graph.TryGetNode(graph.StartNodeId, out var node));
            Assert.AreEqual("arrival", node!.Id);
            Assert.AreEqual("Avery", node.Speaker);
        }

        [Test]
        public void UnknownNodeIdReturnsFalse()
        {
            var graph = new DialogueGraph(LoadFrontCounter());

            Assert.IsFalse(graph.TryGetNode("does-not-exist", out _));
            Assert.IsFalse(graph.TryGetNode(string.Empty, out _));
            Assert.IsFalse(graph.TryGetNode(null, out _));
        }

        [Test]
        public void EmptyChoiceListIsAllowedOnAnEndingNode()
        {
            var scenario = ScenarioLoader.Load(@"{
                ""id"": ""ending-only"",
                ""title"": ""Ending"",
                ""initialTension"": 0.2,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""done"",
                ""nodes"": [
                    { ""id"": ""done"", ""speaker"": ""Avery"", ""text"": ""Okay."", ""ending"": true, ""choices"": [] }
                ]
            }");

            var node = scenario.Nodes[0];
            Assert.IsTrue(node.IsEnding);
            Assert.AreEqual(0, node.Choices.Count);
        }

        [Test]
        public void DuplicateNodeIdThrows()
        {
            const string json = @"{
                ""id"": ""dup"",
                ""title"": ""Dup"",
                ""startNodeId"": ""a"",
                ""nodes"": [
                    { ""id"": ""a"", ""speaker"": ""A"", ""text"": ""Hi"", ""ending"": true, ""choices"": [] },
                    { ""id"": ""a"", ""speaker"": ""A"", ""text"": ""Again"", ""ending"": true, ""choices"": [] }
                ]
            }";

            var error = Assert.Throws<ScenarioLoadException>(() => ScenarioLoader.Load(json));
            StringAssert.Contains("Duplicate node id", error!.Message);
        }

        [Test]
        public void EmptyJsonThrows()
        {
            var error = Assert.Throws<ScenarioLoadException>(() => ScenarioLoader.Load("   "));
            StringAssert.Contains("empty", error!.Message);
        }

        [Test]
        public void ChoiceRoundTripKeepsTheNextNodeAndTheScores()
        {
            var scenario = LoadFrontCounter();
            var graph = new DialogueGraph(scenario);
            graph.TryGetNode("arrival", out var arrival);
            var choice = arrival!.Choices[0];

            Assert.AreEqual("named", choice.NextNodeId);
            Assert.AreEqual(22, choice.Score.Empathy);
            Assert.Less(choice.Mood.Tension, 0f);
            StringAssert.Contains("broken promise", choice.Rationale);
        }

        static DialogueScenario LoadFrontCounter()
        {
            var path = Path.Combine(ScenarioTestFiles.DirectoryPath(), "front-counter.json");
            return ScenarioLoader.Load(File.ReadAllText(path));
        }
    }

    public sealed class ScenarioSessionTests
    {
        [Test]
        public void ThoughtfulPathCompletesAndTheDebriefExplainsEachChoice()
        {
            var session = new ScenarioSession(Load("front-counter.json"));

            session.Choose("name-the-miss");
            session.Choose("concrete-plan");
            session.Choose("confirm");

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual("close-steady", session.Current.Id);
            Assert.AreEqual(MoodBand.Calm, session.Mood.Band);

            var debrief = session.BuildDebrief();
            Assert.AreEqual(3, debrief.Choices.Count);
            Assert.AreEqual(78, debrief.Empathy);
            Assert.AreEqual(82, debrief.Clarity);
            Assert.AreEqual(82, debrief.Deescalation);
            StringAssert.Contains("broken promise", debrief.Choices[0].Rationale);
            StringAssert.Contains("specific next step", debrief.Choices[1].Rationale);
        }

        [Test]
        public void UnknownChoiceThrows()
        {
            var session = new ScenarioSession(Load("front-counter.json"));

            var error = Assert.Throws<ArgumentException>(() => session.Choose("not-a-choice"));
            StringAssert.Contains("not on node", error!.Message);
            Assert.IsFalse(session.IsComplete);
        }

        [Test]
        public void EmptyChoiceIdThrows()
        {
            var session = new ScenarioSession(Load("shared-wall.json"));

            Assert.Throws<ArgumentException>(() => session.Choose("  "));
        }

        [Test]
        public void DebriefBeforeTheEndThrows()
        {
            var session = new ScenarioSession(Load("shared-wall.json"));

            Assert.Throws<InvalidOperationException>(() => session.BuildDebrief());
        }

        [Test]
        public void ChoosingAfterTheEndThrows()
        {
            var session = new ScenarioSession(EndingScenario());

            Assert.Throws<InvalidOperationException>(() => session.Choose("anything"));
        }

        [Test]
        public void EndingWithNoChoicesStartsCompleteAndKeepsTheBaseline()
        {
            var session = new ScenarioSession(EndingScenario());

            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(0, session.History.Count);
            Assert.AreEqual(40, session.Scoring.Empathy);
            var debrief = session.BuildDebrief();
            Assert.AreEqual(0, debrief.Choices.Count);
            Assert.AreEqual(40, debrief.Clarity);
        }

        static DialogueScenario Load(string fileName)
        {
            var path = Path.Combine(ScenarioTestFiles.DirectoryPath(), fileName);
            return ScenarioLoader.Load(File.ReadAllText(path));
        }

        static DialogueScenario EndingScenario()
        {
            return ScenarioLoader.Load(@"{
                ""id"": ""done"",
                ""title"": ""Done"",
                ""initialTension"": 0.1,
                ""scoreBaseline"": 40,
                ""startNodeId"": ""bye"",
                ""nodes"": [
                    { ""id"": ""bye"", ""speaker"": ""Rio"", ""text"": ""Good night."", ""ending"": true, ""choices"": [] }
                ]
            }");
        }
    }
}
