using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BarDisplay : MonoBehaviour
{
    [Header("References")]
    public Image fillImage; // set Image.type to Filled and choose Fill Method
    public TextMeshProUGUI valueText; // optional

    public Gradient colorGradient;

    [Header("Range")]
    public float min = 0f;
    public float max = 100f;

    [Header("Behaviour")]
    public bool useLerp = true;
    public float lerpSpeed = 6f; // higher = faster

    [Header("Events")]
    public UnityEvent<float> onValueChanged;

    float targetValue;
    float displayValue;

    void Awake()
    {
        if (fillImage == null) fillImage = GetComponentInChildren<Image>();
        displayValue = targetValue = min;
        ApplyImmediately();
    }

    void Update()
    {
        if (useLerp)
        {
            if (!Mathf.Approximately(displayValue, targetValue))
            {
                displayValue = Mathf.MoveTowards(displayValue, targetValue, lerpSpeed * Time.unscaledDeltaTime);
                Apply(displayValue);
            }
        }
    }

    float Normalized(float v) => (max - min) == 0f ? 0f : Mathf.Clamp01((v - min) / (max - min));

    void Apply(float v)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Normalized(v);
            fillImage.color = colorGradient.Evaluate(Normalized(v));
        } 
        if (valueText != null) valueText.text = Mathf.RoundToInt(v).ToString();
        onValueChanged?.Invoke(v);
    }

    void ApplyImmediately()
    {
        displayValue = targetValue;
        Apply(displayValue);
    }

    public void SetRange(float newMin, float newMax)
    {
        min = newMin;
        max = newMax;
        Set(targetValue);
    }

    public void Set(float value)
    {
        targetValue = Mathf.Clamp(value, min, max);
        if (!useLerp) { ApplyImmediately(); } else { /* animated by Update */ }
    }

    public void Add(float amount) => Set(targetValue + amount);
    public void Subtract(float amount) => Set(targetValue - amount);
    public float Get() => targetValue;
}
