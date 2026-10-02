using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime uGUI presentation for the two authored scenes. The legacy canvas is retained but hidden;
/// gameplay, economy, and persistence stay in their controllers rather than in UI callbacks.
/// </summary>
public sealed class RunnerUI : MonoBehaviour
{
    public static RunnerUI instance;
    private enum Modal { None, Shop, Settings, Help, Tutorial, Pause, Results, Loading, Error }

    private MainMenuController menu;
    private CharacterSelectScript characters;
    private GameplayController game;
    private GameManager manager;
    private RectTransform canvasRect, safe, content, modalSafe, card, toastSafe;
    private UIConfetti celebration;
    private CanvasGroup contentGroup, overlayGroup, toastGroup, hintGroup;
    private GameObject overlay, powerPanel, saveBanner;
    private RectTransform toastRect;
    private Text toastText, walletText, bestText, scoreText, starsText, paceText, powerText, shopName, shopWallet, selectedRunnerText, saveStatusText;
    private Image shopHero, paceFill, powerFill;
    private Button shopSelect, musicToggle, sfxToggle, motionToggle, pauseButton, menuRunButton;
    private MotionTween starPulse, countdownPulse;
    private Text countdownText;
    private Action backAction;
    private Modal modal;
    private Coroutine toastRoutine;
    private bool resultsShown;
    private int previousScore = -1, previousStars = -1, previousPowerTick = -1;
    private RunState previousState = (RunState)(-1);
    private readonly Sprite[] heroes = new Sprite[RunnerRules.HeroCount];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static void CreateMenu(MainMenuController controller, CharacterSelectScript selection)
    {
        RunnerUI ui = Create();
        ui.menu = controller;
        ui.characters = selection;
        if (selection != null) selection.CharacterChanged += ui.RefreshShop;
        ui.BuildMenu();
        ui.RefreshProgress();
        ui.StartCoroutine(ui.ShowProfileNotice());
    }

    public static void CreateGameplay(GameplayController controller)
    {
        RunnerUI ui = Create();
        ui.game = controller;
        ui.BuildHud();
        ui.RefreshProgress();
        ui.StartCoroutine(ui.ShowProfileNotice());
    }

    private static RunnerUI Create()
    {
        if (instance != null) return instance;
        // Existing component references and animation clips are preserved for authoring/debugging.
        GameObject legacy = GameObject.Find("UI Canvas");
        if (legacy != null) legacy.SetActive(false);
        var ui = new GameObject("Runner UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        return ui.AddComponent<RunnerUI>();
    }

    private void Awake()
    {
        instance = this;
        manager = GameManager.EnsureInstance();
        manager.ProgressChanged += RefreshProgress;
        manager.PreferencesChanged += RefreshSettings;
        manager.SaveProblem += OnSaveProblem;
        canvasRect = GetComponent<RectTransform>();
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720f, 1280f);
        scaler.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
            new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        safe = UIFactory.Rect(canvasRect, "Safe Area", 0f, 0f, 1f, 1f);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        content = UIFactory.Rect(safe, "Content", 0f, 0f, 1f, 1f);
        contentGroup = content.gameObject.AddComponent<CanvasGroup>();
        for (int i = 0; i < heroes.Length; i++) heroes[i] = Resources.Load<Sprite>("Sprites/Player/hero" + i + "_big");
        BuildOverlay();
        BuildToast();
        celebration = toastSafe.gameObject.AddComponent<UIConfetti>();
        celebration.Initialize(toastSafe);
        Image banner = UIFactory.Image(safe, "Save Status", UIFactory.Coral, 0.035f, 0.115f, 0.965f, 0.149f);
        saveStatusText = UIFactory.Text(banner.transform, "Message", "Save pending · Check available storage", 18, 0.02f, 0f, 0.98f, 1f, UIFactory.Ink);
        saveBanner = banner.gameObject;
        saveBanner.SetActive(false);
    }

