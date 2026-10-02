using UnityEngine;

public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource move_Audio_Source, jump_Audio_Source, powerUp_Die_AudioSource, background_Audio_Source;
    public AudioClip power_Up_Clip, die_Clip, coin_Clip, game_Over_Clip;
    public AudioClip ui_Click, purchase_Clip, crash_Clip;

    private bool paused, gameOver;
    private float targetMusicVolume;
    private GameManager preferences;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; Destroy(gameObject); return; }
        instance = this;
        move_Audio_Source = PrepareSource(move_Audio_Source);
        jump_Audio_Source = PrepareSource(jump_Audio_Source);
        powerUp_Die_AudioSource = PrepareSource(powerUp_Die_AudioSource);
        background_Audio_Source = PrepareSource(background_Audio_Source);
        background_Audio_Source.volume = 0f;
        preferences = GameManager.EnsureInstance();
        preferences.PreferencesChanged += ApplyPreferences;
    }

    private AudioSource PrepareSource(AudioSource source)
    {
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.bypassReverbZones = true;
        return source;
    }

    public static void CreateMenuAudio(AudioClip music, AudioClip click, AudioClip buy)
    {
        if (instance == null) new GameObject("Menu Audio").AddComponent<SoundManager>();
        instance.background_Audio_Source.clip = music;
        instance.background_Audio_Source.loop = true;
        instance.ui_Click = click;
        instance.purchase_Clip = buy;
        instance.ApplyPreferences();
    }

    private void Start() { if (instance == this) ApplyPreferences(); }

    private void Update()
    {
        if (background_Audio_Source == null) return;
        background_Audio_Source.volume = Mathf.MoveTowards(background_Audio_Source.volume, targetMusicVolume, Time.unscaledDeltaTime * 1.8f);
        if (targetMusicVolume <= 0f && background_Audio_Source.volume <= 0f && background_Audio_Source.isPlaying)
            background_Audio_Source.Stop();
    }

    private void ApplyPreferences()
    {
        if (preferences == null) return;
        targetMusicVolume = preferences.playSound && !gameOver ? (paused ? 0.14f : 0.42f) : 0f;
        if (targetMusicVolume > 0f && background_Audio_Source.clip != null && !background_Audio_Source.isPlaying)
            background_Audio_Source.Play();
        bool mute = !preferences.PlaySfx;
        move_Audio_Source.mute = jump_Audio_Source.mute = powerUp_Die_AudioSource.mute = mute;
    }

    public void SetPaused(bool value) { paused = value; ApplyPreferences(); }

    private void Play(AudioSource source, AudioClip clip, float volume = 1f, bool varyPitch = false)
    {
        if (source == null || clip == null || preferences == null || !preferences.PlaySfx) return;
        source.pitch = varyPitch ? Random.Range(0.97f, 1.04f) : 1f;
        source.PlayOneShot(clip, volume); // Pickups and impacts no longer cut each other off.
    }

    public void PlayMoveLineSound() { Play(move_Audio_Source, move_Audio_Source.clip, 0.75f); }
    public void PlayJumpSound() { Play(jump_Audio_Source, jump_Audio_Source.clip, 0.8f); }
    public void PlayDeadSound() { Play(powerUp_Die_AudioSource, die_Clip, 0.9f); }
    public void PlayPowerUpSound() { Play(powerUp_Die_AudioSource, power_Up_Clip, 0.8f); }
    public void PlayCoinSound() { Play(powerUp_Die_AudioSource, coin_Clip, 0.65f, true); }
    public void PlayClickSound() { Play(powerUp_Die_AudioSource, ui_Click, 0.55f); }
    public void PlayBuySound() { Play(powerUp_Die_AudioSource, purchase_Clip, 0.8f); }
    public void PlayCrashSound() { Play(powerUp_Die_AudioSource, crash_Clip != null ? crash_Clip : die_Clip, 0.75f); }

    public void PlayGameOverClip()
    {
        if (gameOver) return;
        gameOver = true;
        ApplyPreferences();
        // This is an SFX cue: it obeys the SFX setting, independently of the music setting.
        Play(powerUp_Die_AudioSource, game_Over_Clip, 0.8f);
    }

    private void OnDestroy()
    {
        if (preferences != null) preferences.PreferencesChanged -= ApplyPreferences;
        if (instance == this) instance = null;
    }
}
