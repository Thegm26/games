using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WhoEnters.Core;
using WhoEnters.Presentation;
using WhoEnters.UI;
using WhoEnters.Audio;

namespace WhoEnters.Gameplay
{
    public sealed class GameDirector : MonoBehaviour
    {
        private const string BestScoreKey = "who-enters.best-score";
        private const string TutorialKey = "who-enters.tutorial-complete";
        private readonly Color parchment = new Color(0.95f, 0.88f, 0.67f, 1f);
        private readonly Color gold = new Color(0.93f, 0.65f, 0.20f, 1f);
        private readonly Color moss = new Color(0.22f, 0.48f, 0.31f, 1f);
        private readonly Color crimson = new Color(0.60f, 0.16f, 0.20f, 1f);

        private GameAudio audioSystem;
        private RunStateMachine run;
        private GameContent content;
        private Text titleText;
        private Text statusText;
        private Text decreeText;
        private Text visitorNameText;
        private Text dialogueText;
        private Text evidenceText;
        private Text feedbackText;
        private Text narrativeCaptionText;
        private Text overlayText;
        private Text muteText;
        private GameObject card;
        private GameObject decisionButtons;
        private GameObject overlay;
        private Button overlayPrimary;
        private Text overlayPrimaryText;
        private SwipeCard swipeCard;
        private DebugOverlay debugOverlay;
        private CaptionSequenceController captions;
        private Text activeCaptionTarget;
        private Action afterCaptionSequence;
        private PresentationBindings presentationBindings;
        private bool resolving;

        private void Awake()
        {
            audioSystem = GetComponent<GameAudio>();
            content = DevelopmentContent.Create();
            run = new RunStateMachine(content, ResolveSeed());
            EnsurePresentationController();
            DebugTrace.Log("scene.transition", "scene=Gatehouse;state=Awake");
            BuildInterface();
        }

        private void Start()
        {
            ShowTitle();
        }

        private void Update()
        {
            if (captions != null && captions.IsActive)
            {
                captions.Advance(Time.unscaledDeltaTime);
                RefreshCaptionText();
            }
            if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                debugOverlay.Toggle();
                return;
            }
            if (Input.GetKeyDown(KeyCode.T) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                captions.InstantMode = !captions.InstantMode;
                DebugTrace.Log("presentation.debug_toggle", "instant=" + captions.InstantMode);
                return;
            }
            if (run.State.Phase != RunPhase.Encounter || resolving || !IsDecisionInputAvailable) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Submit(Decision.Admit, "keyboard");
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Submit(Decision.Deny, "keyboard");
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("GatehouseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                system.transform.SetParent(transform, false);
            }

            Panel("Night", canvasObject.transform, new Vector2(720f, 1280f), Vector2.zero, new Color(0.055f, 0.035f, 0.09f));
            Panel("Castle Silhouette", canvasObject.transform, new Vector2(680f, 180f), new Vector2(0f, 430f), new Color(0.16f, 0.11f, 0.22f));
            titleText = Label("Title", canvasObject.transform, "WHO ENTERS?", 52, gold, new Vector2(650f, 74f), new Vector2(0f, 560f), TextAnchor.MiddleCenter, FontStyle.Bold);
            statusText = Label("Status", canvasObject.transform, "", 25, parchment, new Vector2(650f, 42f), new Vector2(0f, 505f), TextAnchor.MiddleCenter);
            var decreePanel = Panel("Decree Panel", canvasObject.transform, new Vector2(610f, 126f), new Vector2(0f, 414f), new Color(0.14f, 0.09f, 0.18f, 0.9f));
            decreeText = Label("Decree", decreePanel.transform, "", 21, parchment, new Vector2(575f, 108f), Vector2.zero, TextAnchor.UpperLeft);

