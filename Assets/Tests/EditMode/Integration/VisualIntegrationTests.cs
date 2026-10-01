using System.Linq;
using System.IO;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WhoEnters.Audio;
using WhoEnters.Content;
using WhoEnters.Gameplay;
using WhoEnters.Integration;
using WhoEnters.UI;
using WhoEnters.Presentation;
using WhoEnters.Core;

namespace WhoEnters.Tests.EditMode.Integration
{
    public sealed class VisualIntegrationTests
    {
        [Test]
        public void SerializedCatalogContainsEveryApprovedReferenceExactlyOnce()
        {
            var catalog = Resources.Load<VisualAssetCatalog>(VisualAssetCatalog.ResourcePath);
            Assert.That(catalog, Is.Not.Null);
            Assert.DoesNotThrow(catalog.ValidateOrThrow);
            Assert.That(catalog.Portraits.Select(item => item.Key).OrderBy(key => key),
                Is.EqualTo(StoryContent.CanonicalPortraitKeys.OrderBy(key => key)));
            Assert.That(catalog.Portraits.Count, Is.EqualTo(16));
            Assert.That(catalog.Evidence.Count, Is.EqualTo(6));
            Assert.That(catalog.AllSprites().Distinct().Count(), Is.EqualTo(36));
            Assert.That(catalog.AllSprites().All(sprite => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sprite))), Is.True);
        }

        [Test]
        public void CatalogUsesApprovedRuntimeLocationsWithoutFallbacksOrPublicMirrors()
        {
            var catalog = VisualAssetCatalog.LoadRequired();
            foreach (var sprite in catalog.AllSprites())
            {
                var path = AssetDatabase.GetAssetPath(sprite);
                Assert.That(path.StartsWith("Assets/Art/"), Is.True, path);
                Assert.That(path.Contains("public/"), Is.False, path);
            }
            foreach (var key in StoryContent.CanonicalPortraitKeys)
                Assert.That(catalog.PortraitFor(key), Is.Not.Null, key);
        }

        [Test]
        public void EnvironmentOrderAndLogicalMotionContractsAreExplicit()
        {
            var root = new GameObject("Environment Test", typeof(RectTransform));
            try
            {
                var rig = GatehouseEnvironmentRig.Create(root.transform, VisualAssetCatalog.LoadRequired());
                var names = rig.transform.Cast<Transform>().Select(child => child.name).ToArray();
                Assert.That(names, Is.EqualTo(new[]
                {
                    "01 Castle Vista [order 0]", "02 Gate Frame [order 10]", "03 Fog Rain [order 20]",
                    "04 Torches Glow [order 30]", "05 Foreground Silhouettes [order 40]",
                }));
                Assert.That(GatehouseEnvironmentRig.VistaParallaxAmplitude, Is.EqualTo(3f));
                Assert.That(GatehouseEnvironmentRig.FogDriftAmplitude, Is.EqualTo(8f));
                Assert.That(GatehouseEnvironmentRig.ForegroundParallaxAmplitude, Is.EqualTo(12f));
            Assert.That(GatehouseEnvironmentRig.TorchMinimumAlpha, Is.EqualTo(.95f));
            Assert.That(GatehouseEnvironmentRig.TorchMaximumAlpha, Is.EqualTo(1f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CardAndVerdictMotionHaveVerifiedEndpointsAndDurations()
        {
            Assert.That(SwipeCard.DistanceThreshold, Is.EqualTo(158f));
            Assert.That(SwipeCard.VelocityThreshold, Is.EqualTo(1100f));
            Assert.That(SwipeCard.SnapbackDurationSeconds, Is.EqualTo(.18f));
            Assert.That(SwipeCard.CommitDurationSeconds, Is.EqualTo(.26f));
            Assert.That(SwipeCard.CommitEndpointX, Is.EqualTo(860f));
            Assert.That(FeedbackMotion.StampDurationSeconds, Is.EqualTo(.38f));
            Assert.That(FeedbackMotion.StampStartScale, Is.EqualTo(1.45f));
            Assert.That(FeedbackMotion.StampEndScale, Is.EqualTo(1f));
            Assert.That(FeedbackMotion.DamageShakeDurationSeconds, Is.EqualTo(.22f));
            Assert.That(FeedbackMotion.DamageShakeAmplitude, Is.EqualTo(13f));
            Assert.That(DecisionButtonFeedback.PressScale, Is.EqualTo(.97f));
            Assert.That(DecisionButtonFeedback.ReturnDurationSeconds, Is.EqualTo(.08f));
        }

        [Test]
        public void RuntimeUsesStoryContentAndBindsProductionPresentationAssets()
        {
            var root = new GameObject("Integration Director Test");
            try
            {
                root.AddComponent<GameAudio>();
                var director = root.AddComponent<GameDirector>();
                typeof(GameDirector).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
                var content = (WhoEnters.Core.GameContent)Field(director, "content");
                var portrait = (Image)Field(director, "portraitImage");
                var card = (GameObject)Field(director, "card");
                var seals = (Image[])Field(director, "integritySeals");
                Assert.That(content.Visitors.Count, Is.GreaterThanOrEqualTo(40));
                Assert.That(content.Visitors.All(visitor => StoryContent.CanonicalPortraitKeys.Contains(visitor.PortraitKey)), Is.True);
                Assert.That(portrait, Is.Not.Null);
                Assert.That(card.GetComponent<Image>().type, Is.EqualTo(Image.Type.Simple));
                Assert.That(card.GetComponent<Image>().preserveAspect, Is.True);
                var aperture = portrait.transform.parent;
                Assert.That(aperture.GetComponent<PortraitFrameGraphic>(), Is.Not.Null, "the arch is code-native and filled");
                Assert.That(aperture.GetComponent<Mask>().showMaskGraphic, Is.False);
                Assert.That(portrait.rectTransform.sizeDelta.x / portrait.rectTransform.sizeDelta.y, Is.EqualTo(2f / 3f).Within(.0001f));
                Assert.That(portrait.rectTransform.anchorMin, Is.EqualTo(new Vector2(.5f, 0f)));
                Assert.That(portrait.rectTransform.anchorMax, Is.EqualTo(new Vector2(.5f, 0f)));
                Assert.That(portrait.rectTransform.pivot.y, Is.EqualTo(0f));
                Assert.That(portrait.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(card.GetComponentsInChildren<Image>(true).Count(image => image.sprite == VisualAssetCatalog.LoadRequired().VisitorCard), Is.EqualTo(1),
                    "one base visitor-card raster belongs below the portrait; a second would be an opaque occluder");
                Assert.That(card.GetComponentsInChildren<Image>(true).Any(image => image.name == "Visitor Card Ornate Foreground"), Is.False,
                    "the opaque card raster must never be redrawn above the portrait aperture");
                foreach (var key in StoryContent.CanonicalPortraitKeys)
                {
                    Invoke(director, "BindPortrait", key);
                    Assert.That(portrait.sprite, Is.SameAs(VisualAssetCatalog.LoadRequired().PortraitFor(key)), key);
                    AssertPortraitCoversApertureFromItsBottom(portrait.rectTransform, aperture.GetComponent<RectTransform>(), key);
                }
                Assert.That(seals.Length, Is.EqualTo(5));
                Assert.That(seals.All(seal => seal.sprite != null), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AudioPaletteAndEvidenceBindingsCoverIntegratedEvents()
        {
            var expectedCues = new[]
            {
                AudioCueIds.DayTransition, AudioCueIds.GateOpen, AudioCueIds.CardPickup, AudioCueIds.CardDrag,
                AudioCueIds.CardSnapback, AudioCueIds.VerdictAdmit, AudioCueIds.VerdictDeny, AudioCueIds.VerdictStamp,
                AudioCueIds.IntegrityCrack, AudioCueIds.StreakChime, AudioCueIds.EndingFallen, AudioCueIds.EndingHollow,
                AudioCueIds.EndingHeld, AudioCueIds.EndingSecret, AudioCueIds.UiClick,
            };
            Assert.That(expectedCues.All(id => AudioCueCatalog.TryGet(id, out _)), Is.True);
            Assert.That(VisualAssetCatalog.EvidenceKeyForAuthoredCue("moon seal"), Is.EqualTo("moon-seal"));
            Assert.That(VisualAssetCatalog.EvidenceKeyForAuthoredCue("cult sigil"), Is.EqualTo("traitor-mark"));
            Assert.That(VisualAssetCatalog.EvidenceKeyForAuthoredCue("not commissioned"), Is.Empty);
        }

        [Test]
        public void EverySupportedAuthoredCueAndNormalizedKeyResolvesTheExactCatalogSprite()
        {
            var catalog = VisualAssetCatalog.LoadRequired();
            var cues = new[]
            {
                new[] { "moon seal", "moon-seal" }, new[] { "forged seal", "forged-seal" },
                new[] { "healer writ", "healer-writ-kit" }, new[] { "healer kit", "healer-writ-kit" },
                new[] { "royal counterseal", "royal-counterseal" }, new[] { "red lantern", "red-lantern" },
                new[] { "cult sigil", "traitor-mark" }, new[] { "traitor mark", "traitor-mark" },
            };
            foreach (var pair in cues)
            {
                var authored = catalog.EvidenceForAuthoredCue(pair[0]);
                var normalized = catalog.EvidenceForKey(pair[1]);
                Assert.That(authored, Is.Not.Null, pair[0]);
                Assert.That(normalized, Is.Not.Null, pair[1]);
                Assert.That(authored, Is.SameAs(normalized), pair[0]);
            }
        }

        [Test]
        public void MobileViewportMatrixPreservesSafeAreasTouchTargetsAndSeparatedHudZones()
        {
            var samples = new[]
            {
                new { Screen = new Vector2(360f, 640f), Safe = new Rect(0f, 0f, 360f, 640f) },
                new { Screen = new Vector2(390f, 844f), Safe = new Rect(0f, 30f, 390f, 780f) },
                new { Screen = new Vector2(720f, 1280f), Safe = new Rect(0f, 0f, 720f, 1280f) },
            };
            foreach (var sample in samples)
            {
                var safe = ViewportLayoutPolicy.NormalizedSafeArea(sample.Safe, sample.Screen);
                Assert.That(safe.xMin, Is.GreaterThanOrEqualTo(0f)); Assert.That(safe.yMin, Is.GreaterThanOrEqualTo(0f));
                Assert.That(safe.xMax, Is.LessThanOrEqualTo(1f)); Assert.That(safe.yMax, Is.LessThanOrEqualTo(1f));
                var cover = ViewportLayoutPolicy.CoverScale(sample.Screen, new Vector2(9f, 16f));
                Assert.That(cover.x / cover.y, Is.EqualTo(9f / 16f).Within(.0001f));
                Assert.That(cover.x, Is.GreaterThanOrEqualTo(sample.Screen.x)); Assert.That(cover.y, Is.GreaterThanOrEqualTo(sample.Screen.y));
            }
            Assert.That(ViewportLayoutPolicy.Admit.Height, Is.GreaterThanOrEqualTo(ViewportLayoutPolicy.MinimumTouchLogicalHeight));
            Assert.That(ViewportLayoutPolicy.Deny.Height, Is.GreaterThanOrEqualTo(ViewportLayoutPolicy.MinimumTouchLogicalHeight));
            Assert.That(ViewportLayoutPolicy.Admit.X, Is.GreaterThan(0f), "admit is the right-hand control");
            Assert.That(ViewportLayoutPolicy.Deny.X, Is.LessThan(0f), "deny is the left-hand control");
            Assert.That(ViewportLayoutPolicy.MinimumBodyLogicalFontSize, Is.GreaterThanOrEqualTo(32));
            Assert.That(ViewportLayoutPolicy.MinimumBodyLogicalFontSize * ViewportLayoutPolicy.EvidencePhysicalScaleAtInset360,
                Is.GreaterThanOrEqualTo(14f));
            Assert.That(ViewportLayoutPolicy.Title.Overlaps(ViewportLayoutPolicy.Status), Is.False);
            Assert.That(ViewportLayoutPolicy.Status.Overlaps(ViewportLayoutPolicy.Seals), Is.False);
            Assert.That(ViewportLayoutPolicy.Seals.Overlaps(ViewportLayoutPolicy.Rulebook), Is.False);
            Assert.That(ViewportLayoutPolicy.Rulebook.Overlaps(ViewportLayoutPolicy.Feedback), Is.False);
            Assert.That(ViewportLayoutPolicy.Feedback.Overlaps(ViewportLayoutPolicy.Card), Is.False);
            Assert.That(ViewportLayoutPolicy.Card.Overlaps(ViewportLayoutPolicy.Caption), Is.False);
            Assert.That(ViewportLayoutPolicy.Caption.Overlaps(ViewportLayoutPolicy.Admit), Is.False);
            Assert.That(ViewportLayoutPolicy.Caption.Overlaps(ViewportLayoutPolicy.Deny), Is.False);
        }

        [Test]
        public void SafeAreaRootAndEnvironmentUseFullStretchAndCoverWithoutRasterDistortion()
        {
            var root = new GameObject("Safe Root Test", typeof(RectTransform), typeof(SafeAreaLayoutRoot));
            try
            {
                var layout = root.GetComponent<SafeAreaLayoutRoot>();
                layout.Configure(root.GetComponent<RectTransform>());
                layout.Apply(new Rect(0f, 30f, 390f, 780f), new Vector2(390f, 844f));
                var rect = root.GetComponent<RectTransform>();
                Assert.That(rect.anchorMin.x, Is.EqualTo(0f)); Assert.That(rect.anchorMin.y, Is.EqualTo(30f / 844f).Within(.0001f));
                Assert.That(rect.anchorMax.x, Is.EqualTo(1f)); Assert.That(rect.anchorMax.y, Is.EqualTo(810f / 844f).Within(.0001f));
                var environmentHost = new GameObject("Canvas Host", typeof(RectTransform));
                environmentHost.transform.SetParent(root.transform, false);
                var rig = GatehouseEnvironmentRig.Create(environmentHost.transform, VisualAssetCatalog.LoadRequired());
                var rigRect = rig.GetComponent<RectTransform>();
                Assert.That(rigRect.anchorMin.x, Is.EqualTo(0f)); Assert.That(rigRect.anchorMin.y, Is.EqualTo(0f));
                Assert.That(rigRect.anchorMax.x, Is.EqualTo(1f)); Assert.That(rigRect.anchorMax.y, Is.EqualTo(1f));
                foreach (var image in rig.GetComponentsInChildren<Image>()) Assert.That(image.preserveAspect, Is.True);
                Object.DestroyImmediate(environmentHost);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DirectorGatesVerdictsForVisitorCaptionsAndKeepsStampOffTheExitingCard()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var run = (WhoEnters.Core.RunStateMachine)Field(director, "run");
                run.StartRun();
                var card = (GameObject)Field(director, "card");
                var buttons = (GameObject)Field(director, "decisionButtons");
                var overlay = (GameObject)Field(director, "overlay");
                overlay.SetActive(false); card.SetActive(true); buttons.SetActive(true);
                Invoke(director, "RenderEncounter");
                Assert.That(director.IsDecisionInputAvailable, Is.False);
                Assert.That(((Button[])Field(director, "verdictButtons")).All(button => !button.interactable), Is.True);
                Assert.That(((Button[])Field(director, "verdictButtons")).All(button => !button.targetGraphic.raycastTarget), Is.True);
                var captions = (CaptionSequenceController)Field(director, "captions");
                captions.HandleAction(); captions.HandleAction();
                Assert.That(director.IsDecisionInputAvailable, Is.True);
                Assert.That(((GameObject)Field(director, "captionPanel")).activeSelf, Is.False, "completed visitor copy releases its large reading lane");
                Assert.That(((Button[])Field(director, "verdictButtons")).All(button => button.targetGraphic.raycastTarget), Is.True);
                Assert.That(((Image)Field(director, "verdictStampImage")).transform.parent.name, Is.EqualTo("Verdict Feedback HUD"));
                var feedback = (FeedbackMotion)Field(director, "feedbackMotion");
                Assert.That(Field(feedback, "shakeTarget"), Is.Not.SameAs(card.GetComponent<RectTransform>()));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ConsumedCaptionSwipeResetsCardAndReducedMotionDisablesEveryAnimatedSubsystem()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var run = (WhoEnters.Core.RunStateMachine)Field(director, "run"); run.StartRun();
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter");
                var swipe = (SwipeCard)Field(director, "swipeCard");
                var cardRect = ((GameObject)Field(director, "card")).GetComponent<RectTransform>();
                var home = cardRect.anchoredPosition;
                cardRect.anchoredPosition = new Vector2(201f, 0f);
                Invoke(director, "Submit", Decision.Deny, "swipe");
                Assert.That(cardRect.anchoredPosition.x, Is.EqualTo(home.x)); Assert.That(cardRect.anchoredPosition.y, Is.EqualTo(home.y));
                Assert.That(cardRect.localRotation.eulerAngles.z, Is.EqualTo(0f).Within(.001f));
                director.SetReducedMotion(true);
                swipe.PlayCommitted(Decision.Admit);
                Assert.That(cardRect.anchoredPosition.x, Is.EqualTo(SwipeCard.CommitEndpointX));
                Assert.That(cardRect.localRotation.eulerAngles.z, Is.EqualTo(0f).Within(.001f));
                Assert.That((bool)Field((GatehouseEnvironmentRig)Field(director, "environmentRig"), "reducedMotion"), Is.True);
                Assert.That((bool)Field((FeedbackMotion)Field(director, "feedbackMotion"), "reducedMotion"), Is.True);
                Assert.That((bool)Field(swipe, "reducedMotion"), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void AuthoredTextUsesBoundedWrappingAndFitsItsAssignedLogicalSurface()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var content = (WhoEnters.Core.GameContent)Field(director, "content");
                var decree = (Text)Field(director, "decreeText");
                var decreeHeading = (Text)Field(director, "decreeHeadingText");
                var dialogue = (Text)Field(director, "dialogueText");
                var evidence = (Text)Field(director, "evidenceText");
                var caption = (Text)Field(director, "narrativeCaptionText");
                foreach (var target in new[] { decree, dialogue, evidence, caption })
                {
                    Assert.That(target.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                    Assert.That(target.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
                    Assert.That(target.resizeTextForBestFit, Is.True);
                    Assert.That(target.resizeTextMinSize, Is.GreaterThanOrEqualTo(ViewportLayoutPolicy.MinimumBodyLogicalFontSize));
                }
                AssertFits(decreeHeading, content.Decrees.Select(item => item.Title.ToUpperInvariant()));
                AssertFits(decree, content.Decrees.Select(item => item.DisplayText));
                AssertFits(dialogue, content.Visitors.Select(visitor => "“" + visitor.Dialogue + "”"));
                AssertMaximumFits(evidence, content.Visitors.Select(visitor => DecisionEvidence.Resolve(visitor,
                    content.Decrees.Single(decreeItem => decreeItem.Day == visitor.Day)).Text), "all authored dossiers");
                AssertFits(caption, content.Visitors.Select(visitor => StoryCaptionCatalog.Visitor(visitor).Lines[0].Text));
                AssertFits(caption, StoryContent.EpilogueInputs.Values);
                AssertFits(caption, content.Visitors.SelectMany(visitor => new[]
                {
                    StoryCaptionCatalog.Verdict(visitor, new VerdictRecord { Chosen = Decision.Admit, Correct = true }, new GameState()).Lines[0].Text,
                    StoryCaptionCatalog.Verdict(visitor, new VerdictRecord { Chosen = Decision.Deny, Correct = false }, new GameState()).Lines[0].Text,
                }));

                var motion = (Text)Field(director, "reducedMotionText");
                Assert.That(motion.resizeTextMinSize, Is.GreaterThanOrEqualTo(ViewportLayoutPolicy.MinimumBodyLogicalFontSize));
                AssertFits(motion, new[] { "Motion On", "Motion Off" });
                var sound = (Text)Field(director, "muteText");
                Assert.That(sound.resizeTextMinSize, Is.GreaterThanOrEqualTo(ViewportLayoutPolicy.MinimumBodyLogicalFontSize));
                AssertFits(sound, new[] { "Sound On", "Sound Off" });

                var overlayBody = (Text)Field(director, "overlayText");
                var summaryStates = Enumerable.Range(1, content.TotalDays).Select(day => new GameState
                {
                    Day = day, Score = day * 800, Integrity = 5 - (day % 3),
                });
                AssertFits(overlayBody, summaryStates.Select(state => StoryCaptionCatalog.DaySummary(state).Lines[0].Text));
                foreach (EndingKind ending in System.Enum.GetValues(typeof(EndingKind)))
                {
                    var endingState = new GameState { Day = 5, Score = 4000, Integrity = 3 };
                    endingState.Verdicts.AddRange(Enumerable.Repeat(new VerdictRecord(), 40));
                    foreach (var terminalFlag in StoryContent.TerminalOutcomeFlags) endingState.StoryFlags.Add(terminalFlag);
                    AssertFits(overlayBody, StoryCaptionCatalog.Ending(ending, endingState).Lines.Select(line => line.Text));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FullCategorizedDossierContainsOnlyStableAuthoredFactsAndReproducesAllVerdicts()
        {
            var content = StoryContent.Create();
            foreach (var visitor in content.Visitors)
            {
                var decree = content.Decrees.Single(item => item.Day == visitor.Day);
                var dossier = DecisionEvidence.Resolve(visitor, decree);
                var expectedFacts = visitor.Documents.Concat(visitor.VisibleCues).Concat(visitor.Traits)
                    .Distinct(System.StringComparer.Ordinal).ToArray();
                Assert.That(dossier.Documents, Is.EqualTo(visitor.Documents), visitor.Id + " documents");
                Assert.That(dossier.VisibleSigns, Is.EqualTo(visitor.VisibleCues), visitor.Id + " visible signs");
                Assert.That(dossier.Traits, Is.EqualTo(visitor.Traits), visitor.Id + " traits");
                Assert.That(dossier.Facts, Is.EqualTo(expectedFacts), visitor.Id);
                Assert.That(dossier.Facts.Length, Is.InRange(1, 4), visitor.Id);
                Assert.That(dossier.Text, Does.Not.Contain("ADMIT").And.Not.Contain("DENY").And.Not.Contain("rule"), visitor.Id);
                Assert.That(RuleEvaluator.Evaluate(dossier.ProjectedVisitor(visitor.Id + "-dossier"), decree).Expected,
                    Is.EqualTo(dossier.Evaluation.Expected), visitor.Id);
                Assert.That(RuleEvaluator.Evaluate(dossier.ProjectedVisitor(visitor.Id + "-dossier"), decree).RuleId,
                    Is.EqualTo(dossier.Evaluation.RuleId), visitor.Id);
            }
        }

        [Test]
        public void EncounterDossierDiagnosticAndVerdictExplanationStayStructuredAndPostChoiceOnly()
        {
            DebugTrace.Clear();
            var root = ConstructDirector(out var director);
            try
            {
                var run = (RunStateMachine)Field(director, "run");
                run.StartRun();
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter");
                var visitor = run.CurrentVisitor();
                var dossierEvent = DebugTrace.Recent.Last(item => item.EventId == "integration.dossier");
                var dossier = DecisionEvidence.Resolve(visitor, run.CurrentDecree());
                Assert.That(dossierEvent.Payload, Does.Contain("visitor=" + visitor.Id)
                    .And.Contain("documents=").And.Contain("visibleSigns=").And.Contain("traits=")
                    .And.Contain("uniqueFacts=" + dossier.Facts.Length).And.Contain("order=documents,visibleSigns,traits"));
                Assert.That(((Text)Field(director, "evidenceText")).text, Is.EqualTo(dossier.Text));
                Assert.That(((Text)Field(director, "evidenceText")).text, Does.Not.Contain("ADMIT").And.Not.Contain("DENY"));

                var captions = (CaptionSequenceController)Field(director, "captions");
                captions.HandleAction(); captions.HandleAction();
                Invoke(director, "Submit", Decision.Admit, "test");
                var record = run.State.Verdicts.Last();
                Assert.That(((Text)Field(director, "feedbackText")).text, Does.Not.Contain(record.RuleExplanation));
                Assert.That(StoryCaptionCatalog.Verdict(visitor, record, run.State).Lines.First().Text,
                    Does.Not.Contain(record.RuleId).And.Not.Contain(record.RuleExplanation));
                var verdictEvent = DebugTrace.Recent.Last(item => item.EventId == "integration.verdict_explanation");
                Assert.That(verdictEvent.Payload, Does.Contain("rule=" + record.RuleId).And.Contain("explanation=" + record.RuleExplanation));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CaptionContinuationCompletesARealVerdictAndAdvancesTheRunWithoutPrivateContinuationCalls()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var run = (WhoEnters.Core.RunStateMachine)Field(director, "run"); run.StartRun();
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter"); // setup only; user actions below are real Button events.
                var continueButton = (Button)Field(director, "captionContinueButton");
                continueButton.onClick.Invoke(); // reveal visitor typewriter
                continueButton.onClick.Invoke(); // leave visitor caption and enable verdicts
                var decision = ((Button[])Field(director, "verdictButtons"))[0];
                Assert.That(decision.interactable, Is.True);
                decision.onClick.Invoke();
                Assert.That((bool)Field(director, "resolving"), Is.True);
                Assert.That(continueButton.gameObject.activeSelf, Is.True, "verdict narration needs its own non-decision continuation surface");
                var verdictActions = 0;
                while ((bool)Field(director, "resolving") && verdictActions++ < 8)
                    continueButton.onClick.Invoke(); // reveal/dismiss result, then provenance, then any story consequence
                Assert.That(run.State.Verdicts.Count, Is.EqualTo(1));
                Assert.That((bool)Field(director, "resolving"), Is.False);
                Assert.That(verdictActions, Is.GreaterThanOrEqualTo(2), "one concise result line retains reveal then continue actions");
                Assert.That(verdictActions, Is.LessThan(8), "caption continuation remains bounded");
                Assert.That(run.State.Phase, Is.EqualTo(WhoEnters.Core.RunPhase.Encounter));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ExactEnvironmentTintAndUiSiblingOrderIsEnforced()
        {
            var root = ConstructDirector(out _);
            try
            {
                var canvas = root.GetComponentInChildren<Canvas>();
                var names = canvas.transform.Cast<Transform>().Select(child => child.name).ToArray();
                var environment = System.Array.IndexOf(names, "Gatehouse Environment");
                var tint = System.Array.IndexOf(names, "Night Tint");
                var safeUi = System.Array.IndexOf(names, "Safe Area UI");
                Assert.That(environment, Is.GreaterThanOrEqualTo(0));
                Assert.That(tint, Is.EqualTo(environment + 1));
                Assert.That(safeUi, Is.GreaterThan(tint));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SafeViewportRectsStayInsideInsetsAndKeepEveryControlAtLeastFortyFourPhysicalPixels()
        {
            var samples = new[]
            {
                new { Screen = new Vector2(360f, 640f), Safe = new Rect(18f, 28f, 324f, 584f) },
                new { Screen = new Vector2(720f, 1280f), Safe = new Rect(36f, 54f, 648f, 1172f) },
            };
            foreach (var sample in samples)
            {
                foreach (var rect in new[] { ViewportLayoutPolicy.Title, ViewportLayoutPolicy.Status, ViewportLayoutPolicy.Seals, ViewportLayoutPolicy.Rulebook, ViewportLayoutPolicy.RulebookPanel, ViewportLayoutPolicy.Feedback, ViewportLayoutPolicy.Card, ViewportLayoutPolicy.DenyPreviewGlow, ViewportLayoutPolicy.AdmitPreviewGlow, ViewportLayoutPolicy.Caption, ViewportLayoutPolicy.Admit, ViewportLayoutPolicy.Deny, ViewportLayoutPolicy.SoundUtility, ViewportLayoutPolicy.MotionUtility })
                    Assert.That(ViewportLayoutPolicy.Contains(sample.Safe, ViewportLayoutPolicy.PhysicalRect(rect, sample.Safe, sample.Screen)), Is.True, rect.Y.ToString());
                var scale = ViewportLayoutPolicy.SafeContentScale(sample.Safe, sample.Screen);
                foreach (var logicalHeight in new[] { 112f, ViewportLayoutPolicy.Admit.Height, ViewportLayoutPolicy.Deny.Height })
                    Assert.That(logicalHeight * scale, Is.GreaterThanOrEqualTo(44f));
            }
        }

        [Test]
        public void PlayerFacingPostLayoutZonesStaySeparatedAcrossTheThreeSupportedPhoneViewports()
        {
            var samples = new[]
            {
                new { Screen = new Vector2(360f, 640f), Safe = new Rect(18f, 28f, 324f, 584f) },
                new { Screen = new Vector2(390f, 844f), Safe = new Rect(0f, 24f, 390f, 796f) },
                new { Screen = new Vector2(720f, 1280f), Safe = new Rect(36f, 54f, 648f, 1172f) },
            };
            foreach (var sample in samples)
            {
                var rulebook = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Rulebook, sample.Safe, sample.Screen);
                var feedback = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Feedback, sample.Safe, sample.Screen);
                var card = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Card, sample.Safe, sample.Screen);
                var denyPreview = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.DenyPreviewGlow, sample.Safe, sample.Screen);
                var admitPreview = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.AdmitPreviewGlow, sample.Safe, sample.Screen);
                var caption = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Caption, sample.Safe, sample.Screen);
                var deny = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Deny, sample.Safe, sample.Screen);
                var admit = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Admit, sample.Safe, sample.Screen);
                Assert.That(rulebook.Overlaps(feedback), Is.False, "rulebook/feedback " + sample.Screen);
                Assert.That(feedback.Overlaps(card), Is.False, "feedback/card " + sample.Screen);
                Assert.That(denyPreview.Overlaps(card), Is.False, "deny glow/card " + sample.Screen);
                Assert.That(admitPreview.Overlaps(card), Is.False, "admit glow/card " + sample.Screen);
                Assert.That(denyPreview.Overlaps(caption), Is.False, "deny glow/caption " + sample.Screen);
                Assert.That(admitPreview.Overlaps(caption), Is.False, "admit glow/caption " + sample.Screen);
                Assert.That(denyPreview.Overlaps(deny), Is.True, "deny glow must sit behind its target " + sample.Screen);
                Assert.That(admitPreview.Overlaps(admit), Is.True, "admit glow must sit behind its target " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.Contains(sample.Safe, denyPreview), Is.True, "deny glow contained " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.Contains(sample.Safe, admitPreview), Is.True, "admit glow contained " + sample.Screen);
                Assert.That(card.Overlaps(caption), Is.False, "card/caption " + sample.Screen);
                Assert.That(caption.Overlaps(deny), Is.False, "caption/deny " + sample.Screen);
                Assert.That(caption.Overlaps(admit), Is.False, "caption/admit " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.Contains(sample.Safe, card), Is.True, "card contained " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.Admit.Height * ViewportLayoutPolicy.SafeContentScale(sample.Safe, sample.Screen), Is.GreaterThanOrEqualTo(44f));
                Assert.That(100f * ViewportLayoutPolicy.SafeContentScale(sample.Safe, sample.Screen), Is.GreaterThanOrEqualTo(44f), "caption CTA " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.UtilityHeight * ViewportLayoutPolicy.SafeContentScale(sample.Safe, sample.Screen), Is.GreaterThanOrEqualTo(44f), "utility target " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.MotionUtility, sample.Safe, sample.Screen).Overlaps(ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Title, sample.Safe, sample.Screen)), Is.False, "motion/title hierarchy " + sample.Screen);
                Assert.That(ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.SoundUtility, sample.Safe, sample.Screen).Overlaps(ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.Title, sample.Safe, sample.Screen)), Is.False, "sound/title hierarchy " + sample.Screen);
            }

            // Asset-content safe zones in the actual 1536x1024 visitor-card raster.
            Assert.That(ViewportLayoutPolicy.VisitorName.Right, Is.LessThanOrEqualTo(240f));
            Assert.That(ViewportLayoutPolicy.Dialogue.Right, Is.LessThanOrEqualTo(240f));
            Assert.That(ViewportLayoutPolicy.Evidence.Right, Is.LessThanOrEqualTo(240f));
            Assert.That(ViewportLayoutPolicy.VisitorName.Bottom, Is.GreaterThan(ViewportLayoutPolicy.Dialogue.Top));
            Assert.That(ViewportLayoutPolicy.Dialogue.Bottom, Is.GreaterThan(ViewportLayoutPolicy.Evidence.Top));
            Assert.That(ViewportLayoutPolicy.EvidenceIcons.Right, Is.LessThan(ViewportLayoutPolicy.Evidence.Left),
                "supplemental evidence art stays in its own lower-left lane");
        }

        [Test]
        public void InstantiatedHudWorldBoundsKeepSealsStatusCardAndRulebookSeparatedAtEverySupportedViewport()
        {
            var samples = new[]
            {
                new { Screen = new Vector2(360f, 640f), Safe = new Rect(18f, 28f, 324f, 584f) },
                new { Screen = new Vector2(390f, 844f), Safe = new Rect(0f, 24f, 390f, 796f) },
                new { Screen = new Vector2(720f, 1280f), Safe = new Rect(36f, 54f, 648f, 1172f) },
            };
            var root = ConstructDirector(out var director);
            try
            {
                var status = ((Text)Field(director, "statusText")).rectTransform;
                var seals = ((Image[])Field(director, "integritySeals"))[0].transform.parent.GetComponent<RectTransform>();
                var rulebook = ((Button)Field(director, "rulebookButton")).GetComponent<RectTransform>();
                var feedback = ((GameObject)Field(director, "feedbackPanel")).GetComponent<RectTransform>();
                var card = ((GameObject)Field(director, "card")).GetComponent<RectTransform>();
                var safeContent = status.parent as RectTransform;

                Assert.That(seals.sizeDelta, Is.EqualTo(new Vector2(ViewportLayoutPolicy.Seals.Width, ViewportLayoutPolicy.Seals.Height)));
                Assert.That(seals.anchoredPosition, Is.EqualTo(new Vector2(ViewportLayoutPolicy.Seals.X, ViewportLayoutPolicy.Seals.Y)));
                foreach (var sample in samples)
                {
                    // These are live RectTransforms from the constructed runtime hierarchy.
                    // Scale the authored safe-content root to each real safe-area contract,
                    // then compare actual world corners rather than only policy rectangles.
                    safeContent.localScale = Vector3.one * ViewportLayoutPolicy.SafeContentScale(sample.Safe, sample.Screen);
                    Canvas.ForceUpdateCanvases();
                    AssertWorldGutter(safeContent, status, seals, "status/seals " + sample.Screen);
                    AssertWorldGutter(safeContent, seals, rulebook, "seals/rulebook " + sample.Screen);
                    AssertWorldGutter(safeContent, rulebook, feedback, "rulebook/feedback " + sample.Screen);
                    AssertWorldGutter(safeContent, feedback, card, "feedback/card " + sample.Screen);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void OverlayHeadingAndBodyUseSeparateTopAlignedReadingLanesAcrossPhoneViewports()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var heading = (Text)Field(director, "overlayHeadingText");
                var body = (Text)Field(director, "overlayText");
                Assert.That(heading.rectTransform.sizeDelta, Is.EqualTo(new Vector2(ViewportLayoutPolicy.OverlayHeading.Width, ViewportLayoutPolicy.OverlayHeading.Height)));
                Assert.That(heading.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(ViewportLayoutPolicy.OverlayHeading.X, ViewportLayoutPolicy.OverlayHeading.Y)));
                Assert.That(body.rectTransform.sizeDelta, Is.EqualTo(new Vector2(ViewportLayoutPolicy.OverlayBody.Width, ViewportLayoutPolicy.OverlayBody.Height)));
                Assert.That(body.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(ViewportLayoutPolicy.OverlayBody.X, ViewportLayoutPolicy.OverlayBody.Y)));
                Assert.That(body.alignment, Is.EqualTo(TextAnchor.UpperCenter), "short tutorial copy stays in its own top-aligned lane");
                Assert.That(heading.rectTransform.anchoredPosition.y - heading.rectTransform.sizeDelta.y * .5f,
                    Is.GreaterThanOrEqualTo(body.rectTransform.anchoredPosition.y + body.rectTransform.sizeDelta.y * .5f
                        + ViewportLayoutPolicy.OverlayHeadingBodyLogicalGap));

                var samples = new[]
                {
                    new { Screen = new Vector2(360f, 640f), Safe = new Rect(18f, 28f, 324f, 584f) },
                    new { Screen = new Vector2(390f, 844f), Safe = new Rect(0f, 24f, 390f, 796f) },
                    new { Screen = new Vector2(720f, 1280f), Safe = new Rect(36f, 54f, 648f, 1172f) },
                };
                foreach (var sample in samples)
                {
                    var headingRect = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.OverlayHeading, sample.Safe, sample.Screen);
                    var bodyRect = ViewportLayoutPolicy.PhysicalRect(ViewportLayoutPolicy.OverlayBody, sample.Safe, sample.Screen);
                    Assert.That(headingRect.yMin, Is.GreaterThan(bodyRect.yMax), "overlay lanes do not overlap " + sample.Screen);
                    Assert.That(headingRect.yMin - bodyRect.yMax,
                        Is.GreaterThanOrEqualTo(ViewportLayoutPolicy.OverlayHeadingBodyLogicalGap
                            * ViewportLayoutPolicy.SafeContentScale(sample.Safe, sample.Screen) - .01f),
                        "meaningful scaled gap " + sample.Screen);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PlayerControlsUseNavyFillsAndRaycastableGoldOutlineWithoutCoveringTheirSurfaces()
        {
            var root = ConstructDirector(out _);
            try
            {
                foreach (var button in root.GetComponentsInChildren<Button>(true).Where(item => item.name != "Debug"))
                {
                    Assert.That(button.targetGraphic, Is.Not.Null, button.name);
                    Assert.That(button.targetGraphic.raycastTarget, Is.True, button.name);
                    Assert.That(button.targetGraphic.transform.IsChildOf(button.transform), Is.True, button.name);
                    var surface = button.transform.Find("Button Surface")?.GetComponent<Image>();
                    Assert.That(surface, Is.Not.Null, button.name + " has a visible backing");
                    Assert.That(surface.raycastTarget, Is.False, button.name + " lets the outline receive the pointer");
                    Assert.That(surface.color.a, Is.GreaterThanOrEqualTo(.95f), button.name + " backing opacity");
                    Assert.That(surface.sprite, Is.Null, button.name + " has a filled backing beneath the outline");
                    Assert.That(surface.type, Is.EqualTo(Image.Type.Simple), button.name + " filled backing");
                    Assert.That(button.transform.Find("Button Ornament"), Is.Null, button.name + " must not put art across text");
                    var outline = button.transform.Find("Button Border")?.GetComponent<GoldBorderGraphic>();
                    Assert.That(outline, Is.Not.Null, button.name + " three-pixel outline");
                    Assert.That(outline.GetComponent<CanvasRenderer>(), Is.Not.Null, button.name + " outline renderer");
                    Assert.That(button.targetGraphic, Is.SameAs(outline), button.name + " full hit target is the outline rect");
                    Assert.That(surface.color.r, Is.LessThan(.20f), button.name + " is a restrained dark fill rather than orange blockout");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator LegacyImageOutlineIsReplacedWithoutGraphicConflictAndKeepsTheThinRaycastMesh()
        {
            var root = new GameObject("Legacy Button Root", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var eventSystem = new GameObject("Legacy Border Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var buttonObject = new GameObject("Legacy Button", typeof(RectTransform), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                var buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, .5f);
                buttonRect.pivot = new Vector2(.5f, .5f);
                buttonRect.sizeDelta = new Vector2(200f, 80f);
                var button = buttonObject.GetComponent<Button>();
                var legacy = new GameObject("Button Border", typeof(RectTransform), typeof(Image));
                legacy.transform.SetParent(buttonObject.transform, false);
                var legacyRect = legacy.GetComponent<RectTransform>();
                legacyRect.anchorMin = legacyRect.anchorMax = new Vector2(.5f, .5f);
                legacyRect.pivot = new Vector2(.5f, .5f);
                legacyRect.sizeDelta = new Vector2(200f, 80f);
                var oldImage = legacy.GetComponent<Image>();
                oldImage.raycastTarget = true;
                button.targetGraphic = oldImage;

                var install = typeof(GameDirector).GetMethod("InstallOutlineTarget", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(install, Is.Not.Null);
                Assert.DoesNotThrow(() => install.Invoke(null, new object[] { button, Color.yellow }));
                LogAssert.NoUnexpectedReceived();

                var outline = buttonObject.transform.Find("Button Border")?.GetComponent<GoldBorderGraphic>();
                Assert.That(outline, Is.Not.Null, "the replacement must be a distinct Graphic object");
                Assert.That(button.targetGraphic, Is.SameAs(outline));
                Assert.That(outline.raycastTarget, Is.True);
                Assert.That(outline.color, Is.EqualTo(Color.yellow));
                Assert.That(outline.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(200f, 80f)));
                Assert.That(outline.GetComponent<CanvasRenderer>(), Is.Not.Null, "a GraphicRaycaster requires a CanvasRenderer");

                using (var mesh = new VertexHelper())
                {
                    typeof(GoldBorderGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(outline, new object[] { mesh });
                    Assert.That(mesh.currentVertCount, Is.EqualTo(16), "four border strips only");
                    Assert.That(mesh.currentIndexCount, Is.EqualTo(24));
                }
                // A Graphic has depth -1 until Unity processes the canvas.  This yields one
                // editor player-loop tick, matching the runtime path before a pointer arrives.
                yield return null;
                Canvas.ForceUpdateCanvases();
                var raycastPosition = RectTransformUtility.WorldToScreenPoint(null,
                    outline.rectTransform.TransformPoint(outline.rectTransform.rect.center));
                Assert.That(RectTransformUtility.RectangleContainsScreenPoint(outline.rectTransform, raycastPosition), Is.True);
                Assert.That(outline.depth, Is.GreaterThanOrEqualTo(0), "the outline must be processed by the canvas before raycasting");
                var pointer = new PointerEventData(eventSystem.GetComponent<EventSystem>())
                {
                    position = raycastPosition,
                };
                var hits = new System.Collections.Generic.List<RaycastResult>();
                Assert.DoesNotThrow(() => root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits));
                Assert.That(hits.Any(hit => hit.gameObject == outline.gameObject), Is.True,
                    "the full border rectangle remains the Button's raycast target");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(eventSystem);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CatalogFontsAndCardUseProductionRolesAndEvidenceSilhouettesAreTouchReadable()
        {
            var catalog = VisualAssetCatalog.LoadRequired();
            Assert.That(AssetDatabase.GetAssetPath(catalog.DisplayFont), Is.EqualTo("Assets/Fonts/GrenzeGotisch-SemiBold.ttf"));
            Assert.That(AssetDatabase.GetAssetPath(catalog.BodyFont), Is.EqualTo("Assets/Fonts/AtkinsonHyperlegible-Regular.otf"));
            var root = ConstructDirector(out var director);
            try
            {
                var title = (Text)Field(director, "titleText");
                var body = (Text)Field(director, "dialogueText");
                Assert.That(title.font, Is.SameAs(catalog.DisplayFont));
                Assert.That(body.font, Is.SameAs(catalog.BodyFont));
                Assert.That(ViewportLayoutPolicy.ContrastRatio(body.color, new Color(.95f, .88f, .67f, 1f)), Is.GreaterThanOrEqualTo(4.5f));
                Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).All(text => text.font != Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")), Is.True);
                foreach (var evidence in catalog.Evidence)
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(evidence.Sprite.texture)));
                    var pixels = texture.GetPixels32();
                    var minX = texture.width; var maxX = -1; var minY = texture.height; var maxY = -1;
                    for (var index = 0; index < pixels.Length; index++)
                    {
                        if (pixels[index].a == 0) continue;
                        var x = index % texture.width; var y = index / texture.width;
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
                    var alphaWidth = (maxX - minX + 1f) / texture.width * 116f * ViewportLayoutPolicy.EvidencePhysicalScaleAtInset360;
                    var alphaHeight = (maxY - minY + 1f) / texture.height * 116f * ViewportLayoutPolicy.EvidencePhysicalScaleAtInset360;
                    Assert.That(Mathf.Max(alphaWidth, alphaHeight), Is.GreaterThanOrEqualTo(44f), evidence.Key);
                    Object.DestroyImmediate(texture);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SafeAreaDeduplicatesAndVerdictCadenceHasStableOrderedOffsets()
        {
            var root = new GameObject("SafeArea Dedup", typeof(RectTransform), typeof(SafeAreaLayoutRoot));
            try
            {
                var safe = root.GetComponent<SafeAreaLayoutRoot>();
                safe.Configure(root.GetComponent<RectTransform>());
                var before = safe.ApplyCount;
                safe.Apply(new Rect(0f, 0f, 360f, 640f), new Vector2(360f, 640f));
                var afterChange = safe.ApplyCount;
                safe.Apply(new Rect(0f, 0f, 360f, 640f), new Vector2(360f, 640f));
                Assert.That(afterChange, Is.GreaterThanOrEqualTo(before));
                Assert.That(safe.ApplyCount, Is.EqualTo(afterChange));
                var cadence = VerdictAudioCadence.Build(Decision.Deny, false, 0);
                Assert.That(cadence.Select(cue => cue.CueId), Is.EqualTo(new[] { AudioCueIds.VerdictDeny, AudioCueIds.VerdictStamp, AudioCueIds.IntegrityCrack }));
                Assert.That(cadence.Select(cue => cue.OffsetSeconds), Is.Ordered);
                Assert.That(cadence[1].OffsetSeconds, Is.EqualTo(.18f));
                Assert.That(cadence[2].OffsetSeconds, Is.EqualTo(.42f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SafeContentUsesParentGeometryRatherThanCanvasScaleAndEveryRuntimeControlRemainsTouchSized()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var canvas = root.GetComponentInChildren<Canvas>();
                var layout = root.GetComponentInChildren<SafeAreaContentLayout>();
                var safe = new Rect(0f, 0f, 324f, 584f);
                canvas.scaleFactor = 1f;
                layout.Apply(safe, new Vector2(360f, 640f));
                var before = layout.ApplyCount;
                canvas.scaleFactor = .5f;
                layout.Apply(safe, new Vector2(360f, 640f));
                var parent = (RectTransform)layout.transform.parent;
                var expectedScale = Mathf.Min(parent.rect.width / ViewportLayoutPolicy.LogicalWidth,
                    parent.rect.height / ViewportLayoutPolicy.LogicalHeight);
                Assert.That(layout.ApplyCount, Is.EqualTo(before + 1), "a real parent-geometry change gets exactly one post-layout application");
                Assert.That(layout.transform.localScale.x, Is.EqualTo(expectedScale).Within(.0001f));
                layout.Apply(safe, new Vector2(360f, 640f));
                Assert.That(layout.ApplyCount, Is.EqualTo(before + 1), "same parent geometry never reapplies a second scale");
                foreach (var control in root.GetComponentsInChildren<Button>(true).Where(button => button.name != "Debug"))
                    Assert.That(control.GetComponent<RectTransform>().sizeDelta.y * ViewportLayoutPolicy.EvidencePhysicalScaleAtInset360,
                        Is.GreaterThanOrEqualTo(44f), control.name);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PanelsKeepLiveCopyInsideCleanMeasuredFields()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var catalog = VisualAssetCatalog.LoadRequired();
                foreach (var name in new[] { "Verdict Feedback HUD", "Overlay" })
                {
                    var image = root.GetComponentsInChildren<Image>(true).Single(item => item.name == name);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), name);
                    Assert.That(image.sprite, Is.SameAs(catalog.DayEndingPanel), name);
                }
                var captionPanel = root.GetComponentsInChildren<Image>(true).Single(item => item.name == "Narrative Caption");
                Assert.That(captionPanel.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(captionPanel.sprite, Is.Null);
                Assert.That(captionPanel.color.a, Is.GreaterThanOrEqualTo(.95f));
                foreach (var name in new[] { "Caption Continue", "Mute", "Reduced Motion", "Overlay Primary", "How To Play", "Rulebook", "Rulebook Close" })
                {
                    var button = root.GetComponentsInChildren<Button>(true).Single(item => item.name == name);
                    Assert.That(button.transform.Find("Button Ornament"), Is.Null, name + " no strike-through ornament");
                    Assert.That(button.targetGraphic, Is.TypeOf<GoldBorderGraphic>(), name + " thin raycast outline");
                    Assert.That(button.GetComponentInChildren<Text>().font, Is.SameAs(catalog.BodyFont), name);
                    var surface = button.transform.Find("Button Surface").GetComponent<Image>();
                    Assert.That(surface.type, Is.EqualTo(Image.Type.Simple), name + " backing");
                    Assert.That(surface.sprite, Is.Null, name + " backing");
                    Assert.That(ViewportLayoutPolicy.ContrastRatio(button.GetComponentInChildren<Text>().color, surface.color), Is.GreaterThanOrEqualTo(4.5f), name);
                }
                var muteRect = root.GetComponentsInChildren<Button>(true).Single(item => item.name == "Mute").GetComponent<RectTransform>();
                var motionRect = root.GetComponentsInChildren<Button>(true).Single(item => item.name == "Reduced Motion").GetComponent<RectTransform>();
                Assert.That(muteRect.sizeDelta, Is.EqualTo(new Vector2(ViewportLayoutPolicy.UtilityWidth, ViewportLayoutPolicy.UtilityHeight)));
                Assert.That(motionRect.sizeDelta, Is.EqualTo(new Vector2(ViewportLayoutPolicy.UtilityWidth, ViewportLayoutPolicy.UtilityHeight)));
                Assert.That(muteRect.sizeDelta.x, Is.EqualTo(172f), "utility label has one-line semantic capacity");
                Assert.That(motionRect.sizeDelta.x, Is.EqualTo(172f), "utility label has one-line semantic capacity");
                var motionLabel = motionRect.GetComponentInChildren<Text>();
                var generator = new TextGenerator();
                var settings = motionLabel.GetGenerationSettings(motionLabel.rectTransform.rect.size);
                Assert.That(generator.GetPreferredWidth("Motion On", settings), Is.LessThanOrEqualTo(motionLabel.rectTransform.rect.width + .1f),
                    "Motion On must not wrap through a word at the 32px logical accessibility floor");
                var overlay = (GameObject)Field(director, "overlay");
                var overlayRect = overlay.GetComponent<RectTransform>();
                foreach (var child in overlay.GetComponentsInChildren<RectTransform>(true).Where(item => item.parent == overlay.transform))
                    AssertContained(overlayRect, child);
                var primary = ((Button)Field(director, "overlayPrimary")).GetComponent<RectTransform>();
                var howTo = ((Button)Field(director, "howToPlayButton")).GetComponent<RectTransform>();
                Assert.That(VerticalOverlap(primary, howTo), Is.False);
                Assert.That(((Text)Field(director, "overlayHeadingText")).rectTransform.rect.width, Is.LessThanOrEqualTo(420f), "rail-safe heading width");
                Assert.That(((Text)Field(director, "overlayText")).rectTransform.rect.width, Is.LessThanOrEqualTo(420f), "rail-safe body width");

                var rulebook = root.GetComponentsInChildren<Image>(true).Single(item => item.name == "Rulebook Panel");
                Assert.That(rulebook.sprite, Is.Null, "rulebook copy is code-native, not a new raster");
                Assert.That(rulebook.type, Is.EqualTo(Image.Type.Simple));
                foreach (var child in rulebook.GetComponentsInChildren<RectTransform>(true).Where(item => item.parent == rulebook.transform))
                    AssertContained(rulebook.rectTransform, child);
                var decreeHeading = ((Text)Field(director, "decreeHeadingText")).rectTransform;
                var decreeBody = ((Text)Field(director, "decreeText")).rectTransform;
                Assert.That(VerticalOverlap(decreeHeading, decreeBody), Is.False, "rulebook heading/body separation");
                Assert.That(((Text)Field(director, "decreeText")).alignment, Is.EqualTo(TextAnchor.UpperLeft),
                    "ordered decree lines begin at a stable reading edge");

                var caption = ((Text)Field(director, "narrativeCaptionText")).rectTransform;
                var captionCta = ((Button)Field(director, "captionContinueButton")).GetComponent<RectTransform>();
                Assert.That(VerticalOverlap(caption, captionCta), Is.False, "caption guidance/CTA separation");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PublicPointerEventDataCaptionSwipeRevealsWithoutMovingTheCardOrEnablingVerdicts()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var run = (RunStateMachine)Field(director, "run");
                run.StartRun();
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter");
                var swipe = (SwipeCard)Field(director, "swipeCard");
                var card = ((GameObject)Field(director, "card")).GetComponent<RectTransform>();
                var home = card.anchoredPosition;
                var data = new PointerEventData(root.GetComponentInChildren<EventSystem>()) { position = new Vector2(140f, 360f) };
                swipe.OnPointerDown(data);
                data.delta = new Vector2(SwipeCard.CaptionSwipeThreshold + 2f, 0f);
                swipe.OnDrag(data);
                swipe.OnPointerUp(data);
                Assert.That(card.anchoredPosition, Is.EqualTo(home));
                Assert.That(card.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(director.IsDecisionInputAvailable, Is.False);
                Assert.That(((CaptionSequenceController)Field(director, "captions")).IsTextComplete, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ActualCanvasHierarchyPointerDragFollowsTiltsSnapsAndKeepsRightAdmitLeftDeny()
        {
            DebugTrace.Clear();
            var root = ConstructDirector(out _);
            try
            {
                var audio = root.GetComponent<GameAudio>();
                audio.NotifyUiInteraction();
                Assert.That(root.name, Is.EqualTo("Integrated Director Test"), "audio voices must not rename the game root");
                var canvas = root.GetComponentInChildren<Canvas>();
                var probeObject = new GameObject("Actual Hierarchy Swipe Probe", typeof(RectTransform), typeof(SwipeCard));
                probeObject.transform.SetParent(canvas.transform, false);
                var probeRect = probeObject.GetComponent<RectTransform>();
                probeRect.sizeDelta = new Vector2(320f, 420f);
                var probe = probeObject.GetComponent<SwipeCard>();
                probe.Configure(probeRect);
                var denyEdge = new GameObject("Deny Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(DecisionTargetGlow)).GetComponent<DecisionTargetGlow>();
                denyEdge.transform.SetParent(canvas.transform, false);
                denyEdge.Configure(new Color(.88f, .17f, .24f));
                var admitEdge = new GameObject("Admit Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(DecisionTargetGlow)).GetComponent<DecisionTargetGlow>();
                admitEdge.transform.SetParent(canvas.transform, false);
                admitEdge.Configure(new Color(.20f, .78f, .40f));
                probe.ConfigureDragPreview(denyEdge, admitEdge);
                Assert.That(probe.PreviewVisualsAreOutsideMovingCard, Is.True, "preview must remain in screen/canvas space while card travels");
                Assert.That(denyEdge.transform.IsChildOf(probeObject.transform), Is.False);
                Assert.That(admitEdge.transform.IsChildOf(probeObject.transform), Is.False);
                Assert.That(denyEdge.raycastTarget, Is.False);
                Assert.That(admitEdge.raycastTarget, Is.False);
                var decisions = new System.Collections.Generic.List<Decision>();
                probe.Decided += decisions.Add;
                var eventData = new PointerEventData(root.GetComponentInChildren<EventSystem>()) { position = new Vector2(180f, 420f) };

                probe.OnPointerDown(eventData);
                eventData.delta = new Vector2(10f, 0f);
                probe.OnDrag(eventData);
                Assert.That(probeRect.anchoredPosition.x, Is.GreaterThan(0f), "drag follows the pointer");
                Assert.That(Mathf.Abs(probeRect.localEulerAngles.z), Is.GreaterThan(.01f), "drag tilts the card");
                Assert.That(probe.PreviewDirection, Is.EqualTo(SwipePreviewDirection.Admit));
                Assert.That(probe.PreviewIntensity, Is.GreaterThan(0f));
                Assert.That(admitEdge.Intensity, Is.GreaterThan(denyEdge.Intensity));
                Assert.That(admitEdge.enabled, Is.True);
                Assert.That(denyEdge.enabled, Is.False);
                var lowIntensity = admitEdge.Intensity;
                var lowAlpha = admitEdge.color.a;
                eventData.delta = new Vector2(10f, 0f);
                probe.OnDrag(eventData);
                var midIntensity = admitEdge.Intensity;
                var midAlpha = admitEdge.color.a;
                eventData.delta = new Vector2(15f, 0f);
                probe.OnDrag(eventData);
                Assert.That(midIntensity, Is.GreaterThan(lowIntensity), "mid drag increases the fixed semantic cue");
                Assert.That(midAlpha, Is.GreaterThan(lowAlpha), "mid drag visibly strengthens the halo");
                Assert.That(admitEdge.Intensity, Is.GreaterThan(midIntensity), "continued drag strengthens the fixed semantic cue");
                Assert.That(admitEdge.color.a, Is.GreaterThan(midAlpha), "continued drag visibly strengthens the halo again");
                probe.OnPointerUp(eventData);
                Assert.That(DebugTrace.Recent.Any(item => item.EventId == "animation.card_start" && item.Payload.Contains("snapback")), Is.True);
                probe.ResetCard();
                Assert.That(probe.PreviewDirection, Is.EqualTo(SwipePreviewDirection.None));
                Assert.That(admitEdge.Intensity, Is.EqualTo(0f));
                Assert.That(denyEdge.Intensity, Is.EqualTo(0f));
                Assert.That(admitEdge.enabled, Is.False);
                Assert.That(denyEdge.enabled, Is.False);

                probe.OnPointerDown(eventData);
                eventData.delta = new Vector2(220f, 0f);
                probe.OnDrag(eventData);
                Assert.That(admitEdge.Intensity, Is.EqualTo(1f), "max drag reaches full fixed target intensity");
                Assert.That(admitEdge.color.a, Is.GreaterThan(midAlpha), "max drag is visibly stronger than mid drag");
                probe.OnPointerUp(eventData);
                Assert.That(decisions.Last(), Is.EqualTo(Decision.Admit));
                probe.ResetCard();

                probe.OnPointerDown(eventData);
                eventData.delta = new Vector2(-220f, 0f);
                probe.OnDrag(eventData);
                Assert.That(probe.PreviewDirection, Is.EqualTo(SwipePreviewDirection.Deny));
                Assert.That(denyEdge.Intensity, Is.GreaterThan(admitEdge.Intensity));
                Assert.That(denyEdge.enabled, Is.True);
                Assert.That(admitEdge.enabled, Is.False);
                probe.OnPointerUp(eventData);
                Assert.That(decisions.Last(), Is.EqualTo(Decision.Deny));
                Assert.That(DebugTrace.Recent.Any(item => item.EventId == "input.drag_preview" && item.Payload.Contains("reason=commit-ready")), Is.True);
                Assert.That(DebugTrace.Recent.Any(item => item.EventId == "input.card_canvas_missing"), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RuntimeSwipePreviewGlowsStayFixedBehindLabeledTargetsAndKeepReducedMotionSemanticFeedback()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var swipe = (SwipeCard)Field(director, "swipeCard");
                var card = ((GameObject)Field(director, "card")).transform;
                var deny = root.GetComponentsInChildren<DecisionTargetGlow>(true).Single(item => item.name == "Deny Drag Preview Glow");
                var admit = root.GetComponentsInChildren<DecisionTargetGlow>(true).Single(item => item.name == "Admit Drag Preview Glow");
                Assert.That(swipe.PreviewVisualsAreOutsideMovingCard, Is.True);
                Assert.That(deny.transform.IsChildOf(card), Is.False);
                Assert.That(admit.transform.IsChildOf(card), Is.False);
                Assert.That(deny.transform.parent.name, Is.EqualTo("Safe Area Content"));
                Assert.That(admit.transform.parent.name, Is.EqualTo("Safe Area Content"));
                Assert.That(deny.raycastTarget, Is.False);
                Assert.That(admit.raycastTarget, Is.False);
                var decisionRoot = ((GameObject)Field(director, "decisionButtons")).transform;
                Assert.That(deny.transform.GetSiblingIndex(), Is.LessThan(decisionRoot.GetSiblingIndex()), "deny glow must render behind button text/surface");
                Assert.That(admit.transform.GetSiblingIndex(), Is.LessThan(decisionRoot.GetSiblingIndex()), "admit glow must render behind button text/surface");
                Assert.That(root.GetComponentsInChildren<Button>(true).Single(item => item.name == "Deny Button").GetComponentInChildren<Text>().text, Is.EqualTo("← DENY"));
                Assert.That(root.GetComponentsInChildren<Button>(true).Single(item => item.name == "Admit Button").GetComponentInChildren<Text>().text, Is.EqualTo("ADMIT →"));

                var eventData = new PointerEventData(root.GetComponentInChildren<EventSystem>()) { position = new Vector2(180f, 420f) };
                swipe.SetReducedMotion(true);
                swipe.OnPointerDown(eventData);
                eventData.delta = new Vector2(SwipeCard.DistanceThreshold, 0f);
                swipe.OnDrag(eventData);
                Assert.That(swipe.PreviewDirection, Is.EqualTo(SwipePreviewDirection.Admit));
                Assert.That(swipe.PreviewIntensity, Is.EqualTo(1f));
                Assert.That(admit.enabled, Is.True, "reduced motion keeps the non-motion admit cue");
                Assert.That(admit.Intensity, Is.EqualTo(1f));
                Assert.That(deny.enabled, Is.False);
                // The director calls this immediately after a committed swipe.  Exercise the
                // reduced-motion path directly so the semantic rail lifecycle is deterministic
                // without requiring a full authored encounter state in this construction test.
                swipe.PlayCommitted(Decision.Admit);
                Assert.That(admit.enabled, Is.False, "commit clears the static cue in reduced motion");
                Assert.That(deny.enabled, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RuntimeFixedNodesKeepEveryAuthoredDossierIconContainedAcrossRebinds()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var card = ((GameObject)Field(director, "card")).GetComponent<RectTransform>();
                var portrait = ((Image)Field(director, "portraitImage")).rectTransform;
                var name = ((Text)Field(director, "visitorNameText")).rectTransform;
                var dialogue = ((Text)Field(director, "dialogueText")).rectTransform;
                var evidence = ((Text)Field(director, "evidenceText")).rectTransform;
                var icons = ((Transform)Field(director, "evidenceIconRoot")).GetComponent<RectTransform>();
                var backing = root.GetComponentsInChildren<Image>(true).Single(item => item.name == "Dossier Text Backing");
                foreach (var rect in new[] { card, name, dialogue, evidence, icons, backing.rectTransform })
                {
                    Assert.That(rect.anchorMin, Is.EqualTo(rect.anchorMax), rect.name);
                    Assert.That(rect.pivot, Is.EqualTo(new Vector2(.5f, .5f)), rect.name);
                }
                Assert.That(backing.sprite, Is.Null, "dossier readability backing is code-native, not a new raster");
                Assert.That(backing.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(backing.color.a, Is.GreaterThanOrEqualTo(.95f));
                Assert.That(backing.raycastTarget, Is.False, "the card remains the only dossier hit surface");
                Assert.That(backing.transform.GetSiblingIndex(), Is.LessThan(name.GetSiblingIndex()));
                Assert.That(backing.transform.GetSiblingIndex(), Is.LessThan(dialogue.GetSiblingIndex()));
                Assert.That(backing.transform.GetSiblingIndex(), Is.LessThan(evidence.GetSiblingIndex()));
                AssertWorldGutter(card, dialogue, evidence, "dialogue/evidence");
                AssertHorizontalWorldGutter(icons, evidence, "evidence/icons");
                AssertHorizontalWorldGutter(portrait, dialogue, "portrait/dialogue");
                AssertHorizontalWorldGutter(portrait, evidence, "portrait/evidence");
                AssertHorizontalWorldGutter(portrait, backing.rectTransform, "portrait/dossier backing");
                AssertHorizontalWorldGutter(icons, backing.rectTransform, "icons/dossier backing");
                foreach (var rect in new[] { name, dialogue, evidence })
                {
                    AssertWorldContained(backing.rectTransform, rect);
                    Assert.That(ViewportLayoutPolicy.ContrastRatio(rect.GetComponent<Text>().color, backing.color), Is.GreaterThanOrEqualTo(4.5f),
                        rect.name + " text-on-backing contrast");
                }
                foreach (var rect in new[] { dialogue, evidence, icons, backing.rectTransform }) AssertWorldContained(card, rect);

                var content = (GameContent)Field(director, "content");
                var twoIconIds = new[] { "d3-02-false-feather", "d4-02-red-lantern-fisher" };
                foreach (var visitor in content.Visitors)
                {
                    var dossier = DecisionEvidence.Resolve(visitor, content.Decrees.Single(decreeItem => decreeItem.Day == visitor.Day));
                    var recognizedKeys = dossier.Facts.Select(VisualAssetCatalog.EvidenceKeyForAuthoredCue)
                        .Where(key => !string.IsNullOrEmpty(key)).Distinct().ToArray();
                    var expectedKeys = recognizedKeys.Take(ViewportLayoutPolicy.MaximumEvidenceIconCount).ToArray();
                    Assert.That(dossier.Facts.All(fact => dossier.Text.Contains(fact)), Is.True,
                        visitor.Id + " keeps every fact in its accessible text dossier");
                    Invoke(director, "BindEvidenceIcons", dossier);
                    Assert.That(icons.childCount, Is.EqualTo(expectedKeys.Length), visitor.Id + " rebind cleanup");
                    Assert.That(icons.childCount, Is.LessThanOrEqualTo(ViewportLayoutPolicy.MaximumEvidenceIconCount), visitor.Id + " mobile icon cap");
                    if (twoIconIds.Contains(visitor.Id)) Assert.That(expectedKeys.Length, Is.EqualTo(2), visitor.Id);
                    foreach (Transform child in icons)
                    {
                        var image = child.GetComponent<Image>();
                        var icon = child.GetComponent<RectTransform>();
                        Assert.That(icon.sizeDelta, Is.EqualTo(Vector2.one * ViewportLayoutPolicy.EvidenceIconLogicalSize), visitor.Id + " " + icon.name);
                        Assert.That(image.raycastTarget, Is.False, visitor.Id + " " + icon.name);
                        Assert.That(image.sprite, Is.SameAs(VisualAssetCatalog.LoadRequired().EvidenceForKey(expectedKeys[child.GetSiblingIndex()])), visitor.Id + " " + icon.name);
                        var cardCorners = new Vector3[4]; var iconCorners = new Vector3[4];
                        card.GetWorldCorners(cardCorners); icon.GetWorldCorners(iconCorners);
                        Assert.That(iconCorners.All(point => point.x >= cardCorners[0].x - .01f && point.x <= cardCorners[2].x + .01f
                            && point.y >= cardCorners[0].y - .01f && point.y <= cardCorners[2].y + .01f), Is.True,
                            visitor.Id + " " + icon.name + " card=" + cardCorners[0] + ".." + cardCorners[2]
                            + " icon=" + iconCorners[0] + ".." + iconCorners[2]);
                    }
                }
                var iconless = content.Visitors.First(visitor =>
                {
                    var dossier = DecisionEvidence.Resolve(visitor, content.Decrees.Single(decreeItem => decreeItem.Day == visitor.Day));
                    return dossier.Facts.All(fact => string.IsNullOrEmpty(VisualAssetCatalog.EvidenceKeyForAuthoredCue(fact)));
                });
                Invoke(director, "BindEvidenceIcons", DecisionEvidence.Resolve(iconless,
                    content.Decrees.Single(decreeItem => decreeItem.Day == iconless.Day)));
                Assert.That(icons.childCount, Is.Zero, iconless.Id + " clears every prior icon child");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RulebookIsModalAndShowsOnlyTheCurrentOrderedDecreeWithoutMutatingTheEncounter()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var run = (RunStateMachine)Field(director, "run");
                run.StartRun();
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter");
                var captions = (CaptionSequenceController)Field(director, "captions");
                captions.HandleAction(); captions.HandleAction();
                var day = run.State.Day;
                var encounter = run.State.EncounterIndex;
                var visitor = run.CurrentVisitor().Id;
                var button = (Button)Field(director, "rulebookButton");
                var panel = (GameObject)Field(director, "rulebookPanel");
                var body = (Text)Field(director, "decreeText");
                button.onClick.Invoke();
                Assert.That(panel.activeSelf, Is.True);
                Assert.That(body.text, Is.EqualTo(run.CurrentDecree().DisplayText));
                Assert.That(director.IsDecisionInputAvailable, Is.False);
                Assert.That(((Button[])Field(director, "verdictButtons")).All(item => !item.interactable), Is.True);
                Assert.That(((Button[])Field(director, "verdictButtons")).All(item => !item.targetGraphic.raycastTarget), Is.True);
                Assert.That((bool)Field((SwipeCard)Field(director, "swipeCard"), "decisionInputEnabled"), Is.False,
                    "the swipe surface follows the same modal gate as buttons and keyboard availability");
                Invoke(director, "Submit", Decision.Admit, "modal-test");
                Assert.That(run.State.Day, Is.EqualTo(day));
                Assert.That(run.State.EncounterIndex, Is.EqualTo(encounter));
                Assert.That(run.CurrentVisitor().Id, Is.EqualTo(visitor));
                Assert.That(run.State.Verdicts, Is.Empty);
                panel.GetComponentsInChildren<Button>(true).Single(item => item.name == "Rulebook Close").onClick.Invoke();
                Assert.That(panel.activeSelf, Is.False);
                Assert.That(director.IsDecisionInputAvailable, Is.True);
                Assert.That(((Button[])Field(director, "verdictButtons")).All(item => item.interactable && item.targetGraphic.raycastTarget), Is.True);
                Assert.That((bool)Field((SwipeCard)Field(director, "swipeCard"), "decisionInputEnabled"), Is.True);
                Assert.That(run.State.Verdicts, Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RulebookKeepsEveryDecisionRouteGatedWhenVisitorCaptionCompletesThenRestoresOneVerdict()
        {
            DebugTrace.Clear();
            var root = ConstructDirector(out var director);
            try
            {
                var run = (RunStateMachine)Field(director, "run");
                run.StartRun();
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter");

                var captions = (CaptionSequenceController)Field(director, "captions");
                var panel = (GameObject)Field(director, "rulebookPanel");
                var buttons = (Button[])Field(director, "verdictButtons");
                var swipe = (SwipeCard)Field(director, "swipeCard");
                ((Button)Field(director, "rulebookButton")).onClick.Invoke();
                var audioEventsBeforeBlockedInputs = DebugTrace.Recent.Count(item => item.EventId.StartsWith("audio."));

                // Keyboard is gated through IsDecisionInputAvailable; button and swipe routes
                // are exercised directly here so this test cannot pass on visibility alone.
                Assert.That(director.IsDecisionInputAvailable, Is.False);
                Assert.That(buttons.All(button => !button.interactable && !button.targetGraphic.raycastTarget), Is.True);
                Assert.That((bool)Field(swipe, "decisionInputEnabled"), Is.False);
                buttons[1].onClick.Invoke();
                var pointer = new PointerEventData(root.GetComponentInChildren<EventSystem>()) { delta = new Vector2(200f, 0f) };
                swipe.OnPointerDown(pointer); swipe.OnDrag(pointer); swipe.OnPointerUp(pointer);
                Invoke(director, "HandleCaptionContinue");
                Assert.That(run.State.Verdicts, Is.Empty, "modal input cannot resolve or advance the encounter");
                Assert.That(captions.IsActive, Is.True, "the hidden continuation surface cannot advance visitor copy");
                Assert.That(DebugTrace.Recent.Count(item => item.EventId.StartsWith("audio.")), Is.EqualTo(audioEventsBeforeBlockedInputs),
                    "blocked routes do not replay interaction audio");

                // Typewriter completion can still occur through time while the modal is open.
                captions.HandleAction(); captions.HandleAction();
                Assert.That(captions.IsActive, Is.False);
                Assert.That(panel.activeSelf, Is.True);
                Assert.That(director.IsDecisionInputAvailable, Is.False);
                Assert.That(buttons.All(button => !button.interactable && !button.targetGraphic.raycastTarget), Is.True);
                Assert.That((bool)Field(swipe, "decisionInputEnabled"), Is.False);

                panel.GetComponentsInChildren<Button>(true).Single(button => button.name == "Rulebook Close").onClick.Invoke();
                Assert.That(director.IsDecisionInputAvailable, Is.True, "close recomputes from the completed caption, not its opening snapshot");
                Assert.That(buttons.All(button => button.interactable && button.targetGraphic.raycastTarget), Is.True);
                Assert.That((bool)Field(swipe, "decisionInputEnabled"), Is.True);
                buttons[1].onClick.Invoke();
                Assert.That(run.State.Verdicts.Count, Is.EqualTo(1), "the restored encounter accepts exactly one verdict");
                Assert.That((bool)Field(director, "resolving"), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RulebookCannotOpenDuringVerdictNarrationOrAfterItsPhaseTransition()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var run = (RunStateMachine)Field(director, "run");
                run.StartRun();
                run.State.EncounterIndex = 7; // last frozen encounter advances to the day summary after its verdict.
                ((GameObject)Field(director, "overlay")).SetActive(false);
                ((GameObject)Field(director, "card")).SetActive(true);
                ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                Invoke(director, "RenderEncounter");
                var captions = (CaptionSequenceController)Field(director, "captions");
                captions.HandleAction(); captions.HandleAction();
                ((Button[])Field(director, "verdictButtons"))[0].onClick.Invoke();

                var panel = (GameObject)Field(director, "rulebookPanel");
                Assert.That((bool)Field(director, "resolving"), Is.True);
                Assert.That(run.State.Phase, Is.EqualTo(RunPhase.DaySummary));
                Invoke(director, "OpenRulebook");
                Invoke(director, "CloseRulebook");
                Assert.That(panel.activeSelf, Is.False, "verdict narration cannot be interleaved by the decree modal");
                Assert.That(director.IsDecisionInputAvailable, Is.False);

                while ((bool)Field(director, "resolving")) captions.HandleAction();
                Assert.That(run.State.Phase, Is.EqualTo(RunPhase.DaySummary));
                Invoke(director, "OpenRulebook");
                Invoke(director, "CloseRulebook");
                Assert.That(panel.activeSelf, Is.False, "the modal remains unavailable after the encounter has transitioned");
                Assert.That(director.IsDecisionInputAvailable, Is.False);
                Assert.That(((Button[])Field(director, "verdictButtons")).All(button => !button.interactable), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SyntheticThreeCueDossierCapsSupplementaryIconsInAuthoredOrderAndCleansUp()
        {
            var root = ConstructDirector(out var director);
            try
            {
                var card = ((GameObject)Field(director, "card")).GetComponent<RectTransform>();
                var icons = ((Transform)Field(director, "evidenceIconRoot")).GetComponent<RectTransform>();
                var visitor = new VisitorDefinition { Id = "synthetic-three-recognized-cues" };
                visitor.Documents.Add("moon seal");
                visitor.VisibleCues.AddRange(new[] { "forged seal", "royal counterseal", "ordinary witness token" });
                var decree = new DecreeDefinition { Day = 1, DefaultVerdict = Decision.Deny };
                var dossier = DecisionEvidence.Resolve(visitor, decree);
                var recognizedKeys = dossier.Facts.Select(VisualAssetCatalog.EvidenceKeyForAuthoredCue)
                    .Where(key => !string.IsNullOrEmpty(key)).Distinct().ToArray();
                Assert.That(ViewportLayoutPolicy.MaximumEvidenceIconCount, Is.EqualTo(2),
                    "the mobile supplementary-art contract is explicitly a two-icon cap");
                var expectedKeys = recognizedKeys.Take(ViewportLayoutPolicy.MaximumEvidenceIconCount).ToArray();

                Assert.That(recognizedKeys, Is.EqualTo(new[] { "moon-seal", "forged-seal", "royal-counterseal" }));
                Assert.That(dossier.Facts.Length, Is.EqualTo(4));
                Assert.That(dossier.Text, Is.EqualTo(
                    "Docs: moon seal\nSigns: forged seal; royal counterseal; ordinary witness token"),
                    "the two-icon cap never omits, abbreviates, or reorders a recognized or ordinary authored fact in the dossier text");
                Invoke(director, "BindEvidenceIcons", dossier);
                Assert.That(icons.childCount, Is.EqualTo(ViewportLayoutPolicy.MaximumEvidenceIconCount));
                var firstBindSprites = icons.Cast<Transform>().Select(child => child.GetComponent<Image>().sprite).ToArray();
                foreach (Transform child in icons)
                {
                    var image = child.GetComponent<Image>();
                    Assert.That(image.sprite, Is.SameAs(VisualAssetCatalog.LoadRequired().EvidenceForKey(expectedKeys[child.GetSiblingIndex()])));
                    Assert.That(image.raycastTarget, Is.False);
                    AssertWorldContained(card, child.GetComponent<RectTransform>());
                }

                Invoke(director, "BindEvidenceIcons", dossier);
                Assert.That(icons.childCount, Is.EqualTo(ViewportLayoutPolicy.MaximumEvidenceIconCount), "rebind replaces rather than accumulates icons");
                Assert.That(icons.Cast<Transform>().Select(child => child.GetComponent<Image>().sprite).ToArray(), Is.EqualTo(firstBindSprites),
                    "the first two recognized authored cues retain deterministic order after rebind");

                var textOnlyVisitor = new VisitorDefinition { Id = "synthetic-text-only-cue" };
                textOnlyVisitor.VisibleCues.Add("ordinary witness token");
                Invoke(director, "BindEvidenceIcons", DecisionEvidence.Resolve(textOnlyVisitor, decree));
                Assert.That(icons.childCount, Is.Zero, "an iconless dossier cleans up every synthetic icon child");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DirectPlayUsesLegacyInputARealGatehouseCameraAndABuildEnabledScene()
        {
            var settings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            Assert.That(settings, Does.Contain("activeInputHandler: 0"));
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/Gatehouse.unity"), Is.True);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Gatehouse.unity", OpenSceneMode.Additive);
            try
            {
                Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Any(camera => camera.enabled && camera.CompareTag("MainCamera")), Is.True);
                Assert.That(typeof(GameBootstrap).GetMethod("CreateRuntime", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null);
                var root = ConstructDirector(out var director);
                try { Assert.That(root.GetComponentInChildren<EventSystem>().GetComponent<StandaloneInputModule>(), Is.Not.Null); }
                finally { Object.DestroyImmediate(root); }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void ActiveOverlayCaptionOwnsItsCtaUntilEndingNarrationCompletes()
        {
            var root = ConstructDirector(out var director);
            try
            {
                Invoke(director, "ShowTitle");
                var primary = (Text)Field(director, "overlayPrimaryText");
                Assert.That(primary.text, Is.EqualTo("TAP TO REVEAL"));
                var run = (RunStateMachine)Field(director, "run");
                run.StartRun();
                run.State.Phase = RunPhase.Ending;
                Invoke(director, "ShowEnding");
                Assert.That(primary.text, Is.EqualTo("TAP TO REVEAL"));
                Invoke(director, "HandleEndingPrimary");
                Assert.That(primary.text, Is.EqualTo("CONTINUE"));
                Invoke(director, "HandleEndingPrimary");
                Assert.That(primary.text, Is.EqualTo("PLAY AGAIN"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void VerdictStampAlwaysUsesChosenDecisionAcrossCorrectAndIncorrectOutcomes()
        {
            var cases = new[]
            {
                new { Encounter = 0, Decision = Decision.Admit }, new { Encounter = 0, Decision = Decision.Deny },
                new { Encounter = 2, Decision = Decision.Admit }, new { Encounter = 2, Decision = Decision.Deny },
            };
            foreach (var test in cases)
            {
                var root = ConstructDirector(out var director);
                try
                {
                    var run = (RunStateMachine)Field(director, "run");
                    run.StartRun();
                    run.State.EncounterIndex = test.Encounter;
                    ((GameObject)Field(director, "overlay")).SetActive(false);
                    ((GameObject)Field(director, "card")).SetActive(true);
                    ((GameObject)Field(director, "decisionButtons")).SetActive(true);
                    Invoke(director, "RenderEncounter");
                    var captions = (CaptionSequenceController)Field(director, "captions");
                    captions.HandleAction(); captions.HandleAction();
                    Invoke(director, "Submit", test.Decision, "test");
                    var expected = test.Decision == Decision.Admit
                        ? VisualAssetCatalog.LoadRequired().VerdictAdmit
                        : VisualAssetCatalog.LoadRequired().VerdictDeny;
                    Assert.That(((Image)Field(director, "verdictStampImage")).sprite, Is.SameAs(expected), test.Decision.ToString());
                }
                finally { Object.DestroyImmediate(root); }
            }
        }

        [Test]
        public void EncounterHudHidesForEveryModalAndRestoresOnlyForAnActiveEncounter()
        {
            var root = ConstructDirector(out var director);
            try
            {
                void AssertHud(bool visible, string state)
                {
                    Assert.That(((Text)Field(director, "titleText")).gameObject.activeSelf, Is.EqualTo(visible), state + " title");
                    Assert.That(((Text)Field(director, "statusText")).gameObject.activeSelf, Is.EqualTo(visible), state + " status");
                    Assert.That(((Image[])Field(director, "integritySeals"))[0].transform.parent.gameObject.activeSelf, Is.EqualTo(visible), state + " seals");
                }

                Invoke(director, "ShowTitle");
                AssertHud(false, "title");
                Invoke(director, "ShowTutorial");
                AssertHud(false, "tutorial");
                Invoke(director, "BeginRun");
                AssertHud(false, "day intro");
                var primary = (Button)Field(director, "overlayPrimary");
                primary.onClick.Invoke();
                primary.onClick.Invoke();
                AssertHud(true, "encounter");
                Invoke(director, "ShowDaySummary");
                AssertHud(false, "summary");
                Invoke(director, "ShowEnding");
                AssertHud(false, "ending");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ReducedMotionMidVerdictRestoresTheActualHudHomeAndVisibleStampEndpoint()
        {
            var root = new GameObject("Feedback Endpoint", typeof(RectTransform), typeof(FeedbackMotion));
            var hud = new GameObject("Hud", typeof(RectTransform));
            var stamp = new GameObject("Stamp", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            try
            {
                hud.transform.SetParent(root.transform, false);
                hud.GetComponent<RectTransform>().anchoredPosition = new Vector2(17f, 23f);
                stamp.transform.SetParent(hud.transform, false);
                var motion = root.GetComponent<FeedbackMotion>();
                motion.Configure(stamp, hud.GetComponent<RectTransform>());
                motion.PlayVerdict(false, VisualAssetCatalog.LoadRequired().VerdictDeny);
                hud.GetComponent<RectTransform>().anchoredPosition += Vector2.right * 11f;
                motion.SetReducedMotion(true);
                Assert.That(stamp.gameObject.activeSelf, Is.True);
                Assert.That(stamp.color.a, Is.EqualTo(1f));
                Assert.That(stamp.rectTransform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(hud.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(17f, 23f)));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PublicDirectorActionsCompleteFiveDaysWithBoundAssetsAndNoFallbackOrErrorTrace()
        {
            var events = new System.Collections.Generic.List<TraceEvent>();
            System.Action<TraceEvent> record = trace => events.Add(trace);
            DebugTrace.Clear();
            DebugTrace.Recorded += record;
            var root = ConstructDirector(out var director);
            try
            {
                Invoke(director, "BeginRun");
                var audio = root.GetComponent<GameAudio>();
                Assert.That(audio.EncounterArrivalCadence.RunGeneration, Is.EqualTo(1));
                var run = (RunStateMachine)Field(director, "run");
                var content = (GameContent)Field(director, "content");
                var captionContinue = (Button)Field(director, "captionContinueButton");
                var overlayPrimary = (Button)Field(director, "overlayPrimary");
                var decisions = (Button[])Field(director, "verdictButtons");
                var maxVerdictLines = content.Visitors.SelectMany(visitor => new[] { Decision.Admit, Decision.Deny }
                    .Select(decision => StoryCaptionCatalog.Verdict(visitor, new VerdictRecord { Chosen = decision }, new GameState()).Lines.Count)).Max();
                var maximumOuterActions = content.TotalDays * 2 + (content.TotalDays - 1) * 2
                    + content.TotalDays * content.VisitorsPerDay * (3 + 2 * maxVerdictLines);
                var guard = 0;
                while (!(run.State.Phase == RunPhase.Ending && overlayPrimary.gameObject.activeInHierarchy) && guard++ < maximumOuterActions)
                {
                    var captions = (CaptionSequenceController)Field(director, "captions");
                    if (overlayPrimary.gameObject.activeInHierarchy)
                    {
                        overlayPrimary.onClick.Invoke();
                    }
                    else if (captions.IsActive && captionContinue.gameObject.activeInHierarchy)
                    {
                        captionContinue.onClick.Invoke();
                    }
                    else if (run.State.Phase == RunPhase.Encounter)
                    {
                        var expected = RuleEvaluator.Evaluate(run.CurrentVisitor(), run.CurrentDecree()).Expected;
                        decisions[expected == Decision.Admit ? 1 : 0].onClick.Invoke();
                    }
                    else Assert.Fail("No active public interaction surface.");
                }
                var expectedOuterActions = content.TotalDays * 2 + (content.TotalDays - 1) * 2 + run.State.Verdicts.Sum(record =>
                {
                    var visitor = content.Visitors.Single(item => item.Id == record.VisitorId);
                    return 3 + 2 * StoryCaptionCatalog.Verdict(visitor, record, run.State).Lines.Count;
                });
                Assert.That(guard, Is.EqualTo(expectedOuterActions), "each day intro/summary and visitor prompt, decision, and result/provenance caption terminates exactly once");
                Assert.That(run.State.Day, Is.EqualTo(5));
                Assert.That(run.State.Verdicts.Count, Is.EqualTo(40));
                var expectedEndingActions = StoryCaptionCatalog.Ending(run.Ending(), run.State).Lines.Count * 2;
                while (((CaptionSequenceController)Field(director, "captions")).IsActive
                    && guard++ < expectedOuterActions + expectedEndingActions) overlayPrimary.onClick.Invoke();
                Assert.That(guard, Is.EqualTo(expectedOuterActions + expectedEndingActions), "ending captions also reveal then continue exactly once per line");
                Assert.That(((Text)Field(director, "overlayPrimaryText")).text, Is.EqualTo("PLAY AGAIN"));
                overlayPrimary.onClick.Invoke();
                run = (RunStateMachine)Field(director, "run");
                Assert.That(run.State.Day, Is.EqualTo(1));
                Assert.That(run.State.Verdicts, Is.Empty);
                Assert.That(audio.EncounterArrivalCadence.RunGeneration, Is.EqualTo(2));
                Assert.That(events.Count(trace => trace.EventId == "audio.arrival_run_started"), Is.EqualTo(2),
                    "only the initial run and PLAY AGAIN restart the arrival cadence; day flow and rerenders do not");
                Assert.That(((Text)Field(director, "titleText")).gameObject.activeSelf, Is.False);
                Assert.That(((Text)Field(director, "statusText")).gameObject.activeSelf, Is.False);
                Assert.That(((Image[])Field(director, "integritySeals"))[0].transform.parent.gameObject.activeSelf, Is.False);
                Assert.That(events.Any(trace => trace.EventId == "asset.visual_bound" && trace.Payload.Contains("kind=portrait")), Is.True);
                Assert.That(events.Count(trace => trace.EventId == "asset.visual_bound" && trace.Payload.Contains("kind=portrait")), Is.EqualTo(40));
                Assert.That(events.Count(trace => trace.EventId == "audio.verdict_scheduled"), Is.GreaterThanOrEqualTo(40));
                Assert.That(events.Any(trace => trace.EventId.Contains("error") || trace.EventId.Contains("fallback")), Is.False,
                    string.Join("\n", events.Where(trace => trace.EventId.Contains("error") || trace.EventId.Contains("fallback")).Select(trace => trace.EventId + " " + trace.Payload)));
            }
            finally
            {
                DebugTrace.Recorded -= record;
                Object.DestroyImmediate(root);
            }
        }

        private static object Field(GameDirector director, string name)
            => typeof(GameDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);

        private static object Field(object instance, string name)
            => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

        private static void Invoke(GameDirector director, string name, params object[] arguments)
            => typeof(GameDirector).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, arguments);

        private static GameObject ConstructDirector(out GameDirector director)
        {
            var root = new GameObject("Integrated Director Test");
            root.AddComponent<GameAudio>();
            director = root.AddComponent<GameDirector>();
            Invoke(director, "Awake");
            return root;
        }

        private static void AssertFits(Text target, System.Collections.Generic.IEnumerable<string> values)
        {
            var generator = new TextGenerator();
            var settings = target.GetGenerationSettings(target.rectTransform.rect.size);
            foreach (var value in values)
            {
                var height = generator.GetPreferredHeight(value ?? string.Empty, settings);
                Assert.That(height, Is.LessThanOrEqualTo(target.rectTransform.rect.height + .1f), target.name + ": " + value);
            }
        }

        private static void AssertMaximumFits(Text target, System.Collections.Generic.IEnumerable<string> values, string scope)
        {
            var generator = new TextGenerator();
            var settings = target.GetGenerationSettings(target.rectTransform.rect.size);
            var measurements = values.Select(value => new
            {
                Text = value ?? string.Empty,
                Height = generator.GetPreferredHeight(value ?? string.Empty, settings),
            }).OrderByDescending(item => item.Height).ToArray();
            var maximum = measurements.FirstOrDefault();
            Assert.That(maximum == null ? 0f : maximum.Height, Is.LessThanOrEqualTo(target.rectTransform.rect.height + .1f),
                target.name + " maximum " + scope + ": " + (maximum?.Text ?? string.Empty));
        }

        private static void AssertContained(RectTransform outer, RectTransform inner)
        {
            var half = inner.sizeDelta * .5f;
            Assert.That(inner.anchoredPosition.x - half.x, Is.GreaterThanOrEqualTo(outer.rect.xMin - .1f), inner.name);
            Assert.That(inner.anchoredPosition.x + half.x, Is.LessThanOrEqualTo(outer.rect.xMax + .1f), inner.name);
            Assert.That(inner.anchoredPosition.y - half.y, Is.GreaterThanOrEqualTo(outer.rect.yMin - .1f), inner.name);
            Assert.That(inner.anchoredPosition.y + half.y, Is.LessThanOrEqualTo(outer.rect.yMax + .1f), inner.name);
        }

        private static bool VerticalOverlap(RectTransform first, RectTransform second)
        {
            var firstBottom = first.anchoredPosition.y - first.sizeDelta.y * .5f;
            var firstTop = first.anchoredPosition.y + first.sizeDelta.y * .5f;
            var secondBottom = second.anchoredPosition.y - second.sizeDelta.y * .5f;
            var secondTop = second.anchoredPosition.y + second.sizeDelta.y * .5f;
            return firstBottom < secondTop && firstTop > secondBottom;
        }

        private static void AssertWorldContained(RectTransform outer, RectTransform inner)
        {
            var outerCorners = new Vector3[4]; var innerCorners = new Vector3[4];
            outer.GetWorldCorners(outerCorners); inner.GetWorldCorners(innerCorners);
            const float epsilon = .01f;
            Assert.That(innerCorners.All(point => point.x >= outerCorners[0].x - epsilon && point.x <= outerCorners[2].x + epsilon
                && point.y >= outerCorners[0].y - epsilon && point.y <= outerCorners[2].y + epsilon), Is.True, inner.name);
        }

        private static void AssertWorldGutter(RectTransform parent, RectTransform upper, RectTransform lower, string label)
        {
            var upperCorners = new Vector3[4]; var lowerCorners = new Vector3[4];
            upper.GetWorldCorners(upperCorners); lower.GetWorldCorners(lowerCorners);
            Assert.That(upperCorners[0].y, Is.GreaterThanOrEqualTo(lowerCorners[1].y + 1.99f), label);
        }

        private static void AssertHorizontalWorldGutter(RectTransform left, RectTransform right, string label)
        {
            var leftCorners = new Vector3[4]; var rightCorners = new Vector3[4];
            left.GetWorldCorners(leftCorners); right.GetWorldCorners(rightCorners);
            Assert.That(leftCorners.Max(point => point.x), Is.LessThanOrEqualTo(rightCorners.Min(point => point.x) - 4.99f), label);
        }

        private static void AssertPortraitCoversApertureFromItsBottom(RectTransform portrait, RectTransform aperture, string key)
        {
            Canvas.ForceUpdateCanvases();
            var portraitCorners = new Vector3[4]; var apertureCorners = new Vector3[4];
            portrait.GetWorldCorners(portraitCorners); aperture.GetWorldCorners(apertureCorners);
            Assert.That(portraitCorners.Min(point => point.y), Is.EqualTo(apertureCorners.Min(point => point.y)).Within(.01f), key + " bottom");
            Assert.That(portraitCorners.Max(point => point.y), Is.GreaterThanOrEqualTo(apertureCorners.Max(point => point.y) - .01f), key + " top");
        }
    }
}
