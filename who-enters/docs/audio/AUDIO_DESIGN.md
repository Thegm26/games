# Who Enters? — Original Procedural Audio Contract

All game sound is synthesized deterministically in `Assets/Scripts/Audio/ProceduralAudioSynthesis.cs`. No downloaded audio, samples, music, or uncertain licences are used.

| Cue family | In-game intent | Trigger |
| --- | --- | --- |
| Castle wind/drone | Low, haunted gatehouse bed | First deliberate user interaction only |
| Bell and card arrival | Day transition and a soft gate/card settle | Day intro / once per new encounter slot |
| Card tactile sounds | Pickup, throttled parchment drag, snapback | Structured swipe input traces |
| Verdicts | Warm lock for admit; cold chain/moat for deny; stamp | Resolved decision |
| Feedback | Seal crack and streak chime | Verdict outcome |
| Endings | Distinct fallen, hollow, held, and secret motifs | Ending selection |
| UI | Restrained click | Deliberate visible UI action |

## Runtime safety contract

- Clips and `AudioSource` objects are created only after a deliberate user interaction.
- Ambient playback never starts automatically. It is looped from a deterministic, seam-compatible signal.
- `who-enters.muted` persists the player’s mute choice. Muted and pre-activation requests are suppressed.
- Each cue has catalogued gain, cooldown, and maximum polyphony; drag sounds are rate-limited through the same policy.
- The `gate.open` arrival is a short, low-gain consonant gate/card motif with a fade-in/out (no noise transient). `GameAudio.PlayEncounterArrival` keys it to the five-day, eight-slot encounter grid, so caption, decree, and continuation rerenders cannot replay it.
- All playback decisions emit removable `DebugTrace` events: `audio.init`, `audio.activation`, `audio.play`, `audio.suppressed`, `audio.missing`, `audio.clip_generated`, `audio.mute`, and `audio.sources_ready`.

## Data-quality gate

EditMode tests verify every catalogued cue for expected sample count, mono channel count, sample rate, duration, finite samples, peak ceiling, RMS range, bounded DC offset, fade boundaries, deterministic output, and ambient loop continuity. Arrival-specific tests also verify its peak/RMS/DC/envelope/frequency band and the complete forty-slot once-per-encounter cadence. They also verify cue routing, cooldown/polyphony, activation, mute persistence, and diagnostic events.
