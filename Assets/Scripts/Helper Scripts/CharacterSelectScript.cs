using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSelectScript : MonoBehaviour
{
    // Existing serialized references are retained for scene compatibility.
    public GameObject[] available_Heroes;
    public Text selectedText;
    public GameObject starIcon;
    public Image selectBtn_Image;
    public Sprite button_Green, button_Blue;
    public Text starScoreText;

    public event Action CharacterChanged;
    public int CurrentIndex { get; private set; }

    private void Start()
    {
        CurrentIndex = GameManager.EnsureInstance().selected_Index;
        Refresh();
    }

    public void NextHero() { Browse(1); }
    public void PreviousHero() { Browse(-1); }

    private void Browse(int direction)
    {
        CurrentIndex = (CurrentIndex + direction + RunnerRules.HeroCount) % RunnerRules.HeroCount;
        Refresh();
    }

    public void ShowSelectedHero()
    {
        CurrentIndex = GameManager.EnsureInstance().selected_Index;
        Refresh();
    }

    public void Refresh()
    {
        GameManager manager = GameManager.EnsureInstance();
        CurrentIndex = Mathf.Clamp(CurrentIndex, 0, RunnerRules.HeroCount - 1);
        if (available_Heroes != null)
            for (int i = 0; i < available_Heroes.Length; i++)
                if (available_Heroes[i] != null) available_Heroes[i].SetActive(i == CurrentIndex);

        bool unlocked = manager.heroes[CurrentIndex];
        bool selected = manager.selected_Index == CurrentIndex;
        if (starIcon != null) starIcon.SetActive(!unlocked);
        if (selectBtn_Image != null) selectBtn_Image.sprite = selected ? button_Green : button_Blue;
        if (selectedText != null)
            selectedText.text = !unlocked ? RunnerRules.HeroPrice.ToString() : selected ? "Selected" : "Select";
        if (starScoreText != null) starScoreText.text = manager.starScore.ToString();
        if (CharacterChanged != null) CharacterChanged();
    }

    public HeroSelectionResult TrySelectHero()
    {
        HeroSelectionResult result = GameManager.EnsureInstance().SelectHero(CurrentIndex);
        Refresh();
        if (result == HeroSelectionResult.Purchased && SoundManager.instance != null)
            SoundManager.instance.PlayBuySound();
        return result;
    }

    public void SelectHero()
    {
        HeroSelectionResult result = TrySelectHero();
        if (RunnerUI.instance == null) return;
        if (result == HeroSelectionResult.NotEnoughStars)
            RunnerUI.instance.ShowToast("Need " + (RunnerRules.HeroPrice - GameManager.instance.starScore) + " more stars");
        else if (result == HeroSelectionResult.Purchased)
            RunnerUI.instance.ShowToast("Runner unlocked. Let's go!");
        else if (result != HeroSelectionResult.InvalidHero)
            RunnerUI.instance.ShowToast("Runner selected");
    }
}
