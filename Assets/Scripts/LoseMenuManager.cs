using UnityEngine;
using TMPro;

public class LoseMenuManager : MonoBehaviour
{
    public TextMeshProUGUI text;
    void OnEnable()
    {
        text.text = "Sait " + GameManager.Instance.score.ToString("F0") + " pistettä\n" + "kirjoitit " + GameManager.Instance.wordCount.ToString("F0") + " sanaa";
    }
}

