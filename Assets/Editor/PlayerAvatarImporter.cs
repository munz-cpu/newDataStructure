using System;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(5, "hdavatars")]
public sealed class PlayerAvatarImporter : ScriptedImporter
{
    public const string CataloguePath = "Assets/DivineWar/Resources/Classes/PlayerAppearances.hdavatars";
    const string SummonPath = "Assets/DivineWar/Resources/Classes/AssignedAppearances.asset";
    const string Pack = "Assets/Images/2D HD Character pack 1 V1.2 (1)/";
    const string Unarmed = "Assets/Images/HiRezBlueKnightUnarmed/";
    const string Mounted = "Assets/DivineWar/Resources/Classes/Variants/";
    const string Greatsword = "Assets/Images/8 Directional Greatsword Knight Character/";
    const int DirectionCount = 8;
    const int FramesPerDirection = 15;
    static readonly string[] Directions = { "E", "SE", "S", "SW", "W", "NW", "N", "NE" };
    static readonly int[] BowAngles = { 0, 315, 270, 225, 180, 135, 90, 45 };

    public override void OnImportAsset(AssetImportContext context)
    {
        var commander = Appearance(왕직업.지휘관, "기마 기사",
            MountedFrames(context, "idle"), MountedFrames(context, "walk"), MountedFrames(context, "attack"));
        var guardian = Appearance(왕직업.수호자, "HiRezBlueKnightUnarmed",
            UnarmedFrames(context, "Idle", 25), UnarmedFrames(context, "WalkFoward", 11),
            UnarmedFrames(context, "Slash", 15));
        var archer = Appearance(왕직업.아처, "9Longbow",
            BowFrames(context, "Idle"), BowFrames(context, "Walk"), BowFrames(context, "QuickShot"));
        var swordsman = Appearance(왕직업.검사, "1Knight",
            KnightFrames(context, "Idle"), KnightFrames(context, "Walk"), KnightFrames(context, "Melee"));

        context.DependsOnArtifact(SummonPath);
        var summons = AssetDatabase.LoadAssetAtPath<왕외형목록>(SummonPath);
        if (summons == null)
            throw new InvalidOperationException($"Missing summon catalogue: {SummonPath}");

        var catalogue = ScriptableObject.CreateInstance<왕외형목록>();
        catalogue.name = "PlayerAppearances";
        var assassin = Appearance(왕직업.암살자, "Greatsword Knight",
            GreatswordFrames(context, "2hIdle5", 6, 26),
            GreatswordFrames(context, "2hWalk", 3, 9),
            GreatswordFrames(context, "Slash", 4, 12));
        catalogue.직업들 = new[] { guardian, commander, archer, swordsman, assassin };
        catalogue.소환물들 = summons.소환물들;
        catalogue.검사회전 = KnightFrames(context, "MeleeSpin");
        catalogue.검사구르기 = KnightFrames(context, "Rolling");
        context.AddObjectToAsset("catalogue", catalogue);
        context.SetMainObject(catalogue);
    }

    static 왕외형목록.배정 Appearance(왕직업 job, string label, Sprite[] idle, Sprite[] walk, Sprite[] attack) =>
        new 왕외형목록.배정 { 직업 = job, 에셋이름 = label, 대기 = idle, 이동 = walk, 공격 = attack };

    static Sprite[] MountedFrames(AssetImportContext context, string action)
    {
        var prefix = $"hecarim_{action}";
        var texture = SourceTexture(context, $"{Mounted}{prefix}.png");
        return GridFrames(context, texture, prefix, .125f);
    }

    static Sprite[] KnightFrames(AssetImportContext context, string action)
    {
        var texture = SourceTexture(context, $"{Pack}Spritesheets/Shadowless/1Knight/{action}_Shadowless.png");
        return GridFrames(context, texture, $"knight_{action}", 28f / 128);
    }

    static Sprite[] GridFrames(AssetImportContext context, Texture2D texture, string prefix, float pivotY)
    {
        const int cell = 128;
        var sprites = new Sprite[DirectionCount * FramesPerDirection];
        for (int direction = 0; direction < DirectionCount; direction++)
        for (int frame = 0; frame < FramesPerDirection; frame++)
        {
            var rect = new Rect(frame * cell, (DirectionCount - 1 - direction) * cell, cell, cell);
            int index = direction * FramesPerDirection + frame;
            sprites[index] = AddFrame(context, $"{prefix}_{index:D3}", texture, rect, pivotY, 120);
        }
        return sprites;
    }

