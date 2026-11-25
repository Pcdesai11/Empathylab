#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EmpathyLab.Core
{
    /// <summary>Thrown when scenario JSON cannot be turned into a graph.</summary>
    public sealed class ScenarioLoadException : Exception
    {
        /// <summary>Creates an exception with a message an author or a test can assert on.</summary>
        /// <param name="message">What was wrong with the file.</param>
        public ScenarioLoadException(string message) : base(message)
        {
        }
    }

    /// <summary>One line the player can say, and the effect that line has.</summary>
    public sealed class DialogueChoice
    {
        /// <summary>Creates a choice. A blank next id means the line ends the conversation.</summary>
        public DialogueChoice(
            string id,
            string text,
            string? nextNodeId,
            MoodDelta mood,
            ScoreDelta score,
            string rationale)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Text = text ?? string.Empty;
            NextNodeId = string.IsNullOrWhiteSpace(nextNodeId) ? null : nextNodeId;
            Mood = mood;
            Score = score;
            Rationale = rationale ?? string.Empty;
        }

        /// <summary>Stable id inside the parent node.</summary>
        public string Id { get; }

        /// <summary>Words the player says.</summary>
        public string Text { get; }

        /// <summary>Node to open next, or null when this line ends the scene.</summary>
        public string? NextNodeId { get; }

        /// <summary>How this line moves tension.</summary>
        public MoodDelta Mood { get; }

        /// <summary>How this line moves the three scores.</summary>
        public ScoreDelta Score { get; }

        /// <summary>Why this line helped or hurt. Shown on the debrief.</summary>
        public string Rationale { get; }
    }

    /// <summary>One beat of the conversation.</summary>
    public sealed class DialogueNode
    {
        /// <summary>Creates a node.</summary>
        public DialogueNode(string id, string speaker, string text, bool ending, IReadOnlyList<DialogueChoice> choices)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            IsEnding = ending;
            Choices = choices ?? Array.Empty<DialogueChoice>();
        }

        /// <summary>Id other nodes link to.</summary>
        public string Id { get; }

        /// <summary>Who is speaking. For player-facing lines this is still the NPC reacting, or the player.</summary>
        public string Speaker { get; }

        /// <summary>Spoken line.</summary>
        public string Text { get; }

        /// <summary>True when the author marked this beat as an ending.</summary>
        public bool IsEnding { get; }

        /// <summary>Lines the player can choose. Empty on an ending.</summary>
        public IReadOnlyList<DialogueChoice> Choices { get; }
    }

    /// <summary>A full scenario loaded from JSON. Adding a file does not require a code change.</summary>
    public sealed class DialogueScenario
    {
        /// <summary>Creates a scenario document.</summary>
        public DialogueScenario(
            string id,
            string title,
            string setting,
            string npcName,
            string npcRole,
            float initialTension,
            int scoreBaseline,
            string startNodeId,
            IReadOnlyList<DialogueNode> nodes)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? string.Empty;
            Setting = setting ?? string.Empty;
            NpcName = npcName ?? string.Empty;
            NpcRole = npcRole ?? string.Empty;
            InitialTension = initialTension;
            ScoreBaseline = scoreBaseline;
            StartNodeId = startNodeId ?? throw new ArgumentNullException(nameof(startNodeId));
            Nodes = nodes ?? Array.Empty<DialogueNode>();
        }

        /// <summary>File-stable id, unique across the scenario folder.</summary>
        public string Id { get; }

        /// <summary>Short title used on the debrief.</summary>
        public string Title { get; }

        /// <summary>One or two sentences that tell the player who they are.</summary>
        public string Setting { get; }

        /// <summary>Name of the person they are talking to.</summary>
        public string NpcName { get; }

        /// <summary>Relationship, such as customer or neighbor.</summary>
        public string NpcRole { get; }

        /// <summary>Tension when the scene starts. Authors should keep this inside 0–1.</summary>
        public float InitialTension { get; }

        /// <summary>Starting score for every dimension, before any line is chosen.</summary>
        public int ScoreBaseline { get; }

        /// <summary>Id of the first node.</summary>
        public string StartNodeId { get; }

        /// <summary>Every node in the file, including ones a validator may flag as unreachable.</summary>
        public IReadOnlyList<DialogueNode> Nodes { get; }
    }

    /// <summary>Lookup over a scenario. Invalid ids fail the lookup instead of throwing.</summary>
    public interface IDialogueGraph
    {
        /// <summary>Scenario this graph was built from.</summary>
        DialogueScenario Scenario { get; }

        /// <summary>Id of the first node.</summary>
        string StartNodeId { get; }

        /// <summary>Every node.</summary>
        IReadOnlyList<DialogueNode> Nodes { get; }

        /// <summary>Resolves a node id.</summary>
        /// <param name="nodeId">Id to find.</param>
        /// <param name="node">The node, when the method returns true.</param>
        /// <returns>False for a null, blank, or unknown id.</returns>
        bool TryGetNode(string? nodeId, out DialogueNode? node);
    }

    /// <summary>In-memory graph. Duplicate ids and a missing start node are load errors.</summary>
    public sealed class DialogueGraph : IDialogueGraph
    {
        readonly Dictionary<string, DialogueNode> _nodes;

        /// <summary>Indexes <paramref name="scenario"/> by node id.</summary>
        /// <param name="scenario">Document to index.</param>
        public DialogueGraph(DialogueScenario scenario)
        {
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            _nodes = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
            foreach (var node in scenario.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                    throw new ScenarioLoadException("A dialogue node is missing an id.");
                if (_nodes.ContainsKey(node.Id))
                    throw new ScenarioLoadException($"Duplicate node id '{node.Id}'.");
                _nodes.Add(node.Id, node);
            }

            if (string.IsNullOrWhiteSpace(scenario.StartNodeId) || !_nodes.ContainsKey(scenario.StartNodeId))
                throw new ScenarioLoadException($"Start node '{scenario.StartNodeId}' does not exist.");
        }

        /// <inheritdoc />
        public DialogueScenario Scenario { get; }

        /// <inheritdoc />
        public string StartNodeId => Scenario.StartNodeId;

        /// <inheritdoc />
        public IReadOnlyList<DialogueNode> Nodes => Scenario.Nodes;

        /// <inheritdoc />
        public bool TryGetNode(string? nodeId, out DialogueNode? node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            if (_nodes.TryGetValue(nodeId, out var found))
            {
                node = found;
                return true;
            }

            node = null;
            return false;
        }
    }

    /// <summary>Reads scenario JSON into the domain model. Content rules live in <see cref="ScenarioValidator"/>.</summary>
    public static class ScenarioLoader
    {
        static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>Parses a scenario. Throws <see cref="ScenarioLoadException"/> when the document cannot form a graph.</summary>
        /// <param name="json">Scenario JSON text.</param>
        public static DialogueScenario Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ScenarioLoadException("Scenario JSON is empty.");

            ScenarioDocument? document;
            try
            {
                document = JsonSerializer.Deserialize<ScenarioDocument>(json, Options);
            }
            catch (JsonException ex)
            {
                throw new ScenarioLoadException("Scenario JSON could not be parsed: " + ex.Message);
            }

            if (document == null)
                throw new ScenarioLoadException("Scenario JSON did not contain an object.");
            if (string.IsNullOrWhiteSpace(document.Id))
                throw new ScenarioLoadException("Scenario is missing an id.");
            if (string.IsNullOrWhiteSpace(document.Title))
                throw new ScenarioLoadException("Scenario is missing a title.");
            if (string.IsNullOrWhiteSpace(document.StartNodeId))
                throw new ScenarioLoadException("Scenario is missing a startNodeId.");
            if (document.Nodes == null || document.Nodes.Count == 0)
                throw new ScenarioLoadException("Scenario has no nodes.");

            var nodes = new List<DialogueNode>(document.Nodes.Count);
            foreach (var node in document.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                    throw new ScenarioLoadException("A dialogue node is missing an id.");

                var choices = new List<DialogueChoice>();
                var choiceIds = new HashSet<string>(StringComparer.Ordinal);
                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (choice == null || string.IsNullOrWhiteSpace(choice.Id))
                            throw new ScenarioLoadException($"Node '{node.Id}' has a choice with no id.");
                        if (!choiceIds.Add(choice.Id))
                            throw new ScenarioLoadException($"Node '{node.Id}' has a duplicate choice id '{choice.Id}'.");

                        choices.Add(new DialogueChoice(
                            choice.Id,
                            choice.Text ?? string.Empty,
                            choice.NextNodeId,
                            new MoodDelta(choice.TensionDelta),
                            new ScoreDelta(choice.Empathy, choice.Clarity, choice.Deescalation),
                            choice.Rationale ?? string.Empty));
                    }
                }

                nodes.Add(new DialogueNode(
                    node.Id,
                    node.Speaker ?? string.Empty,
                    node.Text ?? string.Empty,
                    node.Ending,
                    choices));
            }

            var scenario = new DialogueScenario(
                document.Id,
                document.Title,
                document.Setting ?? string.Empty,
                document.NpcName ?? string.Empty,
                document.NpcRole ?? string.Empty,
                document.InitialTension,
                document.ScoreBaseline ?? 40,
                document.StartNodeId,
                nodes);

            // Indexing throws on duplicate node ids and a missing start node.
            _ = new DialogueGraph(scenario);
            return scenario;
        }

        sealed class ScenarioDocument
        {
            public string? Id { get; set; }
            public string? Title { get; set; }
            public string? Setting { get; set; }
            public string? NpcName { get; set; }
            public string? NpcRole { get; set; }
            public float InitialTension { get; set; }
            public int? ScoreBaseline { get; set; }
            public string? StartNodeId { get; set; }
            public List<NodeDocument>? Nodes { get; set; }
        }

        sealed class NodeDocument
        {
            public string? Id { get; set; }
            public string? Speaker { get; set; }
            public string? Text { get; set; }
            public bool Ending { get; set; }
            public List<ChoiceDocument>? Choices { get; set; }
        }

        sealed class ChoiceDocument
        {
            public string? Id { get; set; }
            public string? Text { get; set; }
            public string? NextNodeId { get; set; }
            public float TensionDelta { get; set; }
            public int Empathy { get; set; }
            public int Clarity { get; set; }
            public int Deescalation { get; set; }
            public string? Rationale { get; set; }
        }
    }
}