    private void BuildMenu()
    {
        Image sky = UIFactory.Art(canvasRect, "Sky", Resources.Load<Sprite>("Sprites/UI/BG/Background"), 0f, 0f, 1f, 1f);
        sky.preserveAspect = false;
        sky.transform.SetAsFirstSibling();
        UIFactory.Text(content, "Eyebrow", "ENDLESS LITTLE ADVENTURE", 17, 0.06f, 0.945f, 0.94f, 0.978f, UIFactory.Muted);
        UIFactory.Text(content, "Title", "LET'S RUN", 76, 0.025f, 0.842f, 0.975f, 0.944f, UIFactory.Ink, true);
        UIFactory.Text(content, "Subtitle", "Small steps. Big adventures.", 24, 0.05f, 0.805f, 0.95f, 0.847f, UIFactory.Muted);
        Image best = UIFactory.Image(content, "Personal Best", UIFactory.Paper, 0.055f, 0.735f, 0.485f, 0.801f, true, true);
        bestText = UIFactory.Text(best.transform, "Value", "BEST  0s", 24, 0.04f, 0f, 0.96f, 1f, UIFactory.Ink);
        Image wallet = UIFactory.Image(content, "Wallet", UIFactory.Gold, 0.515f, 0.735f, 0.945f, 0.801f, true, true);
        walletText = UIFactory.Text(wallet.transform, "Value", "0 STARS", 24, 0.04f, 0f, 0.96f, 1f, UIFactory.Ink);
        Image art = UIFactory.Art(content, "Little Adventure", Resources.Load<Sprite>("Sprites/UI/BG/Menu Scene 1"), 0.015f, 0.255f, 0.985f, 0.735f);
        art.gameObject.AddComponent<IdleFloat>();
        selectedRunnerText = UIFactory.Text(content, "Selected Runner", "Pick a runner. Find your rhythm.", 21, 0.04f, 0.257f, 0.96f, 0.294f, UIFactory.Muted);
        menuRunButton = UIFactory.Button(content, "LET'S RUN!", UIFactory.Coral, menu.PlayGame, 0.055f, 0.163f, 0.945f, 0.246f, 31);
        UIFactory.Button(content, "RUNNERS", UIFactory.Paper, menu.HeroMenu, 0.055f, 0.055f, 0.485f, 0.123f);
        UIFactory.Button(content, "SETTINGS", UIFactory.Paper, ShowSettings, 0.515f, 0.055f, 0.945f, 0.123f);
        UIFactory.Text(content, "Footer", "OFFLINE · NO ADS · JUST RUN", 16, 0.04f, 0.012f, 0.96f, 0.043f, UIFactory.Muted);
        Focus(menuRunButton);
    }

