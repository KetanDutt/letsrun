using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    public GameObject hero_Menu;
    public Text starScoreText;
    public Image music_Img;
    public Sprite music_Off, music_On;
    public AudioClip menuMusic, uiClick, purchaseSound;

    private GameManager manager;

    private void Awake() { manager = GameManager.EnsureInstance(); }

    private void Start()
    {
        Time.timeScale = 1f;
        if (hero_Menu != null) hero_Menu.SetActive(false);
        SoundManager.CreateMenuAudio(menuMusic, uiClick, purchaseSound);
        manager.PreferencesChanged += RefreshMusicIcon;
        manager.ProgressChanged += RefreshWallet;
        RefreshMusicIcon();
        RefreshWallet();
        RunnerUI.CreateMenu(this, GetComponent<CharacterSelectScript>());
    }

    public void PlayGame() { SceneNavigator.Load("Gameplay"); }

    public void HeroMenu()
    {
        if (RunnerUI.instance != null) RunnerUI.instance.ShowShop();
        else if (hero_Menu != null) hero_Menu.SetActive(true);
        RefreshWallet();
    }

    public void HomeButton()
    {
        if (RunnerUI.instance != null) RunnerUI.instance.CloseModal();
        if (hero_Menu != null) hero_Menu.SetActive(false);
    }

    public void MusicButton() { manager.SetMusic(!manager.playSound); }

    public bool HandleBack() { return RunnerUI.instance != null && RunnerUI.instance.HandleBack(); }

    private void RefreshMusicIcon()
    {
        if (music_Img != null) music_Img.sprite = manager.playSound ? music_On : music_Off;
    }

    private void RefreshWallet() { if (starScoreText != null) starScoreText.text = manager.starScore.ToString(); }

    private void OnDestroy()
    {
        if (manager == null) return;
        manager.PreferencesChanged -= RefreshMusicIcon;
        manager.ProgressChanged -= RefreshWallet;
    }
}
