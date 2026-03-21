using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public TextMeshProUGUI highscoreText;

    void Start()
    {
        Application.targetFrameRate = 120;
        if (highscoreText != null)
            highscoreText.text = "Ennätys: " + PlayerPrefs.GetInt("highscore_v1", 0);
    }

    public void LoadGameScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
