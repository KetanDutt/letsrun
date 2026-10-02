using UnityEngine;

public sealed class ObstacleHolder : MonoBehaviour
{
    public GameObject[] childs;
    public float limitAxisX;
    public Vector3 firstPos, secondPos;

    private void Update()
    {
        GameplayController controller = GameplayController.instance;
        if (controller == null || controller.State != RunState.Running) return;
        // Pattern coordinates are authored in the camera's local space, but scroll along world X.
        transform.position += Vector3.left * (controller.moveSpeed * Time.deltaTime);
        if (transform.localPosition.x <= limitAxisX) gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (childs != null)
            for (int i = 0; i < childs.Length; i++)
                if (childs[i] != null) childs[i].SetActive(true);
        transform.localPosition = Random.value <= 0.5f ? firstPos : secondPos;
        // Re-enable/reset all children on reuse, including collected pickups and animated spikes.
        if (childs == null) return;
        for (int i = 0; i < childs.Length; i++)
        {
            if (childs[i] == null) continue;
            Animator animator = childs[i].GetComponent<Animator>();
            if (animator != null) { animator.Rebind(); animator.Update(0f); }
        }
    }

    private void OnDisable()
    {
        if (GameplayController.instance != null) GameplayController.instance.PatternFinished();
    }
}
