using UnityEditor;
using UnityEngine;

public sealed class ClassVariantImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/DivineWar/Resources/Classes/Variants/") || !assetPath.EndsWith(".png")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        // Authored king renderers are scaled x2; summon soldier slots are scaled x1.
        var filename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool summon = filename.StartsWith("sand_soldier_") || filename.StartsWith("turret_")
            || filename.StartsWith("sun_disc_") || filename.StartsWith("daisy_");
        importer.spritePixelsPerUnit = summon ? 90 : 180;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var sprites = new SpriteMetaData[120];
        for (int direction = 0; direction < 8; direction++)
        for (int frame = 0; frame < 15; frame++)
            sprites[direction * 15 + frame] = new SpriteMetaData {
                name = $"{name}_{direction * 15 + frame:D3}",
                rect = new Rect(frame * 128, (7 - direction) * 128, 128, 128),
                alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(.5f, .125f)
            };
#pragma warning disable 618
        importer.spritesheet = sprites;
#pragma warning restore 618
    }
}
