using UnityEditor;
using UnityEngine;

public sealed class TrialArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/DivineWar/Resources/Trials/") || !assetPath.EndsWith(".png")) return;
        var t = (TextureImporter)assetImporter;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool skeleton = name == "skeleton-soldier", warning = name == "trial-warning", stone = name == "trial-stone";
        int size = skeleton ? 80 : 128, count = warning ? 1 : stone ? 4 : 24;
        t.textureType = TextureImporterType.Sprite;
        t.spriteImportMode = SpriteImportMode.Multiple;
        t.spritePixelsPerUnit = warning ? 25.6f : stone ? 52 : skeleton ? 52 : 48;
        t.filterMode = FilterMode.Point; t.mipmapEnabled = false;
        t.alphaIsTransparency = true; t.npotScale = TextureImporterNPOTScale.None;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.maxTextureSize = 4096;
        var sprites = new SpriteMetaData[count];
        for (int i = 0; i < count; i++) sprites[i] = new SpriteMetaData {
            name = $"{name}_{i:D3}", rect = new Rect(i * size, 0, size, size),
            alignment = (int)SpriteAlignment.Custom, pivot = warning ? new Vector2(.5f, .5f) : new Vector2(.46f, .14f)
        };
#pragma warning disable 618
        t.spritesheet = sprites;
#pragma warning restore 618
    }

    [MenuItem("DivineWar/Trials/Import Art")]
    static void Import()
    {
        foreach (string p in System.IO.Directory.GetFiles("Assets/DivineWar/Resources/Trials", "*.png"))
            AssetDatabase.ImportAsset(p.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
    }
}
