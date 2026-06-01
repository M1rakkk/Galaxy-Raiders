using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControllerScript : MonoBehaviour
{
    public UnityEngine.UI.Text scoreText;
    public UnityEngine.UI.Button startButton;
    public GameObject menu;

    int score = 0;

    public bool isStarted = false;

    public static GameControllerScript instance;

    private void Start()
    {
        instance = this;
        startButton.onClick.AddListener(
            delegate
            {
                menu.SetActive(false);
                isStarted = true;
            }
            );
    }

    public void increaseScore(int increment)
    {
        score += increment;
        scoreText.text = "Score: " + score;
    }
}
