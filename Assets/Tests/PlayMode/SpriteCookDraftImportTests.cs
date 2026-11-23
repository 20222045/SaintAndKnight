#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Validate the imported review assets without changing gameplay or scenes.</summary>
public sealed class SpriteCookDraftImportTests
{
    private const string Root = "Assets/SaintKnightSpriteCook/Drafts/Sliced/";

    [Test]
    public void All59FramesImportAsPixelSpritesWithConsistentPivots()
    {
        var paths = Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories);
        Assert.That(paths.Length, Is.EqualTo(59));
        foreach (var path in paths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(128, 96)), path);
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(48), path);
            bool ui = path.Contains("hp_orb");
            Assert.That(sprite.pivot.x, Is.EqualTo(64).Within(.001f), path);
            Assert.That(sprite.pivot.y, Is.EqualTo(ui ? 48 : 25.56f).Within(.001f), path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
        }
    }

    private static Color32[] Pixels(string sheet, int frame)
    {
        string path = Root + sheet + "/" + frame.ToString("00") + ".png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Assert.That(sprite, Is.Not.Null, path);
        return sprite.texture.GetPixels32();
    }

    [Test]
    public void LiftPosesKeepTheirVisibleGapAboveTheGround()
    {
        foreach (string character in new[] { "cecilia", "adela" })
        {
            for (int frame = 4; frame < 8; frame++)
            {
                var pixels = Pixels("2_boss_pattern_" + character + "_v2", frame);
                int bottom = 96;
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i].a > 0) bottom = Mathf.Min(bottom, i / 128);
                // PNG top-down foot y=80: imported texture bottom-up y=15.
                Assert.That(bottom, Is.EqualTo(15), character + " lift " + frame);
            }
        }
    }

    [Test]
    public void GlassAndBothOrnamentsLayerWithoutRimOverlap()
    {
        var glass = Pixels("5_hp_orb_ornaments_v2", 0);
        for (int frame = 1; frame <= 2; frame++)
        {
            var ornament = Pixels("5_hp_orb_ornaments_v2", frame);
            int opaque = 0;
            for (int i = 0; i < glass.Length; i++)
            {
                if (ornament[i].a > 0) opaque++;
                Assert.That(glass[i].a == 0 || ornament[i].a == 0, Is.True,
                    "Overlap at pixel " + i + " in ornament " + frame);
            }
            Assert.That(opaque, Is.GreaterThan(100));
        }
    }
}
#endif
