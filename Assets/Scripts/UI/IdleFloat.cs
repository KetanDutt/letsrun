using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class IdleFloat : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 restPosition;
    private void Awake() { rect = GetComponent<RectTransform>(); restPosition = rect.anchoredPosition; }
    private void Update()
    {
        bool reduced = GameManager.instance != null && GameManager.instance.ReducedMotion;
        rect.anchoredPosition = restPosition + (reduced ? Vector2.zero : Vector2.up * Mathf.Sin(Time.unscaledTime * 0.85f) * 7f);
    }
    private void OnDisable() { if (rect != null) rect.anchoredPosition = restPosition; }
}
