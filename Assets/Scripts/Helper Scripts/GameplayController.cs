using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public sealed class GameplayController : MonoBehaviour
{
    public static GameplayController instance;

    public float moveSpeed, distance_Factor = 1f;
    [SerializeField, Min(0.1f)] private float acceleration = 5f;
    [SerializeField, Min(0f)] private float startingSpeed = 4f;
    public GameObject obstacles_Obj;
    public GameObject[] obstacle_List;
    [HideInInspector] public bool obstacles_Is_Active;

    // Retained so existing scenes and animation events remain compatible.
    public GameObject pause_Panel;
    public Animator pause_Anim;
    public GameObject gameOver_Panel;
    public Animator gameOver_Anim;
    public Text final_Score_Text, best_Score_Text, final_Star_Score_Text;
    [SerializeField] private Text score_Text, star_Score_Text;

    public RunState State { get { return session.State; } }
    public int CurrentScore { get { return session.Score; } }
    public int RunStars { get { return session.Stars; } }
    public int StartingBest { get; private set; }
    public bool AcceptsInput { get { return State == RunState.Running && PlayerController.instance != null && !PlayerController.instance.player_Died; } }
    public Camera RunCamera { get; private set; }
    public RunnerFeedback Feedback { get; private set; }

    private readonly RunSession session = new RunSession();
    private Coroutine countdown;
    private Transform starEffectsRoot;
    private float cameraStartX, spawnTimer;
    private int lastPattern = -1, lastDisplayedScore = -1, lastPace;
    private bool runBegan, committed, leaving;
    private Func<int, bool> availablePattern;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; Destroy(gameObject); return; }
        instance = this;
        GameManager.EnsureInstance();
        RunCamera = Camera.main;
        if (RunCamera != null) cameraStartX = RunCamera.transform.position.x;
        score_Text = score_Text != null ? score_Text : FindText("ScoreText");
        star_Score_Text = star_Score_Text != null ? star_Score_Text : FindText("StarText");
        availablePattern = IsPatternAvailable;
        Time.timeScale = 0f;
    }

    private static Text FindText(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target == null ? null : target.GetComponent<Text>();
    }

    private void Start()
    {
        StartingBest = GameManager.instance.score_Count;
        if (pause_Panel != null) pause_Panel.SetActive(false);
        if (gameOver_Panel != null) gameOver_Panel.SetActive(false);
        GetObstacles();
        GameObject effects = GameObject.Find("Get Star Effects Holder");
        if (effects != null) starEffectsRoot = effects.transform;
        Feedback = gameObject.AddComponent<RunnerFeedback>();
        Feedback.Initialize(RunCamera);
        RunnerUI.CreateGameplay(this);
        UpdateScoreLabels();

        if (RunCamera == null || PlayerController.instance == null)
        {
            Debug.LogError("Gameplay needs a MainCamera-tagged camera and a Root Player prefab.");
            RunnerUI.instance.ShowFatalError("The track could not load. Please return home.");
            return;
        }
        if (!GameManager.instance.TutorialSeen) RunnerUI.instance.ShowTutorial(StartRun);
        else StartRun();
    }

    private void Update()
    {
        if (leaving) return;
        bool pauseKey = Input.GetKeyDown(KeyCode.P) && (State == RunState.Running || State == RunState.Countdown || State == RunState.Paused);
        if (Input.GetKeyDown(KeyCode.Escape) || pauseKey)
        {
            if (RunnerUI.instance != null && RunnerUI.instance.HandleBack()) return;
            if (State == RunState.Paused) ResumeGame();
            else if (State == RunState.Running || State == RunState.Countdown) PauseGame();
        }
        if (State != RunState.Running || RunCamera == null) return;

        float delta = Time.deltaTime;
        session.Advance(delta * Mathf.Max(0.1f, distance_Factor));
        moveSpeed = Mathf.MoveTowards(moveSpeed, RunnerRules.TargetSpeed(session.Elapsed), acceleration * delta);
        RunCamera.transform.position += Vector3.right * (moveSpeed * delta);
        UpdateScoreLabels();
        SpawnObstacles(delta);
        RebaseWorldIfNeeded();

        int pace = CurrentScore >= 60 ? 2 : CurrentScore >= 30 ? 1 : 0;
        if (pace > lastPace)
        {
            lastPace = pace;
            if (RunnerUI.instance != null) RunnerUI.instance.ShowToast(pace == 1 ? "PACE UP · 30 seconds!" : "MAX PACE · 60 seconds!");
        }
    }

    public void StartRun()
    {
        if (leaving || !session.StartCountdown()) return;
        if (!GameManager.instance.TutorialSeen) GameManager.instance.MarkTutorialSeen();
        if (RunnerUI.instance != null) RunnerUI.instance.CloseModal();
        Time.timeScale = 0f;
        if (SoundManager.instance != null) SoundManager.instance.SetPaused(true);
        CancelCountdown();
        countdown = StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        for (int i = 3; i > 0; i--)
        {
            if (RunnerUI.instance != null) RunnerUI.instance.ShowCountdown(i.ToString());
            yield return new WaitForSecondsRealtime(0.7f);
        }
        if (!session.Begin()) yield break;
        if (!runBegan)
        {
            moveSpeed = startingSpeed;
            spawnTimer = 1.2f; // A readable, obstacle-free warm-up.
            runBegan = true;
        }
        Time.timeScale = 1f;
        if (SoundManager.instance != null) SoundManager.instance.SetPaused(false);
        if (RunnerUI.instance != null) RunnerUI.instance.ShowCountdown("GO!");
        yield return new WaitForSecondsRealtime(0.4f);
        if (RunnerUI.instance != null) RunnerUI.instance.HideCountdown();
        countdown = null;
    }

    private void CancelCountdown()
    {
        if (countdown != null) StopCoroutine(countdown);
        countdown = null;
        if (RunnerUI.instance != null) RunnerUI.instance.HideCountdown();
    }

    private void GetObstacles()
    {
        if (obstacles_Obj == null)
        {
            obstacle_List = new GameObject[0];
            Debug.LogError("Assign the Obstacles holder on GameplayController.");
            return;
        }
        ObstacleHolder[] holders = obstacles_Obj.GetComponentsInChildren<ObstacleHolder>(true);
        obstacle_List = new GameObject[holders.Length];
        for (int i = 0; i < holders.Length; i++)
        {
            obstacle_List[i] = holders[i].gameObject;
            obstacle_List[i].SetActive(false);
        }
        obstacles_Is_Active = false;
        if (holders.Length == 0) Debug.LogWarning("No obstacle patterns are configured.");
    }

    private bool IsPatternAvailable(int index)
    {
        return obstacle_List[index] != null && !obstacle_List[index].activeSelf;
    }

    private void SpawnObstacles(float delta)
    {
        spawnTimer -= delta;
        if (obstacles_Is_Active || spawnTimer > 0f || obstacle_List == null || obstacle_List.Length == 0) return;
        int count = obstacle_List.Length;
        int start = lastPattern < 0 ? UnityEngine.Random.Range(0, count)
            : (lastPattern + (count > 1 ? UnityEngine.Random.Range(1, count) : 0)) % count;
        int index = RunnerRules.FindAvailableIndex(count, start, availablePattern);
        spawnTimer = 0.6f;
        if (index < 0) return;
        obstacles_Is_Active = true;
        lastPattern = index;
        obstacle_List[index].SetActive(true);
    }

    public void PatternFinished()
    {
        obstacles_Is_Active = false;
        spawnTimer = 0.35f;
    }

    public void UpdateStarScore()
    {
        if (!session.CollectStar()) return;
        GameManager.instance.BankStar(); // Bank on pickup; never award the same stars again at game over.
        if (star_Score_Text != null) star_Score_Text.text = RunStars.ToString();
        if (RunnerUI.instance != null) RunnerUI.instance.PulseStars();
    }

    private void UpdateScoreLabels()
    {
        if (CurrentScore == lastDisplayedScore) return;
        lastDisplayedScore = CurrentScore;
        if (score_Text != null) score_Text.text = CurrentScore.ToString();
        if (star_Score_Text != null && CurrentScore == 0) star_Score_Text.text = RunStars.ToString();
    }

    public void PauseGame()
    {
        if (leaving || !session.Pause()) return;
        CancelCountdown();
        Time.timeScale = 0f;
        if (PlayerController.instance != null) PlayerController.instance.ClearBufferedInput();
        if (SoundManager.instance != null) SoundManager.instance.SetPaused(true);
        GameManager.instance.CheckpointBestScore(CurrentScore);
        GameManager.instance.FlushSave();
        if (RunnerUI.instance != null) RunnerUI.instance.ShowPause();
    }

    public void ResumeGame() { if (State == RunState.Paused) StartRun(); }
    public void RestartGame() { LeaveFor("Gameplay"); }
    public void HomeButton() { LeaveFor("MainMenu"); }

    private void LeaveFor(string scene)
    {
        if (leaving || !SceneNavigator.CanLoad(scene)) return;
        leaving = true;
        session.Finish();
        CancelCountdown();
        CommitRun();
        SceneNavigator.Load(scene);
    }

    public void GameOver()
    {
        if (leaving || !session.Finish()) return;
        CancelCountdown();
        moveSpeed = 0f;
        Time.timeScale = 0f;
        CommitRun();
        if (final_Score_Text != null) final_Score_Text.text = CurrentScore.ToString();
        if (final_Star_Score_Text != null) final_Star_Score_Text.text = RunStars.ToString();
        if (best_Score_Text != null) best_Score_Text.text = GameManager.instance.score_Count.ToString();
        if (SoundManager.instance != null) SoundManager.instance.PlayGameOverClip();
        if (RunnerUI.instance != null) RunnerUI.instance.ShowResults(CurrentScore, RunStars, CurrentScore > StartingBest);
    }

    public void NavigationFailed()
    {
        leaving = false;
        Time.timeScale = 0f;
        if (RunnerUI.instance != null) RunnerUI.instance.ShowResults(CurrentScore, RunStars, runBegan && CurrentScore > StartingBest);
    }

    private void CommitRun()
    {
        if (committed || GameManager.instance == null) return;
        committed = true;
        if (runBegan) GameManager.instance.RecordRun(CurrentScore);
        else GameManager.instance.FlushSave();
    }

    private void RebaseWorldIfNeeded()
    {
        if (RunCamera.transform.position.x - cameraStartX < 2048f) return;
        Vector3 offset = Vector3.right * (RunCamera.transform.position.x - cameraStartX);
        RunCamera.transform.position -= offset; // Camera children (player and patterns) move together.
        if (MapGenerator.instance != null) MapGenerator.instance.ShiftOrigin(offset);
        if (starEffectsRoot != null)
            for (int i = 0; i < starEffectsRoot.childCount; i++) starEffectsRoot.GetChild(i).position -= offset;
        if (Feedback != null) Feedback.ShiftOrigin(offset);
    }

    private void OnApplicationQuit() { session.Finish(); CommitRun(); }
    private void OnApplicationPause(bool paused) { if (paused) PauseGame(); }
    private void OnApplicationFocus(bool focused) { if (!focused) PauseGame(); }

    private void OnDestroy()
    {
        if (instance != this) return;
        CommitRun();
        instance = null;
        Time.timeScale = 1f;
    }
}
