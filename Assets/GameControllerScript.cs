using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameControllerScript : MonoBehaviour
{
    public Text scoreText;
    public Text levelText;
    public Button startButton;
    public GameObject menu;

    int score = 0;
    int destroyedEnemies = 0;
    int currentLevel = 1;

    public bool isStarted = false;
    public bool canSpawnEnemies = false;
    public bool bossWasSpawned = false;
    public bool isBossLevel = false;

    public static GameControllerScript instance;

    private void Start()
    {
        instance = this;
        PrepareLevelText();
        startButton.onClick.AddListener(
            delegate
            {
                menu.SetActive(false);
                StartCoroutine(StartLevel(1, "РЈСЂРѕРІРµРЅСЊ 1"));
            }
            );
    }

    public int CurrentLevel
    {
        get { return currentLevel; }
    }

    public void increaseScore(int increment)
    {
        score += increment;
        scoreText.text = "Score: " + score;
    }

    public void EnemyDestroyed(bool isBoss)
    {
        increaseScore(isBoss ? 20 : 1);

        if (isBoss)
        {
            StartCoroutine(ShowFinalMessage());
            return;
        }

        destroyedEnemies++;

        if (destroyedEnemies == 10)
        {
            StartCoroutine(StartLevel(2, "РЈСЂРѕРІРµРЅСЊ 2"));
        }
        else if (destroyedEnemies == 20)
        {
            StartCoroutine(StartBossLevel());
        }
    }

    public float GetEnemySpeedMultiplier()
    {
        if (currentLevel >= 2)
            return 1.45f;

        return 1f;
    }

    public int GetEnemyDamage()
    {
        if (currentLevel >= 2)
            return 2;

        return 1;
    }

    public int GetEnemySpawnCount()
    {
        if (currentLevel >= 2)
            return 2;

        return 1;
    }

    public void MarkBossSpawned()
    {
        bossWasSpawned = true;
    }

    IEnumerator StartLevel(int level, string message)
    {
        isStarted = true;
        canSpawnEnemies = false;
        isBossLevel = false;
        currentLevel = level;
        ShowLevelText(message);
        yield return new WaitForSeconds(2f);
        HideLevelText();
        canSpawnEnemies = true;
    }

    IEnumerator StartBossLevel()
    {
        canSpawnEnemies = false;
        isBossLevel = true;
        currentLevel = 3;
        ShowLevelText("Р¤РёРЅР°Р»СЊРЅС‹Р№ Р±РѕСЃСЃ");
        yield return new WaitForSeconds(2.5f);
        HideLevelText();
    }

    IEnumerator ShowFinalMessage()
    {
        canSpawnEnemies = false;
        isStarted = false;
        ShowLevelText("РџРѕР±РµРґР°!");
        yield return new WaitForSeconds(3f);
    }

    void PrepareLevelText()
    {
        if (levelText == null && scoreText != null)
        {
            GameObject levelTextObject = new GameObject("LevelText");
            levelTextObject.transform.SetParent(scoreText.canvas.transform, false);

            RectTransform rectTransform = levelTextObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(600f, 120f);

            levelText = levelTextObject.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.fontSize = 56;
            levelText.color = Color.white;
            levelText.raycastTarget = false;
        }

        HideLevelText();
    }

    void ShowLevelText(string message)
    {
        if (levelText == null)
            return;

        levelText.text = message;
        levelText.gameObject.SetActive(true);
    }

    void HideLevelText()
    {
        if (levelText != null)
            levelText.gameObject.SetActive(false);
    }
}
