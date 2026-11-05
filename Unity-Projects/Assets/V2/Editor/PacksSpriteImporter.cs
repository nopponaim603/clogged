using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports the raw Kenney PNGs under <c>Assets/V2/Sprites/Packs</c> as sprites.
///
/// The project runs in 3D behaviour mode, so a freshly dropped PNG becomes a
/// plain <see cref="Texture2D"/> and cannot be assigned to an <c>Image.sprite</c>
/// until someone flips its type by hand - 1,890 times. Scoping the rule to that
/// one folder keeps it away from every other import in the project.
///
/// The packs are third-party reference art (git-ignored); the icons the game
/// actually ships live in <c>Assets/V2/Sprites/Icons</c> with hand-made metas.
/// </summary>
public class PacksSpriteImporter : AssetPostprocessor
{
    private const string PacksRoot = "Assets/V2/Sprites/Packs/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PacksRoot)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 512;
    }
}
