using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public event Action ProgressChanged;
    public event Action PreferencesChanged;
    public event Action<string> SaveProblem;

    public int starScore { get { return profile.stars; } }
    public int score_Count { get { return profile.bestScore; } }
    public int selected_Index { get { return profile.selectedHero; } }
    public bool[] heroes { get { return profile.unlockedHeroes; } }
    public bool playSound { get { return profile.musicEnabled; } }
    public bool PlaySfx { get { return profile.sfxEnabled; } }
    public bool ReducedMotion { get { return profile.reducedMotion; } }
    public bool TutorialSeen { get { return profile.tutorialSeen; } }
    public int TotalRuns { get { return profile.totalRuns; } }
    public int LifetimeStars { get { return profile.lifetimeStars; } }
    public string ProfileNotice { get; private set; }
    public string LastSaveError { get; private set; }
    public string SavePath { get { return store == null ? string.Empty : store.Path; } }
    public bool IsSaveReadOnly { get { return store != null && store.ReadOnly; } }

    private GameData profile = new GameData();
    private SaveStore store;
    private bool dirty;
    private float nextSaveTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static GameManager EnsureInstance()
    {
        if (instance == null)
            new GameObject("Game Manager").AddComponent<GameManager>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return; // A duplicate must never load or save over the persistent instance.
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Time.timeScale = 1f;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : 1;
        LoadProfile();
    }

    private void Update()
    {
        if (dirty && Time.unscaledTime >= nextSaveTime) FlushSave();
    }

    private void LoadProfile()
    {
        string root = Application.persistentDataPath;
        store = new SaveStore(Path.Combine(root, "profile.json"),
            UnityProfileCodec.Serialize, UnityProfileCodec.Deserialize);
        GameData loaded;
        string message;
        SaveLoadResult result = store.Load(out loaded, out message);
        if (loaded != null)
        {
            profile = loaded;
            if (result == SaveLoadResult.RecoveredBackup)
            {
                ProfileNotice = message;
                dirty = true;
                FlushSave();
            }
        }
        else if (result == SaveLoadResult.NewProfile)
        {
            // Support both the correct legacy location and the old missing-separator bug.
            string[] candidates = { Path.Combine(root, "GameData.dat"), root + "GameData.dat" };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!File.Exists(candidates[i])) continue;
                if (LegacySaveReader.TryRead(candidates[i], out loaded, out message))
                {
                    profile = loaded;
                    ProfileNotice = "Your original stars and runners have been restored.";
                    break;
                }
                ProfileNotice = "The old save could not be imported. It has been kept untouched.";
                Debug.LogWarning(message);
            }
            dirty = true;
            FlushSave();
        }
        else
        {
            ProfileNotice = result == SaveLoadResult.UnsupportedVersion
                ? "Your save needs a newer app version. It is protected from changes."
                : "Your save could not be read. Damaged files are retained for recovery.";
            Debug.LogWarning(message);
            if (result != SaveLoadResult.UnsupportedVersion)
            {
                dirty = true;
                FlushSave();
            }
        }
        profile.Normalize();
    }

    public void BankStar()
    {
        profile.stars = RunnerRules.SaturatingAdd(profile.stars, 1);
        profile.lifetimeStars = RunnerRules.SaturatingAdd(profile.lifetimeStars, 1);
        Changed();
    }

    public HeroSelectionResult SelectHero(int index)
    {
        HeroSelectionResult result = RunnerRules.SelectHero(profile, index);
        if (result == HeroSelectionResult.Purchased || result == HeroSelectionResult.Selected)
        {
            Changed();
            FlushSave();
        }
        return result;
    }

    public void CheckpointBestScore(int score)
    {
        if (score <= profile.bestScore) return;
        profile.bestScore = score;
        Changed();
    }

    public void RecordRun(int score)
    {
        profile.bestScore = Math.Max(profile.bestScore, Math.Max(0, score));
        profile.totalRuns = RunnerRules.SaturatingAdd(profile.totalRuns, 1);
        Changed();
        FlushSave();
    }

    public void SetMusic(bool enabled) { profile.musicEnabled = enabled; SettingsChanged(); }
    public void SetSfx(bool enabled) { profile.sfxEnabled = enabled; SettingsChanged(); }
    public void SetReducedMotion(bool enabled) { profile.reducedMotion = enabled; SettingsChanged(); }

    public void MarkTutorialSeen()
    {
        profile.tutorialSeen = true;
        dirty = true;
        FlushSave();
    }

    private void SettingsChanged()
    {
        dirty = true;
        if (PreferencesChanged != null) PreferencesChanged();
        FlushSave();
    }

    private void Changed()
    {
        dirty = true;
        if (ProgressChanged != null) ProgressChanged();
    }

    // Kept as a public entry point for existing scene/editor integrations.
    public void SaveGameData() { dirty = true; FlushSave(); }

    public bool FlushSave()
    {
        if (store == null || !dirty) return true;
        string message;
        bool success = store.Save(profile, out message);
        nextSaveTime = Time.unscaledTime + (success ? 2f : 10f);
        if (success)
        {
            dirty = false;
            LastSaveError = null;
        }
        else if (LastSaveError != message)
        {
            LastSaveError = message;
            Debug.LogWarning(message);
            if (SaveProblem != null) SaveProblem(message);
        }
        return success;
    }

    private void OnApplicationPause(bool paused) { if (paused) FlushSave(); }
    private void OnApplicationFocus(bool focused) { if (!focused) FlushSave(); }
    private void OnApplicationQuit() { FlushSave(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}
