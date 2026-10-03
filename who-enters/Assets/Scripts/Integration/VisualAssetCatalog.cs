using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WhoEnters.Content;
using WhoEnters.Core;

namespace WhoEnters.Integration
{
    [Serializable]
    public sealed class NamedSprite
    {
        public string Key;
        public Sprite Sprite;
    }

    /// <summary>
    /// The sole serialized visual binding surface. It holds references, never copied textures;
    /// editor validation makes a production key failure visible before Play mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Who Enters/Visual Asset Catalog", fileName = "VisualAssetCatalog")]
    public sealed class VisualAssetCatalog : ScriptableObject
    {
        public const string ResourcePath = "Integration/VisualAssetCatalog";
        public Sprite CastleVista;
        public Sprite GateFrame;
        public Sprite FogRain;
        public Sprite TorchesGlow;
        public Sprite ForegroundSilhouettes;
        public Sprite VisitorCard;
        public Sprite DecreeParchment;
        public Sprite DayEndingPanel;
        public Sprite AdmitButton;
        public Sprite DenyButton;
        public Sprite VerdictAdmit;
        public Sprite VerdictDeny;
        public Sprite IntegrityIntact;
        public Sprite IntegrityBroken;
        public Font DisplayFont;
        public Font BodyFont;
        public List<NamedSprite> Portraits = new List<NamedSprite>();
        public List<NamedSprite> Evidence = new List<NamedSprite>();

        public static readonly string[] EnvironmentKeys =
        {
            "castle-vista", "gate-frame", "fog-rain", "torches-glow", "foreground-silhouettes",
        };

        public static VisualAssetCatalog LoadRequired()
        {
            var catalog = Resources.Load<VisualAssetCatalog>(ResourcePath);
            if (catalog == null)
            {
                DebugTrace.Error("integration.catalog_missing", "path=Resources/" + ResourcePath);
                throw new InvalidOperationException("VisualAssetCatalog is missing. Run the headless visual catalog builder before Play mode.");
            }
            catalog.ValidateOrThrow();
            return catalog;
        }

        public Sprite PortraitFor(string key) => Find(Portraits, key, "portrait");
        /// <summary>Resolves an authored content cue (for example, "moon seal").</summary>
        public Sprite EvidenceForAuthoredCue(string authoredCue)
        {
            var key = EvidenceKeyForAuthoredCue(authoredCue);
            return string.IsNullOrEmpty(key) ? null : EvidenceForKey(key);
        }
        /// <summary>Resolves a catalog-normalized key (for example, "moon-seal") exactly once.</summary>
        public Sprite EvidenceForKey(string normalizedKey) => Find(Evidence, normalizedKey, "evidence", false);

        public void ValidateOrThrow()
        {
            var required = new Dictionary<string, Sprite>
            {
                { "castle-vista", CastleVista }, { "gate-frame", GateFrame }, { "fog-rain", FogRain },
                { "torches-glow", TorchesGlow }, { "foreground-silhouettes", ForegroundSilhouettes },
                { "visitor-card", VisitorCard }, { "decree-parchment", DecreeParchment },
                { "day-ending-panel", DayEndingPanel }, { "admit-button", AdmitButton }, { "deny-button", DenyButton },
                { "verdict-admit", VerdictAdmit }, { "verdict-deny", VerdictDeny },
                { "integrity-intact", IntegrityIntact }, { "integrity-broken", IntegrityBroken },
            };
            foreach (var pair in required)
                if (pair.Value == null) Fail("integration.asset_missing", "key=" + pair.Key);
            if (DisplayFont == null) Fail("integration.font_missing", "role=display;fallback=forbidden");
            if (BodyFont == null) Fail("integration.font_missing", "role=body;fallback=forbidden");
            if (DisplayFont == BodyFont) Fail("integration.font_duplicate", "roles=display,body");

            ValidateMap(Portraits, StoryContent.CanonicalPortraitKeys, "portrait");
            ValidateMap(Evidence, new[] { "moon-seal", "forged-seal", "healer-writ-kit", "royal-counterseal", "red-lantern", "traitor-mark" }, "evidence");
            var spriteRefs = AllSprites().ToArray();
            if (spriteRefs.Distinct().Count() != spriteRefs.Length)
                Fail("integration.asset_duplicate_reference", "count=" + spriteRefs.Length + ";unique=" + spriteRefs.Distinct().Count());
            DebugTrace.Log("integration.catalog_validated", "portraits=16;evidence=6;environment=5;ui=9;duplicateRefs=false");
        }

        public IEnumerable<Sprite> AllSprites()
        {
            yield return CastleVista; yield return GateFrame; yield return FogRain; yield return TorchesGlow; yield return ForegroundSilhouettes;
            yield return VisitorCard; yield return DecreeParchment; yield return DayEndingPanel; yield return AdmitButton; yield return DenyButton;
            yield return VerdictAdmit; yield return VerdictDeny; yield return IntegrityIntact; yield return IntegrityBroken;
            foreach (var pair in Portraits) yield return pair.Sprite;
            foreach (var pair in Evidence) yield return pair.Sprite;
        }

        private void ValidateMap(List<NamedSprite> entries, IReadOnlyList<string> requiredKeys, string category)
        {
            if (entries == null || entries.Count != requiredKeys.Count) Fail("integration.asset_count_invalid", "category=" + category);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || entry.Sprite == null)
                    Fail("integration.asset_missing", "category=" + category);
                if (!keys.Add(entry.Key)) Fail("integration.asset_duplicate_key", "category=" + category + ";key=" + entry.Key);
            }
            foreach (var key in requiredKeys)
                if (!keys.Contains(key)) Fail("integration.asset_key_missing", "category=" + category + ";key=" + key);
        }

        private Sprite Find(List<NamedSprite> entries, string key, string category, bool required = true)
        {
            var sprite = entries?.FirstOrDefault(item => item != null && string.Equals(item.Key, key, StringComparison.Ordinal))?.Sprite;
            if (sprite != null) return sprite;
            if (!required) return null;
            Fail("integration.asset_key_unresolved", "category=" + category + ";key=" + key + ";fallback=forbidden");
            return null;
        }

        public static string EvidenceKeyForAuthoredCue(string authoredCue)
        {
            switch (authoredCue)
            {
                case "moon seal": return "moon-seal";
                case "forged seal": return "forged-seal";
                case "healer writ":
                case "healer kit": return "healer-writ-kit";
                case "royal counterseal": return "royal-counterseal";
                case "red lantern": return "red-lantern";
                case "cult sigil":
                case "traitor mark": return "traitor-mark";
                default: return string.Empty;
            }
        }

        private static void Fail(string eventId, string payload)
        {
            DebugTrace.Error(eventId, payload);
            throw new InvalidOperationException(payload);
        }
    }
}
