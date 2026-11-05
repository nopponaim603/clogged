using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Red mist over the map: two drifting layers of tileable noise plus a dark red
/// vignette, all in the UI so it needs no render-pipeline or post-processing
/// setup. It lives as a child of the map panel, which puts it above the map art
/// and below the dungeon cards and the HUD - the atmosphere never sits between
/// the player and a number they are reading.
///
/// Thickness is one number and one source: the run's Risk. There is no slider -
/// the only way to thin the mist is to clear a cave, which is the point.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RedFogOverlay : MonoBehaviour
{
    public static RedFogOverlay Instance { get; private set; }

    [Header("Look")]
    [SerializeField] private Color fogColor = new Color(0.80f, 0.07f, 0.08f, 1f);
    [SerializeField] private Color vignetteColor = new Color(0.26f, 0.01f, 0.02f, 1f);

    [Tooltip("How far the fog layers reach past the panel, so the drift never shows an edge.")]
    [SerializeField] private Vector2 overscan = new Vector2(1.08f, 1.08f);

    [Tooltip("Noise cells across the texture. Lower is blobbier.")]
    [SerializeField] private int noiseSize = 128;

    private sealed class Layer
    {
        public RawImage image;
        public Vector2 tiling;
        public Vector2 speed;
        public float strength;
        public float phase;
        public Vector2 offset;
    }

    private readonly List<Layer> layers = new List<Layer>();
    private Image vignette;
    private RectTransform fogRoot;
    private RunResources boundResources;
    private float painted;

    /// <summary>
    /// The fog's own layer. Anything that must stay readable through the mist
    /// (the fog gauge) is parented here, so it draws above the layers.
    /// </summary>
    public Transform OverlayRoot => fogRoot != null ? fogRoot : transform;

    /// <summary>What the layers are painted with right now: the run's Risk, 0-1.</summary>
    public float Density => painted;

    private void Awake()
    {
        Instance = this;

        fogRoot = BuildRoot();
        BuildVignette();
        BuildLayer(new Vector2(0.85f, 0.52f), new Vector2(0.005f, 0.002f), 0.90f, 0f);
        BuildLayer(new Vector2(1.45f, 0.95f), new Vector2(-0.008f, -0.0035f), 0.55f, 2.2f);

        Apply();
    }

    private void Start()
    {
        BindResources();
        Apply();
    }

    private void OnDisable() => UnbindResources();

    /// <summary>
    /// Repaints the instant a trip changes Risk, instead of waiting for the next
    /// frame - a cleared cave should visibly thin the fog as the dice popup
    /// closes, not a frame later.
    /// </summary>
    private void BindResources()
    {
        if (RunResources.Instance == null || RunResources.Instance == boundResources) return;

        UnbindResources();
        boundResources = RunResources.Instance;
        boundResources.OnChanged += HandleResourcesChanged;
    }

    private void UnbindResources()
    {
        if (boundResources == null) return;

        boundResources.OnChanged -= HandleResourcesChanged;
        boundResources = null;
    }

    private void HandleResourcesChanged() => Apply();

    /// <summary>
    /// Puts the fog in its own full-screen layer directly above the map panel:
    /// tall enough to sit over the map and its dungeon cards, but below the HUD
    /// panels that come after it, so no number is ever read through the mist.
    /// </summary>
    private RectTransform BuildRoot()
    {
        var go = new GameObject("RedFog", typeof(RectTransform));
        var rect = (RectTransform)go.transform;

        var parent = transform.parent != null ? transform.parent : transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        int index = Mathf.Clamp(transform.GetSiblingIndex() + 1, 0, parent.childCount - 1);
        rect.SetSiblingIndex(index);
        return rect;
    }

    private void OnDestroy()
    {
        UnbindResources();
        if (Instance == this) Instance = null;
        if (fogRoot != null) Destroy(fogRoot.gameObject);
    }

    private void Update()
    {
        // Risk moves only when a trip lands, so the repaint is keyed off the
        // painted value instead of being re-issued every frame.
        float wanted = Danger();
        if (!Mathf.Approximately(wanted, painted)) Apply();

        if (painted <= 0f) return;

        float dt = Time.unscaledDeltaTime;

        foreach (var layer in layers)
        {
            layer.offset += layer.speed * dt;
            layer.offset.x = Mathf.Repeat(layer.offset.x, 1f);
            layer.offset.y = Mathf.Repeat(layer.offset.y, 1f);

            layer.image.uvRect = new Rect(layer.offset.x, layer.offset.y, layer.tiling.x, layer.tiling.y);
        }

        Apply();
    }

    private void BuildLayer(Vector2 tiling, Vector2 speed, float strength, float phase)
    {
        var go = new GameObject("FogLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rect = (RectTransform)go.transform;
        rect.SetParent(fogRoot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = new Vector3(overscan.x, overscan.y, 1f);

        var image = go.GetComponent<RawImage>();
        image.texture = BuildNoise(noiseSize, 1337 + layers.Count * 71);
        image.raycastTarget = false;
        image.uvRect = new Rect(0f, 0f, tiling.x, tiling.y);

        // Below the vignette and, more importantly, below the HUD panels that
        // follow the map in the canvas.
        rect.SetAsFirstSibling();

        layers.Add(new Layer
        {
            image = image,
            tiling = tiling,
            speed = speed,
            strength = strength,
            phase = phase
        });
    }

    private void BuildVignette()
    {
        var go = new GameObject("FogVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(fogRoot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-20f, -20f);
        rect.offsetMax = new Vector2(20f, 20f);
        rect.localScale = Vector3.one;

        var sprite = Sprite.Create(BuildVignetteTexture(128), new Rect(0f, 0f, 128f, 128f),
            new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "FogVignette";

        vignette = go.GetComponent<Image>();
        vignette.sprite = sprite;
        vignette.type = Image.Type.Simple;
        vignette.raycastTarget = false;

        rect.SetAsFirstSibling();
    }

    /// <summary>Pushes the live density into every layer.</summary>
    private void Apply()
    {
        painted = Danger();
        float time = Time.unscaledTime;

        foreach (var layer in layers)
        {
            if (layer.image == null) continue;

            // Each layer breathes on its own phase, so the mist rolls instead of
            // the whole screen pulsing as one sheet.
            float breath = 0.85f + 0.15f * Mathf.Sin(time * 0.35f + layer.phase);

            Color color = fogColor;
            color.a = painted * layer.strength * breath;
            layer.image.color = color;
            layer.image.enabled = color.a > 0.002f;
        }

        if (vignette == null) return;

        Color edge = vignetteColor;
        edge.a = painted;
        vignette.color = edge;
        vignette.enabled = edge.a > 0.002f;
    }

    /// <summary>
    /// How thick the mist should be. Risk is the only input: 0 clears the screen,
    /// <see cref="RunResources.MaxRisk"/> is the full effect.
    /// </summary>
    private float Danger()
    {
        return RunResources.Instance != null ? Mathf.Clamp01(RunResources.Instance.RiskRatio) : 0f;
    }

    // ------------------------------------------------------------- textures

    /// <summary>
    /// Tileable fbm noise: every octave wraps on its own lattice period, so the
    /// scrolling uvRect can repeat forever without a seam.
    /// </summary>
    private static Texture2D BuildNoise(int size, int seed)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "FogNoise",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color32[size * size];
        int[] periods = { 2, 4, 8, 16 };

        for (int y = 0; y < size; y++)
        {
            float v = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;

                float sum = 0f;
                float weight = 0f;
                float amplitude = 0.5f;

                foreach (int period in periods)
                {
                    // Stretched vertically so the cells read as drifting banks
                    // rather than round blobs.
                    sum += amplitude * PeriodicValueNoise(u * period, v * period * 0.5f, period, seed + period);
                    weight += amplitude;
                    amplitude *= 0.55f;
                }

                float value = Mathf.Clamp01(sum / weight);

                // Contrast, so the mist has wispy gaps instead of covering the
                // screen in an even haze.
                value = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 0.82f, value));

                // Ground mist: denser at the bottom of the screen.
                value *= Mathf.Lerp(0.35f, 1f, Mathf.SmoothStep(0f, 1f, 1f - v));

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(value * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static float PeriodicValueNoise(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;

        int xa = Wrap(x0, period);
        int xb = Wrap(x0 + 1, period);
        int ya = Wrap(y0, period);
        int yb = Wrap(y0 + 1, period);

        float sx = fx * fx * (3f - 2f * fx);
        float sy = fy * fy * (3f - 2f * fy);

        float bottom = Mathf.Lerp(Hash(xa, ya, seed), Hash(xb, ya, seed), sx);
        float top = Mathf.Lerp(Hash(xa, yb, seed), Hash(xb, yb, seed), sx);
        return Mathf.Lerp(bottom, top, sy);
    }

    private static int Wrap(int value, int period)
    {
        int wrapped = value % period;
        return wrapped < 0 ? wrapped + period : wrapped;
    }

    /// <summary>Deterministic 0-1 hash, so the same build always looks the same.</summary>
    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 2147483647;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    /// <summary>Dark red ring: transparent in the middle, opaque at the edges.</summary>
    private static Texture2D BuildVignetteTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "FogVignette",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color32[size * size];
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - radius) / radius;
                float dy = (y + 0.5f - radius) / radius;

                // Squared distance keeps the middle clear and the corners dark.
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 0.72f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance));

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
