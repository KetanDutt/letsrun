using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private Vector3 restScale;
    private bool pressed, hovered, selected;
    private Button button;

    private void Awake() { restScale = transform.localScale; button = GetComponent<Button>(); }

    private void Update()
    {
        bool motion = GameManager.instance == null || !GameManager.instance.ReducedMotion;
        float scale = !motion || !button.IsInteractable() ? 1f : pressed ? 0.96f : hovered || selected ? 1.025f : 1f;
        transform.localScale = motion ? Vector3.Lerp(transform.localScale, restScale * scale, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime)) : restScale;
    }

    public void OnPointerDown(PointerEventData data) { pressed = true; }
    public void OnPointerUp(PointerEventData data) { pressed = false; }
    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = pressed = false; }
    public void OnSelect(BaseEventData data) { selected = true; }
    public void OnDeselect(BaseEventData data) { selected = pressed = false; }
    private void OnDisable() { pressed = hovered = selected = false; transform.localScale = restScale; }
}
