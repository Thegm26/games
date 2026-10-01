using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using WhoEnters.Audio;
using WhoEnters.Core;
using WhoEnters.Gameplay;
using WhoEnters.Integration;
using WhoEnters.UI;

namespace WhoEnters.Tests.EditMode.Integration
{
    /// <summary>Connected, headless contract for the complete public mobile interaction path.</summary>
    public sealed class PublicFlowContractTests
    {
        [Test]
        public void DebugTraceKeepsNewestEventsVisibleWithoutWeakeningPlayerFontFloor()
        {
            var priorEnabled = DebugTrace.Enabled;
            DebugTrace.Enabled = true;
            DebugTrace.Clear();
            var root = new GameObject("Debug Trace Contract");
            try
            {
                var director = CreateDirector(root);
                var debugText = root.GetComponentsInChildren<Text>(true).Single(text => text.name == "Debug Trace");
                var debugPanel = debugText.transform.parent.gameObject;
                var overlay = debugPanel.GetComponent<DebugOverlay>();
                Assert.That(overlay, Is.Not.Null);
                Assert.That(debugText.resizeTextMinSize, Is.EqualTo(15));
                Assert.That(debugText.resizeTextMaxSize, Is.EqualTo(15));
                Assert.That(root.GetComponentsInChildren<Text>(true)
                    .Where(text => text != debugText)
                    .All(text => text.resizeTextMinSize >= ViewportLayoutPolicy.MinimumBodyLogicalFontSize), Is.True);

                overlay.Toggle();
                for (var index = 0; index < 18; index++) DebugTrace.Log("debug.contract." + index);
                overlay.Refresh();

                Assert.That(debugText.text, Does.Contain("debug.contract.17"));
                Assert.That(debugText.text, Does.Not.Contain("debug.contract.0"));
                Assert.That(debugText.text.Split('\n').Count(line => line.Contains("debug.contract.")), Is.EqualTo(DebugOverlay.MaximumVisibleEvents));
                Assert.That(director, Is.Not.Null); // Explicitly keeps this a live runtime UI contract.
            }
            finally
            {
                DebugTrace.Clear();
                DebugTrace.Enabled = priorEnabled;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PublicSurfacesReachEndingThenReplayAsFreshDayOneWithoutStaleHud()
        {
            var root = new GameObject("Public Flow Contract");
            try
            {
                var director = CreateDirector(root);
                Invoke(director, "Start"); // Unity lifecycle entry point, not a flow transition shortcut.
                var heading = (Text)Field(director, "overlayHeadingText");
                var primary = (Button)Field(director, "overlayPrimary");
                var howToPlay = (Button)Field(director, "howToPlayButton");
                var captionContinue = (Button)Field(director, "captionContinueButton");
                var decisions = (Button[])Field(director, "verdictButtons");

                Assert.That(heading.text, Is.EqualTo("Who Enters?"));
                Assert.That(decisions[0].name, Is.EqualTo("Deny Button"));
                Assert.That(decisions[0].GetComponentInChildren<Text>().text, Is.EqualTo("← DENY"));
                Assert.That(decisions[1].name, Is.EqualTo("Admit Button"));
                Assert.That(decisions[1].GetComponentInChildren<Text>().text, Is.EqualTo("ADMIT →"));
                howToPlay.onClick.Invoke();
                Assert.That(heading.text, Is.EqualTo("How To Play"));
                primary.onClick.Invoke(); // reveal tutorial
                primary.onClick.Invoke(); // return to title
                Assert.That(heading.text, Is.EqualTo("Who Enters?"));
                primary.onClick.Invoke(); // reveal title
                primary.onClick.Invoke(); // begin run, landing on day intro

                var run = (RunStateMachine)Field(director, "run");
                var resolved = 0;
                var guard = 0;
                while (!(run.State.Phase == RunPhase.Ending && primary.gameObject.activeInHierarchy) && guard++ < 500)
                {
                    var captions = (WhoEnters.Presentation.CaptionSequenceController)Field(director, "captions");
                    if (primary.gameObject.activeInHierarchy && captions.IsActive)
                    {
                        primary.onClick.Invoke();
                    }
                    else if (captionContinue.gameObject.activeInHierarchy && captions.IsActive)
                    {
                        captionContinue.onClick.Invoke();
                    }
                    else if (run.State.Phase == RunPhase.Encounter)
                    {
                        var expected = RuleEvaluator.Evaluate(run.CurrentVisitor(), run.CurrentDecree()).Expected;
                        decisions[expected == Decision.Admit ? 1 : 0].onClick.Invoke();
                        resolved++;
                    }
                    else
                    {
                        Assert.Fail("No public interaction surface for phase " + run.State.Phase + ".");
                    }
                }

                Assert.That(guard, Is.LessThan(500));
                Assert.That(resolved, Is.EqualTo(40));
                Assert.That(run.State.Day, Is.EqualTo(5));
                Assert.That(run.State.Verdicts.Count, Is.EqualTo(40));
                Assert.That(primary.gameObject.activeInHierarchy, Is.True);

                while (((WhoEnters.Presentation.CaptionSequenceController)Field(director, "captions")).IsActive && guard++ < 550)
                    primary.onClick.Invoke();
                Assert.That(guard, Is.LessThan(550));
                Assert.That(((Text)Field(director, "overlayPrimaryText")).text, Is.EqualTo("PLAY AGAIN"));

                primary.onClick.Invoke(); // public replay CTA -> fresh run day intro
                run = (RunStateMachine)Field(director, "run");
                Assert.That(run.State.Day, Is.EqualTo(1));
                Assert.That(run.State.Verdicts, Is.Empty);
                Assert.That(((Text)Field(director, "titleText")).gameObject.activeSelf, Is.False);
                Assert.That(((Text)Field(director, "statusText")).gameObject.activeSelf, Is.False);
                Assert.That(((Image[])Field(director, "integritySeals"))[0].transform.parent.gameObject.activeSelf, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameDirector CreateDirector(GameObject root)
        {
            root.AddComponent<GameAudio>();
            var director = root.AddComponent<GameDirector>();
            Invoke(director, "Awake");
            return director;
        }

        private static object Field(GameDirector director, string name)
            => typeof(GameDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);

        private static void Invoke(GameDirector director, string name, params object[] arguments)
            => typeof(GameDirector).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, arguments);
    }
}
