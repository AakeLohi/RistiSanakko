using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    const string highscoreKey = "highscore_v1";

    public int score;
    public int wordCount = 0;
    public int highscore;

    public float scoreBalance = 3f;
    public float maxBalance = 3f;

    public GameObject loseMenu;

    public UnityEvent<int> onscoreChanged;
    public UnityEvent<int> onWordCountChanged;
    public UnityEvent<int> onhighscoreChanged;

    public UnityEvent<float> onBalanceChanged;

    public GameObject[] gameplayScripts;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Application.targetFrameRate = 120;
        foreach (GameObject go in gameplayScripts) go.SetActive(true);

        loseMenu.SetActive(false);

        Loadhighscore();
        score = 0;
        onscoreChanged?.Invoke(score);
        onBalanceChanged?.Invoke(scoreBalance);
    }

    void Loadhighscore()
    {
        highscore = PlayerPrefs.GetInt(highscoreKey, 0);
        onhighscoreChanged?.Invoke(highscore);
    }

    void Savehighscore()
    {
        PlayerPrefs.SetInt(highscoreKey, highscore);
        PlayerPrefs.Save();
        onhighscoreChanged?.Invoke(highscore);
    }

    public void AddScore(int amount, int bestAmount)
    {
        Setscore(score + amount);

        if (bestAmount <= 0)
        {
            wordCount++;
            onWordCountChanged?.Invoke(wordCount);
            return;
        }

        float baseGain = amount / (float)bestAmount;

        float gainDecay = Mathf.Exp(-wordCount * 0.03f); 
        float adjusted = baseGain * gainDecay;

        scoreBalance += adjusted;
        scoreBalance = Mathf.Clamp(scoreBalance, 0f, maxBalance);
        onBalanceChanged?.Invoke(scoreBalance);

        if (scoreBalance <= 0f) LoseGame();

        wordCount++;
        onWordCountChanged?.Invoke(wordCount);
    }

    public void HintUsed(int amount, int bestAmount)
    {
        if (bestAmount <= 0) return;

        float baseLoss = 1 - Mathf.Clamp01((amount-1) / (float)bestAmount);

        float lossGrowth = 1f + wordCount * 0.03f;
        float adjusted = baseLoss * lossGrowth;

        scoreBalance -= adjusted;
        scoreBalance = Mathf.Clamp(scoreBalance, 0f, maxBalance);
        onBalanceChanged?.Invoke(scoreBalance);

        if (scoreBalance <= 0f) LoseGame();
    }

    public void LoseGame()
    {
        foreach (GameObject go in gameplayScripts) go.SetActive(false);
        loseMenu.SetActive(true);
    }

    public void SetWordCount(int amount)
    {
        if (amount <= 0) return;
        wordCount = amount;
        onWordCountChanged.Invoke(wordCount);
    }

    public void Removescore(int amount)
    {
        if (amount <= 0) return;
        Setscore(Mathf.Max(0, score - amount));
    }

    public void Setscore(int value)
    {
        score = Mathf.Max(0, value);
        onscoreChanged?.Invoke(score);
        if (score > highscore)
        {
            highscore = score;
            Savehighscore();
        }
    }

    public void Resethighscore()
    {
        highscore = 0;
        Savehighscore();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMenu(string sceneName = "MenuScene")
    {
        SceneManager.LoadScene(sceneName);
    }
}
