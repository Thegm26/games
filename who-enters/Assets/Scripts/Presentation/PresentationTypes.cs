using System;
using System.Collections.Generic;

namespace WhoEnters.Presentation
{
    public enum PresentationRole
    {
        Intro,
        Tutorial,
        DayTitle,
        Decree,
        Visitor,
        Verdict,
        DaySummary,
        Ending,
    }

    public enum TypewriterAction
    {
        None,
        CompletedText,
        AdvancedCaption,
        CompletedSequence,
    }

    public sealed class CaptionLine
    {
        public string Id { get; }
        public PresentationRole Role { get; }
        public string Text { get; }

        public CaptionLine(string id, PresentationRole role, string text)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "caption" : id;
            Role = role;
            Text = text ?? string.Empty;
        }
    }

    public sealed class CaptionSequence
    {
        public string Id { get; }
        public IReadOnlyList<CaptionLine> Lines { get; }

        public CaptionSequence(string id, params CaptionLine[] lines)
        {
            Id = string.IsNullOrWhiteSpace(id) ? "sequence" : id;
            Lines = lines ?? Array.Empty<CaptionLine>();
        }
    }

    public readonly struct PresentationTraceEvent
    {
        public readonly string EventId;
        public readonly string Payload;

        public PresentationTraceEvent(string eventId, string payload)
        {
            EventId = eventId;
            Payload = payload;
        }
    }

    public interface IPresentationTraceSink
    {
        void Record(string eventId, string payload);
    }

    public sealed class MemoryPresentationTraceSink : IPresentationTraceSink
    {
        public readonly List<PresentationTraceEvent> Events = new List<PresentationTraceEvent>();
        public void Record(string eventId, string payload) => Events.Add(new PresentationTraceEvent(eventId, payload));
    }
}
