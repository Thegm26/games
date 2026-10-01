using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WhoEnters.Core;
using WhoEnters.Presentation;
using WhoEnters.UI;
using WhoEnters.Audio;
using WhoEnters.Content;
using WhoEnters.Integration;

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
        private Text decreeHeadingText;
        private Text visitorNameText;
        private Text dialogueText;
        private Text evidenceText;
        private Text feedbackText;
        private Text narrativeCaptionText;
        private Text overlayText;
        private Text overlayHeadingText;
        private Text muteText;
        private Text reducedMotionText;
        private VisualAssetCatalog visualCatalog;
        private Image portraitImage;
        private Image verdictStampImage;
        private Transform evidenceIconRoot;
        private Image[] integritySeals;
        private FeedbackMotion feedbackMotion;
        private GatehouseEnvironmentRig environmentRig;
        private GameObject feedbackPanel;
        private Button howToPlayButton;
        private Button rulebookButton;
        private GameObject rulebookPanel;
        private bool rulebookOpen;
        private GameObject captionPanel;
        private Button captionContinueButton;
        private Text captionContinueText;
        private Button[] verdictButtons;
        // True preserves the pure surface predicate before runtime UI has entered an encounter;
        // RenderEncounter always disables it until the visitor caption completes.
        private bool verdictControlsEnabled = true;
        private bool reducedMotion;
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
        private bool endingCaptionComplete;
        private readonly List<ScheduledVerdictCue> scheduledVerdictCues = new List<ScheduledVerdictCue>();

        private void Awake()
        {
            EnsureCameraAudioListener();
            audioSystem = GetComponent<GameAudio>() ?? gameObject.AddComponent<GameAudio>();
            visualCatalog = VisualAssetCatalog.LoadRequired();
            content = StoryContent.Create();
            run = new RunStateMachine(content, ResolveSeed());
            EnsurePresentationController();
            DebugTrace.Log("integration.content_provider", "provider=StoryContent;developmentContent=false;visitors=" + content.Visitors.Count);
            DebugTrace.Log("scene.transition", "scene=Gatehouse;state=Awake;logicalCanvas=720x1280");
            BuildInterface();
        }

        private void Start()
        {
            ShowTitle();
        }

        private void Update()
        {
            PlayDueVerdictAudio();
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
            if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                && TryKeyboardDecision(KeyCode.LeftArrow, out var leftDecision))
                Submit(leftDecision, "keyboard-left");
            else if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                && TryKeyboardDecision(KeyCode.RightArrow, out var rightDecision))
                Submit(rightDecision, "keyboard-right");
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
            scaler.matchWidthOrHeight = 0f;
            environmentRig = GatehouseEnvironmentRig.Create(canvasObject.transform, visualCatalog);
            var safeRootObject = new GameObject("Safe Area UI", typeof(RectTransform), typeof(SafeAreaLayoutRoot));
            safeRootObject.transform.SetParent(canvasObject.transform, false);
            var safeRoot = safeRootObject.GetComponent<RectTransform>();
            safeRootObject.GetComponent<SafeAreaLayoutRoot>().Configure(safeRoot);
            var safeContentObject = new GameObject("Safe Area Content", typeof(RectTransform), typeof(SafeAreaContentLayout));
            safeContentObject.transform.SetParent(safeRoot, false);
            safeContentObject.GetComponent<SafeAreaContentLayout>().Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            var safeContent = safeContentObject.transform;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                system.transform.SetParent(transform, false);
            }

            var night = FullStretchPanel("Night Tint", canvasObject.transform, new Color(.025f, .015f, .06f, .30f));
            night.GetComponent<Image>().raycastTarget = false;
            night.transform.SetSiblingIndex(1); // environment < tint < all safe-area UI
            titleText = Label("Title", safeContent, "Who Enters?", 56, gold, new Vector2(ViewportLayoutPolicy.Title.Width, ViewportLayoutPolicy.Title.Height), new Vector2(ViewportLayoutPolicy.Title.X, ViewportLayoutPolicy.Title.Y), TextAnchor.MiddleCenter, FontStyle.Bold, true);
            statusText = Label("Status", safeContent, "", 32, parchment, new Vector2(ViewportLayoutPolicy.Status.Width, ViewportLayoutPolicy.Status.Height), new Vector2(ViewportLayoutPolicy.Status.X, ViewportLayoutPolicy.Status.Y), TextAnchor.MiddleCenter);
            card = Panel("Visitor Card", safeContent, new Vector2(ViewportLayoutPolicy.Card.Width, ViewportLayoutPolicy.Card.Height), new Vector2(ViewportLayoutPolicy.Card.X, ViewportLayoutPolicy.Card.Y), Color.white);
            ApplySimpleAspect(card.GetComponent<Image>(), visualCatalog.VisitorCard);
            card.GetComponent<Image>().raycastTarget = true;
            // The mask silhouette intentionally lives *inside* the baked arch: it clips the
            // portrait but never paints over the raster gargoyles, banner, or ornate rim.
            var portraitWindow = new GameObject("Portrait Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(PortraitFrameGraphic), typeof(Mask));
            portraitWindow.transform.SetParent(card.transform, false);
            var windowRect = portraitWindow.GetComponent<RectTransform>();
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(.5f, 0f);
            windowRect.pivot = new Vector2(.5f, 0f);
            windowRect.sizeDelta = new Vector2(ViewportLayoutPolicy.PortraitWindow.Width, ViewportLayoutPolicy.PortraitWindow.Height);
            windowRect.anchoredPosition = new Vector2(ViewportLayoutPolicy.PortraitWindow.X, ViewportLayoutPolicy.PortraitApertureBottomInset);
            var aperture = portraitWindow.GetComponent<PortraitFrameGraphic>();
            aperture.color = Color.white;
            aperture.raycastTarget = false;
            var mask = portraitWindow.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            // Portrait source is 2:3. This measured 180x290 inner arch clips the source inside
            // the card's actual baked gold aperture; it never reaches the top arch or plinth.
            portraitImage = ImageNode("Portrait", portraitWindow.transform, new Vector2(580f / 3f, 290f), Vector2.zero);
            // ImageNode's centred anchor is correct for ordinary card children, but is wrong
            // for this bottom-aligned aperture: a bottom pivot with a centred anchor starts at
            // the aperture centre. Anchor the source surface to the aperture bottom instead.
            var portraitRect = portraitImage.rectTransform;
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(.5f, 0f);
            portraitRect.pivot = new Vector2(.5f, 0f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            // The full live-text lane needs its own calm, opaque substrate; the visitor-card
            // raster is intentionally busy and cannot guarantee readable dossier copy. This
            // simple Image remains below the text nodes, above no portrait pixels, and outside
            // the separate evidence-icon lane.
            var dossierTextBacking = ImageNode("Dossier Text Backing", card.transform,
                new Vector2(ViewportLayoutPolicy.DossierTextBacking.Width, ViewportLayoutPolicy.DossierTextBacking.Height),
                new Vector2(ViewportLayoutPolicy.DossierTextBacking.X, ViewportLayoutPolicy.DossierTextBacking.Y));
            dossierTextBacking.sprite = null;
            dossierTextBacking.type = Image.Type.Simple;
            dossierTextBacking.color = new Color(.94f, .87f, .68f, .97f);
            dossierTextBacking.raycastTarget = false;
            var visitorNameBacking = ImageNode("Visitor Name Backing", card.transform, new Vector2(ViewportLayoutPolicy.VisitorName.Width + 12f, ViewportLayoutPolicy.VisitorName.Height + 6f), new Vector2(ViewportLayoutPolicy.VisitorName.X, ViewportLayoutPolicy.VisitorName.Y));
            visitorNameBacking.sprite = null;
            visitorNameBacking.type = Image.Type.Simple;
            visitorNameBacking.color = new Color(.93f, .84f, .64f, .94f);
            visitorNameBacking.raycastTarget = false;
            visitorNameText = Label("Visitor", card.transform, "", 30, ViewportLayoutPolicy.DarkInk, new Vector2(ViewportLayoutPolicy.VisitorName.Width, ViewportLayoutPolicy.VisitorName.Height), new Vector2(ViewportLayoutPolicy.VisitorName.X, ViewportLayoutPolicy.VisitorName.Y), TextAnchor.MiddleCenter, FontStyle.Bold, true);
            dialogueText = Label("Dialogue", card.transform, "", 28, ViewportLayoutPolicy.DarkInk, new Vector2(ViewportLayoutPolicy.Dialogue.Width, ViewportLayoutPolicy.Dialogue.Height), new Vector2(ViewportLayoutPolicy.Dialogue.X, ViewportLayoutPolicy.Dialogue.Y), TextAnchor.MiddleCenter);
            // This is a readable dossier, not a verdict hint: category labels make every
            // authored fact inspectable before the player chooses, while the art below remains
            // an optional recognition cue.
            evidenceText = Label("Evidence", card.transform, "", 28, ViewportLayoutPolicy.DarkInk, new Vector2(ViewportLayoutPolicy.Evidence.Width, ViewportLayoutPolicy.Evidence.Height), new Vector2(ViewportLayoutPolicy.Evidence.X, ViewportLayoutPolicy.Evidence.Y), TextAnchor.UpperLeft);
            evidenceIconRoot = new GameObject("Evidence Icons", typeof(RectTransform)).transform;
            evidenceIconRoot.SetParent(card.transform, false);
            var iconRect = evidenceIconRoot.GetComponent<RectTransform>();
            ConfigureFixedRect(iconRect);
            iconRect.sizeDelta = new Vector2(ViewportLayoutPolicy.EvidenceIcons.Width, ViewportLayoutPolicy.EvidenceIcons.Height);
            iconRect.anchoredPosition = new Vector2(ViewportLayoutPolicy.EvidenceIcons.X, ViewportLayoutPolicy.EvidenceIcons.Y);
            // There is deliberately one visitor-card raster only: it is behind the portrait.
            // Redrawing that opaque source above this aperture would hide the portrait.
            feedbackPanel = Panel("Verdict Feedback HUD", safeContent, new Vector2(ViewportLayoutPolicy.Feedback.Width, ViewportLayoutPolicy.Feedback.Height), new Vector2(ViewportLayoutPolicy.Feedback.X, ViewportLayoutPolicy.Feedback.Y), new Color(.04f, .025f, .08f, .90f));
            ApplySliced(feedbackPanel.GetComponent<Image>(), visualCatalog.DayEndingPanel);
            feedbackText = Label("Feedback", feedbackPanel.transform, "", 32, parchment, new Vector2(430f, 54f), new Vector2(-85f, 0f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
            verdictStampImage = ImageNode("Verdict Stamp", feedbackPanel.transform, new Vector2(150f, 56f), new Vector2(225f, 0f));
            verdictStampImage.preserveAspect = true; verdictStampImage.raycastTarget = false;
            rulebookButton = CreateButton("Rulebook", safeContent, "RULEBOOK", gold,
                new Vector2(ViewportLayoutPolicy.Rulebook.X, ViewportLayoutPolicy.Rulebook.Y), OpenRulebook,
                new Vector2(ViewportLayoutPolicy.Rulebook.Width, ViewportLayoutPolicy.Rulebook.Height), 32, false, false);
            ApplyPlainTextButton(rulebookButton, new Color(.035f, .045f, .14f, .99f), parchment);
            rulebookPanel = CreateFilledBorderPanel("Rulebook Panel", safeContent,
                new Vector2(ViewportLayoutPolicy.RulebookPanel.Width, ViewportLayoutPolicy.RulebookPanel.Height),
                new Vector2(ViewportLayoutPolicy.RulebookPanel.X, ViewportLayoutPolicy.RulebookPanel.Y),
                new Color(.025f, .04f, .12f, .99f), gold);
            decreeHeadingText = Label("Rulebook Heading", rulebookPanel.transform, "RULEBOOK", 42, gold,
                new Vector2(540f, 84f), new Vector2(0f, 250f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
            decreeText = Label("Rulebook Text", rulebookPanel.transform, "", 32, parchment,
                new Vector2(560f, 300f), new Vector2(0f, 55f), TextAnchor.UpperLeft);
            var rulebookClose = CreateButton("Rulebook Close", rulebookPanel.transform, "CLOSE", gold, new Vector2(0f, -245f),
                CloseRulebook, new Vector2(400f, 100f), 32, false, false);
            ApplyPlainTextButton(rulebookClose, new Color(.035f, .045f, .14f, .99f), parchment);
            rulebookPanel.SetActive(false);
            captionPanel = Panel("Narrative Caption", safeContent, new Vector2(ViewportLayoutPolicy.Caption.Width, ViewportLayoutPolicy.Caption.Height), new Vector2(ViewportLayoutPolicy.Caption.X, ViewportLayoutPolicy.Caption.Y), new Color(0.025f, 0.02f, 0.07f, .98f));
            var captionSurface = captionPanel.GetComponent<Image>();
            captionSurface.sprite = null;
            captionSurface.type = Image.Type.Simple;
            // Guidance and CTA intentionally have no artwork between their glyphs. The plain
            // caption field is the reading surface; a thin gold border is enough hierarchy.
            SetFilledBorderPanel(captionPanel, new Color(.025f, .02f, .07f, .98f), gold);
            narrativeCaptionText = Label("Narrative Caption Text", captionPanel.transform, "", 32, parchment, new Vector2(560f, 74f), new Vector2(0f, 47f), TextAnchor.MiddleCenter);
            captionContinueButton = CreateButton("Caption Continue", captionPanel.transform, "TAP TO REVEAL", gold, new Vector2(0f, -46f), HandleCaptionContinue, new Vector2(400f, 100f), 32, false, false);
            ApplyPlainTextButton(captionContinueButton, new Color(.035f, .045f, .14f, .99f), parchment);
            captionContinueText = captionContinueButton.GetComponentInChildren<Text>();
            presentationBindings = new PresentationBindings { CaptionText = narrativeCaptionText };
            swipeCard = card.AddComponent<SwipeCard>();
            swipeCard.Configure(card.GetComponent<RectTransform>(), HandleCaptionSwipe);
            // These halos deliberately live in fixed safe-content space, behind the final
            // labelled targets rather than on the moving card.  Green lights ADMIT → at right;
            // red lights ← DENY at left. Their centre is hidden by the opaque button surface,
            // leaving a polished perimeter that cannot cover a card, decree, or caption.
            var denyPreview = CreateDecisionTargetGlow("Deny Drag Preview Glow", safeContent,
                ViewportLayoutPolicy.DenyPreviewGlow, crimson);
            var admitPreview = CreateDecisionTargetGlow("Admit Drag Preview Glow", safeContent,
                ViewportLayoutPolicy.AdmitPreviewGlow, moss);
            swipeCard.ConfigureDragPreview(denyPreview, admitPreview);
            swipeCard.Decided += decision => Submit(decision, "swipe");

            decisionButtons = new GameObject("Decision Buttons", typeof(RectTransform));
            decisionButtons.transform.SetParent(safeContent, false);
            ConfigureFixedRect(decisionButtons.GetComponent<RectTransform>());
            var deny = CreateButton("Deny Button", decisionButtons.transform, "← DENY", crimson, new Vector2(ViewportLayoutPolicy.Deny.X, ViewportLayoutPolicy.Deny.Y), () => Submit(Decision.Deny, "button"), new Vector2(ViewportLayoutPolicy.Deny.Width, ViewportLayoutPolicy.Deny.Height), 32);
            ApplyDecisionButton(deny, visualCatalog.DenyButton, false);
            deny.gameObject.AddComponent<DecisionButtonFeedback>().Configure(deny, deny.GetComponentInChildren<Text>());
            var admit = CreateButton("Admit Button", decisionButtons.transform, "ADMIT →", moss, new Vector2(ViewportLayoutPolicy.Admit.X, ViewportLayoutPolicy.Admit.Y), () => Submit(Decision.Admit, "button"), new Vector2(ViewportLayoutPolicy.Admit.Width, ViewportLayoutPolicy.Admit.Height), 32);
            ApplyDecisionButton(admit, visualCatalog.AdmitButton, true);
            admit.gameObject.AddComponent<DecisionButtonFeedback>().Configure(admit, admit.GetComponentInChildren<Text>());
            // Ordered in on-screen direction: left deny, right admit. Tests and public-action
            // routing use this order so button, swipe, and keyboard contracts stay aligned.
            verdictButtons = new[] { deny, admit };
            var mute = CreateButton("Mute", safeContent, "Sound On", new Color(0.18f, 0.13f, 0.24f), new Vector2(ViewportLayoutPolicy.SoundUtility.X, ViewportLayoutPolicy.SoundUtility.Y), ToggleMute, new Vector2(ViewportLayoutPolicy.SoundUtility.Width, ViewportLayoutPolicy.SoundUtility.Height), 32, false, false);
            ApplyPlainTextButton(mute, new Color(.025f, .035f, .11f, .97f), Color.white);
            muteText = mute.GetComponentInChildren<Text>();
            var motion = CreateButton("Reduced Motion", safeContent, "Motion On", new Color(0.18f, 0.13f, 0.24f), new Vector2(ViewportLayoutPolicy.MotionUtility.X, ViewportLayoutPolicy.MotionUtility.Y), ToggleReducedMotion, new Vector2(ViewportLayoutPolicy.MotionUtility.Width, ViewportLayoutPolicy.MotionUtility.Height), 32, false, false);
            ApplyPlainTextButton(motion, new Color(.025f, .035f, .11f, .97f), Color.white);
            reducedMotionText = motion.GetComponentInChildren<Text>();
            // Debug is keyboard-only (Ctrl+D), so it cannot crowd mobile player controls.

            overlay = Panel("Overlay", safeContent, new Vector2(640f, 760f), new Vector2(0f, -25f), Color.white);
            ApplySliced(overlay.GetComponent<Image>(), visualCatalog.DayEndingPanel);
            // This is the measured inner field between the side diamonds/rails of the art.
            // The baked crest occupies the upper third of this panel.  Keep authored titles
            // below it so their hierarchy comes from clear space, not a collision with art.
            overlayHeadingText = Label("Overlay Heading", overlay.transform, "", 42, gold,
                new Vector2(ViewportLayoutPolicy.OverlayHeading.Width, ViewportLayoutPolicy.OverlayHeading.Height),
                new Vector2(ViewportLayoutPolicy.OverlayHeading.X, ViewportLayoutPolicy.OverlayHeading.Y), TextAnchor.MiddleCenter, FontStyle.Bold, true);
            // Keep the first instruction at the top of its dedicated body lane. Centre alignment
            // allowed short tutorial text to climb into the heading's glyph space despite the
            // panel bounds, whereas this measured lane has a real 45px logical gap.
            overlayText = Label("Overlay Text", overlay.transform, "", 32, parchment,
                new Vector2(ViewportLayoutPolicy.OverlayBody.Width, ViewportLayoutPolicy.OverlayBody.Height),
                new Vector2(ViewportLayoutPolicy.OverlayBody.X, ViewportLayoutPolicy.OverlayBody.Y), TextAnchor.UpperCenter);
            overlayPrimary = CreateButton("Overlay Primary", overlay.transform, "", gold, new Vector2(0f, -190f), BeginRun, new Vector2(400f, 100f), 32, false, false);
            ApplyPlainTextButton(overlayPrimary, new Color(.035f, .045f, .14f, .99f), parchment);
            overlayPrimaryText = overlayPrimary.GetComponentInChildren<Text>();
            howToPlayButton = CreateButton("How To Play", overlay.transform, "How To Play", new Color(0.20f, 0.15f, 0.28f), new Vector2(0f, -305f), HandleHowToPlay, new Vector2(400f, 100f), 32, false, false);
            ApplyPlainTextButton(howToPlayButton, new Color(.035f, .045f, .14f, .99f), parchment);

            var debugPanel = Panel("Debug Trace Panel", safeContent, new Vector2(680f, 315f), new Vector2(0f, -420f), new Color(0f, 0f, 0f, 0.85f));
            debugPanel.transform.SetAsLastSibling();
            var debugText = Label("Debug Trace", debugPanel.transform, "", 15, Color.white, new Vector2(660f, 295f), Vector2.zero, TextAnchor.UpperLeft, developerDiagnostic: true);
            debugOverlay = debugPanel.AddComponent<DebugOverlay>();
            debugOverlay.Configure(debugText, debugPanel);
            feedbackMotion = canvasObject.AddComponent<FeedbackMotion>();
            feedbackMotion.Configure(verdictStampImage, feedbackPanel.GetComponent<RectTransform>());
            integritySeals = CreateIntegritySeals(safeContent);
            RefreshIntegritySeals(run.State.Integrity);
            RefreshMuteLabel();
            rulebookPanel.transform.SetAsLastSibling();
            DebugTrace.Log("integration.ui_bound", "card=visitor-card;decree=decree-parchment;overlay=day-ending-panel;buttons=2;seals=5");
        }

        private void ShowTitle()
        {
            SetEncounterHudVisible(false);
            overlayHeadingText.text = "Who Enters?";
            card.SetActive(false);
            decisionButtons.SetActive(false);
            captionPanel.SetActive(false);
            feedbackPanel.SetActive(false);
            SetRulebookVisible(false);
            howToPlayButton.gameObject.SetActive(true);
            overlay.SetActive(true);
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleTitlePrimary);
            BeginCaption(StoryCaptionCatalog.Intro(), overlayText);
            DebugTrace.Log("scene.transition", "screen=title");
        }

        private void ShowTutorial()
        {
            SetEncounterHudVisible(false);
            card.SetActive(false);
            decisionButtons.SetActive(false);
            overlayHeadingText.text = "How To Play";
            overlay.SetActive(true);
            captionPanel.SetActive(false);
            feedbackPanel.SetActive(false);
            SetRulebookVisible(false);
            howToPlayButton.gameObject.SetActive(false);
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleTutorialPrimary);
            BeginCaption(StoryCaptionCatalog.Tutorial(), overlayText);
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
            audioSystem.BeginRun();
            audioSystem.UnlockFromInteraction();
            resolving = false;
            endingCaptionComplete = false;
            scheduledVerdictCues.Clear();
            feedbackMotion?.ClearVerdict();
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
            ShowTitle();
        }

        private void ShowDayIntro()
        {
            SetEncounterHudVisible(false);
            var decree = run.CurrentDecree();
            card.SetActive(false);
            decisionButtons.SetActive(false);
            captionPanel.SetActive(false);
            feedbackPanel.SetActive(false);
            SetRulebookVisible(false);
            howToPlayButton.gameObject.SetActive(false);
            overlay.SetActive(true);
            overlayHeadingText.text = "Night " + run.State.Day;
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
            feedbackPanel.SetActive(true);
            card.SetActive(true);
            decisionButtons.SetActive(true);
            RenderEncounter();
        }

        private void RenderEncounter()
        {
            SetEncounterHudVisible(true);
            var visitor = run.CurrentVisitor();
            var decree = run.CurrentDecree();
            if (visitor == null || decree == null)
            {
                DebugTrace.Error("state.render_failed", $"visitor={(visitor == null)};decree={(decree == null)}");
                return;
            }
            titleText.text = "Who Enters?";
            statusText.text = $"Day {run.State.Day}  •  {run.State.EncounterIndex + 1}/{content.VisitorsPerDay}  •  Seals {new string('♦', run.State.Integrity)}";
            decreeHeadingText.text = decree.Title.ToUpperInvariant();
            decreeText.text = decree.DisplayText;
            visitorNameText.text = visitor.DisplayName;
            dialogueText.text = $"“{visitor.Dialogue}”";
            var decisionEvidence = DecisionEvidence.Resolve(visitor, decree);
            evidenceText.text = decisionEvidence.Text;
            feedbackText.text = $"Score {run.State.Score}  •  Streak {run.State.Streak}";
            BindPortrait(visitor.PortraitKey);
            BindEvidenceIcons(decisionEvidence);
            DebugTrace.Log("integration.dossier", "visitor=" + visitor.Id
                + ";documents=" + string.Join("|", decisionEvidence.Documents)
                + ";visibleSigns=" + string.Join("|", decisionEvidence.VisibleSigns)
                + ";traits=" + string.Join("|", decisionEvidence.Traits)
                + ";uniqueFacts=" + decisionEvidence.Facts.Length
                + ";order=documents,visibleSigns,traits");
            RefreshIntegritySeals(run.State.Integrity);
            feedbackMotion.ClearVerdict();
            swipeCard.ResetCard();
            verdictControlsEnabled = false;
            SetVerdictControls(false);
            captionPanel.SetActive(true);
            DebugTrace.Log("decree.selected", $"day={decree.Day};id={decree.Title}");
            DebugTrace.Log("visitor.selected", $"id={visitor.Id};day={visitor.Day};index={run.State.EncounterIndex};portrait={visitor.PortraitKey}");
            DebugTrace.Log("asset.visual_bound", $"key={visitor.PortraitKey};kind=portrait;fallback=forbidden;resolved=true");
            DebugTrace.Log("scene.transition", "screen=encounter");
            audioSystem.PlayEncounterArrival(run.State.Day, run.State.EncounterIndex, visitor.Id);
            BeginCaption(StoryCaptionCatalog.Visitor(visitor), presentationBindings.CaptionText);
        }

        private void Submit(Decision decision, string source)
        {
            if (rulebookOpen) return;
            if (PresentationInputGate.ConsumeDecisionCaption(captions))
            {
                swipeCard.ResetCard();
                DebugTrace.Log("input.decision_consumed", "source=" + source + ";reason=caption-active;card=reset");
                RefreshCaptionText();
                return;
            }
            if (resolving || run.State.Phase != RunPhase.Encounter || !IsDecisionInputAvailable) return;
            resolving = true;
            SetVerdictControls(false);
            narrativeCaptionText.text = string.Empty;
            captionPanel.SetActive(true);
            var visitor = run.CurrentVisitor();
            audioSystem.UnlockFromInteraction();
            swipeCard.PlayCommitted(decision);
            var verdict = run.Resolve(decision);
            QueueVerdictAudio(decision, verdict.Correct, run.State.Streak);
            feedbackText.text = VerdictFeedback(verdict);
            feedbackText.color = verdict.Correct ? gold : new Color(.98f, .62f, .66f, 1f);
            feedbackMotion.PlayVerdict(verdict.Correct, decision == Decision.Admit ? visualCatalog.VerdictAdmit : visualCatalog.VerdictDeny);
            RefreshIntegritySeals(run.State.Integrity);
            DebugTrace.Log("integration.verdict_explanation", "visitor=" + visitor.Id + ";rule=" + verdict.RuleId
                + ";explanation=" + verdict.RuleExplanation + ";correct=" + verdict.Correct);
            DebugTrace.Log("input.decision", $"source={source};decision={decision}");
            BeginCaption(StoryCaptionCatalog.Verdict(visitor, verdict, run.State), narrativeCaptionText, ContinueAfterVerdict);
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
            SetEncounterHudVisible(false);
            overlayHeadingText.text = "Day " + run.State.Day + " Complete";
            card.SetActive(false);
            decisionButtons.SetActive(false);
            captionPanel.SetActive(false);
            feedbackPanel.SetActive(false);
            SetRulebookVisible(false);
            howToPlayButton.gameObject.SetActive(false);
            overlay.SetActive(true);
            ApplySliced(overlay.GetComponent<Image>(), visualCatalog.DayEndingPanel);
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleDaySummaryPrimary);
            BeginCaption(StoryCaptionCatalog.DaySummary(run.State), overlayText);
            DebugTrace.Log("scene.transition", "screen=day-summary");
        }

        private void ShowEnding()
        {
            SetEncounterHudVisible(false);
            endingCaptionComplete = false;
            card.SetActive(false);
            decisionButtons.SetActive(false);
            captionPanel.SetActive(false);
            feedbackPanel.SetActive(false);
            SetRulebookVisible(false);
            howToPlayButton.gameObject.SetActive(false);
            overlay.SetActive(true);
            ApplySliced(overlay.GetComponent<Image>(), visualCatalog.DayEndingPanel);
            var ending = run.Ending();
            overlayHeadingText.text = ending == EndingKind.TheGateRemembers ? "The Gate Remembers" : "The Last Watch";
            audioSystem.PlayEnding(ending);
            overlayPrimary.onClick.RemoveAllListeners();
            overlayPrimary.onClick.AddListener(HandleEndingPrimary);
            BeginCaption(StoryCaptionCatalog.Ending(ending, run.State), overlayText);
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
            if (captions != null && captions.IsActive)
            {
                HandleCaptionAction();
                return;
            }
            if (!endingCaptionComplete) return;
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
            if (line != null && line.Role == PresentationRole.Visitor)
            {
                verdictControlsEnabled = false;
                SetVerdictControls(false);
                if (captionPanel != null) captionPanel.SetActive(true);
            }
            RefreshCaptionContinuation();
        }

        private void OnCaptionSequenceCompleted(CaptionSequence sequence)
        {
            var continuation = afterCaptionSequence;
            afterCaptionSequence = null;
            continuation?.Invoke();
            if (sequence != null && sequence.Lines.Any(line => line.Role == PresentationRole.Visitor) && !resolving)
            {
                RefreshVerdictControls();
                // Once the player can decide, reclaim this substantial reading lane for the
                // card and fallback actions. Verdict narration reopens it in Submit.
                if (captionPanel != null) captionPanel.SetActive(false);
                DebugTrace.Log("presentation.caption_collapsed", "reason=visitor-decision-ready");
            }
            if (run != null && run.State.Phase == RunPhase.Ending)
            {
                endingCaptionComplete = true;
                if (overlayPrimaryText != null) overlayPrimaryText.text = "PLAY AGAIN";
            }
            RefreshCaptionContinuation();
        }

        private void RefreshCaptionText()
        {
            if (activeCaptionTarget != null && captions != null) activeCaptionTarget.text = captions.VisibleText;
            RefreshCaptionContinuation();
        }

        public void SetReducedMotion(bool enabled)
        {
            EnsurePresentationController();
            reducedMotion = enabled;
            captions.InstantMode = enabled;
            environmentRig?.SetReducedMotion(enabled);
            swipeCard?.SetReducedMotion(enabled);
            feedbackMotion?.SetReducedMotion(enabled);
            if (verdictButtons != null)
                    foreach (var button in verdictButtons) button?.GetComponent<DecisionButtonFeedback>()?.SetReducedMotion(enabled);
            if (reducedMotionText != null) reducedMotionText.text = enabled ? "Motion Off" : "Motion On";
            DebugTrace.Log("presentation.reduced_motion", "enabled=" + enabled);
        }

        public bool IsDecisionInputAvailable => !rulebookOpen && card != null && decisionButtons != null && overlay != null
            && verdictControlsEnabled && PresentationInputGate.IsDecisionSurfaceAvailable(card.activeInHierarchy, decisionButtons.activeInHierarchy, overlay.activeInHierarchy);

        /// <summary>Pure keyboard fallback mapping, shared with the public directional contract.</summary>
        public static bool TryKeyboardDecision(KeyCode key, out Decision decision)
        {
            switch (key)
            {
                case KeyCode.LeftArrow:
                case KeyCode.A:
                    decision = Decision.Deny;
                    return true;
                case KeyCode.RightArrow:
                case KeyCode.D:
                    decision = Decision.Admit;
                    return true;
                default:
                    decision = default;
                    return false;
            }
        }

        private void EnsurePresentationController()
        {
            if (captions != null) return;
            captions = new CaptionSequenceController();
            captions.CaptionStarted += OnCaptionStarted;
            captions.SequenceCompleted += OnCaptionSequenceCompleted;
        }

        private void HandleCaptionContinue()
        {
            if (rulebookOpen) return;
            audioSystem.NotifyUiInteraction();
            if (captions == null || !captions.IsActive) return;
            var action = HandleCaptionAction();
            DebugTrace.Log("presentation.continue_action", "action=" + action);
        }

        private void RefreshCaptionContinuation()
        {
            if (overlayPrimaryText != null && captions != null && captions.IsActive && activeCaptionTarget == overlayText)
                overlayPrimaryText.text = captions.IsTextComplete ? "CONTINUE" : "TAP TO REVEAL";
            if (captionContinueButton == null) return;
            var onEncounterSurface = captions != null && captions.IsActive && activeCaptionTarget == narrativeCaptionText;
            captionContinueButton.gameObject.SetActive(onEncounterSurface);
            if (!onEncounterSurface) return;
            var visitor = captions.CurrentLine != null && captions.CurrentLine.Role == PresentationRole.Visitor;
            if (!captions.IsTextComplete) captionContinueText.text = "TAP TO REVEAL";
            else captionContinueText.text = visitor ? "CONTINUE TO VERDICT" : "CONTINUE";
        }

        private void SetVerdictControls(bool enabled)
        {
            // A rulebook is a modal surface, not merely a visual overlay. Keep every decision
            // route disabled even when an asynchronous caption completion arrives underneath it.
            enabled &= !rulebookOpen;
            verdictControlsEnabled = enabled;
            if (swipeCard != null) swipeCard.SetDecisionInputEnabled(enabled);
            if (verdictButtons != null)
                foreach (var button in verdictButtons)
                    if (button != null)
                    {
                        button.interactable = enabled;
                        if (button.targetGraphic != null) button.targetGraphic.raycastTarget = enabled;
                    }
            DebugTrace.Log("input.verdict_controls", "enabled=" + enabled + ";reason=" + (enabled ? "caption-complete" : "caption-active"));
        }

        private void HandleCaptionSwipe()
        {
            if (rulebookOpen) return;
            if (captions == null || !captions.IsActive || activeCaptionTarget != narrativeCaptionText) return;
            HandleCaptionContinue();
        }

        private void RefreshVerdictControls()
        {
            var canDecide = !rulebookOpen && run != null && run.State.Phase == RunPhase.Encounter && !resolving
                && run.CurrentVisitor() != null && (captions == null || !captions.IsActive);
            SetVerdictControls(canDecide);
        }

        private void ToggleReducedMotion()
        {
            SetReducedMotion(!reducedMotion);
            audioSystem.NotifyUiInteraction();
            DebugTrace.Log("accessibility.reduced_motion_toggle", "enabled=" + reducedMotion);
        }

        private void ToggleMute()
        {
            audioSystem.ToggleMute();
            if (!audioSystem.IsMuted) audioSystem.Play(AudioCueIds.UiClick);
            RefreshMuteLabel();
        }

        private void RefreshMuteLabel() => muteText.text = audioSystem.IsMuted ? "Sound Off" : "Sound On";

        private void QueueVerdictAudio(Decision decision, bool correct, int streak)
        {
            scheduledVerdictCues.Clear();
            foreach (var cue in VerdictAudioCadence.Build(decision, correct, streak))
            {
                scheduledVerdictCues.Add(new ScheduledVerdictCue(cue.CueId, Time.unscaledTime + cue.OffsetSeconds));
                DebugTrace.Log("audio.verdict_scheduled", $"cue={cue.CueId};offset={cue.OffsetSeconds:0.00}");
            }
            PlayDueVerdictAudio();
        }

        private void PlayDueVerdictAudio()
        {
            scheduledVerdictCues.Sort((left, right) => left.OffsetSeconds.CompareTo(right.OffsetSeconds));
            while (scheduledVerdictCues.Count > 0)
            {
                var cue = scheduledVerdictCues[0];
                if (cue.OffsetSeconds > Time.unscaledTime) return;
                audioSystem?.Play(cue.CueId);
                DebugTrace.Log("audio.verdict_played", "cue=" + cue.CueId);
                scheduledVerdictCues.RemoveAt(0);
            }
        }

        private static int ResolveSeed()
        {
            var argument = Environment.GetCommandLineArgs().FirstOrDefault(value => value.StartsWith("-seed=", StringComparison.Ordinal));
            if (argument != null && int.TryParse(argument.Substring(6), out var seed)) return seed;
            return 260928;
        }

        private void BindEvidenceIcons(DecisionEvidence decisionEvidence)
        {
            if (evidenceIconRoot == null) return;
            for (var index = evidenceIconRoot.childCount - 1; index >= 0; index--)
            {
                var icon = evidenceIconRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(icon);
                else DestroyImmediate(icon);
            }
            var authored = decisionEvidence.Facts.Select(VisualAssetCatalog.EvidenceKeyForAuthoredCue)
                .Where(key => !string.IsNullOrEmpty(key)).Distinct()
                // The accessible dossier always renders every fact. Decorative overlays are a
                // compact recognition aid, so retain their authored order but never turn a
                // four-fact card into a third icon that cannot fit the mobile icon lane.
                .Take(ViewportLayoutPolicy.MaximumEvidenceIconCount).ToArray();
            for (var index = 0; index < authored.Length; index++)
            {
                var icon = ImageNode("Evidence " + authored[index], evidenceIconRoot,
                    Vector2.one * ViewportLayoutPolicy.EvidenceIconLogicalSize,
                    // 120px centres retain full 116px silhouettes and leave a positive
                    // physical gutter before the dossier backing at the narrow inset.
                    new Vector2((index - (authored.Length - 1) * .5f) * 120f, 0f));
                icon.sprite = visualCatalog.EvidenceForKey(authored[index]);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                DebugTrace.Log("asset.visual_bound", $"key={authored[index]};kind=evidence;fallback=none;resolved=true");
            }
            DebugTrace.Log("integration.evidence_bound", $"icons={authored.Length};text=dossier;source=authored-facts");
        }

        private static string VerdictFeedback(VerdictRecord verdict)
        {
            if (verdict == null) return string.Empty;
            return verdict.Correct ? "Correct  +" + verdict.ScoreDelta : "Incorrect  −1 seal";
        }

        /// <summary>
        /// Every canonical portrait has a direct catalog reference and is rendered under only the
        /// code-native arch mask. This prevents a second opaque visitor-card image from hiding
        /// the portrait above its aperture.
        /// </summary>
        private void BindPortrait(string portraitKey)
        {
            var sprite = visualCatalog.PortraitFor(portraitKey);
            if (sprite == null || portraitImage == null)
                throw new InvalidOperationException("A canonical portrait could not be bound: " + portraitKey);
            if (HasOpaquePortraitOccluder())
                throw new InvalidOperationException("Portrait binding blocked by an opaque foreground overlay.");
            portraitImage.sprite = sprite;
            DebugTrace.Log("asset.portrait_visible", "key=" + portraitKey + ";mask=arch;opaqueForeground=false");
        }

        private bool HasOpaquePortraitOccluder()
        {
            if (portraitImage == null || card == null || visualCatalog == null) return true;
            var window = portraitImage.transform.parent;
            return card.GetComponentsInChildren<Image>(true).Any(image => image != portraitImage
                && image.sprite == visualCatalog.VisitorCard
                && image.transform != card.transform
                && image.transform.GetSiblingIndex() > window.GetSiblingIndex()
                && image.color.a > .99f);
        }

        private Image[] CreateIntegritySeals(Transform parent)
        {
            var root = new GameObject("Integrity Seals", typeof(RectTransform)).transform;
            root.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            ConfigureFixedRect(rect);
            rect.sizeDelta = new Vector2(ViewportLayoutPolicy.Seals.Width, ViewportLayoutPolicy.Seals.Height);
            rect.anchoredPosition = new Vector2(ViewportLayoutPolicy.Seals.X, ViewportLayoutPolicy.Seals.Y);
            var seals = new Image[5];
            for (var index = 0; index < seals.Length; index++)
            {
                seals[index] = ImageNode("Seal " + (index + 1), root, new Vector2(38f, 38f), new Vector2((index - 2) * 46f, 0f));
                seals[index].preserveAspect = true;
                seals[index].raycastTarget = false;
            }
            RefreshIntegritySeals(5);
            return seals;
        }

        private void RefreshIntegritySeals(int integrity)
        {
            if (integritySeals == null) return;
            for (var index = 0; index < integritySeals.Length; index++)
            {
                integritySeals[index].sprite = index < integrity ? visualCatalog.IntegrityIntact : visualCatalog.IntegrityBroken;
                integritySeals[index].color = Color.white;
            }
            DebugTrace.Log("integration.seals_bound", "integrity=" + integrity + ";intact=" + Mathf.Clamp(integrity, 0, 5) + ";broken=" + Mathf.Clamp(5 - integrity, 0, 5));
        }

        private void SetEncounterHudVisible(bool visible)
        {
            if (titleText != null) titleText.gameObject.SetActive(visible);
            if (statusText != null) statusText.gameObject.SetActive(visible);
            if (integritySeals != null && integritySeals.Length > 0) integritySeals[0].transform.parent.gameObject.SetActive(visible);
            if (rulebookButton != null) rulebookButton.gameObject.SetActive(visible);
        }

        private void OpenRulebook()
        {
            if (rulebookPanel == null || run == null || run.State.Phase != RunPhase.Encounter || resolving || rulebookOpen) return;
            var decree = run.CurrentDecree();
            if (decree == null) return;
            decreeText.text = decree.DisplayText;
            rulebookOpen = true;
            rulebookPanel.SetActive(true);
            SetVerdictControls(false);
            DebugTrace.Log("rulebook.opened", "day=" + decree.Day + ";state=preserved");
        }

        private void CloseRulebook()
        {
            if (!rulebookOpen) return;
            rulebookOpen = false;
            if (rulebookPanel != null) rulebookPanel.SetActive(false);
            RefreshVerdictControls();
            DebugTrace.Log("rulebook.closed", "state=preserved;verdicts=" + (run == null ? 0 : run.State.Verdicts.Count));
        }

        private void SetRulebookVisible(bool visible)
        {
            if (visible) return;
            rulebookOpen = false;
            if (rulebookPanel != null) rulebookPanel.SetActive(false);
        }

        private static Image ImageNode(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>();
            ConfigureFixedRect(rect);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return node.GetComponent<Image>();
        }

        private static DecisionTargetGlow CreateDecisionTargetGlow(string name, Transform parent, LogicalRect rect, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(DecisionTargetGlow));
            node.transform.SetParent(parent, false);
            var target = node.GetComponent<RectTransform>();
            ConfigureFixedRect(target);
            target.sizeDelta = new Vector2(rect.Width, rect.Height);
            target.anchoredPosition = new Vector2(rect.X, rect.Y);
            var glow = node.GetComponent<DecisionTargetGlow>();
            glow.Configure(color);
            return glow;
        }

        private static void ApplySliced(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }

        private static void ApplySimpleAspect(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        private void ApplyDecisionButton(Button button, Sprite sprite, bool admit)
        {
            SetButtonSurface(button, admit ? new Color(.025f, .15f, .09f, .96f) : new Color(.18f, .035f, .07f, .96f));
            InstallOutlineTarget(button, gold);
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = admit ? ParseHtml("FFF1C9") : ParseHtml("FFE0D6");
            colors.pressedColor = admit ? ParseHtml("C2E1C4") : ParseHtml("E8A8A8");
            colors.selectedColor = admit ? ParseHtml("E2F2D7") : ParseHtml("F4CCC7");
            colors.disabledColor = admit ? ParseHtml("5A6359A6") : ParseHtml("66565AA6");
            colors.fadeDuration = .08f;
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        private void ApplyPlainTextButton(Button button, Color fill, Color textColor)
        {
            SetButtonSurface(button, fill);
            InstallOutlineTarget(button, gold);
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = ParseHtml("FFF1C9");
            colors.pressedColor = ParseHtml("D9C293");
            colors.selectedColor = ParseHtml("FFF7DE");
            colors.disabledColor = ParseHtml("6A6255A6");
            colors.colorMultiplier = 1f;
            button.colors = colors;
            var caption = button.GetComponentInChildren<Text>();
            if (caption != null) caption.color = textColor;
        }

        /// <summary>
        /// The small approved button ornaments have intentionally transparent centres. A real
        /// dark, sliced panel beneath them gives text and touch targets an unambiguous surface
        /// instead of leaving labels to float over a moving castle illustration.
        /// </summary>
        private void SetButtonSurface(Button button, Color color)
        {
            var surface = button.transform.Find("Button Surface")?.GetComponent<Image>();
            if (surface == null) return;
            // The approved ornament has a transparent middle. This layer is deliberately
            // opaque so text never turns into a thin line over the environment.
            surface.sprite = null;
            surface.type = Image.Type.Simple;
            surface.preserveAspect = false;
            surface.color = color;
            surface.raycastTarget = false;
        }

        /// <summary>
        /// A normal Image fills its entire rectangle, so using it as a gold "border" hid the
        /// navy surface and produced the orange blockout seen in actual Play.  This custom
        /// Graphic is both the full-area raycast target and only a three-pixel outline.
        /// </summary>
        private static void InstallOutlineTarget(Button button, Color borderColor)
        {
            var node = button.transform.Find("Button Border");
            if (node == null) return;
            var outline = node.GetComponent<GoldBorderGraphic>();
            if (outline == null)
            {
                // Graphic is disallow-multiple-component. In Play mode Destroy(Image) is deferred,
                // so adding an outline to this exact object in the same frame fails and returns
                // null. Make a new target object first, then retire the legacy Image object.
                outline = ReplaceLegacyOutlineNode(node);
            }
            outline.Configure(borderColor);
            button.targetGraphic = outline;
            var ornament = button.transform.Find("Button Ornament");
            if (ornament != null)
            {
                if (Application.isPlaying) Destroy(ornament.gameObject);
                else DestroyImmediate(ornament.gameObject);
            }
        }

        private static GoldBorderGraphic ReplaceLegacyOutlineNode(Transform legacyNode)
        {
            var legacyRect = legacyNode.GetComponent<RectTransform>();
            var replacement = new GameObject("Button Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(GoldBorderGraphic));
            replacement.transform.SetParent(legacyNode.parent, false);
            var replacementRect = replacement.GetComponent<RectTransform>();
            replacementRect.anchorMin = legacyRect.anchorMin;
            replacementRect.anchorMax = legacyRect.anchorMax;
            replacementRect.pivot = legacyRect.pivot;
            replacementRect.anchoredPosition = legacyRect.anchoredPosition;
            replacementRect.sizeDelta = legacyRect.sizeDelta;
            replacementRect.localScale = legacyRect.localScale;
            replacementRect.SetSiblingIndex(legacyNode.GetSiblingIndex());
            legacyNode.name = "Retired Button Border";
            if (Application.isPlaying) Destroy(legacyNode.gameObject);
            else DestroyImmediate(legacyNode.gameObject);
            return replacement.GetComponent<GoldBorderGraphic>();
        }

        private static Color ParseHtml(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }

        private void EnsureCameraAudioListener()
        {
            if (FindAnyObjectByType<AudioListener>() != null) return;
            var listener = new GameObject("Gatehouse Audio Listener", typeof(Camera), typeof(AudioListener));
            listener.transform.SetParent(transform, false);
            var camera = listener.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Depth;
            camera.cullingMask = 0;
            DebugTrace.Log("audio.listener_ready", "created=true;camera=Gatehouse Audio Listener");
        }

        private GameObject Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            ConfigureFixedRect(rect);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = panel.GetComponent<Image>();
            image.color = color;
            // All player-facing panels use a real approved sliced asset. Specific surfaces can
            // still replace it with their semantic card/parchment/button art immediately after.
            if (visualCatalog != null && visualCatalog.DayEndingPanel != null) ApplySliced(image, visualCatalog.DayEndingPanel);
            return panel;
        }

        private GameObject CreateFilledBorderPanel(string name, Transform parent, Vector2 size, Vector2 position, Color fill, Color border)
        {
            var panel = Panel(name, parent, size, position, border);
            SetFilledBorderPanel(panel, fill, border);
            return panel;
        }

        private void SetFilledBorderPanel(GameObject panel, Color fill, Color border)
        {
            var outer = panel.GetComponent<Image>();
            outer.sprite = null;
            outer.type = Image.Type.Simple;
            outer.color = border;
            outer.raycastTarget = false;
            var surface = panel.transform.Find("Panel Surface")?.GetComponent<Image>();
            if (surface == null)
                surface = ImageNode("Panel Surface", panel.transform,
                    panel.GetComponent<RectTransform>().sizeDelta - new Vector2(6f, 6f), Vector2.zero);
            surface.sprite = null;
            surface.type = Image.Type.Simple;
            surface.color = fill;
            surface.raycastTarget = false;
            surface.transform.SetAsFirstSibling();
        }

        private GameObject FullStretchPanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color, Vector2 dimensions, Vector2 position, TextAnchor alignment, FontStyle style = FontStyle.Normal, bool displayRole = false, bool developerDiagnostic = false)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            ConfigureFixedRect(rect);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            var label = labelObject.GetComponent<Text>();
            label.font = displayRole ? visualCatalog.DisplayFont : visualCatalog.BodyFont;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            // Diagnostic trace is not player-facing copy. It deliberately keeps its compact
            // developer font while every player-visible label retains the 32px logical floor.
            label.resizeTextMinSize = developerDiagnostic ? size : ViewportLayoutPolicy.MinimumBodyLogicalFontSize;
            label.resizeTextMaxSize = developerDiagnostic ? size : Mathf.Max(size, ViewportLayoutPolicy.MinimumBodyLogicalFontSize);
            label.lineSpacing = .86f;
            return label;
        }

        private static void ConfigureFixedRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
        }

        private Button CreateButton(string name, Transform parent, string text, Color color, Vector2 position, UnityEngine.Events.UnityAction action, Vector2? dimensions = null, int fontSize = 25, bool displayRole = false, bool withOrnament = true)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            ConfigureFixedRect(buttonRect);
            buttonRect.sizeDelta = dimensions ?? new Vector2(270f, 82f);
            buttonRect.anchoredPosition = position;
            var button = buttonObject.AddComponent<Button>();
            var surface = ImageNode("Button Surface", buttonObject.transform, buttonRect.sizeDelta - new Vector2(6f, 6f), Vector2.zero);
            surface.sprite = null;
            surface.type = Image.Type.Simple;
            surface.color = color;
            surface.raycastTarget = false;
            var borderObject = new GameObject("Button Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(GoldBorderGraphic));
            borderObject.transform.SetParent(buttonObject.transform, false);
            var borderRect = borderObject.GetComponent<RectTransform>();
            ConfigureFixedRect(borderRect);
            borderRect.sizeDelta = buttonRect.sizeDelta;
            borderRect.anchoredPosition = Vector2.zero;
            var border = borderObject.GetComponent<GoldBorderGraphic>();
            border.Configure(gold);
            button.targetGraphic = border;
            if (withOrnament)
            {
                var ornament = ImageNode("Button Ornament", buttonObject.transform, buttonRect.sizeDelta, Vector2.zero);
                ornament.raycastTarget = true;
                button.targetGraphic = ornament;
            }
            button.onClick.AddListener(action);
            var caption = Label("Caption", buttonObject.transform, text, fontSize, Color.white, buttonRect.sizeDelta - new Vector2(20f, 12f), Vector2.zero, TextAnchor.MiddleCenter, FontStyle.Bold, displayRole);
            caption.raycastTarget = false;
            return button;
        }
    }
}
