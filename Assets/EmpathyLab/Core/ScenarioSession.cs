#nullable enable
using System;
using System.Collections.Generic;

namespace EmpathyLab.Core
{
    /// <summary>One chosen line, kept so the debrief can explain it.</summary>
    public sealed class ChoiceRecord
    {
        /// <summary>Creates a record of a choice after the mood and scores have moved.</summary>
        public ChoiceRecord(
            string choiceId,
            string playerText,
            string rationale,
            float tensionBefore,
            float tensionAfter,
            int empathyDelta,
            int clarityDelta,
            int deescalationDelta)
        {
            ChoiceId = choiceId;
            PlayerText = playerText;
            Rationale = rationale;
            TensionBefore = tensionBefore;
            TensionAfter = tensionAfter;
            EmpathyDelta = empathyDelta;
            ClarityDelta = clarityDelta;
            DeescalationDelta = deescalationDelta;
        }

        /// <summary>Id of the choice inside its node.</summary>
        public string ChoiceId { get; }

        /// <summary>Words the player said.</summary>
        public string PlayerText { get; }

        /// <summary>Author's explanation of why the line helped or hurt.</summary>
        public string Rationale { get; }

        /// <summary>Tension before the line.</summary>
        public float TensionBefore { get; }

        /// <summary>Tension after the line.</summary>
        public float TensionAfter { get; }

        /// <summary>Actual empathy change after clamping.</summary>
        public int EmpathyDelta { get; }

        /// <summary>Actual clarity change after clamping.</summary>
        public int ClarityDelta { get; }

        /// <summary>Actual de-escalation change after clamping.</summary>
        public int DeescalationDelta { get; }
    }

    /// <summary>End-of-scene scores and the reasons behind them.</summary>
    public sealed class DebriefReport
    {
        /// <summary>Creates a debrief from a finished session.</summary>
        public DebriefReport(
            DialogueScenario scenario,
            int empathy,
            int clarity,
            int deescalation,
            MoodBand finalMood,
            float finalTension,
            IReadOnlyList<ChoiceRecord> choices)
        {
            ScenarioTitle = scenario.Title;
            ScenarioId = scenario.Id;
            NpcName = scenario.NpcName;
            Empathy = empathy;
            Clarity = clarity;
            Deescalation = deescalation;
            FinalMood = finalMood;
            FinalTension = finalTension;
            Choices = choices;
        }

        /// <summary>Scenario id.</summary>
        public string ScenarioId { get; }

        /// <summary>Scenario title.</summary>
        public string ScenarioTitle { get; }

        /// <summary>Name of the person the player spoke with.</summary>
        public string NpcName { get; }

        /// <summary>Final empathy score, 0–100.</summary>
        public int Empathy { get; }

        /// <summary>Final clarity score, 0–100.</summary>
        public int Clarity { get; }

        /// <summary>Final de-escalation score, 0–100.</summary>
        public int Deescalation { get; }

        /// <summary>Mood band after the last line.</summary>
        public MoodBand FinalMood { get; }

        /// <summary>Tension after the last line.</summary>
        public float FinalTension { get; }

        /// <summary>Lines the player chose, in order.</summary>
        public IReadOnlyList<ChoiceRecord> Choices { get; }
    }

    /// <summary>Plays one scenario from the start node to an ending.</summary>
    public interface IScenarioSession
    {
        /// <summary>Scenario being played.</summary>
        DialogueScenario Scenario { get; }

        /// <summary>Node currently on screen.</summary>
        DialogueNode Current { get; }

        /// <summary>Live tension.</summary>
        IMoodModel Mood { get; }

        /// <summary>Live scores.</summary>
        IScoringEngine Scoring { get; }

        /// <summary>True once an ending has been reached.</summary>
        bool IsComplete { get; }

        /// <summary>Choices made so far.</summary>
        IReadOnlyList<ChoiceRecord> History { get; }