    static Sprite[] GreatswordFrames(AssetImportContext context, string action, int columns, int sourceFrameCount)
    {
        const int sourceCell = 512;
        int rows = (sourceFrameCount + columns - 1) / columns;
        var sprites = new Sprite[DirectionCount * FramesPerDirection];
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            // The supplied sheets start at southwest and progress clockwise through west.
            int sourceDirection = (direction + 5) % DirectionCount + 1;
            string path = $"{Greatsword}{action}/{action}/GreatSwordKnight_{action}_dir{sourceDirection}.png";
            var texture = SourceTexture(context, path);
            float cellWidth = (float)texture.width / columns;
            float cellHeight = (float)texture.height / rows;
            var sourceFrames = new Sprite[sourceFrameCount];
            for (int frame = 0; frame < sourceFrameCount; frame++)
            {
                var rect = new Rect(frame % columns * cellWidth, texture.height - (frame / columns + 1) * cellHeight,
                    cellWidth, cellHeight);
                sourceFrames[frame] = AddFrame(context, $"greatsword_{action}_{direction}_{frame:D2}",
                    texture, rect, 241f / sourceCell, 210f * cellHeight / sourceCell);
            }
            for (int frame = 0; frame < FramesPerDirection; frame++)
                sprites[direction * FramesPerDirection + frame] = sourceFrames[frame * sourceFrameCount / FramesPerDirection];
        }
        return sprites;
    }

    static Sprite[] BowFrames(AssetImportContext context, string action)
    {
        var sprites = new Sprite[DirectionCount * FramesPerDirection];
        for (int direction = 0; direction < DirectionCount; direction++)
        for (int frame = 0; frame < FramesPerDirection; frame++)
        {
            string file = $"{action}_{BowAngles[direction]}_{frame * 2 + 1:D3}.png";
            var texture = SourceTexture(context, $"{Pack}9Longbow/{action}/{Directions[direction]}/{file}");
            var rect = new Rect(0, 0, texture.width, texture.height);
            int index = direction * FramesPerDirection + frame;
            sprites[index] = AddFrame(context, $"longbow_{action}_{index:D3}", texture, rect, 28f / 128, 110);
        }
        return sprites;
    }

    static Sprite[] UnarmedFrames(AssetImportContext context, string action, int sourceFrameCount)
    {
        const int cell = 256;
        var sprites = new Sprite[DirectionCount * FramesPerDirection];
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            // The unarmed sheets start at west. DivineWar starts at east.
            int sourceDirection = (direction + 4) % DirectionCount + 1;
            var texture = SourceTexture(context, $"{Unarmed}{action}/HiRezUnarmedKnight_Unarmed_export_{action}_dir{sourceDirection}.png");
            int columns = texture.width / cell;
            var sourceFrames = new Sprite[sourceFrameCount];
            for (int frame = 0; frame < sourceFrameCount; frame++)
            {
                var rect = new Rect(frame % columns * cell, texture.height - (frame / columns + 1) * cell, cell, cell);
                sourceFrames[frame] = AddFrame(context, $"unarmed_{action}_{direction}_{frame:D2}", texture, rect, 119f / cell, 175);
            }
            for (int frame = 0; frame < FramesPerDirection; frame++)
                sprites[direction * FramesPerDirection + frame] = sourceFrames[frame * sourceFrameCount / FramesPerDirection];
        }
        return sprites;
    }

    static Texture2D SourceTexture(AssetImportContext context, string path)
    {
        context.DependsOnArtifact(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path)
            ?? throw new InvalidOperationException($"Missing player avatar texture: {path}");
    }

    static Sprite AddFrame(AssetImportContext context, string id, Texture2D texture, Rect rect, float pivotY, float pixelsPerUnit)
    {
        // Keep the original pixels, including any details beyond the auto-sliced bounds.
        var sprite = Sprite.Create(texture, rect, new Vector2(.5f, pivotY), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = id;
        context.AddObjectToAsset(id, sprite);
        return sprite;
    }
}
