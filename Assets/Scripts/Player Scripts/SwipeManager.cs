using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Poll before the player consumes the one-frame gestures.
[DefaultExecutionOrder(-200)]
public sealed class SwipeManager : MonoBehaviour
{
    public static bool tap, swipeLeft, swipeRight, swipeUp, swipeDown;

    [SerializeField, Range(0.02f, 0.2f)] private float thresholdScreenFraction = 0.06f;
    private bool dragging;
    private int fingerId = -1;
    private Vector2 startTouch;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
    private PointerEventData pointer;
    private EventSystem pointerEventSystem;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { ClearFlags(); }

    private static void ClearFlags() { tap = swipeDown = swipeUp = swipeLeft = swipeRight = false; }

    private void Update()
    {
        ClearFlags();
        if (GameplayController.instance == null || !GameplayController.instance.AcceptsInput)
        {
            ResetGesture();
            return;
        }

        // Touch takes precedence over Unity's emulated mouse events. No Input.touches allocations.
        if (Input.touchCount > 0 || fingerId >= 0)
        {
            HandleTouch();
            return;
        }

        if (Input.GetMouseButtonDown(0)) BeginGesture(Input.mousePosition, -1);
        else if (Input.GetMouseButtonUp(0)) ResetGesture();
        if (dragging && Input.GetMouseButton(0)) CheckSwipe(Input.mousePosition);
    }

    private void HandleTouch()
    {
        if (fingerId < 0)
        {
            if (Input.touchCount == 0) return;
            Touch first = Input.GetTouch(0);
            if (first.phase == TouchPhase.Began) BeginGesture(first.position, first.fingerId);
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != fingerId) continue;
            if (touch.phase == TouchPhase.Canceled)
            {
                ResetGesture();
                return;
            }
            CheckSwipe(touch.position); // Also recognize short flicks on the release frame.
            if (touch.phase == TouchPhase.Ended) ResetGesture();
            return;
        }
        ResetGesture(); // The tracked finger disappeared; do not adopt a different touch mid-gesture.
    }

    private void BeginGesture(Vector2 position, int id)
    {
        ResetGesture();
        if (IsOverUI(position, id)) return;
        tap = true;
        dragging = true;
        fingerId = id;
        startTouch = position;
    }

    private bool IsOverUI(Vector2 position, int id)
    {
        if (EventSystem.current == null) return false;
        // Raycast explicitly: this remains correct even before EventSystem's Update this frame.
        if (pointer == null || pointerEventSystem != EventSystem.current)
        {
            pointerEventSystem = EventSystem.current;
            pointer = new PointerEventData(pointerEventSystem);
        }
        pointer.Reset();
        pointer.position = position;
        pointer.pointerId = id;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        return uiHits.Count > 0;
    }

    private void CheckSwipe(Vector2 position)
    {
        if (!dragging) return;
        Vector2 delta = position - startTouch;
        float threshold = Mathf.Max(24f, Mathf.Min(Screen.width, Screen.height) * thresholdScreenFraction);
        if (delta.sqrMagnitude < threshold * threshold) return;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            swipeLeft = delta.x < 0f;
            swipeRight = !swipeLeft;
        }
        else
        {
            swipeDown = delta.y < 0f;
            swipeUp = !swipeDown;
        }
        ResetGesture(); // Exactly one action per gesture.
    }

    private void ResetGesture() { dragging = false; fingerId = -1; startTouch = Vector2.zero; }
    private void OnDisable() { ClearFlags(); ResetGesture(); }
}
