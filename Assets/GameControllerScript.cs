using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameControllerScript : MonoBehaviour
{
    const int ReviveCost = 10;
    const int MaxStoredResults = 10;
    const float ControlHintDuration = 5f;
    const string VolumePrefsKey = "GalaxyRaiders.Volume";
    const string ResultsPrefsKey = "GalaxyRaiders.Results";
    static readonly Color DialogBackgroundColor = new Color(0.05f, 0.08f, 0.14f, 0.94f);
    static readonly Color DialogOverlayColor = new Color(0f, 0f, 0f, 0.78f);
    static readonly Color ButtonNormalColor = new Color(0.13f, 0.2f, 0.33f, 0.96f);
    static readonly Color ButtonHighlightedColor = new Color(0.22f, 0.34f, 0.55f, 1f);
    static readonly Color ButtonPressedColor = new Color(0.08f, 0.14f, 0.24f, 1f);
    static readonly Vector2 MainTitlePosition = new Vector2(0f, 165f);
    static readonly Vector2 MainTitleSize = new Vector2(560f, 80f);
    static readonly Vector2 PlayButtonPosition = new Vector2(0f, -145f);
    static readonly Vector2 PlayButtonSize = new Vector2(300f, 58f);
    static readonly Vector2 VolumeGroupPosition = new Vector2(0f, -225f);
    static readonly Vector2 VolumeGroupSize = new Vector2(300f, 70f);
    static readonly Vector2 VolumeLabelPosition = new Vector2(0f, 15f);
    static readonly Vector2 VolumeLabelSize = new Vector2(300f, 22f);
    static readonly Vector2 VolumeSliderPosition = new Vector2(0f, -15f);
    static readonly Vector2 VolumeSliderSize = new Vector2(270f, 26f);
    static readonly Vector2 ControlHintPosition = new Vector2(0f, -84f);
    static readonly Vector2 ControlHintSize = new Vector2(420f, 42f);

    public Text scoreText;
    public Button startButton;
    public GameObject menu;
    public Slider hpSlider;

    [Header("Main Menu")]
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
        SetUiState(GameUiState.MainMenu);
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
        SetUiState(GameUiState.Playing);
        ShowControlHint();

        // Назначаем HP бар игроку
        PLayerScript player = FindObjectOfType<PLayerScript>();
        if (player != null)
        {
            player.hpBar = hpSlider;
            player.ResetPlayer();
        }
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

        if (previousResultsText != null)
        {
            previousResultsText.text = FormatResults(LoadResults());
        }

        if (!resultSavedThisRound)
        {
            SaveResult(score);
            resultSavedThisRound = true;
        }

        SetUiState(GameUiState.Victory);
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        isStarted = false;
        resultSavedThisRound = false;
        score = 0;
        UpdateScoreText();
        ClearDynamicObjects();
        ResetPlayer();
        SetUiState(GameUiState.MainMenu);
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
        StyleHpBar();
        StyleMainMenu();
        StyleOverlay(pausePanel);
        StyleOverlay(gameOverPanel);
        StyleButton(continueButton, new Vector2(300f, 58f), 26);
        StyleButton(pauseMainMenuButton, new Vector2(300f, 58f), 26);
        StyleButton(reviveButton, new Vector2(300f, 58f), 26);
        StyleButton(gameOverMainMenuButton, new Vector2(300f, 58f), 26);
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

    void StyleHpBar()
    {
        if (hpSlider == null)
        {
            return;
        }

        RectTransform rectTransform = hpSlider.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-24f, 20f);
            rectTransform.sizeDelta = new Vector2(200f, 30f);
        }

        Image backgroundImage = hpSlider.transform.Find("Background")?.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }

        Image fillImage = hpSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = Color.red;
        }

        // Hide handle for HP bar
        if (hpSlider.handleRect != null)
        {
            hpSlider.handleRect.gameObject.SetActive(false);
        }

        hpSlider.interactable = false;
        hpSlider.transition = Selectable.Transition.None;
    }

    void StyleMainMenu()
    {
        if (menu == null)
        {
            return;
        }

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

        StyleTitle(mainTitleText, 46, MainTitleSize, MainTitlePosition);

        if (startButton != null)
        {
            SetCenteredRect(startButton.GetComponent<RectTransform>(), PlayButtonPosition, PlayButtonSize);
            StyleButton(startButton, PlayButtonSize, 28);
            SetButtonLabel(startButton, "PLAY");
        }

        StyleVolumeControl();
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
            StyleTitle(label, 16, VolumeLabelSize, VolumeLabelPosition);
        }

        Transform backgroundTransform = volumeSlider.transform.Find("Background");
        Image backgroundImage = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.08f, 0.12f, 0.2f, 0.95f);
            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -4f);
            backgroundRect.offsetMax = new Vector2(0f, 4f);
        }

        Image fillImage = volumeSlider.fillRect != null ? volumeSlider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null)
        {
            fillImage.color = new Color(0.45f, 0.75f, 1f, 1f);
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        Image handleImage = volumeSlider.handleRect != null ? volumeSlider.handleRect.GetComponent<Image>() : null;
        if (handleImage != null)
        {
            handleImage.color = new Color(1f, 1f, 1f, 0.08f);
            handleImage.rectTransform.sizeDelta = new Vector2(28f, 28f);
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
            starText = CreateText(slider.handleRect, "Star", "★", 26, Vector2.zero, Vector2.zero);
            StretchToParent(starText.rectTransform);
        }

        starText.text = "★";
        starText.fontSize = 26;
        starText.alignment = TextAnchor.MiddleCenter;
        starText.color = new Color(0.88f, 0.9f, 0.94f, 1f);
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

        if (hpSlider != null)
        {
            hpSlider.gameObject.SetActive(state == GameUiState.Playing || state == GameUiState.Paused);
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
        }
    }

    void SaveResult(int result)
    {
        List<int> results = LoadResults();
        results.Add(result);
        results.Sort((left, right) => right.CompareTo(left));

        if (results.Count > MaxStoredResults)
        {
            results.RemoveRange(MaxStoredResults, results.Count - MaxStoredResults);
        }

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
        return results;
    }

    string FormatResults(List<int> results)
    {
        if (results.Count == 0)
        {
            return "Previous results:\nNo results yet";
        }

        StringBuilder builder = new StringBuilder("Previous results:");
        for (int i = 0; i < results.Count; i++)
        {
            builder.AppendLine();
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(results[i]);
        }

        return builder.ToString();
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

        if (hpSlider == null)
        {
            hpSlider = CreateHpBar(canvas.transform);
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
            CreateTitle(victoryPanel.transform, "VICTORY", 175f);
            victoryScoreText = CreateText(victoryPanel.transform, "VictoryScoreText", "Score: 0", 32, new Vector2(420f, 60f), new Vector2(0f, 105f));
            previousResultsText = CreateText(victoryPanel.transform, "PreviousResultsText", "Previous results:", 24, new Vector2(420f, 180f), new Vector2(0f, -15f));
            victoryMainMenuButton = CreateButton(victoryPanel.transform, "VictoryMainMenuButton", "MAIN MENU", -180f);
        }
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

        CreateText(group.transform, "VolumeLabel", "VOLUME", 16, VolumeLabelSize, VolumeLabelPosition);

        GameObject sliderObject = CreateUiObject("VolumeSlider", group.transform);
        RectTransform sliderTransform = sliderObject.GetComponent<RectTransform>();
        SetCenteredRect(sliderTransform, VolumeSliderPosition, VolumeSliderSize);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.08f, 0.12f, 0.2f, 0.95f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.offsetMin = new Vector2(0f, -4f);
        backgroundRect.offsetMax = new Vector2(0f, 4f);

        RectTransform fillArea = CreateUiObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(10f, 0f);
        fillArea.offsetMax = new Vector2(-10f, 0f);
        fillArea.sizeDelta = new Vector2(-20f, 8f);

        Image fill = CreateImage(fillArea, "Fill", new Color(0.45f, 0.72f, 1f, 1f));
        StretchToParent(fill.rectTransform);

        RectTransform handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform).GetComponent<RectTransform>();
        StretchToParent(handleArea);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);

        Image handle = CreateImage(handleArea, "Handle", Color.white);
        handle.rectTransform.sizeDelta = new Vector2(28f, 28f);

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

    Slider CreateHpBar(Transform parent)
    {
        GameObject sliderObject = CreateUiObject("HpBar", parent);
        RectTransform sliderTransform = sliderObject.GetComponent<RectTransform>();

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.1f, 0.1f, 0.1f, 0.8f));
        StretchToParent(background.rectTransform);

        RectTransform fillArea = CreateUiObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        StretchToParent(fillArea);
        fillArea.offsetMin = new Vector2(2f, 2f);
        fillArea.offsetMax = new Vector2(-2f, -2f);

        Image fill = CreateImage(fillArea, "Fill", Color.red);
        StretchToParent(fill.rectTransform);

        slider.fillRect = fill.rectTransform;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        sliderObject.SetActive(false);

        return slider;
    }
}