    private void BuildHud()
    {
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        Image score = UIFactory.Image(content, "Survival Card", UIFactory.Paper, 0.035f, 0.913f, 0.49f, 0.983f, true, true);
        UIFactory.Text(score.transform, "Caption", "SURVIVAL", 15, 0.06f, 0.59f, 0.94f, 0.95f, UIFactory.Muted);
        scoreText = UIFactory.Text(score.transform, "Score", "0s", 37, 0.04f, 0.02f, 0.96f, 0.62f, UIFactory.Ink, true);
        Image stars = UIFactory.Image(content, "Stars Card", UIFactory.Gold, 0.515f, 0.913f, 0.795f, 0.983f, true, true);
        UIFactory.Text(stars.transform, "Caption", "STARS", 15, 0.03f, 0.59f, 0.97f, 0.95f, UIFactory.Ink);
        starsText = UIFactory.Text(stars.transform, "Stars", "0", 35, 0.05f, 0.02f, 0.95f, 0.63f, UIFactory.Ink, true);
        starPulse = starsText.gameObject.AddComponent<MotionTween>();
        pauseButton = UIFactory.Button(content, "II", UIFactory.Ink, game.PauseGame, 0.82f, 0.913f, 0.965f, 0.983f, 28, true);
        NoNavigation(pauseButton);
        UIFactory.Image(content, "Pace Track", new Color(1f, 1f, 1f, 0.55f), 0.045f, 0.893f, 0.955f, 0.9f);
        paceFill = UIFactory.Progress(content, "Pace Progress", UIFactory.Coral, 0.045f, 0.893f, 0.955f, 0.9f);
        paceText = UIFactory.Text(content, "Pace", "CRUISING · NEXT PACE AT 30s", 16, 0.04f, 0.865f, 0.96f, 0.891f, UIFactory.Ink);

        Image power = UIFactory.Image(content, "T-Rex Timer", UIFactory.Ink, 0.075f, 0.799f, 0.925f, 0.853f, true, true);
        powerText = UIFactory.Text(power.transform, "Power Label", "T-REX · 7.0s", 23, 0.06f, 0.32f, 0.94f, 0.94f, UIFactory.Paper);
        UIFactory.Image(power.transform, "Track", new Color(1f, 1f, 1f, 0.22f), 0.06f, 0.16f, 0.94f, 0.23f);
        powerFill = UIFactory.Progress(power.transform, "Time Left", UIFactory.Teal, 0.06f, 0.16f, 0.94f, 0.23f);
        powerPanel = power.gameObject;
        powerPanel.SetActive(false);

        NoNavigation(UIFactory.Button(content, "LOWER", UIFactory.Ink, () => { if (PlayerController.instance != null) PlayerController.instance.RequestLane(0); }, 0.035f, 0.024f, 0.325f, 0.094f, 23, true));
        NoNavigation(UIFactory.Button(content, "JUMP", UIFactory.Coral, () => { if (PlayerController.instance != null) PlayerController.instance.RequestJump(); }, 0.355f, 0.024f, 0.645f, 0.094f, 26));
        NoNavigation(UIFactory.Button(content, "UPPER", UIFactory.Ink, () => { if (PlayerController.instance != null) PlayerController.instance.RequestLane(1); }, 0.675f, 0.024f, 0.965f, 0.094f, 23, true));
        Image hint = UIFactory.Image(content, "Controls Hint", new Color(UIFactory.Ink.r, UIFactory.Ink.g, UIFactory.Ink.b, 0.82f), 0.035f, 0.107f, 0.965f, 0.145f);
        UIFactory.Text(hint.transform, "Hint", Application.isMobilePlatform ? "Swipe lanes · Swipe up to jump · Or use the buttons" : "A / D or arrows · SPACE jump · P pause", 17, 0.025f, 0f, 0.975f, 1f, UIFactory.Paper);
        hintGroup = hint.gameObject.AddComponent<CanvasGroup>();

        Image count = UIFactory.Image(safe, "Countdown Badge", UIFactory.Paper, 0.32f, 0.46f, 0.68f, 0.595f, true, true);
        countdownText = UIFactory.Text(count.transform, "Count", "3", 83, 0f, 0.02f, 1f, 0.98f, UIFactory.Ink, true);
        countdownPulse = count.gameObject.AddComponent<MotionTween>();
        count.gameObject.SetActive(false);
    }

    private void BuildOverlay()
    {
        Image shade = UIFactory.Image(canvasRect, "Modal Backdrop", new Color(UIFactory.Ink.r, UIFactory.Ink.g, UIFactory.Ink.b, 0.8f), 0f, 0f, 1f, 1f, false);
        shade.raycastTarget = true;
        overlay = shade.gameObject;
        overlayGroup = overlay.AddComponent<CanvasGroup>();
        modalSafe = UIFactory.Rect(shade.transform, "Modal Safe Area", 0f, 0f, 1f, 1f);
        modalSafe.gameObject.AddComponent<SafeAreaFitter>();
        overlay.SetActive(false);
    }

