using System;
using WhoEnters.Core;

namespace WhoEnters.Presentation
{
    /// <summary>Owns the skip/continue contract: an action first completes text, then advances it.</summary>
    public sealed class CaptionSequenceController
    {
        private readonly IPresentationTraceSink trace;
        private readonly float charactersPerSecond;
        private CaptionSequence sequence;
        private TypewriterTimeline timeline;
        private int lineIndex;
        private int lastProgressSample = -1;

        public CaptionSequence ActiveSequence => sequence;
        public CaptionLine CurrentLine => sequence == null || lineIndex >= sequence.Lines.Count ? null : sequence.Lines[lineIndex];
        public string VisibleText => timeline == null ? string.Empty : timeline.VisibleText;
        public bool IsActive => sequence != null;
        public bool IsTextComplete => timeline != null && timeline.IsComplete;
        private bool instantMode;
        public bool InstantMode
        {
            get => instantMode;
            set
            {
                instantMode = value;
                if (!instantMode || timeline == null || timeline.IsComplete) return;
                timeline.CompleteImmediately();
                Trace("typewriter.skipped", CaptionPayload() + ";source=instant-toggle");
                CompleteCurrentText("instant-toggle");
            }
        }
        public event Action<CaptionLine> CaptionStarted;
        public event Action<CaptionLine> CaptionCompleted;
        public event Action<CaptionSequence> SequenceCompleted;

        public CaptionSequenceController(IPresentationTraceSink trace = null, float charactersPerSecond = TypewriterTimeline.DefaultCharactersPerSecond)
        {
            this.trace = trace ?? new DebugTracePresentationSink();
            this.charactersPerSecond = charactersPerSecond;
        }

        public bool Begin(CaptionSequence next)
        {
            if (next == null || next.Lines == null || next.Lines.Count == 0)
            {
                Trace("presentation.error/fallback", "reason=empty-sequence");
                return false;
            }
            sequence = next;
            lineIndex = 0;
            Trace("presentation.sequence_started", "id=" + sequence.Id + ";lines=" + sequence.Lines.Count);
            StartLine();
            return true;
        }

        public bool Advance(float deltaSeconds)
        {
            if (timeline == null) return false;
            var before = timeline.VisibleCount;
            var changed = timeline.Advance(deltaSeconds);
            if (changed) EmitProgress(before, timeline.VisibleCount);
            if (changed && timeline.IsComplete) CompleteCurrentText("natural");
            return changed;
        }

        public TypewriterAction HandleAction()
        {
            if (!IsActive || timeline == null) return TypewriterAction.None;
            if (!timeline.IsComplete)
            {
                timeline.CompleteImmediately();
                Trace("typewriter.skipped", CaptionPayload());
                CompleteCurrentText("skipped");
                return TypewriterAction.CompletedText;
            }

            Trace("caption.dismissed", CaptionPayload());
            if (lineIndex + 1 < sequence.Lines.Count)
            {
                lineIndex++;
                StartLine();
                return TypewriterAction.AdvancedCaption;
            }

            var completed = sequence;
            sequence = null;
            timeline = null;
            Trace("presentation.sequence_completed", "id=" + completed.Id);
            SequenceCompleted?.Invoke(completed);
            return TypewriterAction.CompletedSequence;
        }

        public void Cancel(string reason)
        {
            if (!IsActive) return;
            Trace("presentation.error/fallback", "reason=" + reason + ";id=" + sequence.Id);
            sequence = null;
            timeline = null;
        }

        private void StartLine()
        {
            var line = CurrentLine;
            if (line == null)
            {
                Cancel("missing-line");
                return;
            }
            timeline = new TypewriterTimeline(line.Text, charactersPerSecond, InstantMode);
            lastProgressSample = -1;
            Trace("caption.started", CaptionPayload());
            CaptionStarted?.Invoke(line);
            if (timeline.IsComplete) CompleteCurrentText("instant");
        }

        private void CompleteCurrentText(string source)
        {
            var line = CurrentLine;
            if (line == null) return;
            Trace("typewriter.completed", CaptionPayload() + ";source=" + source);
            CaptionCompleted?.Invoke(line);
        }

        private void EmitProgress(int before, int after)
        {
            if (after == before) return;
            var total = Math.Max(1, timeline.ElementCount);
            var bucket = after == total ? 4 : Math.Min(3, (after * 4) / total);
            if (bucket == lastProgressSample) return;
            lastProgressSample = bucket;
            Trace("typewriter.progress", CaptionPayload() + ";visible=" + after + ";total=" + total + ";sampled=true");
        }

        private string CaptionPayload()
        {
            var line = CurrentLine;
            return "sequence=" + (sequence == null ? "none" : sequence.Id) + ";caption=" + (line == null ? "none" : line.Id) + ";role=" + (line == null ? "none" : line.Role.ToString());
        }

        private void Trace(string eventId, string payload) => trace.Record(eventId, payload);
    }

    public sealed class DebugTracePresentationSink : IPresentationTraceSink
    {
        public void Record(string eventId, string payload) => DebugTrace.Log(eventId, payload);
    }
}
