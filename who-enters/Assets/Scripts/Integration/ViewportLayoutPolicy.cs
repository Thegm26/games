using UnityEngine;
using WhoEnters.Core;

namespace WhoEnters.Integration
{
    public readonly struct LogicalRect
    {
        public readonly float X; public readonly float Y; public readonly float Width; public readonly float Height;
        public LogicalRect(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
        public float Left => X - Width * .5f; public float Right => X + Width * .5f;
        public float Bottom => Y - Height * .5f; public float Top => Y + Height * .5f;
        public bool Overlaps(LogicalRect other) => Left < other.Right && Right > other.Left && Bottom < other.Top && Top > other.Bottom;
    }

    /// <summary>Pure, testable mobile viewport contract. UI tracks safe area; environment covers full screen.</summary>
    public static class ViewportLayoutPolicy
    {
        public const float LogicalWidth = 720f;
        public const float LogicalHeight = 1280f;
        public const float MinimumTouchLogicalHeight = 88f;
        // 32 logical * .45 (the supported 360px inset safe-content scale) = 14.4 physical px.
        public const int MinimumBodyLogicalFontSize = 32;
        public const float MinimumPhysicalTouchPixels = 44f;
        // Measured from the bottom of the baked 1536x1024 card: this maps the runtime mask
        // to the native stone arch rather than the lower wooden plinth.
        public const float PortraitApertureBottomInset = 96f;
        public const float EvidencePhysicalScaleAtInset360 = .45f;
        public const float EvidenceIconLogicalSize = 116f;
        public const float EvidenceIconLogicalSpacing = 124f;
        public const int MaximumEvidenceIconCount = 2;
        public static readonly Color DarkInk = new Color(.105f, .065f, .13f, 1f);
        public static readonly Color ParchmentInk = new Color(.18f, .11f, .08f, 1f);

        public static Rect NormalizedSafeArea(Rect safePixels, Vector2 screenPixels)
        {
            if (screenPixels.x <= 0f || screenPixels.y <= 0f) return new Rect(0f, 0f, 1f, 1f);
            return new Rect(safePixels.x / screenPixels.x, safePixels.y / screenPixels.y,
                safePixels.width / screenPixels.x, safePixels.height / screenPixels.y);
        }

        /// <summary>9:16 art uses cover: it is never stretched; surplus viewport area is cropped.</summary>
        public static Vector2 CoverScale(Vector2 viewport, Vector2 source)
        {
            var scale = Mathf.Max(viewport.x / source.x, viewport.y / source.y);
            return source * scale;
        }

        /// <summary>Maps a logical authored rect into the largest centred 9:16 canvas inside a real safe area.</summary>
        public static Rect PhysicalRect(LogicalRect logical, Rect safePixels, Vector2 screenPixels)
        {
            var scale = SafeContentScale(safePixels, screenPixels);
            var safeCenter = safePixels.center;
            return new Rect(
                safeCenter.x + logical.Left * scale,
                safeCenter.y + logical.Bottom * scale,
                logical.Width * scale,
                logical.Height * scale);
        }

        public static float SafeContentScale(Rect safePixels, Vector2 screenPixels)
        {
            if (screenPixels.x <= 0f || screenPixels.y <= 0f || safePixels.width <= 0f || safePixels.height <= 0f) return 1f;
            return Mathf.Min(safePixels.width / LogicalWidth, safePixels.height / LogicalHeight);
        }

        public static float MinimumLogicalTouchHeight(Rect safePixels, Vector2 screenPixels)
            => MinimumPhysicalTouchPixels / Mathf.Max(.001f, SafeContentScale(safePixels, screenPixels));

        public static bool Contains(Rect outer, Rect inner)
            => inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;

        public static float ContrastRatio(Color foreground, Color background)
        {
            float Linear(float channel) => channel <= .04045f ? channel / 12.92f : Mathf.Pow((channel + .055f) / 1.055f, 2.4f);
            var fore = .2126f * Linear(foreground.r) + .7152f * Linear(foreground.g) + .0722f * Linear(foreground.b);
            var back = .2126f * Linear(background.r) + .7152f * Linear(background.g) + .0722f * Linear(background.b);
            return (Mathf.Max(fore, back) + .05f) / (Mathf.Min(fore, back) + .05f);
        }

        // Authored RectTransform geometry. Runtime construction and tests share this contract.
        // The player surface intentionally leaves a clear top strip for a compact title and
        // utility controls, then gives each encounter band a non-overlapping vertical lane.
        // These are visual-safe zones, not merely RectTransform bookkeeping.
        public static readonly LogicalRect Title = new LogicalRect(0f, 525f, 360f, 68f);
        public static readonly LogicalRect Status = new LogicalRect(0f, 470f, 460f, 38f);
        public static readonly LogicalRect Seals = new LogicalRect(0f, 425f, 250f, 42f);
        // The compact Rulebook control owns the only persistent route to decree copy.
        public static readonly LogicalRect Rulebook = new LogicalRect(0f, 300f, 280f, 100f);
        public static readonly LogicalRect RulebookPanel = new LogicalRect(0f, -20f, 640f, 660f);
        // Keep a visible breathing gutter before the visitor card even on the 360px inset
        // canvas; verdict feedback is a short HUD acknowledgement, never a card overlay.
        public static readonly LogicalRect Feedback = new LogicalRect(0f, 185f, 620f, 50f);
        // Preserve the native 3:2 visitor-card art ratio while creating a genuine 32px-floor
        // dossier lane. The larger card remains within every supported safe-area canvas.
        // This centre leaves a positive gap from both the feedback HUD above and the caption
        // reading lane below at every supported safe-area scale.
        public static readonly LogicalRect Card = new LogicalRect(0f, -80f, 690f, 460f);
        // A deliberate visual breathing gap remains above and below this reading field.
        public static readonly LogicalRect Caption = new LogicalRect(0f, -402f, 620f, 180f);
        public static readonly LogicalRect Admit = new LogicalRect(160f, -555f, 270f, 104f);
        public static readonly LogicalRect Deny = new LogicalRect(-160f, -555f, 270f, 104f);
        // Fixed, layered swipe feedback lives behind its labelled decision target.  It is wider
        // than the opaque target on every side, leaving a visible halo at low drag instead of a
        // debug-like strip beside the moving card.  Intent never crosses copy lanes.
        public static readonly LogicalRect DenyPreviewGlow = new LogicalRect(Deny.X, Deny.Y, 310f, 124f);
        public static readonly LogicalRect AdmitPreviewGlow = new LogicalRect(Admit.X, Admit.Y, 310f, 124f);
        // 100 logical pixels is the smallest verified utility height for the 324px-wide 360
        // inset safe area: 100 * .45 = 45 physical px. 172px width keeps the complete
        // “Motion On/Off” state on one semantic line at the 32px accessibility floor.
        public const float UtilityWidth = 172f;
        public const float UtilityHeight = 100f;
        public static readonly LogicalRect SoundUtility = new LogicalRect(274f, 555f, UtilityWidth, UtilityHeight);
        public static readonly LogicalRect MotionUtility = new LogicalRect(-274f, 555f, UtilityWidth, UtilityHeight);
        // Debug is a deliberately compact developer affordance and is not a player touch target.
        public static readonly LogicalRect Debug = new LogicalRect(300f, 472f, 84f, 58f);

        // Overlay copy occupies explicit, non-overlapping reading lanes. The heading must not
        // share the large body field: vertical centering in that field made the tutorial's
        // first instruction rise into “How To Play” in actual mobile Play.
        public static readonly LogicalRect OverlayHeading = new LogicalRect(0f, 157f, 410f, 64f);
        public static readonly LogicalRect OverlayBody = new LogicalRect(0f, -25f, 410f, 210f);
        public const float OverlayHeadingBodyLogicalGap = 45f;

        // Card-local zones. PortraitWindow is positioned from the card's bottom edge.
        // Shifted 10 logical pixels left so the 220px portrait source has a measured >=5px
        // world-corner gutter before the right-hand live-text column.
        public static readonly LogicalRect PortraitWindow = new LogicalRect(-222f, 31f, 180f, 290f);
        public static readonly LogicalRect VisitorName = new LogicalRect(70f, 184f, 340f, 50f);
        // The portrait/card quiet field provides an explicit 32px-floor dossier lane. A
        // visitor may expose one through four facts; category rows keep those facts readable
        // without turning decorative evidence art into the only source of information.
        // The left-shifted 220px portrait source reaches card-local x=-125. Text begins at -110,
        // preserving a measured >=5px world-corner gutter at the .375 runtime canvas scale.
        // The 350px column keeps full category labels and four facts legible at 32px.
        public static readonly LogicalRect Dialogue = new LogicalRect(70f, 103f, 340f, 110f);
        // Measured maximum authored dossier height is 175 logical pixels at the 32px floor;
        // this 184px lane retains explicit padding without clipping.
        // Keep the raster's right-side live-copy limit at x=240 while shifting the text's
        // left edge right with the paired icon lane. The 340px width still fits the measured
        // maximum authored dossier at the 32px floor in this unchanged 184px reading lane.
        public static readonly LogicalRect Evidence = new LogicalRect(70f, -50f, 340f, 184f);
        // One opaque but restrained reading field supports the complete live dossier: name,
        // dialogue, and factual categories. Its left edge leaves a positive gutter from the
        // portrait and icon lanes, so it never conceals either illustrated evidence source.
        public static readonly LogicalRect DossierTextBacking = new LogicalRect(80f, 34f, 360f, 370f);
        // 116 logical pixels retains a >=44 px visible alpha extent at the narrowest inset 360px viewport.
        // Two icons require 124px centre spacing.  This lane is shifted right far enough that
        // the left icon remains on the card, while the evidence column keeps a positive gutter
        // from the right icon.
        // Decorative evidence art moves into the lower-left lane, beside rather than below the
        // full dossier. This retains a real horizontal gutter while preserving every 116px
        // silhouette and card containment.
        // The root is a compact lane, not a clipping surface. Its narrower measured span
        // makes the text-backing gutter explicit while silhouettes retain their full size.
        public static readonly LogicalRect EvidenceIcons = new LogicalRect(-225f, -172f, 200f, EvidenceIconLogicalSize);
    }

    public sealed class SafeAreaLayoutRoot : MonoBehaviour
    {
        private RectTransform target;
        private Rect lastSafePixels;
        private Vector2 lastScreenPixels;
        private bool hasApplied;
        public int ApplyCount { get; private set; }
        public void Configure(RectTransform value) { target = value; Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height)); }
        public void Apply(Rect safePixels, Vector2 screenPixels)
        {
            if (target == null) return;
            if (hasApplied && safePixels == lastSafePixels && screenPixels == lastScreenPixels) return;
            var normalized = ViewportLayoutPolicy.NormalizedSafeArea(safePixels, screenPixels);
            target.anchorMin = normalized.min;
            target.anchorMax = normalized.max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            lastSafePixels = safePixels;
            lastScreenPixels = screenPixels;
            hasApplied = true;
            ApplyCount++;
            DebugTrace.Log("layout.safe_area", $"x={normalized.x:0.###};y={normalized.y:0.###};w={normalized.width:0.###};h={normalized.height:0.###}");
        }
        private void Update() => Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
    }

    /// <summary>Keeps authored logical coordinates inside the safe root without letting a notch crop fixed UI.</summary>
    public sealed class SafeAreaContentLayout : MonoBehaviour
    {
        private Rect lastSafePixels;
        private Vector2 lastScreenPixels;
        private Rect lastParentRect;
        private bool hasApplied;
        public int ApplyCount { get; private set; }

        private void OnEnable()
        {
            // CanvasScaler and the safe-area anchors settle during the canvas layout pass.  A
            // frame Update can observe either one half-applied and briefly rescale authored
            // content twice, which was the cause of the shifted 720px Game view.
            Canvas.willRenderCanvases += ApplyAfterCanvasLayout;
        }

        private void OnDisable() => Canvas.willRenderCanvases -= ApplyAfterCanvasLayout;

        private void ApplyAfterCanvasLayout() => Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));

        public void Apply(Rect safePixels, Vector2 screenPixels)
        {
            var rect = transform as RectTransform;
            if (rect == null) return;
            var canvas = GetComponentInParent<Canvas>();
            var parent = rect.parent as RectTransform;
            if (canvas == null || parent == null) return;

            // The CanvasScaler has already converted physical safe-area pixels into this
            // parent's logical coordinate system. Dividing by canvas.scaleFactor here made
            // the result depend on Unity's layout timing and could crop/shift the UI while a
            // Game view was resized. Measure the final parent rect instead.
            var parentRect = parent.rect;
            if (parentRect.width <= 0f || parentRect.height <= 0f) return;
            if (hasApplied && safePixels == lastSafePixels && screenPixels == lastScreenPixels && parentRect == lastParentRect) return;
            var scale = Mathf.Min(parentRect.width / ViewportLayoutPolicy.LogicalWidth,
                parentRect.height / ViewportLayoutPolicy.LogicalHeight);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(ViewportLayoutPolicy.LogicalWidth, ViewportLayoutPolicy.LogicalHeight);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one * scale;
            lastSafePixels = safePixels;
            lastScreenPixels = screenPixels;
            lastParentRect = parentRect;
            hasApplied = true;
            ApplyCount++;
            DebugTrace.Log("layout.safe_content", $"screen={screenPixels.x:0}x{screenPixels.y:0};safe={safePixels.x:0},{safePixels.y:0},{safePixels.width:0},{safePixels.height:0};canvasScale={canvas.scaleFactor:0.###};parent={parentRect.width:0.###}x{parentRect.height:0.###};scale={scale:0.###};logical=720x1280");
        }

        // Deliberately no Update polling: the post-layout canvas callback above owns timing.
    }
}
