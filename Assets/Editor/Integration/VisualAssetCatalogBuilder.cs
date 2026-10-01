using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using WhoEnters.Content;
using WhoEnters.Integration;

namespace WhoEnters.Editor.Integration
{
    /// <summary>Repeatable headless creation of direct Sprite references. Safe to invoke many times.</summary>
    public static class VisualAssetCatalogBuilder
    {
        private const string CatalogPath = "Assets/Resources/Integration/VisualAssetCatalog.asset";

        [MenuItem("Who Enters/Integration/Rebuild Visual Asset Catalog")]
        public static void RebuildFromMenu() => Rebuild();

        public static void Rebuild()
        {
            Directory.CreateDirectory("Assets/Resources/Integration");
            var catalog = AssetDatabase.LoadAssetAtPath<VisualAssetCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VisualAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.CastleVista = SpriteAt("Assets/Art/Environment/Runtime/01-castle-vista.png");
            catalog.GateFrame = SpriteAt("Assets/Art/Environment/Runtime/02-gate-frame.png");
            catalog.FogRain = SpriteAt("Assets/Art/Environment/Runtime/03-fog-rain.png");
            catalog.TorchesGlow = SpriteAt("Assets/Art/Environment/Runtime/04-torches-glow.png");
            catalog.ForegroundSilhouettes = SpriteAt("Assets/Art/Environment/Runtime/05-foreground-silhouettes.png");
            catalog.VisitorCard = SpriteAt("Assets/Art/UI/visitor-card.png");
            catalog.DecreeParchment = SpriteAt("Assets/Art/UI/decree-parchment.png");
            catalog.DayEndingPanel = SpriteAt("Assets/Art/UI/day-ending-panel.png");
            catalog.AdmitButton = SpriteAt("Assets/Art/UI/admit-button.png");
            catalog.DenyButton = SpriteAt("Assets/Art/UI/deny-button.png");
            catalog.VerdictAdmit = SpriteAt("Assets/Art/UI/verdict-admit.png");
            catalog.VerdictDeny = SpriteAt("Assets/Art/UI/verdict-deny.png");
            catalog.IntegrityIntact = SpriteAt("Assets/Art/UI/integrity-intact.png");
            catalog.IntegrityBroken = SpriteAt("Assets/Art/UI/integrity-broken.png");
            catalog.DisplayFont = FontAt("Assets/Fonts/GrenzeGotisch-SemiBold.ttf");
            catalog.BodyFont = FontAt("Assets/Fonts/AtkinsonHyperlegible-Regular.otf");
            catalog.Portraits = new List<NamedSprite>();
            foreach (var key in StoryContent.CanonicalPortraitKeys)
                catalog.Portraits.Add(new NamedSprite { Key = key, Sprite = SpriteAt("Assets/Art/Characters/" + key + ".png") });
            catalog.Evidence = new List<NamedSprite>
            {
                Pair("moon-seal"), Pair("forged-seal"), Pair("healer-writ-kit"), Pair("royal-counterseal"), Pair("red-lantern"), Pair("traitor-mark"),
            };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            catalog.ValidateOrThrow();
            Debug.Log("[WHO_ENTERS] integration.catalog_rebuilt | path=" + CatalogPath + ";sprites=36;directReferences=true");
        }

        private static NamedSprite Pair(string key) => new NamedSprite { Key = key, Sprite = SpriteAt("Assets/Art/UI/Evidence/" + key + ".png") };
        private static Sprite SpriteAt(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new System.InvalidOperationException("Missing Sprite asset at " + path);
            return sprite;
        }
        private static Font FontAt(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (font == null) throw new System.InvalidOperationException("Missing Font asset at " + path);
            return font;
        }
    }
}