            card = Panel("Visitor Card", canvasObject.transform, new Vector2(600f, 470f), new Vector2(0f, -35f), new Color(0.22f, 0.15f, 0.28f));
            card.GetComponent<Image>().raycastTarget = true;
            visitorNameText = Label("Visitor", card.transform, "", 38, parchment, new Vector2(530f, 60f), new Vector2(0f, 166f), TextAnchor.MiddleCenter, FontStyle.Bold);
            dialogueText = Label("Dialogue", card.transform, "", 27, Color.white, new Vector2(500f, 140f), new Vector2(0f, 52f), TextAnchor.MiddleCenter);
            evidenceText = Label("Evidence", card.transform, "", 20, gold, new Vector2(500f, 106f), new Vector2(0f, -112f), TextAnchor.MiddleCenter);
            feedbackText = Label("Feedback", canvasObject.transform, "", 25, parchment, new Vector2(660f, 46f), new Vector2(0f, -302f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var captionPanel = Panel("Narrative Caption", canvasObject.transform, new Vector2(620f, 94f), new Vector2(0f, -355f), new Color(0.08f, 0.05f, 0.13f, 0.93f));
            narrativeCaptionText = Label("Narrative Caption Text", captionPanel.transform, "", 19, parchment, new Vector2(585f, 78f), Vector2.zero, TextAnchor.MiddleCenter);
            presentationBindings = new PresentationBindings { CaptionText = narrativeCaptionText };
            swipeCard = card.AddComponent<SwipeCard>();
            swipeCard.Configure(card.GetComponent<RectTransform>());
            swipeCard.Decided += decision => Submit(decision, "swipe");

            decisionButtons = new GameObject("Decision Buttons", typeof(RectTransform));
            decisionButtons.transform.SetParent(canvasObject.transform, false);
            CreateButton("Admit Button", decisionButtons.transform, "← ADMIT", moss, new Vector2(-160f, -445f), () => Submit(Decision.Admit, "button"));
            CreateButton("Deny Button", decisionButtons.transform, "DENY →", crimson, new Vector2(160f, -445f), () => Submit(Decision.Deny, "button"));
            var mute = CreateButton("Mute", canvasObject.transform, "", new Color(0.18f, 0.13f, 0.24f), new Vector2(285f, 560f), ToggleMute, new Vector2(110f, 44f), 18);
            muteText = mute.GetComponentInChildren<Text>();
            CreateButton("Debug", canvasObject.transform, "DEBUG", new Color(0.18f, 0.13f, 0.24f), new Vector2(-275f, 560f), () => debugOverlay.Toggle(), new Vector2(110f, 44f), 16);

            overlay = Panel("Overlay", canvasObject.transform, new Vector2(650f, 620f), Vector2.zero, new Color(0.06f, 0.04f, 0.10f, 0.98f));
            overlayText = Label("Overlay Text", overlay.transform, "", 30, parchment, new Vector2(560f, 330f), new Vector2(0f, 85f), TextAnchor.MiddleCenter);
            overlayPrimary = CreateButton("Overlay Primary", overlay.transform, "", gold, new Vector2(0f, -170f), BeginRun, new Vector2(360f, 78f), 25);
            overlayPrimaryText = overlayPrimary.GetComponentInChildren<Text>();
            CreateButton("How To Play", overlay.transform, "HOW TO PLAY", new Color(0.20f, 0.15f, 0.28f), new Vector2(0f, -265f), HandleHowToPlay, new Vector2(360f, 58f), 18);

            var debugPanel = Panel("Debug Trace Panel", canvasObject.transform, new Vector2(680f, 315f), new Vector2(0f, -460f), new Color(0f, 0f, 0f, 0.85f));
            debugPanel.transform.SetAsLastSibling();
            var debugText = Label("Debug Trace", debugPanel.transform, "", 15, Color.white, new Vector2(660f, 295f), Vector2.zero, TextAnchor.UpperLeft);
            debugOverlay = debugText.gameObject.AddComponent<DebugOverlay>();
            debugOverlay.Configure(debugText);
            RefreshMuteLabel();
        }

        private void ShowTitle()
        {
            card.SetActive(false);
            decisionButtons.SetActive(false);
            decreeText.transform.parent.gameObject.SetActive(false);
            overlay.SetActive(true);
            BeginCaption(StoryCaptionCatalog.Intro(), overlayText);
            overlayPrimaryText.text = "CONTINUE";
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleTitlePrimary);
            DebugTrace.Log("scene.transition", "screen=title");
        }

        private void ShowTutorial()
        {
            overlay.SetActive(true);
            BeginCaption(StoryCaptionCatalog.Tutorial(), overlayText);
            overlayPrimaryText.text = "CONTINUE";
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleTutorialPrimary);
            PlayerPrefs.SetInt(TutorialKey, 1);
            PlayerPrefs.Save();
            DebugTrace.Log("persistence.saved", $"key={TutorialKey};value=1");
            DebugTrace.Log("scene.transition", "screen=tutorial");
        }

        private void HandleHowToPlay()
        {
            audioSystem.NotifyUiInteraction();
            ShowTutorial();
        }

        private void BeginRun()
        {
            audioSystem.UnlockFromInteraction();
            run = new RunStateMachine(content, ResolveSeed());
            run.StartRun();
            ShowDayIntro();
        }

        private void HandleTitlePrimary()
        {
            audioSystem.NotifyUiInteraction();
            if (HandleCaptionAction() != TypewriterAction.CompletedSequence) return;
            BeginRun();
        }

        private void HandleTutorialPrimary()
        {
            audioSystem.NotifyUiInteraction();
            if (HandleCaptionAction() != TypewriterAction.CompletedSequence) return;
            BeginRun();
        }

