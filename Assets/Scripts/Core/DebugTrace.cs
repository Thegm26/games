using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoEnters.Core
{
    /// <summary>Single removable gate for structured development diagnostics.</summary>
    public static class DebugTrace
    {
        public static bool Enabled { get; set; } = true;
        public static event Action<TraceEvent> Recorded;
        private static readonly Queue<TraceEvent> RecentEvents = new Queue<TraceEvent>();

        public static IReadOnlyCollection<TraceEvent> Recent => RecentEvents;

        public static void Clear()
        {
            if (!Enabled) return;
            RecentEvents.Clear();
        }

        public static void Log(string eventId, string payload = "")
        {
            if (!Enabled) return;
            var entry = Record(eventId, payload);
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
