using UnityEngine;
using TMPro;

public class IntTextDisplay : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI text;

    [Header("Behavior")]
    public bool useLerp = true;
    public float lerpSpeed = 8f; // higher = faster
    public string format = "F0"; // numeric format used when converting float->string

    int targetValue = 0;
    float displayFloat = 0f;

    void Awake()
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
        displayFloat = targetValue;
        RefreshText();
    }

    void Update()
    {
        if (!useLerp)
        {
            if ((int)displayFloat != targetValue)
            {
                displayFloat = targetValue;
                RefreshText();
            }
            return;
        }

        if (Mathf.Approximately(displayFloat, targetValue)) return;

        displayFloat = Mathf.MoveTowards(displayFloat, targetValue, lerpSpeed * Time.unscaledDeltaTime);
        int intNow = Mathf.RoundToInt(displayFloat);
        text.text = intNow.ToString(format);
    }

    void RefreshText()
    {
        text.text = Mathf.RoundToInt(displayFloat).ToString(format);
    }

    public void Set(int value, bool instant = false)
    {
        targetValue = value;
        if (instant || !useLerp)
        {
            displayFloat = targetValue;
            RefreshText();
        }
    }

    public void SetLerped(int newValue)
    {
        targetValue = newValue;
    }

    public void Add(int amount)
    {
        Set(targetValue + amount, false);
    }

    public void Subtract(int amount)
    {
        Set(targetValue - amount, false);
    }

    public int GetTargetValue() => targetValue;
}