    private RectTransform ShowModal(Modal kind, string title, Action onBack, bool tall = false)
    {
        if (card != null) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
        modal = kind;
        backAction = onBack;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        toastSafe.SetAsLastSibling();
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        overlayGroup.alpha = 1f;
        contentGroup.interactable = false;
        contentGroup.blocksRaycasts = false;
        Image panel = UIFactory.Image(modalSafe, kind + " Card", UIFactory.Paper, 0.055f, tall ? 0.13f : 0.205f, 0.945f, tall ? 0.87f : 0.795f, true, true);
        card = panel.rectTransform;
        UIFactory.Text(card, "Title", title, 40, 0.05f, 0.855f, 0.805f, 0.963f, UIFactory.Ink, true);
        if (onBack != null)
            UIFactory.Button(card, "X", UIFactory.Teal, onBack, 0.81f, 0.857f, 0.963f, 0.976f, 22);
        UIFactory.Image(card, "Divider", new Color(UIFactory.Ink.r, UIFactory.Ink.g, UIFactory.Ink.b, 0.1f), 0.06f, 0.83f, 0.94f, 0.834f, false);
        RectTransform body = UIFactory.Rect(card, "Body", 0.065f, 0.055f, 0.935f, 0.815f);
        panel.gameObject.AddComponent<MotionTween>().Open();
        return body;
    }

    public void CloseModal()
    {
        modal = Modal.None;
        backAction = null;
        overlay.SetActive(false);
        contentGroup.interactable = true;
        contentGroup.blocksRaycasts = true;
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = menu != null;
        if (menu != null) Focus(menuRunButton);
        else ClearFocus();
    }

    public void ShowShop()
    {
        if (characters == null) { ShowToast("Runner selection is unavailable"); return; }
        RectTransform body = ShowModal(Modal.Shop, "YOUR RUNNERS", CloseModal, true);
        shopName = UIFactory.Text(body, "Runner Name", "RUNNER 01", 30, 0.05f, 0.863f, 0.95f, 0.975f, UIFactory.Ink, true);
        shopHero = UIFactory.Art(body, "Runner Preview", heroes[0], 0.22f, 0.35f, 0.78f, 0.855f);
        shopHero.gameObject.AddComponent<MotionTween>();
        UIFactory.Button(body, "PREV", UIFactory.Teal, characters.PreviousHero, 0f, 0.535f, 0.195f, 0.66f, 20);
        UIFactory.Button(body, "NEXT", UIFactory.Teal, characters.NextHero, 0.805f, 0.535f, 1f, 0.66f, 20);
        shopWallet = UIFactory.Text(body, "Wallet", "YOUR STARS  0", 22, 0.02f, 0.248f, 0.98f, 0.33f, UIFactory.Muted);
        shopSelect = UIFactory.Button(body, "SELECT", UIFactory.Coral, characters.SelectHero, 0.04f, 0.085f, 0.96f, 0.223f, 25);
        UIFactory.Text(body, "Shop Hint", "Collect stars on the road to unlock all nine runners.", 18, 0f, 0f, 1f, 0.073f, UIFactory.Muted);
        characters.ShowSelectedHero();
        RefreshShop();
        Focus(shopSelect.IsInteractable() ? shopSelect : body.GetComponentInChildren<Button>());
    }

    private void RefreshShop()
    {
        if (modal != Modal.Shop || shopHero == null || characters == null) return;
        int index = characters.CurrentIndex;
        bool owned = manager.heroes[index], selected = index == manager.selected_Index;
        shopHero.sprite = heroes[index];
        shopHero.color = owned ? Color.white : new Color(0.62f, 0.7f, 0.72f, 0.8f);
        shopHero.GetComponent<MotionTween>().Pulse(0.07f);
        shopName.text = "RUNNER " + (index + 1).ToString("00") + (owned ? "" : " · LOCKED");
        shopWallet.text = "YOUR STARS  " + manager.starScore.ToString("N0");
        UIFactory.SetLabel(shopSelect, selected ? "SELECTED" : owned ? "SELECT RUNNER" : "UNLOCK · " + RunnerRules.HeroPrice + " STARS");
        shopSelect.interactable = !selected;
    }

