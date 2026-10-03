# Narrative presentation contract

`Assets/Scripts/Presentation` owns deterministic, headless-testable text presentation. The
default rate is 35 grapheme clusters per second; commas pause briefly and sentence punctuation
pauses longer. The system accepts an injected delta, has instant/reduced-motion mode, and never
uses frame screenshots or editor automation for validation.

Every caption sequence follows the same safety rule: the first deliberate action completes text;
a later action advances or dismisses it. `GameDirector` consumes caption actions before an Admit
or Deny can resolve, so reading/skipping a caption can never accidentally make a verdict.

`StoryCaptionCatalog.MissingVisitorMappings` is a required content integration gate. It emits
`presentation.error/fallback` for every missing id and test failures treat those omissions as
blocking, even though the runtime shows a neutral fallback sentence rather than crashing.

Art hooks live in `PresentationBindings`. They intentionally name future parchment, card-frame,
portrait, and verdict-stamp bindings without producing replacement art or owning raster assets.

Structured removable diagnostics: `presentation.sequence_started`, `caption.started`, sampled
`typewriter.progress`, `typewriter.completed`/`typewriter.skipped`, `caption.dismissed`,
`presentation.sequence_completed`, and `presentation.error/fallback`.
