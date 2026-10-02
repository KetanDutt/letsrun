using UnityEngine;

public sealed class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public GameObject player, shadow;
    public Vector3 first_PosOfPlayer, second_PosOfPlayer;
    [HideInInspector] public bool player_Died, player_Jumped;
    public GameObject explosion;
    public Sprite TRex_Sprite, player_Sprite;
    [SerializeField, Range(0.08f, 0.3f)] private float laneTweenSeconds = 0.16f;
    [SerializeField, Range(0f, 0.3f)] private float jumpBufferSeconds = 0.15f;

    public float PowerUpRemaining { get; private set; }
    public bool IsPowered { get { return PowerUpRemaining > 0f; } }

    private static readonly int JumpAnimation = Animator.StringToHash("PlayerJump");
    private static readonly int LaneAnimation = Animator.StringToHash("ChangeLine");
    private static readonly int WalkAnimation = Animator.StringToHash("PlayerWalk");
    private Animator anim;
    private SpriteRenderer playerRenderer;
    private Collider2D playerCollider;
    private GameObject[] starEffects;
    private Vector3 laneStart, laneTarget;
    private float laneElapsed, jumpStartedAt, bufferedJumpUntil = -1f;
    private int lane;
    private bool changingLane;
    private Quaternion restRotation;
    private Color restColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; Destroy(gameObject); return; }
        instance = this;
        GameManager.EnsureInstance();
        if (player == null)
        {
            Debug.LogError("Assign the animated Player child on PlayerController.");
            enabled = false;
            return;
        }
        anim = player.GetComponent<Animator>();
        playerRenderer = player.GetComponent<SpriteRenderer>();
        playerCollider = player.GetComponent<Collider2D>();
        restRotation = player.transform.localRotation;
        if (playerRenderer != null) restColor = playerRenderer.color;
        // Lane changes must not move the runner through the camera near plane.
        first_PosOfPlayer.z = second_PosOfPlayer.z = transform.localPosition.z;
        lane = (transform.localPosition - first_PosOfPlayer).sqrMagnitude <= (transform.localPosition - second_PosOfPlayer).sqrMagnitude ? 0 : 1;
        laneTarget = transform.localPosition;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Start()
    {
        Sprite chosen = Resources.Load<Sprite>("Sprites/Player/hero" + GameManager.instance.selected_Index + "_big");
        if (chosen != null) player_Sprite = chosen;
        if (playerRenderer != null && player_Sprite != null) playerRenderer.sprite = player_Sprite;

        // Find pooled effects including inactive children; FindGameObjectsWithTag missed all of them.
        GameObject holder = GameObject.Find("Get Star Effects Holder");
        Animator[] effects = holder == null ? new Animator[0] : holder.GetComponentsInChildren<Animator>(true);
        starEffects = new GameObject[effects.Length];
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i].updateMode = AnimatorUpdateMode.UnscaledTime;
            starEffects[i] = effects[i].gameObject;
            starEffects[i].SetActive(false);
        }
    }

    private void Update()
    {
        if (!CanAct()) return;
        PowerUpRemaining = Mathf.Max(0f, PowerUpRemaining - Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.D) || SwipeManager.swipeRight)
            RequestLane(1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) || SwipeManager.swipeLeft || SwipeManager.swipeDown)
            RequestLane(0);
        if (Input.GetKeyDown(KeyCode.Space) || SwipeManager.swipeUp) RequestJump();

        if (changingLane)
        {
            laneElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(laneElapsed / laneTweenSeconds);
            transform.localPosition = Vector3.LerpUnclamped(laneStart, laneTarget, RunnerRules.EaseOutCubic(t));
            if (t >= 1f) changingLane = false;
        }
        // A lost animation event must not leave jumping/collisions permanently disabled.
        if (player_Jumped && Time.time - jumpStartedAt > 0.9f) CompleteMovementAnimation();
        if (!player_Jumped && bufferedJumpUntil >= Time.time)
        {
            bufferedJumpUntil = -1f;
            StartJump();
        }
    }

    private void LateUpdate()
    {
        if (playerRenderer == null || player_Died) return;
        playerRenderer.sprite = IsPowered && TRex_Sprite != null ? TRex_Sprite : player_Sprite;
        // A steady tint communicates power; no rapid flashing or photosensitive strobing.
        playerRenderer.color = IsPowered ? Color.Lerp(restColor, new Color(0.55f, 1f, 0.9f), 0.3f) : restColor;
    }

    private bool CanAct()
    {
        return !player_Died && GameplayController.instance != null && GameplayController.instance.AcceptsInput;
    }

    public void RequestLane(int index)
    {
        if (!CanAct() || index < 0 || index > 1 || index == lane) return;
        lane = index;
        laneStart = transform.localPosition;
        laneTarget = index == 0 ? first_PosOfPlayer : second_PosOfPlayer;
        laneElapsed = 0f;
        changingLane = true;
        // Preserve a jump in progress rather than interrupting it with the lane squash clip.
        if (!player_Jumped && anim != null) anim.Play(LaneAnimation, 0, 0f);
        if (SoundManager.instance != null) SoundManager.instance.PlayMoveLineSound();
    }

    public void RequestJump()
    {
        if (!CanAct()) return;
        if (player_Jumped) bufferedJumpUntil = Time.time + jumpBufferSeconds;
        else StartJump();
    }

    private void StartJump()
    {
        bufferedJumpUntil = -1f;
        player_Jumped = true;
        jumpStartedAt = Time.time;
        if (anim != null) anim.Play(JumpAnimation, 0, 0f);
        if (SoundManager.instance != null) SoundManager.instance.PlayJumpSound();
    }

    public void CompleteMovementAnimation()
    {
        player_Jumped = false;
        if (playerCollider != null) playerCollider.enabled = true;
        if (player != null)
        {
            player.transform.localScale = Vector3.one;
            player.transform.localRotation = restRotation;
        }
        if (!player_Died && anim != null) anim.Play(WalkAnimation, 0, 0f);
    }

    public void ClearBufferedInput() { bufferedJumpUntil = -1f; }

    private void PlayExplosion(Vector3 position)
    {
        if (explosion == null) return;
        explosion.SetActive(false);
        explosion.transform.position = position;
        Animator explosionAnimator = explosion.GetComponent<Animator>();
        if (explosionAnimator != null) explosionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        explosion.SetActive(true);
    }

    private void Die(Collider2D obstacle)
    {
        if (player_Died) return;
        player_Died = true; // Set before any effects/callbacks: death and rewards are idempotent.
        ClearBufferedInput();
        PlayExplosion(obstacle.transform.position);
        obstacle.gameObject.SetActive(false);
        if (GameplayController.instance.Feedback != null) GameplayController.instance.Feedback.Impact(player.transform.position, true);
        if (player != null) player.SetActive(false);
        if (shadow != null) shadow.SetActive(false);
        if (SoundManager.instance != null) SoundManager.instance.PlayDeadSound();
        GameplayController.instance.GameOver();
    }

    private void CollectPowerUp(Collider2D pickup)
    {
        PowerUpRemaining = RunnerRules.PowerUpSeconds; // Refresh the timer; old coroutines cannot expire a newer pickup.
        Vector3 position = pickup.transform.position;
        pickup.gameObject.SetActive(false);
        if (SoundManager.instance != null) SoundManager.instance.PlayPowerUpSound();
        if (GameplayController.instance.Feedback != null) GameplayController.instance.Feedback.PowerUp(position);
        if (RunnerUI.instance != null) RunnerUI.instance.ShowToast("T-REX! Smash obstacles for 7 seconds");
    }

    private void CollectStar(Collider2D pickup)
    {
        Vector3 position = pickup.transform.position;
        pickup.gameObject.SetActive(false);
        if (starEffects != null)
            for (int i = 0; i < starEffects.Length; i++)
            {
                if (starEffects[i] == null || starEffects[i].activeSelf) continue;
                starEffects[i].transform.position = position;
                starEffects[i].SetActive(true);
                break;
            }
        if (GameplayController.instance.Feedback != null) GameplayController.instance.Feedback.Collect(position);
        if (SoundManager.instance != null) SoundManager.instance.PlayCoinSound();
        GameplayController.instance.UpdateStarScore();
    }

    private void OnTriggerEnter2D(Collider2D target)
    {
        if (!CanAct() || target == null || !target.gameObject.activeInHierarchy) return;
        if (target.CompareTag(MyTags.OBSTACLE))
        {
            if (!IsPowered) Die(target);
            else
            {
                PlayExplosion(target.transform.position);
                if (GameplayController.instance.Feedback != null) GameplayController.instance.Feedback.Impact(target.transform.position, false);
                target.gameObject.SetActive(false);
                if (SoundManager.instance != null) SoundManager.instance.PlayCrashSound();
            }
        }
        else if (target.CompareTag(MyTags.T_REX)) CollectPowerUp(target);
        else if (target.CompareTag(MyTags.STAR)) CollectStar(target);
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