    public void ShowSettings()
    {
        RectTransform body = ShowModal(Modal.Settings, "SETTINGS", ReturnFromSettings);
        UIFactory.Text(body, "Settings Hint", Compact(manager.TotalRuns) + " RUNS · " + Compact(manager.LifetimeStars) + " STARS COLLECTED", 17, 0f, 0.895f, 1f, 0.97f, UIFactory.Muted);
        musicToggle = UIFactory.Button(body, "MUSIC  ON", UIFactory.Teal, () => manager.SetMusic(!manager.playSound), 0f, 0.715f, 1f, 0.875f);
        sfxToggle = UIFactory.Button(body, "SOUND FX  ON", UIFactory.Teal, () => manager.SetSfx(!manager.PlaySfx), 0f, 0.52f, 1f, 0.68f);
        motionToggle = UIFactory.Button(body, "REDUCED MOTION  OFF", UIFactory.Teal, () => manager.SetReducedMotion(!manager.ReducedMotion), 0f, 0.325f, 1f, 0.485f, 22);
        UIFactory.Button(body, "HOW TO PLAY", UIFactory.Gold, () => ShowHelp(ShowSettings), 0f, 0.14f, 1f, 0.285f, 23);
        UIFactory.Text(body, "Saved Locally", "Preferences save automatically on this device.", 17, 0f, 0.015f, 1f, 0.1f, UIFactory.Muted);
        RefreshSettings();
        Focus(musicToggle);
    }

    private void ReturnFromSettings()
    {
        if (game != null && game.State == RunState.Paused) ShowPause();
        else if (game != null && game.State == RunState.GameOver) ShowResults(game.CurrentScore, game.RunStars, game.CurrentScore > game.StartingBest);
        else CloseModal();
    }

    private void RefreshSettings()
    {
        if (modal != Modal.Settings) return;
        UIFactory.SetLabel(musicToggle, "MUSIC  " + (manager.playSound ? "ON" : "OFF"));
        UIFactory.SetLabel(sfxToggle, "SOUND FX  " + (manager.PlaySfx ? "ON" : "OFF"));
        UIFactory.SetLabel(motionToggle, "REDUCED MOTION  " + (manager.ReducedMotion ? "ON" : "OFF"));
    }

    public void ShowTutorial(Action start)
    {
        BuildHelp(Modal.Tutorial, "READY TO RUN?", start, game.HomeButton, "LET'S GO!");
    }

    private void ShowHelp(Action close) { BuildHelp(Modal.Help, "HOW TO RUN", close, close, "GOT IT"); }

    private void BuildHelp(Modal kind, string title, Action primary, Action back, string button)
    {
        RectTransform body = ShowModal(kind, title, back, true);
        UIFactory.Text(body, "Intro", "Two lanes. One little adventure.", 24, 0f, 0.86f, 1f, 0.97f, UIFactory.Ink);
        HelpRow(body, "01", "SWITCH LANES", "Swipe left / right or use A / D.\nOr tap LOWER / UPPER.", 0.66f, 0.84f, UIFactory.Teal);
        HelpRow(body, "02", "JUMP THE OBSTACLES", "Swipe up, SPACE or tap JUMP.\nTime it to avoid obstacles.", 0.435f, 0.615f, UIFactory.Coral);
        HelpRow(body, "03", "STARS & T-REX", "Collect stars to unlock runners.\nT-Rex smashes obstacles for 7s.", 0.21f, 0.39f, UIFactory.Gold);
        Button action = UIFactory.Button(body, button, UIFactory.Coral, primary, 0f, 0.015f, 1f, 0.155f, 26);
        Focus(action);
    }

