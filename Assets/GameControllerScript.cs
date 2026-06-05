using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameControllerScript : MonoBehaviour
{
    const int ReviveCost = 10;
    const int MaxStoredResults = 5;
    const float ControlHintDuration = 5f;
    const string VolumePrefsKey = "GalaxyRaiders.Volume";
    const string ResultsPrefsKey = "GalaxyRaiders.Results";
    static readonly Color DialogBackgroundColor = new Color(0.05f, 0.08f, 0.14f, 0.94f);
    static readonly Color DialogOverlayColor = new Color(0f, 0f, 0f, 0.78f);
    static readonly Color ButtonNormalColor = new Color(0.13f, 0.2f, 0.33f, 0.96f);
    static readonly Color ButtonHighlightedColor = new Color(0.22f, 0.34f, 0.55f, 1f);
    static readonly Color ButtonPressedColor = new Color(0.08f, 0.14f, 0.24f, 1f);
    static readonly Color PauseOverlayColor = new Color(0f, 0.015f, 0.055f, 0.68f);
    static readonly Color PausePanelColor = new Color(0.018f, 0.055f, 0.12f, 0.84f);
    static readonly Color PauseGlowColor = new Color(0.24f, 0.78f, 1f, 0.18f);
    static readonly Color PauseLineColor = new Color(0.45f, 0.88f, 1f, 0.92f);
    static readonly Color PauseTextColor = new Color(0.9f, 0.96f, 1f, 1f);
    static readonly Vector2 PauseWindowSize = new Vector2(430f, 290f);
    static readonly Vector2 PauseTitlePosition = new Vector2(0f, 86f);
    static readonly Vector2 PauseTitleSize = new Vector2(360f, 62f);
    static readonly Vector2 PauseButtonSize = new Vector2(270f, 54f);
    static readonly Vector2 VictoryWindowSize = new Vector2(470f, 380f);
    static readonly Vector2 VictoryTitlePosition = new Vector2(0f, 142f);
    static readonly Vector2 VictoryTitleSize = new Vector2(390f, 64f);
    static readonly Vector2 VictoryScorePosition = new Vector2(0f, 93f);
    static readonly Vector2 VictoryScoreSize = new Vector2(360f, 38f);
    static readonly Vector2 VictoryResultsPosition = new Vector2(0f, -2f);
    static readonly Vector2 VictoryResultsSize = new Vector2(360f, 150f);
    static readonly Vector2 VictoryButtonPosition = new Vector2(0f, -120f);
    static readonly Vector2 MainTitlePosition = new Vector2(0f, 165f);
    static readonly Vector2 MainTitleSize = new Vector2(560f, 80f);
    static readonly Vector2 MainLogoPosition = new Vector2(0f, -36f);
    static readonly Vector2 MainLogoSize = new Vector2(520f, 346.6667f);
    static readonly Vector2 MainPlayButtonPosition = new Vector2(0f, -176.8f);
    static readonly Vector2 MainPlayButtonSize = new Vector2(340f, 58f);
    static readonly Vector2 MainPlayButtonImageSize = new Vector2(428.145f, 285.43f);
    static readonly Vector2 PlayButtonPosition = new Vector2(0f, -145f);
    static readonly Vector2 PlayButtonSize = new Vector2(300f, 58f);
    static readonly Vector2 VolumeGroupPosition = new Vector2(0f, -295f);
    static readonly Vector2 VolumeGroupSize = new Vector2(360f, 70f);
    static readonly Vector2 VolumeLabelPosition = new Vector2(0f, 18f);
    static readonly Vector2 VolumeLabelSize = new Vector2(220f, 22f);
    static readonly Vector2 VolumeSliderPosition = new Vector2(0f, -12f);
    static readonly Vector2 VolumeSliderSize = new Vector2(320f, 24f);
    static readonly Vector2 ControlHintPosition = new Vector2(0f, -84f);
    static readonly Vector2 ControlHintSize = new Vector2(420f, 42f);

    public Text scoreText;
    public Button startButton;
    public GameObject menu;
    public Slider playerHpSlider;

    [Header("Main Menu")]
    public Image mainLogoImage;
    public Text mainTitleText;
    public Slider volumeSlider;
    public Text controlHintText;

    [Header("Pause UI")]
    public GameObject pausePanel;
    public Button continueButton;
    public Button pauseMainMenuButton;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Button reviveButton;
    public Button gameOverMainMenuButton;

    [Header("Victory UI")]
    public GameObject victoryPanel;
    public Text victoryScoreText;
    public Text previousResultsText;
    public Button victoryMainMenuButton;

    int score = 0;
    bool resultSavedThisRound = false;
    GameObject playerForRevive;
    Coroutine controlHintCoroutine;
    GameUiState currentState = GameUiState.MainMenu;

    public bool isStarted = false;

    public static GameControllerScript instance;

    [Header("Difficulty / Levels")]
    [SerializeField] float levelBannerDuration = 2f;
    [SerializeField] float spawnPauseBetweenLevels = 2f;
    [SerializeField] int level2StartsAfterEnemyKills = 10;
    [SerializeField] int bossStartsAfterEnemyKills = 20;
    [SerializeField] int enemySpawnsPerTickLevel1 = 1;
    [SerializeField] int enemySpawnsPerTickLevel2 = 2;
    [SerializeField] float enemySpeedMultiplierLevel1 = 1f;
    [SerializeField] float enemySpeedMultiplierLevel2 = 1.25f;
    [SerializeField] float enemySpeedMultiplierLevel3 = 1.35f;
    [SerializeField] int enemyDamageLevel1 = 10;
    [SerializeField] int enemyDamageLevel2 = 10;
    [SerializeField] int enemyDamageLevel3 = 10;
    [SerializeField] float minSpawnDelayLevel1 = 0.20f;
    [SerializeField] float maxSpawnDelayLevel1 = 0.80f;
    [SerializeField] float minSpawnDelayLevel2 = 0.12f;
    [SerializeField] float maxSpawnDelayLevel2 = 0.55f;

    int enemyKills = 0;
    int difficultyLevel = 1;
    float spawnPausedUntilUnscaled = 0f;
    Text levelBannerText;
    Coroutine levelBannerCoroutine;

    enum GameUiState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        CreateMissingUi();
        StyleUi();
        BindUiEvents();

        float savedVolume = PlayerPrefs.GetFloat(VolumePrefsKey, AudioListener.volume);
        ApplyVolume(savedVolume);
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
        }

        UpdateScoreText();
        ResetPlayer();
        SetUiState(GameUiState.MainMenu);

        ResetDifficultyProgress();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameUiState.Playing)
            {
                ShowPause();
            }
            else if (currentState == GameUiState.Paused)
            {
                ContinueGame();
            }
        }
    }

    public void increaseScore(int increment)
    {
        score += increment;
        UpdateScoreText();
        UpdateReviveButton();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        isStarted = true;
        resultSavedThisRound = false;
        ResetDifficultyProgress();
        ResetEmitters();
        SetUiState(GameUiState.Playing);
        ShowControlHint();
    }

    public void ShowPause()
    {
        if (currentState != GameUiState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        isStarted = false;
        SetUiState(GameUiState.Paused);
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        isStarted = true;
        SetUiState(GameUiState.Playing);
    }

    public void ShowGameOver(GameObject player)
    {
        playerForRevive = player;
        Time.timeScale = 0f;
        isStarted = false;
        UpdateReviveButton();
        SetUiState(GameUiState.GameOver);
    }

    public void Revive()
    {
        if (score < ReviveCost || playerForRevive == null)
        {
            return;
        }

        score -= ReviveCost;
        UpdateScoreText();

        PLayerScript player = playerForRevive.GetComponent<PLayerScript>();
        if (player != null)
        {
            player.Revive();
        }
        else
        {
            playerForRevive.SetActive(true);
        }

        playerForRevive = null;
        ContinueGame();
    }

    public void ShowVictory()
    {
        Time.timeScale = 0f;
        isStarted = false;

        if (victoryScoreText != null)
        {
            victoryScoreText.text = "Score: " + score;
        }

        if (!resultSavedThisRound)
        {
            SaveResult(score);
            resultSavedThisRound = true;
        }

        if (previousResultsText != null)
        {
            previousResultsText.text = FormatResults(LoadResults());
        }

        SetUiState(GameUiState.Victory);
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        isStarted = false;
        resultSavedThisRound = false;
        score = 0;
        ResetDifficultyProgress();
        UpdateScoreText();
        ClearDynamicObjects();
        ResetEmitters();
        ResetPlayer();
        SetUiState(GameUiState.MainMenu);
    }

    public void RegisterEnemyKill(int scoreIncrement)
    {
        increaseScore(scoreIncrement);
        enemyKills += 1;
        CheckLevelProgression();
    }

    void ResetDifficultyProgress()
    {
        enemyKills = 0;
        difficultyLevel = 1;
        spawnPausedUntilUnscaled = 0f;
        HideLevelBannerInstant();
    }

    void ResetEmitters()
    {
        EmitterScript[] emitters = FindObjectsOfType<EmitterScript>();
        for (int i = 0; i < emitters.Length; i++)
        {
            emitters[i].ResetEmitterState();
        }
    }

    void CheckLevelProgression()
    {
        if (difficultyLevel == 1 && enemyKills >= level2StartsAfterEnemyKills)
        {
            difficultyLevel = 2;
            StartLevelTransition("УРОВЕНЬ 2");
            return;
        }

        if (difficultyLevel == 2 && enemyKills >= bossStartsAfterEnemyKills)
        {
            difficultyLevel = 3;
            StartLevelTransition("ФИНАЛЬНЫЙ БОСС");
            return;
        }
    }

    void StartLevelTransition(string label)
    {
        spawnPausedUntilUnscaled = Time.unscaledTime + spawnPauseBetweenLevels;
        ShowLevelBanner(label);

        if (levelBannerCoroutine != null)
        {
            StopCoroutine(levelBannerCoroutine);
        }
        levelBannerCoroutine = StartCoroutine(HideLevelBannerAfterDelay());
    }

    IEnumerator HideLevelBannerAfterDelay()
    {
        yield return new WaitForSecondsRealtime(levelBannerDuration);
        HideLevelBannerInstant();
        levelBannerCoroutine = null;
    }

    void ShowLevelBanner(string label)
    {
        if (levelBannerText == null)
        {
            return;
        }

        levelBannerText.text = string.IsNullOrEmpty(label) ? string.Empty : label;
        levelBannerText.gameObject.SetActive(true);
    }

    void HideLevelBannerInstant()
    {
        if (levelBannerText == null)
        {
            return;
        }

        levelBannerText.text = string.Empty;
        levelBannerText.gameObject.SetActive(false);
    }

    public bool IsInSpawnPause()
    {
        if (!isStarted)
        {
            return true;
        }
        return Time.unscaledTime < spawnPausedUntilUnscaled;
    }

    public bool IsBossTime()
    {
        return difficultyLevel >= 3;
    }

    public int GetDifficultyLevel()
    {
        return difficultyLevel;
    }

    public float GetEnemySpeedMultiplier()
    {
        if (difficultyLevel == 1) return enemySpeedMultiplierLevel1;
        if (difficultyLevel == 2) return enemySpeedMultiplierLevel2;
        return enemySpeedMultiplierLevel3;
    }

    public int GetEnemyDamage()
    {
        if (difficultyLevel == 1) return enemyDamageLevel1;
        if (difficultyLevel == 2) return enemyDamageLevel2;
        return enemyDamageLevel3;
    }

    public int GetEnemySpawnsPerTick()
    {
        if (difficultyLevel == 1) return Mathf.Max(1, enemySpawnsPerTickLevel1);
        if (difficultyLevel == 2) return Mathf.Max(1, enemySpawnsPerTickLevel2);
        return 0; // boss level: no normal spawns
    }

    public float GetMinSpawnDelay()
    {
        if (difficultyLevel == 1) return minSpawnDelayLevel1;
        if (difficultyLevel == 2) return minSpawnDelayLevel2;
        return 999f;
    }

    public float GetMaxSpawnDelay()
    {
        if (difficultyLevel == 1) return maxSpawnDelayLevel1;
        if (difficultyLevel == 2) return maxSpawnDelayLevel2;
        return 999f;
    }

    public void SetVolume(float volume)
    {
        ApplyVolume(volume);
        PlayerPrefs.SetFloat(VolumePrefsKey, AudioListener.volume);
        PlayerPrefs.Save();
    }

    void ApplyVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    void StyleUi()
    {
        StyleScoreText();
        StyleMainMenu();
        StylePauseMenu();
        StyleGameOverMenu();
        StyleVictoryMenu();
        StyleControlHint();
    }

    void StyleScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        RectTransform rectTransform = scoreText.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(24f, 20f);
            rectTransform.sizeDelta = new Vector2(320f, 52f);
        }

        scoreText.color = Color.white;
        scoreText.fontSize = 36;
        scoreText.alignment = TextAnchor.MiddleLeft;
        scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        scoreText.verticalOverflow = VerticalWrapMode.Overflow;
        AddTextShadow(scoreText, new Color(0f, 0f, 0f, 0.7f), new Vector2(2f, -2f));
    }

    void StyleMainMenu()
    {
        if (menu == null)
        {
            return;
        }

        if (mainLogoImage == null)
        {
            Transform logoTransform = menu.transform.Find("MainLogoImage");
            if (logoTransform != null)
            {
                mainLogoImage = logoTransform.GetComponent<Image>();
            }
        }

        if (mainLogoImage != null)
        {
            StyleMainLogo(mainLogoImage);
            if (mainTitleText != null)
            {
                mainTitleText.gameObject.SetActive(false);
            }
        }
        else
        {
            if (mainTitleText == null)
            {
                Transform titleTransform = menu.transform.Find("MainTitleText");
                if (titleTransform != null)
                {
                    mainTitleText = titleTransform.GetComponent<Text>();
                }
            }

            if (mainTitleText == null)
            {
                mainTitleText = CreateText(menu.transform, "MainTitleText", "GALAXY RAIDERS", 46, MainTitleSize, MainTitlePosition);
            }

            mainTitleText.gameObject.SetActive(true);
            StyleTitle(mainTitleText, 46, MainTitleSize, MainTitlePosition);
        }

        if (startButton != null)
        {
            StyleMainPlayButton(startButton);
        }

        StyleVolumeControl();
    }

    void StylePauseMenu()
    {
        if (pausePanel == null)
        {
            return;
        }

        RectTransform panelRect = pausePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            StretchToParent(panelRect);
        }

        Image overlayImage = pausePanel.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = pausePanel.AddComponent<Image>();
        }

        overlayImage.sprite = null;
        overlayImage.type = Image.Type.Simple;
        overlayImage.color = PauseOverlayColor;
        overlayImage.raycastTarget = true;

        Transform windowTransform = pausePanel.transform.Find("PauseWindow");
        if (windowTransform == null)
        {
            windowTransform = CreateUiObject("PauseWindow", pausePanel.transform).transform;
        }

        RectTransform windowRect = windowTransform.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            SetCenteredRect(windowRect, Vector2.zero, PauseWindowSize);
        }

        Image windowImage = windowTransform.GetComponent<Image>();
        if (windowImage == null)
        {
            windowImage = windowTransform.gameObject.AddComponent<Image>();
        }

        windowImage.sprite = null;
        windowImage.type = Image.Type.Simple;
        windowImage.color = PausePanelColor;
        windowImage.raycastTarget = true;

        StylePauseWindowDecor(windowTransform);
        StylePauseTitle(windowTransform);

        if (continueButton == null)
        {
            Transform continueTransform = windowTransform.Find("ContinueButton");
            if (continueTransform != null)
            {
                continueButton = continueTransform.GetComponent<Button>();
            }
        }

        if (pauseMainMenuButton == null)
        {
            Transform mainMenuTransform = windowTransform.Find("PauseMainMenuButton");
            if (mainMenuTransform != null)
            {
                pauseMainMenuButton = mainMenuTransform.GetComponent<Button>();
            }
        }

        StylePauseButton(continueButton, "CONTINUE", 0f);
        StylePauseButton(pauseMainMenuButton, "MAIN MENU", -70f);
    }

    void StyleGameOverMenu()
    {
        if (gameOverPanel == null)
        {
            return;
        }

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            StretchToParent(panelRect);
        }

        Image overlayImage = gameOverPanel.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = gameOverPanel.AddComponent<Image>();
        }

        overlayImage.sprite = null;
        overlayImage.type = Image.Type.Simple;
        overlayImage.color = PauseOverlayColor;
        overlayImage.raycastTarget = true;

        Transform windowTransform = gameOverPanel.transform.Find("GameOverWindow");
        if (windowTransform == null)
        {
            windowTransform = CreateUiObject("GameOverWindow", gameOverPanel.transform).transform;
        }

        RectTransform windowRect = windowTransform.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            SetCenteredRect(windowRect, Vector2.zero, PauseWindowSize);
        }

        Image windowImage = windowTransform.GetComponent<Image>();
        if (windowImage == null)
        {
            windowImage = windowTransform.gameObject.AddComponent<Image>();
        }

        windowImage.sprite = null;
        windowImage.type = Image.Type.Simple;
        windowImage.color = PausePanelColor;
        windowImage.raycastTarget = true;

        StylePauseWindowDecor(windowTransform);
        StyleGameOverTitle(windowTransform);

        if (reviveButton == null)
        {
            Transform reviveTransform = windowTransform.Find("ReviveButton");
            if (reviveTransform != null)
            {
                reviveButton = reviveTransform.GetComponent<Button>();
            }
        }

        if (gameOverMainMenuButton == null)
        {
            Transform mainMenuTransform = windowTransform.Find("GameOverMainMenuButton");
            if (mainMenuTransform != null)
            {
                gameOverMainMenuButton = mainMenuTransform.GetComponent<Button>();
            }
        }

        StylePauseButton(reviveButton, "REVIVE - " + ReviveCost, 0f);
        StylePauseButton(gameOverMainMenuButton, "MAIN MENU", -70f);
        ApplySciFiButtonState(reviveButton);
    }

    void StyleGameOverTitle(Transform windowTransform)
    {
        if (windowTransform == null)
        {
            return;
        }

        Transform titleTransform = windowTransform.Find("GameOverTitle");
        if (titleTransform == null)
        {
            titleTransform = windowTransform.Find("GAME OVERText");
        }

        Text title = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
        if (title == null)
        {
            title = CreateText(windowTransform, "GameOverTitle", "GAME OVER", 44, PauseTitleSize, PauseTitlePosition);
        }

        RectTransform titleRect = title.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            SetCenteredRect(titleRect, PauseTitlePosition, PauseTitleSize);
        }

        title.gameObject.SetActive(true);
        title.text = "GAME OVER";
        title.fontSize = 46;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        title.color = new Color(0.96f, 0.98f, 1f, 1f);
        AddTextShadow(title, new Color(0f, 0.32f, 0.58f, 0.9f), new Vector2(2f, -2f));
        AddTextOutline(title, new Color(0.45f, 0.86f, 1f, 0.48f), new Vector2(1.2f, -1.2f));
        title.transform.SetAsLastSibling();
    }

    void StyleVictoryMenu()
    {
        if (victoryPanel == null)
        {
            return;
        }

        RectTransform panelRect = victoryPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            StretchToParent(panelRect);
        }

        Image overlayImage = victoryPanel.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = victoryPanel.AddComponent<Image>();
        }

        overlayImage.sprite = null;
        overlayImage.type = Image.Type.Simple;
        overlayImage.color = PauseOverlayColor;
        overlayImage.raycastTarget = true;

        Transform windowTransform = victoryPanel.transform.Find("VictoryWindow");
        if (windowTransform == null)
        {
            windowTransform = CreateUiObject("VictoryWindow", victoryPanel.transform).transform;
        }

        RectTransform windowRect = windowTransform.GetComponent<RectTransform>();
        if (windowRect != null)
        {
            SetCenteredRect(windowRect, Vector2.zero, VictoryWindowSize);
        }

        Image windowImage = windowTransform.GetComponent<Image>();
        if (windowImage == null)
        {
            windowImage = windowTransform.gameObject.AddComponent<Image>();
        }

        windowImage.sprite = null;
        windowImage.type = Image.Type.Simple;
        windowImage.color = PausePanelColor;
        windowImage.raycastTarget = true;

        if (victoryScoreText == null)
        {
            Transform scoreTransform = victoryPanel.transform.Find("VictoryScoreText");
            if (scoreTransform == null)
            {
                scoreTransform = windowTransform.Find("VictoryScoreText");
            }
            if (scoreTransform != null)
            {
                victoryScoreText = scoreTransform.GetComponent<Text>();
            }
        }

        if (previousResultsText == null)
        {
            Transform resultsTransform = victoryPanel.transform.Find("PreviousResultsText");
            if (resultsTransform == null)
            {
                resultsTransform = windowTransform.Find("PreviousResultsText");
            }
            if (resultsTransform != null)
            {
                previousResultsText = resultsTransform.GetComponent<Text>();
            }
        }

        if (victoryMainMenuButton == null)
        {
            Transform mainMenuTransform = victoryPanel.transform.Find("VictoryMainMenuButton");
            if (mainMenuTransform == null)
            {
                mainMenuTransform = windowTransform.Find("VictoryMainMenuButton");
            }
            if (mainMenuTransform != null)
            {
                victoryMainMenuButton = mainMenuTransform.GetComponent<Button>();
            }
        }

        MoveVictoryChildrenIntoWindow(windowTransform);
        StyleVictoryWindowDecor(windowTransform);
        StyleVictoryTitle(windowTransform);
        StyleVictoryScore(windowTransform);
        StyleVictoryResults(windowTransform);

        if (victoryMainMenuButton == null)
        {
            Transform mainMenuTransform = windowTransform.Find("VictoryMainMenuButton");
            if (mainMenuTransform != null)
            {
                victoryMainMenuButton = mainMenuTransform.GetComponent<Button>();
            }
        }

        StylePauseButton(victoryMainMenuButton, "MAIN MENU", VictoryButtonPosition.y);
    }

    void MoveVictoryChildrenIntoWindow(Transform windowTransform)
    {
        if (windowTransform == null)
        {
            return;
        }

        Transform titleTransform = victoryPanel.transform.Find("VICTORYText");
        if (titleTransform != null && titleTransform.parent != windowTransform)
        {
            titleTransform.SetParent(windowTransform, false);
        }

        if (victoryScoreText != null && victoryScoreText.transform.parent != windowTransform)
        {
            victoryScoreText.transform.SetParent(windowTransform, false);
        }

        if (previousResultsText != null && previousResultsText.transform.parent != windowTransform)
        {
            previousResultsText.transform.SetParent(windowTransform, false);
        }

        if (victoryMainMenuButton != null && victoryMainMenuButton.transform.parent != windowTransform)
        {
            victoryMainMenuButton.transform.SetParent(windowTransform, false);
        }
    }

    void StyleVictoryWindowDecor(Transform windowTransform)
    {
        if (windowTransform == null)
        {
            return;
        }

        ConfigureDecorImage(windowTransform, "PanelGlow", Vector2.zero, new Vector2(510f, 420f), new Color(0.12f, 0.56f, 1f, 0.08f), true);
        ConfigureDecorImage(windowTransform, "PanelTopGlow", new Vector2(0f, 190f), new Vector2(430f, 6f), PauseGlowColor, false);
        ConfigureDecorImage(windowTransform, "PanelBottomGlow", new Vector2(0f, -190f), new Vector2(430f, 6f), PauseGlowColor, false);
        ConfigureDecorImage(windowTransform, "PanelTopLine", new Vector2(0f, 189f), new Vector2(432f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelBottomLine", new Vector2(0f, -189f), new Vector2(432f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelLeftLine", new Vector2(-234f, 0f), new Vector2(2f, 305f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelRightLine", new Vector2(234f, 0f), new Vector2(2f, 305f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelTopAccent", new Vector2(0f, 166f), new Vector2(260f, 2f), new Color(0.84f, 0.97f, 1f, 0.42f), false);
        ConfigureDecorImage(windowTransform, "PanelBottomAccent", new Vector2(0f, -166f), new Vector2(260f, 2f), new Color(0.84f, 0.97f, 1f, 0.3f), false);
        ConfigureDecorImage(windowTransform, "PanelLeftCornerTop", new Vector2(-198f, 177f), new Vector2(58f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelRightCornerTop", new Vector2(198f, 177f), new Vector2(58f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelLeftCornerBottom", new Vector2(-198f, -177f), new Vector2(58f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelRightCornerBottom", new Vector2(198f, -177f), new Vector2(58f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "ResultsGlow", VictoryResultsPosition, new Vector2(386f, 168f), new Color(0.1f, 0.46f, 0.85f, 0.07f), false);
        ConfigureDecorImage(windowTransform, "ResultsPanel", VictoryResultsPosition, new Vector2(370f, 154f), new Color(0.01f, 0.04f, 0.09f, 0.42f), false);
        ConfigureDecorImage(windowTransform, "ResultsTopLine", new Vector2(0f, 70f), new Vector2(330f, 1.5f), new Color(0.45f, 0.88f, 1f, 0.55f), false);
        ConfigureDecorImage(windowTransform, "ResultsBottomLine", new Vector2(0f, -71f), new Vector2(330f, 1.5f), new Color(0.45f, 0.88f, 1f, 0.36f), false);
    }

    void StyleVictoryTitle(Transform windowTransform)
    {
        if (windowTransform == null)
        {
            return;
        }

        Transform titleTransform = windowTransform.Find("VictoryTitle");
        if (titleTransform == null)
        {
            titleTransform = windowTransform.Find("VICTORYText");
        }

        Text title = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
        if (title == null)
        {
            title = CreateText(windowTransform, "VictoryTitle", "VICTORY", 48, VictoryTitleSize, VictoryTitlePosition);
        }

        RectTransform titleRect = title.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            SetCenteredRect(titleRect, VictoryTitlePosition, VictoryTitleSize);
        }

        title.gameObject.SetActive(true);
        title.text = "VICTORY";
        title.fontSize = 48;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        title.color = new Color(0.96f, 0.99f, 1f, 1f);
        AddTextShadow(title, new Color(0f, 0.34f, 0.62f, 0.9f), new Vector2(2f, -2f));
        AddTextOutline(title, new Color(0.45f, 0.9f, 1f, 0.5f), new Vector2(1.2f, -1.2f));
        title.transform.SetAsLastSibling();
    }

    void StyleVictoryScore(Transform windowTransform)
    {
        if (victoryScoreText == null && windowTransform != null)
        {
            victoryScoreText = CreateText(windowTransform, "VictoryScoreText", "Score: 0", 24, VictoryScoreSize, VictoryScorePosition);
        }

        if (victoryScoreText == null)
        {
            return;
        }

        RectTransform scoreRect = victoryScoreText.GetComponent<RectTransform>();
        if (scoreRect != null)
        {
            SetCenteredRect(scoreRect, VictoryScorePosition, VictoryScoreSize);
        }

        victoryScoreText.fontSize = 24;
        victoryScoreText.fontStyle = FontStyle.Bold;
        victoryScoreText.alignment = TextAnchor.MiddleCenter;
        victoryScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        victoryScoreText.verticalOverflow = VerticalWrapMode.Overflow;
        victoryScoreText.color = new Color(0.86f, 0.94f, 1f, 0.96f);
        victoryScoreText.raycastTarget = false;
        AddTextShadow(victoryScoreText, new Color(0f, 0.16f, 0.32f, 0.85f), new Vector2(1.5f, -1.5f));
        AddTextOutline(victoryScoreText, new Color(0.34f, 0.74f, 1f, 0.28f), new Vector2(1f, -1f));
        victoryScoreText.transform.SetAsLastSibling();
    }

    void StyleVictoryResults(Transform windowTransform)
    {
        if (previousResultsText == null && windowTransform != null)
        {
            previousResultsText = CreateText(windowTransform, "PreviousResultsText", "Previous results:", 18, VictoryResultsSize, VictoryResultsPosition);
        }

        if (previousResultsText == null)
        {
            return;
        }

        RectTransform resultsRect = previousResultsText.GetComponent<RectTransform>();
        if (resultsRect != null)
        {
            SetCenteredRect(resultsRect, VictoryResultsPosition, VictoryResultsSize);
        }

        previousResultsText.fontSize = 18;
        previousResultsText.fontStyle = FontStyle.Normal;
        previousResultsText.alignment = TextAnchor.MiddleCenter;
        previousResultsText.horizontalOverflow = HorizontalWrapMode.Overflow;
        previousResultsText.verticalOverflow = VerticalWrapMode.Overflow;
        previousResultsText.color = new Color(0.82f, 0.91f, 1f, 0.92f);
        previousResultsText.raycastTarget = false;
        AddTextShadow(previousResultsText, new Color(0f, 0.08f, 0.18f, 0.85f), new Vector2(1f, -1f));
        AddTextOutline(previousResultsText, new Color(0.22f, 0.62f, 0.95f, 0.18f), new Vector2(0.8f, -0.8f));
        previousResultsText.transform.SetAsLastSibling();
    }

    void StylePauseWindowDecor(Transform windowTransform)
    {
        if (windowTransform == null)
        {
            return;
        }

        ConfigureDecorImage(windowTransform, "PanelGlow", Vector2.zero, new Vector2(470f, 330f), new Color(0.12f, 0.56f, 1f, 0.08f), true);
        ConfigureDecorImage(windowTransform, "PanelTopGlow", new Vector2(0f, 145f), new Vector2(390f, 6f), PauseGlowColor, false);
        ConfigureDecorImage(windowTransform, "PanelBottomGlow", new Vector2(0f, -145f), new Vector2(390f, 6f), PauseGlowColor, false);
        ConfigureDecorImage(windowTransform, "PanelTopLine", new Vector2(0f, 144f), new Vector2(392f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelBottomLine", new Vector2(0f, -144f), new Vector2(392f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelLeftLine", new Vector2(-214f, 0f), new Vector2(2f, 232f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelRightLine", new Vector2(214f, 0f), new Vector2(2f, 232f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelTopAccent", new Vector2(0f, 121f), new Vector2(230f, 2f), new Color(0.84f, 0.97f, 1f, 0.42f), false);
        ConfigureDecorImage(windowTransform, "PanelBottomAccent", new Vector2(0f, -121f), new Vector2(230f, 2f), new Color(0.84f, 0.97f, 1f, 0.3f), false);
        ConfigureDecorImage(windowTransform, "PanelLeftCornerTop", new Vector2(-185f, 132f), new Vector2(54f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelRightCornerTop", new Vector2(185f, 132f), new Vector2(54f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelLeftCornerBottom", new Vector2(-185f, -132f), new Vector2(54f, 2f), PauseLineColor, false);
        ConfigureDecorImage(windowTransform, "PanelRightCornerBottom", new Vector2(185f, -132f), new Vector2(54f, 2f), PauseLineColor, false);
    }

    void StylePauseTitle(Transform windowTransform)
    {
        if (windowTransform == null)
        {
            return;
        }

        Transform titleTransform = windowTransform.Find("PauseTitle");
        if (titleTransform == null)
        {
            titleTransform = windowTransform.Find("PAUSEText");
        }

        Text title = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
        if (title == null)
        {
            title = CreateText(windowTransform, "PauseTitle", "MISSION PAUSED", 40, PauseTitleSize, PauseTitlePosition);
        }

        RectTransform titleRect = title.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            SetCenteredRect(titleRect, PauseTitlePosition, PauseTitleSize);
        }

        title.gameObject.SetActive(true);
        title.text = "MISSION PAUSED";
        title.fontSize = 40;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        title.color = PauseTextColor;
        AddTextShadow(title, new Color(0f, 0.35f, 0.62f, 0.9f), new Vector2(2f, -2f));
        AddTextOutline(title, new Color(0.3f, 0.8f, 1f, 0.45f), new Vector2(1.2f, -1.2f));
        title.transform.SetAsLastSibling();
    }

    void StylePauseButton(Button button, string label, float yPosition)
    {
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            SetCenteredRect(buttonRect, new Vector2(0f, yPosition), PauseButtonSize);
        }

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = button.gameObject.AddComponent<Image>();
        }

        buttonImage.sprite = null;
        buttonImage.type = Image.Type.Simple;
        buttonImage.color = new Color(0.02f, 0.09f, 0.22f, 0.96f);
        buttonImage.raycastTarget = true;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.02f, 0.09f, 0.22f, 0.96f);
        colors.highlightedColor = new Color(0.08f, 0.26f, 0.48f, 1f);
        colors.pressedColor = new Color(0.01f, 0.06f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.06f, 0.08f, 0.12f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;

        ConfigureDecorImage(button.transform, "ButtonGlow", Vector2.zero, new Vector2(294f, 70f), new Color(0.12f, 0.55f, 1f, 0.1f), true);
        ConfigureDecorImage(button.transform, "ButtonTopLine", new Vector2(0f, 26f), new Vector2(230f, 2f), PauseLineColor, false);
        ConfigureDecorImage(button.transform, "ButtonBottomLine", new Vector2(0f, -26f), new Vector2(230f, 2f), PauseLineColor, false);
        ConfigureDecorImage(button.transform, "ButtonLeftLine", new Vector2(-134f, 0f), new Vector2(2f, 34f), PauseLineColor, false);
        ConfigureDecorImage(button.transform, "ButtonRightLine", new Vector2(134f, 0f), new Vector2(2f, 34f), PauseLineColor, false);
        ConfigureDecorImage(button.transform, "ButtonInnerShine", new Vector2(0f, 18f), new Vector2(190f, 1.5f), new Color(0.9f, 1f, 1f, 0.48f), false);

        Text text = button.GetComponentInChildren<Text>(true);
        if (text == null)
        {
            text = CreateText(button.transform, "Text", label, 22, Vector2.zero, Vector2.zero);
            StretchToParent(text.rectTransform);
        }

        text.gameObject.SetActive(true);
        text.text = label;
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(0.88f, 0.95f, 1f, 1f);
        text.raycastTarget = false;
        AddTextShadow(text, new Color(0f, 0f, 0f, 0.75f), new Vector2(2f, -2f));
        AddTextOutline(text, new Color(0.36f, 0.76f, 1f, 0.36f), new Vector2(1f, -1f));
        text.transform.SetAsLastSibling();

        ApplySciFiButtonState(button);
    }

    void ApplySciFiButtonState(Button button)
    {
        if (button == null)
        {
            return;
        }

        bool isActive = button.interactable;
        Color backgroundColor = isActive ? new Color(0.02f, 0.09f, 0.22f, 0.96f) : new Color(0.035f, 0.055f, 0.08f, 0.82f);
        Color lineColor = isActive ? PauseLineColor : new Color(0.18f, 0.32f, 0.42f, 0.46f);
        Color glowColor = isActive ? new Color(0.12f, 0.55f, 1f, 0.1f) : new Color(0.06f, 0.14f, 0.2f, 0.025f);
        Color shineColor = isActive ? new Color(0.9f, 1f, 1f, 0.48f) : new Color(0.35f, 0.48f, 0.58f, 0.16f);
        Color textColor = isActive ? new Color(0.88f, 0.95f, 1f, 1f) : new Color(0.48f, 0.58f, 0.66f, 0.84f);

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = backgroundColor;
        }

        SetDecorColor(button.transform, "ButtonGlow", glowColor);
        SetDecorColor(button.transform, "ButtonTopLine", lineColor);
        SetDecorColor(button.transform, "ButtonBottomLine", lineColor);
        SetDecorColor(button.transform, "ButtonLeftLine", lineColor);
        SetDecorColor(button.transform, "ButtonRightLine", lineColor);
        SetDecorColor(button.transform, "ButtonInnerShine", shineColor);

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.color = textColor;
        }
    }

    void SetDecorColor(Transform parent, string objectName, Color color)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = parent.Find(objectName);
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image != null)
        {
            image.color = color;
        }
    }

    void StyleVolumeControl()
    {
        if (volumeSlider == null)
        {
            return;
        }

        RectTransform sliderTransform = volumeSlider.GetComponent<RectTransform>();
        if (sliderTransform != null)
        {
            SetCenteredRect(sliderTransform, VolumeSliderPosition, VolumeSliderSize);
        }

        RectTransform groupTransform = volumeSlider.transform.parent as RectTransform;
        if (groupTransform != null && groupTransform.name == "VolumeControl")
        {
            SetCenteredRect(groupTransform, VolumeGroupPosition, VolumeGroupSize);
        }

        Transform labelTransform = volumeSlider.transform.parent != null ? volumeSlider.transform.parent.Find("VolumeLabel") : null;
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;

        if (label != null)
        {
            StyleTitle(label, 15, VolumeLabelSize, VolumeLabelPosition);
            label.text = "VOLUME";
            label.color = new Color(1f, 1f, 1f, 0.9f);
        }

        Transform backgroundTransform = volumeSlider.transform.Find("Background");
        Image backgroundImage = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.03f, 0.09f, 0.18f, 0.72f);
            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -3f);
            backgroundRect.offsetMax = new Vector2(0f, 3f);
        }

        Transform fillAreaTransform = volumeSlider.transform.Find("Fill Area");
        RectTransform fillAreaRect = fillAreaTransform as RectTransform;
        if (fillAreaRect != null)
        {
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(11f, -3f);
            fillAreaRect.offsetMax = new Vector2(-11f, 3f);
        }

        Image fillImage = volumeSlider.fillRect != null ? volumeSlider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null)
        {
            fillImage.color = new Color(0.52f, 0.86f, 1f, 1f);
            fillImage.raycastTarget = false;
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        Transform handleAreaTransform = volumeSlider.transform.Find("Handle Slide Area");
        RectTransform handleAreaRect = handleAreaTransform as RectTransform;
        if (handleAreaRect != null)
        {
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(11f, 0f);
            handleAreaRect.offsetMax = new Vector2(-11f, 0f);
        }

        Image handleImage = volumeSlider.handleRect != null ? volumeSlider.handleRect.GetComponent<Image>() : null;
        if (handleImage != null)
        {
            handleImage.color = new Color(1f, 1f, 1f, 0f);
            handleImage.raycastTarget = true;
            handleImage.rectTransform.sizeDelta = new Vector2(22f, 22f);
        }

        StyleSliderHandle(volumeSlider);
    }

    void StyleControlHint()
    {
        if (controlHintText == null)
        {
            return;
        }

        RectTransform rectTransform = controlHintText.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetTopCenterRect(rectTransform, ControlHintPosition, ControlHintSize);
        }

        controlHintText.text = "Press ESC to pause";
        controlHintText.color = new Color(1f, 1f, 1f, 0.92f);
        controlHintText.fontSize = 22;
        controlHintText.alignment = TextAnchor.MiddleCenter;
        AddTextShadow(controlHintText, new Color(0f, 0f, 0f, 0.75f), new Vector2(2f, -2f));
        controlHintText.gameObject.SetActive(false);
    }

    void StyleSliderHandle(Slider slider)
    {
        if (slider == null || slider.handleRect == null)
        {
            return;
        }

        Text starText = slider.handleRect.GetComponentInChildren<Text>();
        if (starText == null)
        {
            starText = CreateText(slider.handleRect, "Star", "★", 22, Vector2.zero, Vector2.zero);
            StretchToParent(starText.rectTransform);
        }

        starText.text = "★";
        starText.fontSize = 22;
        starText.alignment = TextAnchor.MiddleCenter;
        starText.color = new Color(0.9f, 0.96f, 1f, 1f);
        starText.raycastTarget = false;
        AddTextShadow(starText, new Color(0f, 0f, 0f, 0.3f), new Vector2(1f, -1f));
        slider.targetGraphic = starText;
    }

    void StyleOverlay(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.color = DialogOverlayColor;
        }
    }

    void StyleTitle(Text text, int fontSize, Vector2 size, Vector2 position)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetCenteredRect(rectTransform, position, size);
        }

        text.color = Color.white;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        AddTextShadow(text, new Color(0f, 0f, 0f, 0.65f), new Vector2(2f, -2f));
    }

    void StyleMainLogo(Image logoImage)
    {
        if (logoImage == null)
        {
            return;
        }

        RectTransform rectTransform = logoImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetTopCenterRect(rectTransform, MainLogoPosition, MainLogoSize);
        }

        logoImage.color = Color.white;
        logoImage.type = Image.Type.Simple;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
    }

    void StyleMainPlayButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetCenteredRect(rectTransform, MainPlayButtonPosition, MainPlayButtonSize);
            rectTransform.localScale = Vector3.one;
        }

        Image hitboxImage = button.GetComponent<Image>();
        if (hitboxImage == null)
        {
            hitboxImage = button.gameObject.AddComponent<Image>();
        }

        Transform imageTransform = button.transform.Find("ButtonImage");
        Image image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
        if (image == null)
        {
            image = hitboxImage;
        }

        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        RectTransform imageTransformRect = image.GetComponent<RectTransform>();
        if (imageTransformRect != null && image.transform != button.transform)
        {
            SetCenteredRect(imageTransformRect, Vector2.zero, MainPlayButtonImageSize);
        }

        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = image == hitboxImage;

        if (image != hitboxImage)
        {
            hitboxImage.type = Image.Type.Simple;
            hitboxImage.sprite = null;
            hitboxImage.color = new Color(1f, 1f, 1f, 0f);
            hitboxImage.preserveAspect = true;
            hitboxImage.raycastTarget = true;
        }

        button.transition = Selectable.Transition.SpriteSwap;
        button.targetGraphic = image;

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.gameObject.SetActive(false);
        }

        if (button.GetComponent<ButtonPressScaleEffect>() == null)
        {
            button.gameObject.AddComponent<ButtonPressScaleEffect>();
        }
    }

    void StyleButton(Button button, Vector2 size, int fontSize)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = ButtonNormalColor;

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonNormalColor;
        colors.highlightedColor = ButtonHighlightedColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonHighlightedColor;
        colors.disabledColor = new Color(0.15f, 0.17f, 0.2f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.color = Color.white;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            AddTextShadow(text, new Color(0f, 0f, 0f, 0.45f), new Vector2(1f, -1f));
        }
    }

    void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
        }
    }

    void AddTextShadow(Text text, Color color, Vector2 distance)
    {
        if (text == null)
        {
            return;
        }

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    void AddTextOutline(Text text, Color color, Vector2 distance)
    {
        if (text == null)
        {
            return;
        }

        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    Image ConfigureDecorImage(Transform parent, string objectName, Vector2 position, Vector2 size, Color color, bool sendToBack)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(objectName);
        Image image;
        if (existing == null)
        {
            image = CreateImage(parent, objectName, color);
        }
        else
        {
            image = existing.GetComponent<Image>();
            if (image == null)
            {
                image = existing.gameObject.AddComponent<Image>();
            }
        }

        RectTransform rectTransform = image.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetCenteredRect(rectTransform, position, size);
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;

        if (sendToBack)
        {
            image.transform.SetAsFirstSibling();
        }

        return image;
    }

    void SetCenteredRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    void SetTopCenterRect(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    void BindUiEvents()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (pauseMainMenuButton != null)
        {
            pauseMainMenuButton.onClick.AddListener(OpenMainMenu);
        }

        if (reviveButton != null)
        {
            reviveButton.onClick.AddListener(Revive);
        }

        if (gameOverMainMenuButton != null)
        {
            gameOverMainMenuButton.onClick.AddListener(OpenMainMenu);
        }

        if (victoryMainMenuButton != null)
        {
            victoryMainMenuButton.onClick.AddListener(OpenMainMenu);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    void SetUiState(GameUiState state)
    {
        currentState = state;

        if (menu != null)
        {
            menu.SetActive(state == GameUiState.MainMenu);
        }

        SetPanelActive(pausePanel, state == GameUiState.Paused);
        SetPanelActive(gameOverPanel, state == GameUiState.GameOver);
        SetPanelActive(victoryPanel, state == GameUiState.Victory);

        if (playerHpSlider != null)
        {
            playerHpSlider.gameObject.SetActive(state == GameUiState.Playing || state == GameUiState.Paused || state == GameUiState.GameOver);
        }

        if (state != GameUiState.Playing)
        {
            HideControlHint();
        }
    }

    void ShowControlHint()
    {
        if (controlHintText == null)
        {
            return;
        }

        if (controlHintCoroutine != null)
        {
            StopCoroutine(controlHintCoroutine);
        }

        SetControlHintVisible(true);
        controlHintCoroutine = StartCoroutine(HideControlHintAfterDelay());
    }

    IEnumerator HideControlHintAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ControlHintDuration);
        controlHintCoroutine = null;
        SetControlHintVisible(false);
    }

    void HideControlHint()
    {
        if (controlHintCoroutine != null)
        {
            StopCoroutine(controlHintCoroutine);
            controlHintCoroutine = null;
        }

        SetControlHintVisible(false);
    }

    void SetControlHintVisible(bool isVisible)
    {
        if (controlHintText != null)
        {
            controlHintText.gameObject.SetActive(isVisible);
        }
    }

    void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    void UpdateReviveButton()
    {
        if (reviveButton != null)
        {
            reviveButton.interactable = score >= ReviveCost && playerForRevive != null;
            ApplySciFiButtonState(reviveButton);
        }
    }

    void SaveResult(int result)
    {
        List<int> results = LoadResults();
        results.Add(result);
        results.Sort((left, right) => right.CompareTo(left));

        TrimStoredResults(results);

        string[] values = new string[results.Count];
        for (int i = 0; i < results.Count; i++)
        {
            values[i] = results[i].ToString();
        }

        PlayerPrefs.SetString(ResultsPrefsKey, string.Join(",", values));
        PlayerPrefs.Save();
    }

    List<int> LoadResults()
    {
        List<int> results = new List<int>();
        string rawResults = PlayerPrefs.GetString(ResultsPrefsKey, string.Empty);

        if (!string.IsNullOrEmpty(rawResults))
        {
            string[] values = rawResults.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                int parsedResult;
                if (int.TryParse(values[i], out parsedResult))
                {
                    results.Add(parsedResult);
                }
            }
        }

        results.Sort((left, right) => right.CompareTo(left));
        TrimStoredResults(results);
        return results;
    }

    string FormatResults(List<int> results)
    {
        if (results.Count == 0)
        {
            return "Previous results:\nNo results yet";
        }

        StringBuilder builder = new StringBuilder("Previous results:");
        int visibleResults = Mathf.Min(results.Count, MaxStoredResults);
        for (int i = 0; i < visibleResults; i++)
        {
            builder.AppendLine();
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(results[i]);
        }

        return builder.ToString();
    }

    void TrimStoredResults(List<int> results)
    {
        if (results.Count > MaxStoredResults)
        {
            results.RemoveRange(MaxStoredResults, results.Count - MaxStoredResults);
        }
    }

    void ClearDynamicObjects()
    {
        ClearObjectsWithTag("Asteroid");
        ClearObjectsWithTag("Enemy");
        ClearObjectsWithTag("PowerUp");
        ClearObjectsWithTag("LazerShot");
        ClearObjectsWithTag("LazerEnemyShot");
    }

    void ClearObjectsWithTag(string tagName)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tagName);
        for (int i = 0; i < objects.Length; i++)
        {
            Destroy(objects[i]);
        }
    }

    void ResetPlayer()
    {
        PLayerScript player = null;
        if (playerForRevive != null)
        {
            player = playerForRevive.GetComponent<PLayerScript>();
        }

        if (player == null)
        {
            player = FindObjectOfType<PLayerScript>();
        }

        if (player != null)
        {
            player.hpSlider = playerHpSlider;
            player.ResetPlayer();
            playerForRevive = player.gameObject;
        }
    }

    void CreateMissingUi()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            return;
        }

        if (levelBannerText == null)
        {
            Transform existing = canvas.transform.Find("LevelBannerText");
            if (existing != null)
            {
                levelBannerText = existing.GetComponent<Text>();
            }
        }

        if (levelBannerText == null)
        {
            levelBannerText = CreateText(canvas.transform, "LevelBannerText", "", 54, new Vector2(680f, 90f), Vector2.zero);
            RectTransform rt = levelBannerText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            levelBannerText.gameObject.SetActive(false);
            AddTextShadow(levelBannerText, new Color(0f, 0f, 0f, 0.75f), new Vector2(3f, -3f));
        }

        if (volumeSlider == null && menu != null)
        {
            Transform existingSlider = menu.transform.Find("VolumeControl/VolumeSlider");
            if (existingSlider != null)
            {
                volumeSlider = existingSlider.GetComponent<Slider>();
            }
        }

        if (volumeSlider == null && menu != null)
        {
            volumeSlider = CreateVolumeControl(menu.transform);
        }

        if (controlHintText == null)
        {
            Transform hintTransform = canvas.transform.Find("ControlHintText");
            if (hintTransform != null)
            {
                controlHintText = hintTransform.GetComponent<Text>();
            }
        }

        if (controlHintText == null)
        {
            controlHintText = CreateControlHint(canvas.transform);
        }

        if (pausePanel == null)
        {
            pausePanel = CreatePanel(canvas.transform, "PausePanel");
            GameObject pauseWindow = CreateDialogWindow(pausePanel.transform, "PauseWindow");
            CreateTitle(pauseWindow.transform, "PAUSE", 72f);
            continueButton = CreateButton(pauseWindow.transform, "ContinueButton", "CONTINUE", 4f);
            pauseMainMenuButton = CreateButton(pauseWindow.transform, "PauseMainMenuButton", "MAIN MENU", -66f);
        }

        if (gameOverPanel == null)
        {
            gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel");
            GameObject gameOverWindow = CreateDialogWindow(gameOverPanel.transform, "GameOverWindow");
            CreateTitle(gameOverWindow.transform, "GAME OVER", 72f);
            reviveButton = CreateButton(gameOverWindow.transform, "ReviveButton", "REVIVE - 10", 4f);
            gameOverMainMenuButton = CreateButton(gameOverWindow.transform, "GameOverMainMenuButton", "MAIN MENU", -66f);
        }

        if (victoryPanel == null)
        {
            victoryPanel = CreatePanel(canvas.transform, "VictoryPanel");
            GameObject victoryWindow = CreateDialogWindow(victoryPanel.transform, "VictoryWindow");
            CreateTitle(victoryWindow.transform, "VICTORY", VictoryTitlePosition.y);
            victoryScoreText = CreateText(victoryWindow.transform, "VictoryScoreText", "Score: 0", 24, VictoryScoreSize, VictoryScorePosition);
            previousResultsText = CreateText(victoryWindow.transform, "PreviousResultsText", "Previous results:", 18, VictoryResultsSize, VictoryResultsPosition);
            victoryMainMenuButton = CreateButton(victoryWindow.transform, "VictoryMainMenuButton", "MAIN MENU", VictoryButtonPosition.y);
        }

        if (playerHpSlider == null)
        {
            playerHpSlider = CreatePlayerHpBar(canvas.transform);
        }
    }

    Slider CreatePlayerHpBar(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("PlayerHpSlider", parent);
        RectTransform sliderTransform = sliderObject.GetComponent<RectTransform>();

        sliderTransform.anchorMin = new Vector2(0f, 0f);
        sliderTransform.anchorMax = new Vector2(0f, 0f);
        sliderTransform.pivot = new Vector2(0f, 0f);
        sliderTransform.anchoredPosition = new Vector2(24f, 82f);
        sliderTransform.sizeDelta = new Vector2(200f, 12f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.wholeNumbers = true;
        slider.interactable = false;

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.05f, 0.05f, 0.05f, 0.75f));
        StretchToParent(background.rectTransform);

        RectTransform fillArea = CreateUiObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        StretchToParent(fillArea);
        fillArea.offsetMax = new Vector2(-1f, -1f);
        fillArea.offsetMin = new Vector2(1f, 1f);

        Image fill = CreateImage(fillArea, "Fill", new Color(0.85f, 0.15f, 0.15f, 0.95f));
        StretchToParent(fill.rectTransform);

        slider.fillRect = fill.rectTransform;

        Text label = CreateText(sliderObject.transform, "HpLabel", "HULL INTEGRITY", 14, new Vector2(160f, 20f), new Vector2(0f, 16f));
        label.alignment = TextAnchor.LowerLeft;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        RectTransform labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(0f, 1f);
        labelRt.pivot = new Vector2(0f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 2f);
        AddTextShadow(label, new Color(0f, 0f, 0f, 0.5f), new Vector2(1f, -1f));

        return slider;
    }

    Canvas FindCanvas()
    {
        if (menu != null)
        {
            Canvas parentCanvas = menu.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                return parentCanvas;
            }
        }

        return FindObjectOfType<Canvas>();
    }

    Slider CreateVolumeControl(Transform parent)
    {
        GameObject group = CreateUiObject("VolumeControl", parent);
        RectTransform groupTransform = group.GetComponent<RectTransform>();
        SetCenteredRect(groupTransform, VolumeGroupPosition, VolumeGroupSize);

        Text label = CreateText(group.transform, "VolumeLabel", "VOLUME", 15, VolumeLabelSize, VolumeLabelPosition);
        label.color = new Color(1f, 1f, 1f, 0.9f);

        GameObject sliderObject = CreateUiObject("VolumeSlider", group.transform);
        RectTransform sliderTransform = sliderObject.GetComponent<RectTransform>();
        SetCenteredRect(sliderTransform, VolumeSliderPosition, VolumeSliderSize);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.03f, 0.09f, 0.18f, 0.72f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.offsetMin = new Vector2(0f, -3f);
        backgroundRect.offsetMax = new Vector2(0f, 3f);

        RectTransform fillArea = CreateUiObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(11f, -3f);
        fillArea.offsetMax = new Vector2(-11f, 3f);

        Image fill = CreateImage(fillArea, "Fill", new Color(0.52f, 0.86f, 1f, 1f));
        fill.raycastTarget = false;
        StretchToParent(fill.rectTransform);

        RectTransform handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform).GetComponent<RectTransform>();
        StretchToParent(handleArea);
        handleArea.offsetMin = new Vector2(11f, 0f);
        handleArea.offsetMax = new Vector2(-11f, 0f);

        Image handle = CreateImage(handleArea, "Handle", new Color(1f, 1f, 1f, 0f));
        handle.rectTransform.sizeDelta = new Vector2(22f, 22f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        StyleSliderHandle(slider);

        return slider;
    }

    GameObject CreatePanel(Transform parent, string panelName)
    {
        GameObject panel = CreateUiObject(panelName, parent);
        StretchToParent(panel.GetComponent<RectTransform>());
        Image image = panel.AddComponent<Image>();
        image.color = DialogOverlayColor;
        panel.SetActive(false);
        return panel;
    }

    GameObject CreateDialogWindow(Transform parent, string objectName)
    {
        GameObject window = CreateUiObject(objectName, parent);
        RectTransform rectTransform = window.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(390f, 275f);

        Image image = window.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = DialogBackgroundColor;

        return window;
    }

    Text CreateTitle(Transform parent, string title, float yPosition)
    {
        Text titleText = CreateText(parent, title + "Text", title, 36, new Vector2(360f, 58f), new Vector2(0f, yPosition));
        StyleTitle(titleText, 36, new Vector2(360f, 58f), new Vector2(0f, yPosition));
        return titleText;
    }

    Text CreateControlHint(Transform parent)
    {
        Text hintText = CreateText(parent, "ControlHintText", "Press ESC to pause", 22, ControlHintSize, ControlHintPosition);
        SetTopCenterRect(hintText.GetComponent<RectTransform>(), ControlHintPosition, ControlHintSize);
        hintText.gameObject.SetActive(false);
        return hintText;
    }

    Button CreateButton(Transform parent, string objectName, string label, float yPosition)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(buttonTransform, new Vector2(0f, yPosition), PlayButtonSize);

        Image image = buttonObject.AddComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text buttonText = CreateText(buttonObject.transform, "Text", label, 26, Vector2.zero, Vector2.zero);
        StretchToParent(buttonText.rectTransform);
        StyleButton(button, PlayButtonSize, 26);

        return button;
    }

    Text CreateText(Transform parent, string objectName, string text, int fontSize, Vector2 size, Vector2 position)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = position;
        textTransform.sizeDelta = size;

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;
        textComponent.text = text;

        return textComponent;
    }

    Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = CreateUiObject(objectName, parent);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.layer = LayerMask.NameToLayer("UI");
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
