using UnityEditor;
using UnityEngine;

public sealed class FacilityArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath != "Assets/DivineWar/Resources/Buildings/facilities.png"
            && assetPath != "Assets/DivineWar/Resources/Buildings/specialists.png") return;
        bool units = assetPath.EndsWith("specialists.png");
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 48;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        int size = units ? 80 : 160, rows = units ? 4 : 10;
        var sprites = new SpriteMetaData[rows * 8];
        for (int row = 0; row < rows; row++)
        for (int dir = 0; dir < 8; dir++)
            sprites[row * 8 + dir] = new SpriteMetaData {
                name = $"{(units ? "specialist" : "facility")}_{row * 8 + dir:D3}",
                rect = new Rect(dir * size, (rows - row - 1) * size, size, size),
                alignment = (int)SpriteAlignment.Custom,
                pivot = units ? new Vector2(34f / 80, 24f / 80) : new Vector2(.5f, 30f / 160)
            };
#pragma warning disable 618
        importer.spritesheet = sprites;
#pragma warning restore 618
    }
}
