using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WhoEnters.Tests.EditMode.Art
{
    /// <summary>Guards independently-owned art paths, import settings, mirrors, and redistributable typography.</summary>
    public sealed class ArtAssetContractTests
    {
        private const string CardPath = "Assets/Art/UI/visitor-card.png";
        private const string CardMirrorPath = "public/assets/ui/visitor-card.png";
        private const string DisplayFontPath = "Assets/Fonts/GrenzeGotisch-SemiBold.ttf";
        private const string BodyFontPath = "Assets/Fonts/AtkinsonHyperlegible-Regular.otf";
        private const string ManifestPath = "docs/art/asset-manifest.json";

        [Test]
        public void LandscapeVisitorCardUsesTheNativeRuntimeAspectWithoutSlicedDistortion()
        {
            var importer = AssetImporter.GetAtPath(CardPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            Assert.That(width, Is.EqualTo(1536));
            Assert.That(height, Is.EqualTo(1024));
            Assert.That((float)width / height, Is.EqualTo(1.5f).Within(.001f));
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteBorder, Is.EqualTo(Vector4.zero));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
            Assert.That(importer.GetPlatformTextureSettings("Android").format, Is.EqualTo(TextureImporterFormat.ASTC_4x4));
            Assert.That(importer.GetPlatformTextureSettings("iPhone").format, Is.EqualTo(TextureImporterFormat.ASTC_4x4));
            CollectionAssert.AreEqual(File.ReadAllBytes(CardPath), File.ReadAllBytes(CardMirrorPath));
        }

        [Test]
        public void PortraitManifestStatesTheFramedVignetteAndBottomAlignmentContract()
        {
            var manifest = File.ReadAllText(ManifestPath);
            StringAssert.Contains("not clean cutouts", manifest);
            StringAssert.Contains("anchor bottom-centre", manifest);
            StringAssert.Contains("orange/black", manifest);
            StringAssert.DoesNotContain("LegacyRuntime.ttf", manifest);
        }

        [TestCase(DisplayFontPath, "Assets/Fonts/Licenses/Grenze-Gotisch-OFL-1.1.txt", "public/assets/fonts/GrenzeGotisch-SemiBold.ttf")]
        [TestCase(BodyFontPath, "Assets/Fonts/Licenses/Atkinson-Hyperlegible-OFL-1.1.txt", "public/assets/fonts/AtkinsonHyperlegible-Regular.otf")]
        public void ProductionFontsAreDirectlyImportableAndCarryTheirOflLicense(string fontPath, string licensePath, string mirrorPath)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Font>(fontPath), Is.Not.Null, fontPath);
            Assert.That(AssetImporter.GetAtPath(fontPath).GetType().Name, Is.EqualTo("TrueTypeFontImporter"));
            Assert.That(File.Exists(licensePath), Is.True, licensePath);
            StringAssert.Contains("SIL OPEN FONT LICENSE", File.ReadAllText(licensePath));
            CollectionAssert.AreEqual(File.ReadAllBytes(fontPath), File.ReadAllBytes(mirrorPath));
        }
    }
}
