using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeSpeedController : MonoBehaviour
{
    [Header("UI")]
    public Slider speedSlider;
    public TMP_Text speedValueText;

    [Header("Speed Settings")]
    public int startSpeedIndex = 1;

    private float speedMultiplier = 1f;

    public float SpeedMultiplier => speedMultiplier;

    void Start()
    {
        SetupSlider();
        SetSpeedByIndex(startSpeedIndex);
    }

    private void SetupSlider()
    {
        if (speedSlider == null)
            return;

        speedSlider.minValue = 0;
        speedSlider.maxValue = 5;
        speedSlider.wholeNumbers = true;
        speedSlider.value = startSpeedIndex;

        speedSlider.onValueChanged.RemoveListener(OnSliderChanged);
        speedSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        int index = Mathf.RoundToInt(value);
        SetSpeedByIndex(index);
    }

    private void SetSpeedByIndex(int index)
    {
        switch (index)
        {
            case 0:
                speedMultiplier = 0f;
                break;

            case 1:
                speedMultiplier = 1f;
                break;

            case 2:
                speedMultiplier = 100f;
                break;

            case 3:
                speedMultiplier = 1000f;
                break;

            case 4:
                speedMultiplier = 10000f;
                break;

            case 5:
                speedMultiplier = 100000f;
                break;

            default:
                speedMultiplier = 1f;
                break;
        }

        UpdateSpeedText();
    }

    private void UpdateSpeedText()
    {
        if (speedValueText != null)
        {
            speedValueText.text = FormatSpeedText(speedMultiplier);
        }
    }

    private string FormatSpeedText(float speed)
    {
        if (Mathf.Approximately(speed, 0f))
            return "0x";

        return speed.ToString("0") + "x";
    }

    public void ResetToNormalSpeed()
    {
        speedMultiplier = 1f;

        if (speedSlider != null)
        {
            speedSlider.SetValueWithoutNotify(1);
        }

        UpdateSpeedText();
    }
}