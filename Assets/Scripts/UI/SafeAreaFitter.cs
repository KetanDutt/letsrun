using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect previous;
    private int previousWidth, previousHeight;
    private void Awake() { rect = GetComponent<RectTransform>(); Apply(); }
    private void Update()
    {
        if (previous != Screen.safeArea || previousWidth != Screen.width || previousHeight != Screen.height) Apply();
    }
    private void Apply()
    {
        int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, width, height);
        rect.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        rect.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        previous = Screen.safeArea;
        previousWidth = Screen.width;
        previousHeight = Screen.height;
    }
}
