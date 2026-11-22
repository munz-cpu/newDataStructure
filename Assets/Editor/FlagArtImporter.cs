using UnityEditor;
using UnityEngine;

public sealed class FlagArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath != "Assets/DivineWar/Resources/Buildings/flags.png") return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Multiple;
        t.spritePixelsPerUnit = 48; t.filterMode = FilterMode.Point;
        t.mipmapEnabled = false; t.alphaIsTransparency = true;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.npotScale = TextureImporterNPOTScale.None; t.maxTextureSize = 4096;
        var frames = new SpriteMetaData[16];
        for (int i = 0; i < frames.Length; i++) frames[i] = new SpriteMetaData {
            name = $"flag_{i:D3}", rect = new Rect(i * 160, 0, 160, 160),
            alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(.5f, 30f / 160)
        };
#pragma warning disable 618
        t.spritesheet = frames;
#pragma warning restore 618
    }
}
