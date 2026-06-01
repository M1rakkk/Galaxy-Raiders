using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameControllerScript : MonoBehaviour
{
    const int ReviveCost = 10;
    const int MaxStoredResults = 10;
    const string VolumePrefsKey = "GalaxyRaiders.Volume";
    const string ResultsPrefsKey = "GalaxyRaiders.Results";

    public Text scoreText;
    public Button startButton;
    public GameObject menu;

    [Header("Main Menu")]
    public Slider volumeSlider;

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

        if (volumeSlider == null && menu != null)
        {
            volumeSlider = CreateVolumeControl(menu.transform);
        }

        if (pausePanel == null)
        {
            pausePanel = CreatePanel(canvas.transform, "PausePanel");
            CreateTitle(pausePanel.transform, "PAUSE", 120f);
            continueButton = CreateButton(pausePanel.transform, "ContinueButton", "CONTINUE", 25f);
            pauseMainMenuButton = CreateButton(pausePanel.transform, "PauseMainMenuButton", "MAIN MENU", -55f);
        }

        if (gameOverPanel == null)
        {
            gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel");
            CreateTitle(gameOverPanel.transform, "GAME OVER", 130f);
            reviveButton = CreateButton(gameOverPanel.transform, "ReviveButton", "REVIVE - 10", 25f);
            gameOverMainMenuButton = CreateButton(gameOverPanel.transform, "GameOverMainMenuButton", "MAIN MENU", -55f);
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
        groupTransform.anchorMin = new Vector2(0.5f, 0.5f);
        groupTransform.anchorMax = new Vector2(0.5f, 0.5f);
        groupTransform.anchoredPosition = new Vector2(0f, -155f);
        groupTransform.sizeDelta = new Vector2(360f, 85f);

        CreateText(group.transform, "VolumeLabel", "VOLUME", 24, new Vector2(360f, 35f), new Vector2(0f, 22f));

        GameObject sliderObject = CreateUiObject("VolumeSlider", group.transform);
        RectTransform sliderTransform = sliderObject.GetComponent<RectTransform>();
        sliderTransform.anchorMin = new Vector2(0.5f, 0.5f);
        sliderTransform.anchorMax = new Vector2(0.5f, 0.5f);
        sliderTransform.anchoredPosition = new Vector2(0f, -20f);
        sliderTransform.sizeDelta = new Vector2(300f, 24f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.1f, 0.12f, 0.18f, 0.85f));
        StretchToParent(background.rectTransform);

        RectTransform fillArea = CreateUiObject("Fill Area", sliderObject.transform).GetComponent<RectTransform>();
        fillArea.anchorMin = new Vector2(0f, 0.25f);
        fillArea.anchorMax = new Vector2(1f, 0.75f);
        fillArea.offsetMin = new Vector2(10f, 0f);
        fillArea.offsetMax = new Vector2(-10f, 0f);

        Image fill = CreateImage(fillArea, "Fill", new Color(0.45f, 0.72f, 1f, 1f));
        StretchToParent(fill.rectTransform);

        RectTransform handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform).GetComponent<RectTransform>();
        StretchToParent(handleArea);
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);

        Image handle = CreateImage(handleArea, "Handle", Color.white);
        handle.rectTransform.sizeDelta = new Vector2(22f, 32f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;

        return slider;
    }

    GameObject CreatePanel(Transform parent, string panelName)
    {
        GameObject panel = CreateUiObject(panelName, parent);
        StretchToParent(panel.GetComponent<RectTransform>());
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);
        panel.SetActive(false);
        return panel;
    }

    Text CreateTitle(Transform parent, string title, float yPosition)
    {
        return CreateText(parent, title + "Text", title, 42, new Vector2(420f, 70f), new Vector2(0f, yPosition));
    }

    Button CreateButton(Transform parent, string objectName, string label, float yPosition)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonTransform.anchoredPosition = new Vector2(0f, yPosition);
        buttonTransform.sizeDelta = new Vector2(300f, 60f);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.82f, 0.86f, 1f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text buttonText = CreateText(buttonObject.transform, "Text", label, 30, Vector2.zero, Vector2.zero);
        StretchToParent(buttonText.rectTransform);
        buttonText.color = new Color(0.3f, 0.3f, 0.3f, 1f);

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
