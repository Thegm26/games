using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoEnters.Core
{
    /// <summary>Single removable gate for structured development diagnostics.</summary>
    public static class DebugTrace
    {
        public const int DefaultRoutineConsoleMirrorLimit = 64;
        public static bool Enabled { get; set; } = true;
        /// <summary>
        /// Routine traces remain in the structured in-memory stream by default. Console
        /// mirroring is an explicit, bounded opt-in so batchmode diagnostics do not turn a
        /// complete run into an unmanageable Unity log.
        /// </summary>
        public static bool ConsoleMirroringEnabled { get; set; }
        public static int RoutineConsoleMirrorLimit { get; set; } = DefaultRoutineConsoleMirrorLimit;
        public static event Action<TraceEvent> Recorded;
        private static readonly Queue<TraceEvent> RecentEvents = new Queue<TraceEvent>();
        private static int routineConsoleMirrorCount;

        public static IReadOnlyCollection<TraceEvent> Recent => RecentEvents;
        public static int RoutineConsoleMirrorCount => routineConsoleMirrorCount;

        public static void Clear()
        {
            if (!Enabled) return;
            RecentEvents.Clear();
            routineConsoleMirrorCount = 0;
        }

        public static void Log(string eventId, string payload = "")
        {
            if (!Enabled) return;
            var entry = Record(eventId, payload);
            if (!ConsoleMirroringEnabled || routineConsoleMirrorCount >= Math.Max(0, RoutineConsoleMirrorLimit)) return;
            routineConsoleMirrorCount++;
            Debug.Log($"[WHO_ENTERS] {entry.EventId} | {entry.Payload}");
        }

        public static void Error(string eventId, string payload)
        {
            if (!Enabled) return;
            Record(eventId, payload);
            Debug.LogError($"[WHO_ENTERS] {eventId} | {payload}");
        }

        private static TraceEvent Record(string eventId, string payload)
        {
            var entry = new TraceEvent(eventId, payload, Time.realtimeSinceStartup);
            RecentEvents.Enqueue(entry);
            while (RecentEvents.Count > 18) RecentEvents.Dequeue();
            Recorded?.Invoke(entry);
            return entry;
        }
    }

    public readonly struct TraceEvent
    {
        public readonly string EventId;
        public readonly string Payload;
        public readonly float Timestamp;

        public TraceEvent(string eventId, string payload, float timestamp)
        {
            EventId = eventId;
            Payload = payload;
            Timestamp = timestamp;
        }

        public override string ToString() => $"{Timestamp:0.00} {EventId} {Payload}";
    }
}
