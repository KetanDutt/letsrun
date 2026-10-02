using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small, dependency-free uGUI primitives shared by the runtime menu and HUD.</summary>
public static class UIFactory
{
    public static readonly Color Ink = new Color(0.075f, 0.17f, 0.21f);
    public static readonly Color Muted = new Color(0.27f, 0.38f, 0.41f);
    public static readonly Color Paper = new Color(0.98f, 0.99f, 0.95f);
    public static readonly Color Coral = new Color(1f, 0.55f, 0.38f);
    public static readonly Color Teal = new Color(0.37f, 0.85f, 0.83f);
    public static readonly Color Gold = new Color(1f, 0.81f, 0.32f);

    private static Sprite rounded, white;
    private static Font bodyFont, titleFont;

    public static RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    public static Image Image(Transform parent, string name, Color color, float x0, float y0, float x1, float y1, bool round = true, bool shadow = false)
    {
        var image = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Image>();
        image.sprite = round ? RoundedSprite() : WhiteSprite();
        image.type = round ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        if (shadow)
        {
            Shadow depth = image.gameObject.AddComponent<Shadow>();
            depth.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.16f);
            depth.effectDistance = new Vector2(0f, -5f);
        }
        return image;
    }

    public static Image Art(Transform parent, string name, Sprite sprite, float x0, float y0, float x1, float y1)
    {
        var image = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    public static Text Text(Transform parent, string name, string value, int size, float x0, float y0, float x1, float y1,
        Color color, bool title = false, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        if (bodyFont == null) bodyFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/LuckiestGuy");
        var text = Rect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<Text>();
        text.font = title && titleFont != null ? titleFont : bodyFont;
        text.fontSize = title ? size : Mathf.Max(22, size);
        text.color = color;
        text.text = value;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.lineSpacing = 1.06f;
        return text;
    }

    public static Button Button(Transform parent, string label, Color color, Action action, float x0, float y0, float x1, float y1, int size = 24, bool dark = false)
    {
        Image image = Image(parent, label + " Button", color, x0, y0, x1, y1, true, true);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.88f, 0.9f);
        colors.selectedColor = new Color(0.84f, 1f, 0.97f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        Text(image.transform, "Label", label, size, 0.05f, 0.08f, 0.95f, 0.92f, dark ? Paper : Ink);
        button.gameObject.AddComponent<ButtonMotion>();
        button.onClick.AddListener(() =>
        {
            if (SceneNavigator.IsLoading) return;
            if (SoundManager.instance != null) SoundManager.instance.PlayClickSound();
            if (action != null) action();
        });
        return button;
    }

    public static Image Progress(Transform parent, string name, Color color, float x0, float y0, float x1, float y1)
    {
        Image image = Image(parent, name, color, x0, y0, x1, y1, false);
        image.type = UnityEngine.UI.Image.Type.Filled;
        image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        image.fillOrigin = 0;
        image.fillAmount = 0f;
        return image;
    }

    public static void SetLabel(Button button, string value)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.text = value;
    }

    private static Sprite WhiteSprite()
    {
        if (white == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            white = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            white.hideFlags = HideFlags.DontSave;
        }
        return white;
    }

    private static Sprite RoundedSprite()
    {
        if (rounded != null) return rounded;
        const int size = 64;
        const float radius = 18f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runner UI Rounded Rectangle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)), 0f);
                float dy = Mathf.Max(Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)), 0f);
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        rounded = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        rounded.hideFlags = HideFlags.DontSave;
        return rounded;
    }
}
