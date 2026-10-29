using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace V2.EditorTools
{
/// <summary>
/// Bakes a TextMeshPro bitmap font out of the letter/number tiles that live in
/// Assets/Day/Sprites/tilemap_packed.png.
///
/// The sheet is a 16x16 grid. Three rows carry characters:
///   row 6 -> A..M   row 7 -> N..Z   row 8 -> % + - 0..9
/// Rows 9-10 hold a flat A..Z variant with no digits, so they are not used.
///
/// Only the white letterform is kept: the tiles draw each glyph on a dark plate
/// with a lavender lower shade, and TMP tints glyphs by alpha, so copying the
/// plate would paint solid blocks instead of letters.
///
/// The sheet carries no punctuation, so the marks the UI actually prints
/// ( : / ( ) [ ] , . - ) are drawn procedurally in the same 2px-stroke style.
/// Without them every colon and bracket would fall back to LiberationSans and
/// the HUD would mix two typefaces.
///
/// FaceInfo.pointSize is set to 11 rather than 16 so cap height / em is ~0.73,
/// matching the LiberationSans metrics the layout was built against - swapping
/// the font does not resize the text.
///
/// Re-runnable from Tools > V2 > Rebuild Pixel Font; it overwrites the atlas,
/// material and font asset in place.
/// </summary>
public static class PixelFontBuilder
{
    private const string SourcePath = "Assets/Day/Sprites/tilemap_packed.png";
    private const string SourceCopyPath = "Assets/V2/Fonts/Source/tilemap_packed.png";
    private const string OutDir = "Assets/V2/Fonts";
    private const string AtlasPath = OutDir + "/PixelTilesetAtlas.png";
    private const string MaterialPath = OutDir + "/PixelTileset.mat";
    private const string FontPath = OutDir + "/PixelTileset.asset";
    private const string FallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private const int Cell = 16;
    private const int AdvanceGap = 1;
    private const int SpaceAdvance = 6;
    private const int AtlasCols = 8;

    // Cap height is 8px. Point size 11 puts cap/em at 0.727, the same optical
    // size LiberationSans renders at, so existing font sizes keep their layout.
    private const int FacePointSize = 11;
    private const int BaselineRow = 5;   // y of the ink bottom inside a 16px cell

    private static readonly StringBuilder Log = new StringBuilder();

    [MenuItem("Tools/V2/Rebuild Pixel Font")]
    public static void Run()
    {
        Log.Length = 0;

        Texture2D sheet = LoadSheet();
        if (sheet == null) { Debug.LogError("PixelFontBuilder: source sheet not found."); return; }

        List<GlyphSource> glyphs = CollectGlyphs(sheet);
        Log.AppendLine($"collected {glyphs.Count} glyphs from {sheet.width}x{sheet.height}");

        EnsureFolder(OutDir);
        EnsureFolder(OutDir + "/Source");

        // Keep a copy next to the font so the pipeline no longer depends on Assets/Day.
        File.WriteAllBytes(SourceCopyPath, File.ReadAllBytes(SourcePath));
        AssetDatabase.ImportAsset(SourceCopyPath, ImportAssetOptions.ForceUpdate);

        Texture2D atlas = BuildAtlas(sheet, glyphs, out int atlasW, out int atlasH);
        UnityEngine.Object.DestroyImmediate(sheet);

        File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        ConfigureAtlasImporter();
        atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        TMP_FontAsset fontAsset = BuildFontAsset(atlas, glyphs, atlasW, atlasH);
        if (fontAsset == null) return;

        Preview(fontAsset);

        Debug.Log(Log.ToString());
        Debug.Log($"PixelFontBuilder: created {FontPath}, {MaterialPath}, {AtlasPath}");
    }

    private sealed class GlyphSource
    {
        public char Character;
        public int Col;          // tile column in the sheet, then reused as atlas column
        public int Row;          // tile row in the sheet, then reused as atlas row
        public string[] Pattern; // non-null for procedural glyphs, row 0 = top
        public int InkX;         // ink bounds inside the cell, bottom-up
        public int InkY;
        public int InkW;
        public int InkH;
        public bool IsBlank;
        public bool IsSynthetic => Pattern != null;
    }

    /// <summary>
    /// Marks the sheet has no tile for. Every one is drawn in the same 2px
    /// stroke weight the letters use, in a 6x8 cell sitting on the same baseline.
    /// </summary>
    private static readonly (char Character, string[] Rows)[] SyntheticGlyphs =
    {
        ('.', new[] { "......", "......", "......", "......", "......", "......", "..##..", "..##.." }),
        (',', new[] { "......", "......", "......", "......", "......", "..##..", "..##..", ".##..." }),
        (':', new[] { "......", "..##..", "..##..", "......", "......", "..##..", "..##..", "......" }),
        (';', new[] { "......", "..##..", "..##..", "......", "......", "..##..", "..##..", ".##..." }),
        ('!', new[] { "..##..", "..##..", "..##..", "..##..", "..##..", "......", "..##..", "..##.." }),
        ('?', new[] { ".####.", "##..##", "....##", "...##.", "..##..", "......", "..##..", "..##.." }),
        ('(', new[] { "...##.", "..##..", ".##...", ".##...", ".##...", ".##...", "..##..", "...##." }),
        (')', new[] { ".##...", "..##..", "...##.", "...##.", "...##.", "...##.", "..##..", ".##..." }),
        ('/', new[] { "....##", "....##", "...##.", "..##..", "..##..", ".##...", "##....", "##...." }),
        ('\\', new[] { "##....", "##....", ".##...", "..##..", "..##..", "...##.", "....##", "....##" }),
        ('[', new[] { ".####.", ".##...", ".##...", ".##...", ".##...", ".##...", ".##...", ".####." }),
        (']', new[] { ".####.", "...##.", "...##.", "...##.", "...##.", "...##.", "...##.", ".####." }),
        ('_', new[] { "......", "......", "......", "......", "......", "......", "......", "######" }),
        ('-', new[] { "......", "......", "......", "......", "......", ".####.", "......", "......" }),
        ('=', new[] { "......", "......", "######", "......", "######", "......", "......", "......" }),
        ('<', new[] { "....##", "...##.", "..##..", ".##...", "..##..", "...##.", "....##", "......" }),
        ('>', new[] { "##....", ".##...", "..##..", "...##.", "..##..", ".##...", "##....", "......" }),
        ('\'', new[] { "..##..", "..##..", "......", "......", "......", "......", "......", "......" }),
        ('"', new[] { ".##.##", ".##.##", "......", "......", "......", "......", "......", "......" }),
        ('\u2013', new[] { "........", "........", "........", "........", "########", "........", "........", "........" }),
        ('\u2014', new[] { "........", "........", "........", "........", "########", "........", "........", "........" }),
        ('\u2022', new[] { "......", "......", "..##..", ".####.", ".####.", "..##..", "......", "......" }),
        ('\u2026', new[] { "........", "........", "........", "........", "........", "........", "##.##.##", "##.##.##" }),
    };

    private static Texture2D LoadSheet()
    {
        string path = File.Exists(SourcePath) ? SourcePath : SourceCopyPath;
        if (!File.Exists(path)) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        return tex;
    }

    private static List<GlyphSource> CollectGlyphs(Texture2D sheet)
    {
        // Build the requested glyph list first, then measure each cell.
        var map = new List<GlyphSource>();

        for (int i = 0; i < 13; i++) map.Add(new GlyphSource { Character = (char)('A' + i), Row = 6, Col = i });
        for (int i = 0; i < 13; i++) map.Add(new GlyphSource { Character = (char)('N' + i), Row = 7, Col = i });

        map.Add(new GlyphSource { Character = '%', Row = 8, Col = 0 });
        map.Add(new GlyphSource { Character = '+', Row = 8, Col = 1 });
        map.Add(new GlyphSource { Character = '-', Row = 8, Col = 2 });
        for (int i = 0; i < 10; i++) map.Add(new GlyphSource { Character = (char)('0' + i), Row = 8, Col = 3 + i });

        foreach (var g in map)
        {
            int cellY = sheet.height - (g.Row + 1) * Cell;   // bottom-up cell origin

            int minX = Cell, minY = Cell, maxX = -1, maxY = -1;
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    if (cellY + y < 0 || cellY + y >= sheet.height) continue;
                    if (g.Col * Cell + x >= sheet.width) continue;
                    if (!IsInk(sheet.GetPixel(g.Col * Cell + x, cellY + y))) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

            if (maxX < 0)
            {
                g.IsBlank = true;
                continue;
            }

            g.InkX = minX;
            g.InkY = minY;
            g.InkW = maxX - minX + 1;
            g.InkH = maxY - minY + 1;
        }

        // Add the procedural marks, skipping any the sheet already covers.
        var taken = new HashSet<char>(map.Select(g => g.Character));
        foreach (var (character, rows) in SyntheticGlyphs)
        {
            if (!taken.Add(character)) continue;

            var g = new GlyphSource { Character = character, Pattern = rows };
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

            for (int row = 0; row < rows.Length; row++)
                for (int x = 0; x < rows[row].Length; x++)
                {
                    if (rows[row][x] == '.') continue;

                    // Row 7 sits on the baseline, matching the tile letterforms.
                    int y = BaselineRow + (rows.Length - 1 - row);
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

            if (maxX < 0) { g.IsBlank = true; map.Add(g); continue; }

            g.InkX = minX;
            g.InkY = minY;
            g.InkW = maxX - minX + 1;
            g.InkH = maxY - minY + 1;
            map.Add(g);
        }

        return map;
    }

    /// <summary>
    /// True for the white letterform pixels. The tiles draw each glyph as a
    /// white letter on a dark plate with a lavender lower shade; only the white
    /// part is the typeface. TMP's bitmap shader tints glyphs by their alpha, so
    /// leaving the plate opaque would paint a solid block instead of a letter.
    /// </summary>
    private static bool IsInk(Color c)
    {
        return c.a > 0.5f && c.r > 0.85f && c.g > 0.85f && c.b > 0.85f;
    }

    private static Texture2D BuildAtlas(Texture2D sheet, List<GlyphSource> glyphs, out int atlasW, out int atlasH)
    {
        // One extra slot at the end for the space glyph's blank pixel.
        int slots = glyphs.Count + 1;
        int rows = Mathf.CeilToInt(slots / (float)AtlasCols);
        atlasW = AtlasCols * Cell;
        atlasH = rows * Cell;

        var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
        var clear = new Color[atlasW * atlasH];
        atlas.SetPixels(clear);

        for (int i = 0; i < glyphs.Count; i++)
        {
            var g = glyphs[i];
            int destX = (i % AtlasCols) * Cell;
            int destY = (i / AtlasCols) * Cell;

            if (g.IsBlank)
            {
                g.Col = destX / Cell;
                g.Row = destY / Cell;
                continue;
            }

            // Write the glyph as white-on-transparent so the shader tint applies
            // cleanly at any text colour.
            Color[] block = new Color[Cell * Cell];

            if (g.IsSynthetic)
            {
                for (int row = 0; row < g.Pattern.Length; row++)
                    for (int x = 0; x < g.Pattern[row].Length; x++)
                    {
                        if (g.Pattern[row][x] == '.') continue;
                        int y = BaselineRow + (g.Pattern.Length - 1 - row);
                        if (x < Cell && y < Cell) block[y * Cell + x] = Color.white;
                    }
            }
            else
            {
                int srcY = sheet.height - (g.Row + 1) * Cell;
                Color[] tile = sheet.GetPixels(g.Col * Cell, srcY, Cell, Cell);
                for (int p = 0; p < tile.Length; p++)
                    block[p] = IsInk(tile[p]) ? Color.white : Color.clear;
            }

            atlas.SetPixels(destX, destY, Cell, Cell, block);
            g.Col = destX / Cell;   // reuse Col/Row as the atlas cell index
            g.Row = destY / Cell;
        }

        atlas.Apply(false, false);
        return atlas;
    }

    private static void ConfigureAtlasImporter()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static TMP_FontAsset BuildFontAsset(Texture2D atlas, List<GlyphSource> glyphs, int atlasW, int atlasH)
    {
        var shader = Shader.Find("TextMeshPro/Bitmap");
        if (shader == null) shader = Shader.Find("TextMeshPro/Mobile/Bitmap");
        if (shader == null) { Debug.LogError("PixelFontBuilder: no bitmap shader found."); return null; }

        // Reuse the existing assets instead of delete + create: recreating them
        // gives a new object identity, so every component referencing the old
        // one sees a destroyed asset and TMP logs "assign a Font Asset".
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "PixelTileset Material" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else material.shader = shader;

        material.SetTexture("_MainTex", atlas);
        material.SetFloat("_TextureWidth", atlasW);
        material.SetFloat("_TextureHeight", atlasH);
        material.SetFloat("_GradientScale", 1f);
        material.SetFloat("_ScaleRatioA", 1f);
        material.SetColor("_FaceColor", Color.white);
        material.SetFloat("_WeightNormal", 0f);
        material.SetFloat("_WeightBold", 0.75f);
        EditorUtility.SetDirty(material);

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (fontAsset == null)
        {
            fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            fontAsset.name = "PixelTileset";
            AssetDatabase.CreateAsset(fontAsset, FontPath);
        }

        fontAsset.atlas = atlas;

        // TMP exposes these as read-only to outside code (internal setters), so
        // the serialised fields are written directly.
        SetField(fontAsset, "m_Version", "1.1.0");
        SetField(fontAsset, "m_AtlasTextures", new[] { atlas });
        SetField(fontAsset, "m_AtlasTextureIndex", 0);
        SetField(fontAsset, "m_AtlasWidth", atlasW);
        SetField(fontAsset, "m_AtlasHeight", atlasH);
        SetField(fontAsset, "m_AtlasPadding", 0);
        SetField(fontAsset, "m_AtlasPopulationMode", AtlasPopulationMode.Static);
        SetField(fontAsset, "m_AtlasRenderMode", GlyphRenderMode.SMOOTH);
        SetField(fontAsset, "m_IsMultiAtlasTexturesEnabled", false);
        SetField(fontAsset, "m_Material", material);

        // Cap height is the distance from the ink bottom to its top. The tiles
        // carry 5px of plate below the letterform, so that padding is removed
        // here: a glyph's bearing is measured from the baseline, not the cell.
        int capHeight = 0;
        foreach (var g in glyphs.Where(g => !g.IsBlank))
            capHeight = Mathf.Max(capHeight, g.InkY + g.InkH - BaselineRow);

        fontAsset.faceInfo = new FaceInfo
        {
            familyName = "Clogged Pixel",
            styleName = "Regular",
            pointSize = FacePointSize,
            scale = 1f,
            lineHeight = FacePointSize + 2,
            ascentLine = capHeight + 0.5f,
            capLine = capHeight,
            meanLine = capHeight * 0.69f,
            baseline = 0f,
            descentLine = -2.5f,
            underlineOffset = -2f,
            underlineThickness = 1f,
            strikethroughOffset = capHeight * 0.45f,
            strikethroughThickness = 1f,
            superscriptOffset = capHeight * 0.6f,
            superscriptSize = 0.5f,
            subscriptOffset = -2f,
            subscriptSize = 0.5f,
            tabWidth = FacePointSize * 2f
        };

        var glyphTable = new List<Glyph>();
        var characterTable = new List<TMP_Character>();
        uint nextIndex = 0;

        // Letters are inset 5px inside the tile because of the plate art. That
        // padding is not a type bearing: keeping it makes every advance 13 units
        // and the text reads as spaced-out caps. Strip it so advances come from
        // the ink width.
        const int TileLeftInset = 5;
        int capTop = capHeight;

        Glyph MakeGlyph(GlyphSource g)
        {
            int bearingX = Mathf.Max(0, g.InkX - TileLeftInset);

            // Full-height glyphs share the cap line even when the source art sits
            // a pixel low (the digits do); short marks keep their own offset so
            // a dash or a dot stays where it belongs.
            int bearingY = g.InkH == capTop
                ? capTop
                : g.InkY + g.InkH - BaselineRow;

            var glyph = new Glyph(
                nextIndex++,
                new GlyphMetrics(
                    g.InkW,
                    g.InkH,
                    bearingX,
                    bearingY,
                    bearingX + g.InkW + AdvanceGap),
                new GlyphRect(g.Col * Cell + g.InkX, g.Row * Cell + g.InkY, g.InkW, g.InkH));
            glyphTable.Add(glyph);
            return glyph;
        }

        foreach (var g in glyphs.Where(g => !g.IsBlank).OrderBy(g => g.Character))
        {
            Glyph glyph = MakeGlyph(g);
            characterTable.Add(new TMP_Character(g.Character, glyph));

            // Uppercase-only sheet: fold lowercase onto the same shape.
            if (g.Character >= 'A' && g.Character <= 'Z')
            {
                Glyph lower = MakeGlyph(g);
                characterTable.Add(new TMP_Character(char.ToLowerInvariant(g.Character), lower));
            }
        }

        // Space has no tile, so it gets an empty 1px rect and its own advance.
        var spaceGlyph = new Glyph(
            nextIndex++,
            new GlyphMetrics(0f, 0f, 0f, 0f, SpaceAdvance),
            new GlyphRect(atlasW - 1, atlasH - 1, 1, 1));
        glyphTable.Add(spaceGlyph);
        characterTable.Add(new TMP_Character(' ', spaceGlyph));

        SetField(fontAsset, "m_GlyphTable", glyphTable);
        SetField(fontAsset, "m_CharacterTable", characterTable);

        // The sheet plus the procedural marks cover the UI, but the stock TMP
        // face stays as a fallback so an unexpected character degrades instead
        // of turning into a box.
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        if (fallback != null && fallback != fontAsset)
        {
            SetField(fontAsset, "m_FallbackFontAssetTable", new List<TMP_FontAsset> { fallback });
            Log.AppendLine("fallback: LiberationSans SDF");
        }
        else Log.AppendLine("WARN no fallback font found");

        fontAsset.ReadFontAssetDefinition();

        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log.AppendLine($"glyphs={glyphTable.Count} characters={characterTable.Count} " +
                       $"atlas={atlasW}x{atlasH} materials={fontAsset.material?.shader?.name}");
        Log.AppendLine("ink: " + string.Join(" ", glyphs.Where(g => !g.IsBlank)
            .OrderBy(g => g.Character)
            .Select(g => $"{g.Character}={g.InkW}x{g.InkH}@{g.InkX},{g.InkY}")));
        Log.AppendLine($"lookup characters={fontAsset.characterLookupTable?.Count} " +
                       $"lookup glyphs={fontAsset.glyphLookupTable?.Count}");
        return fontAsset;
    }

    private static void Preview(TMP_FontAsset fontAsset)
    {
        const int width = 960;
        const int height = 260;
        const int previewFontSize = 22;
        const float camZ = -10f;

        var camGo = new GameObject("PixelFontPreviewCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = height * 0.5f;
        cam.aspect = width / (float)height;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.11f, 0.13f, 1f);
        cam.transform.position = new Vector3(0f, 0f, camZ);
        cam.transform.rotation = Quaternion.identity;

        var canvasGo = new GameObject("PixelFontPreviewCanvas", typeof(Canvas))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = -camZ;
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(width, height);

        var textGo = new GameObject("PixelFontPreviewText", typeof(RectTransform))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        textGo.transform.SetParent(canvasGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.font = fontAsset;
        tmp.fontSize = previewFontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz\n0123456789  %+-  Day 7\n" +
                   "Food: 16 (need 4 tonight)  [D1] Hard cave \u2014 3 AP\n" +
                   "Ann was hurt.  Core A +  AP 3/3\n" +
                   "Day 2 \u2014 everyone ate (4 fed).  Materials x2";

        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        tmp.ForceMeshUpdate();
        int visible = 0;
        for (int i = 0; i < tmp.textInfo.characterCount; i++)
            if (tmp.textInfo.characterInfo[i].isVisible) visible++;

        Log.AppendLine($"preview: chars={tmp.textInfo.characterCount} visible={visible} " +
                       $"preferredWidth={tmp.preferredWidth:F1} preferredHeight={tmp.preferredHeight:F1}");

        var rtTex = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rtTex;
        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rtTex;
        var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        shot.Apply();
        RenderTexture.active = null;

        int lit = 0;
        foreach (var p in shot.GetPixels()) if (p.r > 0.6f && p.g > 0.6f && p.b > 0.6f) lit++;
        Log.AppendLine($"render: brightPixels={lit}");

        string outPath = Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "Temp", "pixel-font-preview.png"));
        File.WriteAllBytes(outPath, shot.EncodeToPNG());
        Log.AppendLine($"preview saved: {outPath}");

        cam.targetTexture = null;
        rtTex.Release();
        UnityEngine.Object.DestroyImmediate(rtTex);
        UnityEngine.Object.DestroyImmediate(shot);
        UnityEngine.Object.DestroyImmediate(canvasGo);
        UnityEngine.Object.DestroyImmediate(camGo);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            Log.AppendLine($"WARN missing field {fieldName} on {target.GetType().Name}");
            return;
        }
        field.SetValue(target, value);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
}