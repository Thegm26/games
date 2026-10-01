using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WhoEnters.Core;

namespace WhoEnters.Tests.EditMode.Core
{
    public sealed class DebugTraceTests
    {
        [Test]
        public void RoutineEventsRemainExhaustiveInMemoryWhileConsoleMirroringIsBoundedAndOptIn()
        {
            var priorEnabled = DebugTrace.Enabled;
            var priorConsoleEnabled = DebugTrace.ConsoleMirroringEnabled;
            var priorLimit = DebugTrace.RoutineConsoleMirrorLimit;
            var recorded = new List<TraceEvent>();
            var console = new List<string>();
            System.Action<TraceEvent> record = entry => recorded.Add(entry);
            Application.LogCallback receive = (condition, _, __) =>
            {
                if (condition.StartsWith("[WHO_ENTERS] debug.console.")) console.Add(condition);
            };
            try
            {
                DebugTrace.Enabled = true;
                DebugTrace.ConsoleMirroringEnabled = true;
                DebugTrace.RoutineConsoleMirrorLimit = 3;
                DebugTrace.Clear();
                DebugTrace.Recorded += record;
                Application.logMessageReceived += receive;

                for (var index = 0; index < 24; index++) DebugTrace.Log("debug.console." + index, "index=" + index);

                Assert.That(recorded.Select(entry => entry.EventId), Is.EqualTo(Enumerable.Range(0, 24).Select(index => "debug.console." + index)));
                Assert.That(recorded.Select(entry => entry.Payload), Is.EqualTo(Enumerable.Range(0, 24).Select(index => "index=" + index)));
                Assert.That(DebugTrace.Recent.Count, Is.EqualTo(18), "only the overlay tail is bounded");
                Assert.That(DebugTrace.RoutineConsoleMirrorCount, Is.EqualTo(3));
                Assert.That(console, Has.Count.EqualTo(3));

                DebugTrace.ConsoleMirroringEnabled = false;
                DebugTrace.Log("debug.console.disabled", "still-recorded=true");
                Assert.That(recorded.Last().EventId, Is.EqualTo("debug.console.disabled"));
                Assert.That(recorded.Last().Payload, Is.EqualTo("still-recorded=true"));
                Assert.That(console, Has.Count.EqualTo(3), "disabled mirroring must not emit routine diagnostics");
            }
            finally
            {
                Application.logMessageReceived -= receive;
                DebugTrace.Recorded -= record;
                DebugTrace.Enabled = priorEnabled;
                DebugTrace.ConsoleMirroringEnabled = priorConsoleEnabled;
                DebugTrace.RoutineConsoleMirrorLimit = priorLimit;
                DebugTrace.Clear();
            }
        }
    }
}
