#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using EmpathyLab.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EmpathyLab.Runtime
{
    /// <summary>
    /// Scene adapter for one scenario. It loads JSON, draws world-space text, and forwards a selection to <see cref="ScenarioSession"/>.
    /// </summary>
    public sealed class ScenarioPresenter : MonoBehaviour
    {
        /// <summary>File name inside Assets/StreamingAssets/Scenarios.</summary>
        [SerializeField] string scenarioFileName = "front-counter.json";

        ScenarioSession? _session;
        Font? _font;
        RectTransform? _choiceRoot;
        Text? _speaker;
        Text? _line;
        Text? _mood;
        Text? _setting;
        GameObject? _debriefRoot;
        Text? _debriefBody;
        readonly List<GameObject> _choiceButtons = new List<GameObject>();

        /// <summary>True after the canvas exists and a load has been attempted.</summary>
        public bool IsReady { get; private set; }

        /// <summary>True when the session has reached an ending.</summary>
        public bool IsComplete => _session != null && _session.IsComplete;

        /// <summary>True when the debrief panel is active.</summary>
        public bool IsDebriefVisible => _debriefRoot != null && _debriefRoot.activeSelf;

        /// <summary>Current node id, or null before the file loads.</summary>
        public string? CurrentNodeId => _session?.Current.Id;

        /// <summary>Debrief after the scene ends, otherwise null.</summary>
        public DebriefReport? Debrief => _session != null && _session.IsComplete ? _session.BuildDebrief() : null;

        /// <summary>Load error, when the file could not be played.</summary>
        public string? Error { get; private set; }

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            BuildCanvas();
            LoadScenario();
            IsReady = true;
        }

        void Update()
        {
            if (!IsReady || _session == null || _session.IsComplete || Keyboard.current == null)
                return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                ChooseAt(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                ChooseAt(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                ChooseAt(2);
        }

        /// <summary>Selects the first choice on the current node. Used by PlayMode tests.</summary>
        public void ChooseFirst()
        {
            if (_session == null || _session.IsComplete || _session.Current.Choices.Count == 0)
                return;
            Choose(_session.Current.Choices[0].Id);
        }

        /// <summary>Selects a choice by its position on screen. 0 is the top button.</summary>
        /// <param name="index">Zero-based index.</param>
        public void ChooseAt(int index)
        {
            if (_session == null || _session.IsComplete)
                return;
            if (index < 0 || index >= _session.Current.Choices.Count)
                return;
            Choose(_session.Current.Choices[index].Id);
        }

        /// <summary>Selects a choice by id and refreshes the panel.</summary>
        /// <param name="choiceId">Choice id on the current node.</param>
        public void Choose(string choiceId)
        {
            if (_session == null || _session.IsComplete)
                return;

            _session.Choose(choiceId);
            Refresh();
        }

        /// <summary>Recenters the view. Wired to the Reset view button.</summary>
        public void ResetView()
        {
            var rig = FindAnyObjectByType<XrRigBootstrap>();
            if (rig != null)
                rig.ResetView();
        }

        void LoadScenario()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Scenarios", scenarioFileName);
            if (!File.Exists(path))
            {
                Error = "Scenario file not found: " + scenarioFileName;
                ShowStatus("Scenario", Error);
                return;
            }

            try
            {
                _session = new ScenarioSession(ScenarioLoader.Load(File.ReadAllText(path)));
                Refresh();
            }
            catch (ScenarioLoadException ex)
            {
                Error = ex.Message;
                ShowStatus("Scenario", ex.Message);
            }
        }

        void Refresh()
        {
            if (_session == null)
                return;

            if (_setting != null)
                _setting.text = _session.Scenario.Setting;
            if (_speaker != null)
                _speaker.text = _session.Current.Speaker;
            if (_line != null)
                _line.text = _session.Current.Text;
            if (_mood != null)
                _mood.text = _session.Scenario.NpcName + " is " + MoodLabel(_session.Mood.Band);

            TintNpc(_session.Mood.Band);
            RebuildChoices();

            if (_session.IsComplete)
                ShowDebrief(_session.BuildDebrief());
        }

        void ShowStatus(string speaker, string message)
        {
            if (_speaker != null)
                _speaker.text = speaker;
            if (_line != null)
                _line.text = message;
        }

        void RebuildChoices()
        {
            for (var i = 0; i < _choiceButtons.Count; i++)
                Destroy(_choiceButtons[i]);
            _choiceButtons.Clear();

            if (_choiceRoot == null || _session == null || _session.IsComplete)
                return;

            var index = 1;
            foreach (var choice in _session.Current.Choices)
            {
                var id = choice.Id;
                var button = CreateButton(_choiceRoot, index + ".  " + choice.Text, () => Choose(id));
                _choiceButtons.Add(button.gameObject);
                index++;
            }
        }

        void ShowDebrief(DebriefReport report)
        {
            if (_debriefRoot == null || _debriefBody == null)
                return;

            var builder = new StringBuilder();
            builder.AppendLine(report.ScenarioTitle);
            builder.AppendLine("Empathy " + report.Empathy + "    Clarity " + report.Clarity + "    De-escalation " + report.Deescalation);
            builder.AppendLine("Mood now: " + MoodLabel(report.FinalMood));
            builder.AppendLine();
            foreach (var choice in report.Choices)
            {
                builder.AppendLine("You said: " + choice.PlayerText);
                builder.AppendLine(choice.Rationale);
                builder.AppendLine();
            }

            _debriefBody.text = builder.ToString();
            _debriefRoot.SetActive(true);
        }

        void TintNpc(MoodBand band)
        {
            var npc = GameObject.Find("Avery");
            if (npc == null)
                return;

            Color color;
            if (band == MoodBand.Calm)
                color = new Color(0.45f, 0.62f, 0.56f);
            else if (band == MoodBand.Neutral)
                color = new Color(0.70f, 0.62f, 0.48f);
            else
                color = new Color(0.72f, 0.45f, 0.34f);

            RoomDressing.Paint(npc, color);
        }

        static string MoodLabel(MoodBand band)
        {
            if (band == MoodBand.Calm)
                return "calmer";
            if (band == MoodBand.Neutral)
                return "steady";
            return "upset";
        }

        void BuildCanvas()
        {
            var anchor = GameObject.Find("Dialogue Anchor");
            var canvasObject = new GameObject("Dialogue Canvas", typeof(RectTransform));
            if (anchor != null)
                canvasObject.transform.SetParent(anchor.transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(980f, 820f);
            rect.localScale = new Vector3(0.0013f, 0.0013f, 0.0013f);

            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            var background = CreateImage(rect, "Panel", new Color(0.96f, 0.94f, 0.89f, 0.96f));
            Stretch(background.rectTransform);

            _setting = CreateText(rect, "Setting", 22, FontStyle.Italic, TextAnchor.UpperLeft);
            Place(_setting.rectTransform, 36f, -28f, 900f, 90f);
            _setting.color = new Color(0.28f, 0.26f, 0.22f);

            _mood = CreateText(rect, "Mood", 26, FontStyle.Bold, TextAnchor.UpperLeft);
            Place(_mood.rectTransform, 36f, -120f, 640f, 40f);

            _speaker = CreateText(rect, "Speaker", 30, FontStyle.Bold, TextAnchor.UpperLeft);
            Place(_speaker.rectTransform, 36f, -168f, 860f, 42f);

            _line = CreateText(rect, "Line", 32, FontStyle.Normal, TextAnchor.UpperLeft);
            Place(_line.rectTransform, 36f, -214f, 900f, 150f);

            var choices = new GameObject("Choices", typeof(RectTransform));
            choices.transform.SetParent(rect, false);
            _choiceRoot = choices.GetComponent<RectTransform>();
            Place(_choiceRoot, 36f, -380f, 900f, 250f);
            var layout = choices.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var reset = CreateButton(rect, "Reset view", ResetView);
            Place(reset.GetComponent<RectTransform>(), 720f, -24f, 220f, 56f);

            _debriefRoot = new GameObject("Debrief", typeof(RectTransform));
            _debriefRoot.transform.SetParent(rect, false);
            var debriefRect = _debriefRoot.GetComponent<RectTransform>();
            Place(debriefRect, 36f, -370f, 910f, 410f);
            var debriefBack = CreateImage(debriefRect, "Debrief Back", new Color(0.93f, 0.95f, 0.93f, 1f));
            Stretch(debriefBack.rectTransform);
            _debriefBody = CreateText(debriefRect, "Debrief Body", 24, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(_debriefBody.rectTransform);
            _debriefBody.rectTransform.offsetMin = new Vector2(18f, 12f);
            _debriefBody.rectTransform.offsetMax = new Vector2(-18f, -12f);
            _debriefRoot.SetActive(false);

            var hint = CreateText(rect, "Hint", 20, FontStyle.Italic, TextAnchor.LowerLeft);
            Place(hint.rectTransform, 36f, -780f, 900f, 28f);
            hint.text = "Look at a response. Click it, or press 1, 2, or 3. Reset view looks back at the counter.";
            hint.color = new Color(0.33f, 0.31f, 0.27f);
        }

        Text CreateText(RectTransform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.11f, 0.10f, 0.08f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        Image CreateImage(RectTransform parent, string name, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var image = CreateImage(parent, "Choice", new Color(0.99f, 0.98f, 0.95f, 1f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.86f, 0.91f, 0.88f);
            colors.pressedColor = new Color(0.72f, 0.80f, 0.76f);
            colors.normalColor = new Color(0.99f, 0.98f, 0.95f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var layout = image.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 72f;
            layout.preferredHeight = 76f;

            var text = CreateText(image.rectTransform, "Label", 26, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(16f, 8f);
            text.rectTransform.offsetMax = new Vector2(-16f, -8f);
            text.text = label;
            return button;
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
