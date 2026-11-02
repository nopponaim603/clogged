using UnityEngine;

/// <summary>
/// Generated fallback art for the card's status / talent / buff icons.
///
/// The sprint checklist asked for a sword and dashboard-style lamps before any
/// real sprites exist, so each icon is drawn here as a small pixel mask and
/// cached. They are white with alpha, which lets every caller tint them.
/// Real art can override any of them through the matching serialized field on
/// <see cref="UnitIconView"/> - nothing else needs to change.
/// </summary>
public static class PlaceholderIcons
{
    private const int Size = 16;

    private static Sprite sword;
    private static Sprite plus;
    private static Sprite bowl;
    private static Sprite arrowUp;
    private static Sprite arrowDown;
    private static Sprite star;
    private static Sprite flask;
    private static Sprite cave;
    private static Sprite selector;
    private static Sprite roundedSquare;

    /// <summary>Ready state: a small upright sword.</summary>
    public static Sprite Sword => Cache(ref sword, "Sword", DrawSword);

    /// <summary>Hurt / negative condition: a cross.</summary>
    public static Sprite Plus => Cache(ref plus, "Plus", DrawPlus);

    /// <summary>Hungry / cooking: a bowl.</summary>
    public static Sprite Bowl => Cache(ref bowl, "Bowl", DrawBowl);

    public static Sprite ArrowUp => Cache(ref arrowUp, "ArrowUp", DrawArrowUp);
    public static Sprite ArrowDown => Cache(ref arrowDown, "ArrowDown", DrawArrowDown);
    public static Sprite Star => Cache(ref star, "Star", DrawStar);
    public static Sprite Flask => Cache(ref flask, "Flask", DrawFlask);

    /// <summary>Cave mouth used on the dungeon cards until the real art lands.</summary>
    public static Sprite Cave => Cache(ref cave, "Cave", DrawCave);

    /// <summary>Marker shown over the selected dungeon card.</summary>
    public static Sprite Selector => Cache(ref selector, "Selector", DrawSelector);

    /// <summary>9-sliced rounded square, used as the aptitude badge plate.</summary>
    public static Sprite RoundedSquare => Cache(ref roundedSquare, "RoundedSquare", DrawRoundedSquare, 26, new Vector4(8f, 8f, 8f, 8f));

    private delegate bool Mask(int x, int y);

    private static Sprite Cache(ref Sprite slot, string name, Mask mask, int size = Size, Vector4 border = default)
    {
        if (slot != null) return slot;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = name
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float coverage = Coverage(mask, x, y, size);
                pixels[y * size + x] = new Color(1f, 1f, 1f, coverage);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        slot = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
        slot.name = name;
        return slot;
    }

    /// <summary>Full alpha inside the mask, plus a soft edge where a neighbour is empty.</summary>
    private static float Coverage(Mask mask, int x, int y, int size)
    {
        if (mask(x, y)) return 1f;

        bool nearFilled =
            (x > 0 && mask(x - 1, y)) ||
            (x < size - 1 && mask(x + 1, y)) ||
            (y > 0 && mask(x, y - 1)) ||
            (y < size - 1 && mask(x, y + 1));

        return nearFilled ? 0.45f : 0f;
    }

    // ------------------------------------------------------------- masks

    private static bool DrawSword(int x, int y)
    {
        // Blade: two pixel columns running up the middle.
        if (x >= 7 && x <= 8 && y >= 4 && y <= 13) return true;

        // Tip.
        if (x >= 6 && x <= 9 && y == 14) return true;

        // Guard.
        if (y >= 2 && y <= 3 && x >= 3 && x <= 12) return true;

        // Grip.
        return x >= 7 && x <= 8 && y >= 0 && y <= 1;
    }

    private static bool DrawPlus(int x, int y)
    {
        bool vertical = x >= 6 && x <= 9 && y >= 2 && y <= 13;
        bool horizontal = y >= 6 && y <= 9 && x >= 2 && x <= 13;
        return vertical || horizontal;
    }

    private static bool DrawBowl(int x, int y)
    {
        // Rim.
        if (y >= 10 && y <= 11 && x >= 2 && x <= 13) return true;

        // Body, narrowing towards the base.
        for (int row = 5; row <= 9; row++)
        {
            int inset = (10 - row) / 2;
            if (y == row && x >= 2 + inset && x <= 13 - inset) return true;
        }

        // Base.
        return y >= 3 && y <= 4 && x >= 5 && x <= 10;
    }

    private static bool DrawArrowUp(int x, int y)
    {
        // Triangle with the point at the top (y = 13) widening towards the base.
        for (int row = 3; row <= 13; row++)
        {
            int half = (13 - row) / 2 + 2;
            if (y == row && x >= 8 - half && x <= 7 + half) return true;
        }
        return false;
    }

    private static bool DrawArrowDown(int x, int y)
    {
        // Same triangle as ArrowUp, flipped vertically.
        return DrawArrowUp(x, 16 - y);
    }

    private static bool DrawStar(int x, int y)
    {
        int dx = Mathf.Abs(x - 7);
        int dy = Mathf.Abs(y - 8);
        if (dx + dy <= 5) return true;

        // Four short points.
        bool verticalPoint = dx <= 1 && dy <= 7;
        bool horizontalPoint = dy <= 1 && dx <= 7;
        return verticalPoint || horizontalPoint;
    }

    private static bool DrawFlask(int x, int y)
    {
        // Neck.
        if (x >= 6 && x <= 9 && y >= 11 && y <= 14) return true;

        // Body.
        int dx = x - 7;
        int dy = y - 6;
        return dx * dx + dy * dy <= 20;
    }

    private static bool DrawRoundedSquare(int x, int y)
    {
        const int size = 26;
        const float radius = 7f;
        float half = size * 0.5f;

        float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
        float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
        return Mathf.Sqrt(dx * dx + dy * dy) - radius <= 0f;
    }

    /// <summary>Dungeon entrance: an arch with a dark mouth cut out of the middle.</summary>
    private static bool DrawCave(int x, int y)
    {
        const float cx = 7.5f;
        const float baseY = 2f;

        float dx = x - cx;
        float top = baseY + 3f + Mathf.Sqrt(Mathf.Max(0f, 30f - dx * dx));
        if (y < baseY || y > top) return false;

        bool inMouth = Mathf.Abs(dx) <= 3f &&
                       y <= baseY + 3f + Mathf.Sqrt(Mathf.Max(0f, 9f - dx * dx));

        return !inMouth;
    }

    /// <summary>Downward chevron marking the currently selected dungeon.</summary>
    private static bool DrawSelector(int x, int y)
    {
        return DrawArrowUp(x, 16 - y);
    }
}