        private void ShowDayIntro()
        {
            var decree = run.CurrentDecree();
            card.SetActive(false);
            decisionButtons.SetActive(false);
            decreeText.transform.parent.gameObject.SetActive(false);
            overlay.SetActive(true);
            overlayPrimaryText.text = "CONTINUE";
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleDayIntroPrimary);
            BeginCaption(StoryCaptionCatalog.DayAndDecree(run.State.Day, decree), overlayText);
            audioSystem.Play(AudioCueIds.DayTransition);
            DebugTrace.Log("scene.transition", "screen=day-intro;day=" + run.State.Day);
        }

        private void HandleDayIntroPrimary()
        {
            audioSystem.NotifyUiInteraction();
            if (HandleCaptionAction() != TypewriterAction.CompletedSequence) return;
            overlay.SetActive(false);
            decreeText.transform.parent.gameObject.SetActive(true);
            card.SetActive(true);
            decisionButtons.SetActive(true);
            RenderEncounter();
        }

        private void RenderEncounter()
        {
            var visitor = run.CurrentVisitor();
            var decree = run.CurrentDecree();
            if (visitor == null || decree == null)
            {
                DebugTrace.Error("state.render_failed", $"visitor={(visitor == null)};decree={(decree == null)}");
                return;
            }
            titleText.text = "WHO ENTERS?";
            statusText.text = $"DAY {run.State.Day}  •  {run.State.EncounterIndex + 1}/{content.VisitorsPerDay}  •  SEALS {new string('♦', run.State.Integrity)}";
            decreeText.text = $"<b>{decree.Title.ToUpperInvariant()}</b>\n{decree.DisplayText}";
            visitorNameText.text = visitor.DisplayName.ToUpperInvariant();
            dialogueText.text = $"“{visitor.Dialogue}”";
            evidenceText.text = Evidence(visitor);
            feedbackText.text = $"SCORE {run.State.Score}  •  STREAK {run.State.Streak}";
            swipeCard.ResetCard();
            swipeCard.enabled = true;
            DebugTrace.Log("decree.selected", $"day={decree.Day};id={decree.Title}");
            DebugTrace.Log("visitor.selected", $"id={visitor.Id};day={visitor.Day};index={run.State.EncounterIndex};portrait={visitor.PortraitKey}");
            DebugTrace.Log("asset.visual_bound", $"key={visitor.PortraitKey};fallback=runtime-card");
            DebugTrace.Log("scene.transition", "screen=encounter");
            audioSystem.Play(AudioCueIds.GateOpen);
            BeginCaption(StoryCaptionCatalog.Visitor(visitor), presentationBindings.CaptionText);
        }

        private void Submit(Decision decision, string source)
        {
            if (PresentationInputGate.ConsumeDecisionCaption(captions))
            {
                RefreshCaptionText();
                return;
            }
            if (resolving || run.State.Phase != RunPhase.Encounter || !IsDecisionInputAvailable) return;
            resolving = true;
            var visitor = run.CurrentVisitor();
            audioSystem.UnlockFromInteraction();
            audioSystem.Play(decision == Decision.Admit ? "verdict.admit" : "verdict.deny");
            var verdict = run.Resolve(decision);
            audioSystem.Play(AudioCueIds.VerdictStamp);
            if (!verdict.Correct) audioSystem.Play(AudioCueIds.IntegrityCrack);
            else if (run.State.Streak >= 2) audioSystem.Play(AudioCueIds.StreakChime);
            feedbackText.text = verdict.Correct ? $"THE DECREE HOLDS  +{verdict.ScoreDelta}" : "A SEAL SHATTERS  −1";
            feedbackText.color = verdict.Correct ? gold : crimson;
            DebugTrace.Log("input.decision", $"source={source};decision={decision}");
            BeginCaption(StoryCaptionCatalog.Verdict(visitor, verdict, run.State), feedbackText, ContinueAfterVerdict);
        }

        private void ContinueAfterVerdict()
        {
            resolving = false;
            feedbackText.color = parchment;
            if (run.State.Phase == RunPhase.Encounter) RenderEncounter();
            else if (run.State.Phase == RunPhase.DaySummary) ShowDaySummary();
            else if (run.State.Phase == RunPhase.Ending) ShowEnding();
        }

        private void ShowDaySummary()
        {
            card.SetActive(false);
            decisionButtons.SetActive(false);
            overlay.SetActive(true);
            BeginCaption(StoryCaptionCatalog.DaySummary(run.State), overlayText);
            overlayPrimaryText.text = "CONTINUE";
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleDaySummaryPrimary);
            DebugTrace.Log("scene.transition", "screen=day-summary");
        }

        private void ShowEnding()
        {
            card.SetActive(false);
            decisionButtons.SetActive(false);
            overlay.SetActive(true);
            var ending = run.Ending();
            audioSystem.PlayEnding(ending);
            BeginCaption(StoryCaptionCatalog.Ending(ending, run.State), overlayText);
            overlayPrimaryText.text = "CONTINUE";
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleEndingPrimary);
            var best = PlayerPrefs.GetInt(BestScoreKey, 0);
            if (run.State.Score > best)
            {
                PlayerPrefs.SetInt(BestScoreKey, run.State.Score);
                PlayerPrefs.Save();
                DebugTrace.Log("persistence.saved", $"key={BestScoreKey};value={run.State.Score}");
            }
            DebugTrace.Log("scene.transition", $"screen=ending;ending={ending}");
        }

        private void HandleDaySummaryPrimary()
        {
            audioSystem.NotifyUiInteraction();
            if (HandleCaptionAction() != TypewriterAction.CompletedSequence) return;
            overlay.SetActive(false);
            card.SetActive(true);
            decisionButtons.SetActive(true);
            run.AdvanceDay();
            ShowDayIntro();
        }

        private void HandleEndingPrimary()
        {
            audioSystem.NotifyUiInteraction();
            if (HandleCaptionAction() != TypewriterAction.CompletedSequence) return;
            BeginRun();
        }

        /// <summary>First deliberate action completes text; only a later action reaches gameplay.</summary>
        private TypewriterAction HandleCaptionAction()
        {
            if (captions == null || !captions.IsActive) return TypewriterAction.None;
            var action = captions.HandleAction();
            RefreshCaptionText();
            return action;
        }

        private void BeginCaption(CaptionSequence sequence, Text target, Action afterComplete = null)
        {
            activeCaptionTarget = target;
            afterCaptionSequence = afterComplete;
            if (!captions.Begin(sequence))
            {
                DebugTrace.Error("presentation.error/fallback", "reason=begin-failed;target=" + (target == null ? "none" : target.name));
                afterCaptionSequence?.Invoke();
                return;
            }
            RefreshCaptionText();
        }

        private void OnCaptionStarted(CaptionLine line)
        {
            if (activeCaptionTarget != null) activeCaptionTarget.text = string.Empty;
        }

        private void OnCaptionSequenceCompleted(CaptionSequence sequence)
        {
            var continuation = afterCaptionSequence;
            afterCaptionSequence = null;
            continuation?.Invoke();
        }

        private void RefreshCaptionText()
        {
            if (activeCaptionTarget != null && captions != null) activeCaptionTarget.text = captions.VisibleText;
        }

        public void SetReducedMotion(bool enabled)
        {
            EnsurePresentationController();
            captions.InstantMode = enabled;
            DebugTrace.Log("presentation.reduced_motion", "enabled=" + enabled);
        }

        public bool IsDecisionInputAvailable => card != null && decisionButtons != null && overlay != null
            && PresentationInputGate.IsDecisionSurfaceAvailable(card.activeInHierarchy, decisionButtons.activeInHierarchy, overlay.activeInHierarchy);

        private void EnsurePresentationController()
        {
            if (captions != null) return;
            captions = new CaptionSequenceController();
            captions.CaptionStarted += OnCaptionStarted;
            captions.SequenceCompleted += OnCaptionSequenceCompleted;
        }

        private void ToggleMute()
        {
            audioSystem.ToggleMute();
            if (!audioSystem.IsMuted) audioSystem.Play(AudioCueIds.UiClick);
            RefreshMuteLabel();
        }

        private void RefreshMuteLabel() => muteText.text = audioSystem.IsMuted ? "UNMUTE" : "MUTE";

        private static string Evidence(VisitorDefinition visitor)
        {
            var details = visitor.Documents.Concat(visitor.VisibleCues).Concat(visitor.Traits).ToArray();
            return details.Length == 0 ? "No documents. No clear sign." : string.Join("  •  ", details.Select(detail => detail.ToUpperInvariant()));
        }

        private static int ResolveSeed()
        {
            var argument = Environment.GetCommandLineArgs().FirstOrDefault(value => value.StartsWith("-seed=", StringComparison.Ordinal));
            if (argument != null && int.TryParse(argument.Substring(6), out var seed)) return seed;
            return 260928;
        }

        private GameObject Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color, Vector2 dimensions, Vector2 position, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private Button CreateButton(string name, Transform parent, string text, Color color, Vector2 position, UnityEngine.Events.UnityAction action, Vector2? dimensions = null, int fontSize = 25)
        {
            var buttonObject = Panel(name, parent, dimensions ?? new Vector2(270f, 82f), position, color);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(action);
            var caption = Label("Caption", buttonObject.transform, text, fontSize, Color.white, buttonObject.GetComponent<RectTransform>().sizeDelta, Vector2.zero, TextAnchor.MiddleCenter, FontStyle.Bold);
            caption.raycastTarget = false;
            return button;
        }
    }
}