    private static void HelpRow(Transform parent, string number, string title, string description, float y0, float y1, Color color)
    {
        Image badge = UIFactory.Image(parent, title + " Badge", color, 0f, y0 + 0.035f, 0.13f, y1 - 0.035f);
        UIFactory.Text(badge.transform, "Number", number, 25, 0f, 0f, 1f, 1f, UIFactory.Ink, true);
        UIFactory.Text(parent, title, title, 21, 0.17f, y0 + 0.11f, 1f, y1, UIFactory.Ink, false, TextAnchor.MiddleLeft);
        UIFactory.Text(parent, "Description", description, 26, 0.17f, y0, 1f, y0 + 0.105f, UIFactory.Muted, false, TextAnchor.MiddleLeft);
    }

    public void ShowPause()
    {
        if (game == null) return;
        RectTransform body = ShowModal(Modal.Pause, "TAKE A BREATHER", game.ResumeGame);
        UIFactory.Text(body, "Current Run", game.CurrentScore + "s  ·  " + game.RunStars + " stars banked", 28, 0f, 0.81f, 1f, 0.96f, UIFactory.Ink, true);
        UIFactory.Text(body, "Pause Hint", "Your adventure will be right here.", 21, 0f, 0.7f, 1f, 0.805f, UIFactory.Muted);
        Button resume = UIFactory.Button(body, "KEEP RUNNING", UIFactory.Coral, game.ResumeGame, 0f, 0.475f, 1f, 0.65f, 27);
        UIFactory.Button(body, "RETRY", UIFactory.Teal, game.RestartGame, 0f, 0.25f, 0.475f, 0.42f);
        UIFactory.Button(body, "HOME", UIFactory.Teal, game.HomeButton, 0.525f, 0.25f, 1f, 0.42f);
        UIFactory.Button(body, "SETTINGS", UIFactory.Gold, ShowSettings, 0f, 0.025f, 1f, 0.195f);
        Focus(resume);
    }

    public void ShowResults(int score, int stars, bool newBest)
    {
        if (game == null) return;
        RectTransform body = ShowModal(Modal.Results, newBest ? "NEW PERSONAL BEST!" : "ONE MORE RUN?", game.HomeButton, true);
        UIFactory.Text(body, "Result Caption", newBest ? "That was your best adventure yet." : "Every run is a fresh start.", 22, 0f, 0.865f, 1f, 0.97f, UIFactory.Muted);
        Text value = UIFactory.Text(body, "Final Score", "0s", 90, 0f, 0.6f, 1f, 0.858f, UIFactory.Ink, true);
        UIFactory.Text(body, "Survived", "SECONDS SURVIVED", 18, 0f, 0.55f, 1f, 0.622f, UIFactory.Muted);
        Image starCard = UIFactory.Image(body, "Banked Stars", UIFactory.Gold, 0f, 0.418f, 0.475f, 0.525f);
        UIFactory.Text(starCard.transform, "Value", stars + " STARS", 24, 0f, 0f, 1f, 1f, UIFactory.Ink);
        Image bestCard = UIFactory.Image(body, "Best", UIFactory.Teal, 0.525f, 0.418f, 1f, 0.525f);
        UIFactory.Text(bestCard.transform, "Value", "BEST " + manager.score_Count + "s", 23, 0f, 0f, 1f, 1f, UIFactory.Ink);
        UIFactory.Text(body, "Progress", "Run " + manager.TotalRuns + " · " + Compact(manager.starScore) + " stars in your wallet", 20, 0f, 0.335f, 1f, 0.405f, UIFactory.Muted);
        Button retry = UIFactory.Button(body, "RUN AGAIN", UIFactory.Coral, game.RestartGame, 0f, 0.163f, 1f, 0.305f, 29);
        UIFactory.Button(body, "HOME", UIFactory.Teal, game.HomeButton, 0f, 0f, 0.475f, 0.123f);
        UIFactory.Button(body, "SETTINGS", UIFactory.Teal, ShowSettings, 0.525f, 0f, 1f, 0.123f);
        if (!resultsShown)
        {
            StartCoroutine(CountScore(value, score));
            if (newBest && celebration != null) celebration.Burst();
        }
        else value.text = score + "s";
        resultsShown = true;
        Focus(retry);
    }