        /// <summary>Selects a choice on the current node.</summary>
        /// <param name="choiceId">Id of the choice.</param>
        void Choose(string choiceId);

        /// <summary>Builds the debrief. Throws if the scene is still in progress.</summary>
        DebriefReport BuildDebrief();
    }

    /// <summary>
    /// Applies a choice to the mood model and the scoring engine, then walks the graph.
    /// An empty choice list ends the scene so a missing button cannot trap the player.
    /// </summary>
    public sealed class ScenarioSession : IScenarioSession
    {
        readonly DialogueGraph _graph;
        readonly List<ChoiceRecord> _history = new List<ChoiceRecord>();

        /// <summary>Starts at the scenario's start node.</summary>
        /// <param name="scenario">Loaded scenario.</param>
        public ScenarioSession(DialogueScenario scenario)
            : this(new DialogueGraph(scenario))
        {
        }

        /// <summary>Starts at the graph's start node.</summary>
        /// <param name="graph">Indexed scenario.</param>
        public ScenarioSession(DialogueGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (!_graph.TryGetNode(_graph.StartNodeId, out var start) || start == null)
                throw new ScenarioLoadException($"Start node '{_graph.StartNodeId}' does not exist.");

            Current = start;
            Mood = new MoodModel(graph.Scenario.InitialTension);
            Scoring = new ScoringEngine(graph.Scenario.ScoreBaseline);
            IsComplete = IsTerminal(start);
        }

        /// <inheritdoc />
        public DialogueScenario Scenario => _graph.Scenario;

        /// <inheritdoc />
        public DialogueNode Current { get; private set; }

        /// <inheritdoc />
        public IMoodModel Mood { get; }

        /// <inheritdoc />
        public IScoringEngine Scoring { get; }

        /// <inheritdoc />
        public bool IsComplete { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<ChoiceRecord> History => _history;

        /// <inheritdoc />
        public void Choose(string choiceId)
        {
            if (IsComplete)
                throw new InvalidOperationException("The scenario is already complete.");
            if (string.IsNullOrWhiteSpace(choiceId))
                throw new ArgumentException("Choice id is empty.", nameof(choiceId));

            DialogueChoice? selected = null;
            foreach (var choice in Current.Choices)
            {
                if (choice.Id == choiceId)
                {
                    selected = choice;
                    break;
                }
            }

            if (selected == null)
                throw new ArgumentException($"Choice '{choiceId}' is not on node '{Current.Id}'.", nameof(choiceId));

            var tensionBefore = Mood.Tension;
            var empathyBefore = Scoring.Empathy;
            var clarityBefore = Scoring.Clarity;
            var deescalationBefore = Scoring.Deescalation;
            Mood.Apply(selected.Mood);
            Scoring.Record(selected.Score);

            if (!string.IsNullOrEmpty(selected.NextNodeId))
            {
                if (!_graph.TryGetNode(selected.NextNodeId, out var next) || next == null)
                    throw new InvalidOperationException($"Choice '{selected.Id}' points at missing node '{selected.NextNodeId}'.");
                Current = next;
            }

            _history.Add(new ChoiceRecord(
                selected.Id,
                selected.Text,
                selected.Rationale,
                tensionBefore,
                Mood.Tension,
                Scoring.Empathy - empathyBefore,
                Scoring.Clarity - clarityBefore,
                Scoring.Deescalation - deescalationBefore));

            if (string.IsNullOrEmpty(selected.NextNodeId) || IsTerminal(Current))
                IsComplete = true;
        }

        /// <inheritdoc />
        public DebriefReport BuildDebrief()
        {
            if (!IsComplete)
                throw new InvalidOperationException("The debrief is available after the scenario ends.");

            return new DebriefReport(
                _graph.Scenario,
                Scoring.Empathy,
                Scoring.Clarity,
                Scoring.Deescalation,
                Mood.Band,
                Mood.Tension,
                _history);
        }

        static bool IsTerminal(DialogueNode node) => node.IsEnding || node.Choices.Count == 0;
    }
}
