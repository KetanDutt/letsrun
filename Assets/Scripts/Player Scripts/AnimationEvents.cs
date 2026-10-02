using UnityEngine;

/// <summary>Receivers for the authored clips. Gameplay state is never changed by a visual animation.</summary>
public sealed class AnimationEvents : MonoBehaviour
{
    private Animator anim;
    private PlayerController owner;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        owner = GetComponentInParent<PlayerController>();
        if (anim != null && (owner == null || owner.player != gameObject))
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    public void PlayerWalkAnimation()
    {
        if (owner != null && owner.player == gameObject) owner.CompleteMovementAnimation();
        else if (anim != null) anim.Play("PlayerWalk", 0, 0f);
    }

    public void AnimationEnded() { gameObject.SetActive(false); }

    // Legacy panels can close visually, but cannot unexpectedly unpause a finished run.
    public void PausePanelClose() { gameObject.SetActive(false); }
}
