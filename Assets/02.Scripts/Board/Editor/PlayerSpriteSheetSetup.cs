using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerSpriteSheetSetup
{
    private const string SourceFolder = "Assets/05.Art/Player";
    private const string OutputFolder = "Assets/Resources/Player";
    private const int FrameWidth = 240;
    private const int FrameHeight = 288;
    private const int SheetHeight = 4608;
    private static readonly string[] AnimationNames = { "idle", "hop", "cheer", "sad" };
    private static readonly string[] DirectionNames = { "se", "sw", "ne", "nw" };
    private static readonly int[] FrameCounts = { 6, 8, 6, 6 };
    private static bool isRunning;

    static PlayerSpriteSheetSetup()
    {
        EditorApplication.delayCall += EnsurePlayerSprites;
    }

    [MenuItem("Tools/Our Marble/Setup Player Sprites")]
    public static void EnsurePlayerSprites()
    {
        if (isRunning || EditorApplication.isPlayingOrWillChangePlaymode) return;

        isRunning = true;
        try
        {
            EnsureOutputFolder();
            for (int player = 1; player <= 4; player++)
            {
                string texturePath = $"{SourceFolder}/player_{player}.png";
                ConfigureImporter(texturePath, $"player_{player}");
                CreateOrUpdateSpriteSet(texturePath, player);
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            isRunning = false;
        }
    }

    private static void ConfigureImporter(string texturePath, string spritePrefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"플레이어 스프라이트를 찾을 수 없습니다: {texturePath}");
            return;
        }

        SpriteMetaData[] expectedSprites = BuildSpriteMetadata(spritePrefix);
#pragma warning disable CS0618
        bool needsImport = importer.spriteImportMode != SpriteImportMode.Multiple
            || importer.spritePixelsPerUnit != 100f
            || importer.mipmapEnabled
            || importer.maxTextureSize != 8192
            || importer.spritesheet.Length != expectedSprites.Length
            || importer.spritesheet.Length == 0
            || importer.spritesheet[0].name != expectedSprites[0].name;
#pragma warning restore CS0618

        if (!needsImport) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 8192;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
#pragma warning disable CS0618
        importer.spritesheet = expectedSprites;
#pragma warning restore CS0618

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static SpriteMetaData[] BuildSpriteMetadata(string spritePrefix)
    {
        var metadata = new List<SpriteMetaData>(104);
        int row = 0;

        for (int animation = 0; animation < AnimationNames.Length; animation++)
        {
            int frameCount = FrameCounts[animation];
            for (int direction = 0; direction < DirectionNames.Length; direction++, row++)
            {
                for (int frame = 0; frame < frameCount; frame++)
                {
                    metadata.Add(new SpriteMetaData
                    {
                        name = $"{spritePrefix}_{AnimationNames[animation]}_{DirectionNames[direction]}_{frame}",
                        rect = new Rect(frame * FrameWidth, SheetHeight - (row + 1) * FrameHeight, FrameWidth, FrameHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 28f / FrameHeight),
                        border = Vector4.zero
                    });
                }
            }
        }

        return metadata.ToArray();
    }

    private static void CreateOrUpdateSpriteSet(string texturePath, int player)
    {
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name);

        string assetPath = $"{OutputFolder}/player_{player}.asset";
        PlayerSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<PlayerSpriteSet>(assetPath);
        if (spriteSet == null)
        {
            spriteSet = ScriptableObject.CreateInstance<PlayerSpriteSet>();
            AssetDatabase.CreateAsset(spriteSet, assetPath);
        }

        spriteSet.SetFrames(
            CollectFrames(sprites, player, "idle", 6),
            CollectFrames(sprites, player, "hop", 8),
            CollectFrames(sprites, player, "cheer", 6),
            CollectFrames(sprites, player, "sad", 6));
        EditorUtility.SetDirty(spriteSet);
    }

    private static Sprite[] CollectFrames(Dictionary<string, Sprite> sprites, int player, string animation, int frameCount)
    {
        var result = new Sprite[DirectionNames.Length * frameCount];
        for (int direction = 0; direction < DirectionNames.Length; direction++)
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                string name = $"player_{player}_{animation}_{DirectionNames[direction]}_{frame}";
                sprites.TryGetValue(name, out result[direction * frameCount + frame]);
            }
        }
        return result;
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Player");
    }
}
