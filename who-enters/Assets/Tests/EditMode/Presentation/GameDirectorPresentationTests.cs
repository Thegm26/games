using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using WhoEnters.Gameplay;

namespace WhoEnters.Tests.Presentation
{
    public sealed class GameDirectorPresentationTests
    {
        [Test]
        public void DirectorConstructsWithPresentationAndReducedMotionHook()
        {
            var root = new GameObject("PresentationDirectorTest");
            try
            {
                root.AddComponent<GameAudio>();
                var director = root.AddComponent<GameDirector>();

                Assert.That(director, Is.Not.Null);
                Assert.DoesNotThrow(() => director.SetReducedMotion(true));
                Assert.DoesNotThrow(() => director.SetReducedMotion(false));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OverlayStateBlocksDecisionInputEvenWhenRunWouldBeInEncounter()
        {
            var root = new GameObject("PresentationModalGateTest");
            try
            {
                var director = root.AddComponent<GameDirector>();
                var card = new GameObject("Card");
                card.transform.SetParent(root.transform);
                var buttons = new GameObject("Buttons");
                buttons.transform.SetParent(root.transform);
                SetField(director, "card", card);
                SetField(director, "decisionButtons", buttons);
                var overlay = new GameObject("Overlay");
                overlay.transform.SetParent(root.transform);
                SetField(director, "overlay", overlay);

                Assert.That(director.IsDecisionInputAvailable, Is.False, "The unseen day/decree modal must block arrow, A/D, swipe, and button verdict routes.");
                overlay.SetActive(false);
                Assert.That(director.IsDecisionInputAvailable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetField(GameDirector director, string name, GameObject value)
        {
            typeof(GameDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(director, value);
        }
    }
}