    private IEnumerator CountScore(Text target, int score)
    {
        if (manager.ReducedMotion) { target.text = score + "s"; yield break; }
        int previous = -1;
        for (float elapsed = 0f; elapsed < 0.7f && target != null && !manager.ReducedMotion; elapsed += Time.unscaledDeltaTime)
        {
            int value = (int)Math.Min(score, Math.Round((double)score * RunnerRules.EaseOutCubic(elapsed / 0.7f)));
            if (value != previous) { previous = value; target.text = value + "s"; }
            yield return null;
        }
        if (target != null) target.text = score + "s";
    }

    public void SceneLoadFailed()
    {
        if (game != null) game.NavigationFailed();
        else CloseModal();
        ShowToast("The track couldn't be loaded. Try again.");
    }

    public void ShowFatalError(string message)
    {
        RectTransform body = ShowModal(Modal.Error, "TRACK UNAVAILABLE", game.HomeButton);
        UIFactory.Text(body, "Error", message, 27, 0f, 0.47f, 1f, 0.91f, UIFactory.Ink);
        Button home = UIFactory.Button(body, "RETURN HOME", UIFactory.Coral, game.HomeButton, 0f, 0.08f, 1f, 0.3f, 27);
        Focus(home);
    }

    public void ShowLoading()
    {
        RectTransform body = ShowModal(Modal.Loading, "GET READY...", null);
        UIFactory.Text(body, "Loading", "A little adventure is on its way.", 26, 0.05f, 0.25f, 0.95f, 0.75f, UIFactory.Ink);
        ClearFocus();
    }

    public bool HandleBack()
    {
        if (modal == Modal.None || modal == Modal.Pause) return false;
        if (modal == Modal.Loading) return true;
        if (backAction != null) backAction();
        else CloseModal();
        return true;
    }

    public void ShowCountdown(string text)
    {
        if (countdownText == null) return;
        countdownText.transform.parent.gameObject.SetActive(true);
        countdownText.text = text;
        countdownPulse.Pulse(0.1f);
        ClearFocus();
    }

    public void HideCountdown()
    {
        if (countdownText != null) countdownText.transform.parent.gameObject.SetActive(false);
    }

    public void PulseStars()
    {
        if (starsText != null) starsText.text = Compact(game.RunStars);
        if (starPulse != null) starPulse.Pulse();
    }

    private void RefreshProgress()
    {
        if (walletText != null) walletText.text = Compact(manager.starScore) + " STARS";
        if (bestText != null) bestText.text = "BEST  " + manager.score_Count + "s";
        if (selectedRunnerText != null) selectedRunnerText.text = "RUNNER " + (manager.selected_Index + 1).ToString("00") + " IS READY";
        RefreshShop();
    }

