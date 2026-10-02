using System.Collections;
using UnityEngine;

/// <summary>Small unscaled UI tweens. Every animation ends at an exact, predictable value.</summary>
public sealed class MotionTween : MonoBehaviour
{
    private Coroutine tween;
    private Vector3 restScale;

    private bool Reduced { get { return GameManager.instance != null && GameManager.instance.ReducedMotion; } }
    private void Awake() { restScale = transform.localScale; }

    public void Open()
    {
        StopCurrent();
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (Reduced) { group.alpha = 1f; group.interactable = true; return; }
        tween = StartCoroutine(OpenRoutine(group));
    }

    public void Pulse(float amount = 0.15f)
    {
        StopCurrent();
        if (!Reduced) tween = StartCoroutine(PulseRoutine(amount));
    }

    private void StopCurrent()
    {
        if (tween != null) StopCoroutine(tween);
        tween = null;
        transform.localScale = restScale;
    }

    private IEnumerator OpenRoutine(CanvasGroup group)
    {
        group.alpha = 0f;
        group.interactable = false;
        for (float elapsed = 0f; elapsed < 0.28f && !Reduced; elapsed += Time.unscaledDeltaTime)
        {
            float t = RunnerRules.EaseOutCubic(elapsed / 0.28f);
            group.alpha = t;
            transform.localScale = restScale * Mathf.Lerp(0.94f, 1f, t);
            yield return null;
        }
        transform.localScale = restScale;
        group.alpha = 1f;
        group.interactable = true;
        tween = null;
    }

    private IEnumerator PulseRoutine(float amount)
    {
        for (float elapsed = 0f; elapsed < 0.28f && !Reduced; elapsed += Time.unscaledDeltaTime)
        {
            transform.localScale = restScale * (1f + Mathf.Sin(elapsed / 0.28f * Mathf.PI) * amount);
            yield return null;
        }
        transform.localScale = restScale;
        tween = null;
    }

    private void OnDisable() { StopCurrent(); }
}
