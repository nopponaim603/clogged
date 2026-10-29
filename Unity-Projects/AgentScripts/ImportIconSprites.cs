using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports every PNG dropped into Assets/V2/Sprites/Icons as a pixel-art UI sprite.
/// Run: unity command script.run --file AgentScripts/ImportIconSprites.cs --entry ImportIconSprites.Run
/// </summary>
public static class ImportIconSprites
{
    const string Folder = "Assets/V2/Sprites/Icons";

    public static string Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        if (!Directory.Exists(Folder)) return "missing folder " + Folder;

        var report = new List<string>();
        foreach (var file in Directory.GetFiles(Folder, "*.png"))
        {
            var path = file.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                report.Add($"MISSING IMPORTER {path}");
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            report.Add(sprite == null
                ? $"FAIL {Path.GetFileName(path)}"
                : $"ok {Path.GetFileName(path),-32} {sprite.rect.width}x{sprite.rect.height}");
        }

        return string.Join("\n", report);
    }
}