    private void Update()
    {
        bool saveProblem = manager.IsSaveReadOnly || !string.IsNullOrEmpty(manager.LastSaveError);
        saveStatusText.text = manager.IsSaveReadOnly ? "Save protected · Install the newer app version" : "Save pending · Check available storage";
        if (saveBanner.activeSelf != saveProblem) saveBanner.SetActive(saveProblem);
        if (game == null) return;
        if (previousState != game.State)
        {
            previousState = game.State;
            pauseButton.interactable = game.State == RunState.Running || game.State == RunState.Countdown;
            if (game.State == RunState.Running)
            {
                if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
                ClearFocus();
            }
        }
        if (previousScore != game.CurrentScore)
        {
            previousScore = game.CurrentScore;
            scoreText.text = game.CurrentScore + "s";
            int score = game.CurrentScore;
            paceFill.fillAmount = score >= 60 ? 1f : score >= 30 ? (score - 30) / 30f : score / 30f;
            paceText.text = score >= 60 ? "MAX PACE · FIND YOUR RHYTHM" : score >= 30 ? "FAST · NEXT PACE AT 60s" : "CRUISING · NEXT PACE AT 30s";
        }
        if (previousStars != game.RunStars) { previousStars = game.RunStars; starsText.text = Compact(game.RunStars); }
        float remaining = PlayerController.instance == null ? 0f : PlayerController.instance.PowerUpRemaining;
        if (powerPanel.activeSelf != (remaining > 0f)) powerPanel.SetActive(remaining > 0f);
        if (remaining > 0f)
        {
            powerFill.fillAmount = Mathf.Clamp01(remaining / RunnerRules.PowerUpSeconds);
            int tick = Mathf.CeilToInt(remaining * 10f);
            if (tick != previousPowerTick) { previousPowerTick = tick; powerText.text = "T-REX · " + (tick / 10f).ToString("0.0") + "s"; }
        }
        if (hintGroup != null && hintGroup.gameObject.activeSelf)
        {
            if (saveProblem) hintGroup.gameObject.SetActive(false);
            else if (game.CurrentScore >= 8) hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, 0f, Time.unscaledDeltaTime);
            if (hintGroup.alpha <= 0f) hintGroup.gameObject.SetActive(false);
        }
    }

    private void BuildToast()
    {
        toastSafe = UIFactory.Rect(canvasRect, "Feedback Safe Area", 0f, 0f, 1f, 1f);
        toastSafe.gameObject.AddComponent<SafeAreaFitter>();
        Image toast = UIFactory.Image(toastSafe, "Toast", UIFactory.Ink, 0.075f, 0.657f, 0.925f, 0.739f, true, true);
        toastRect = toast.rectTransform;
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.interactable = false;
        toastGroup.blocksRaycasts = false;
        toastText = UIFactory.Text(toast.transform, "Message", "", 22, 0.045f, 0.08f, 0.955f, 0.92f, UIFactory.Paper);
        toast.gameObject.SetActive(false);
    }

    public void ShowToast(string message)
    {
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(Toast(message));
    }

    private IEnumerator Toast(string message)
    {
        toastRect.gameObject.SetActive(true);
        toastSafe.SetAsLastSibling();
        toastRect.SetAsLastSibling();
        toastText.text = message;
        toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.4f);
        if (!manager.ReducedMotion)
            for (float elapsed = 0f; elapsed < 0.25f; elapsed += Time.unscaledDeltaTime)
            {
                toastGroup.alpha = 1f - elapsed / 0.25f;
                yield return null;
            }
        toastGroup.alpha = 0f;
        toastRect.gameObject.SetActive(false);
        toastRoutine = null;
    }

    private IEnumerator ShowProfileNotice()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        if (!string.IsNullOrEmpty(manager.LastSaveError)) OnSaveProblem(manager.LastSaveError);
        else if (!string.IsNullOrEmpty(manager.ProfileNotice)) ShowToast(manager.ProfileNotice);
    }

    private void OnSaveProblem(string message) { ShowToast("Progress couldn't be saved. Check available storage."); }

    private static string Compact(int value)
    {
        if (value >= 1000000000) return (value / 1000000000f).ToString("0.#") + "B";
        if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
        if (value >= 10000) return (value / 1000f).ToString("0.#") + "K";
        return value.ToString();
    }

    private static void NoNavigation(Button button)
    {
        button.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    private static void Focus(Button button)
    {
        if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private static void ClearFocus()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDestroy()
    {
        if (characters != null) characters.CharacterChanged -= RefreshShop;
        if (manager != null)
        {
            manager.ProgressChanged -= RefreshProgress;
            manager.PreferencesChanged -= RefreshSettings;
            manager.SaveProblem -= OnSaveProblem;
        }
        if (instance == this) instance = null;
    }
}
