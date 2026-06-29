using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DateTimeSelector : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField dateInput;
    public TMP_InputField timeInput;

    [Header("Now Button")]
    public Button nowButton;

    [Header("Speed Controller")]
    public TimeSpeedController speedController;

    [Header("Button Colors")]
    public Color nowActiveColor = new Color(0.2f, 0.55f, 1f);
    public Color nowInactiveColor = Color.gray;

    [Header("Text Colors")]
    public Color activeTextColor = Color.black;
    public Color inactiveTextColor = Color.black;

    [Header("Settings")]
    public bool startWithNow = true;

    private bool nowMode;
    private DateTime selectedLocalDateTime;

    public DateTime SelectedLocalDateTime => selectedLocalDateTime;
    [Header("Time Zone")]
    public int simulationUtcOffsetHours = 3;

    public DateTime SelectedUtcDateTime =>
        DateTime.SpecifyKind(
            selectedLocalDateTime.AddHours(-simulationUtcOffsetHours),
            DateTimeKind.Utc
        );
    public bool NowMode => nowMode;

    void Awake()
    {
        selectedLocalDateTime = DateTime.Now;
    }

    void Start()
    {
        nowMode = startWithNow;

        if (speedController == null)
        {
            speedController = FindObjectOfType<TimeSpeedController>();
        }

        if (nowButton != null)
        {
            nowButton.onClick.AddListener(ToggleNowMode);
        }

        if (dateInput != null)
        {
            dateInput.onEndEdit.AddListener(OnDateChanged);
        }

        if (timeInput != null)
        {
            timeInput.onEndEdit.AddListener(OnTimeChanged);
        }

        if (nowMode)
        {
            SetToNow();
        }
        else
        {
            UpdateInputTexts();
        }

        UpdateVisualState();
    }

    void Update()
    {
        if (IsAnyInputFocused())
            return;

        float speed = 1f;

        if (speedController != null)
        {
            speed = speedController.SpeedMultiplier;
        }

        // x0 ise zaman tamamen durur.
        if (speed <= 0f)
            return;

        if (nowMode && Mathf.Approximately(speed, 1f))
        {
            // Gerçek zaman modu.
            selectedLocalDateTime = DateTime.Now;
            UpdateInputTexts();
            return;
        }

        // x10, x1000 gibi hızlarda artık simülasyon zamanı akar.
        if (nowMode && !Mathf.Approximately(speed, 1f))
        {
            nowMode = false;
            UpdateVisualState();
        }

        double secondsToAdd = Time.deltaTime * speed;
        selectedLocalDateTime = selectedLocalDateTime.AddSeconds(secondsToAdd);
        UpdateInputTexts();
    }

    public void ToggleNowMode()
    {
        nowMode = !nowMode;

        if (nowMode)
        {
            SetToNow();

            if (speedController != null)
            {
                speedController.ResetToNormalSpeed();
            }
        }

        UpdateVisualState();
    }

    private void SetToNow()
    {
        selectedLocalDateTime = DateTime.Now;
        UpdateInputTexts();
    }

    private void UpdateInputTexts()
    {
        if (dateInput != null)
        {
            dateInput.SetTextWithoutNotify(selectedLocalDateTime.ToString("dd-MM-yyyy"));
        }

        if (timeInput != null)
        {
            timeInput.SetTextWithoutNotify(selectedLocalDateTime.ToString("HH:mm:ss"));
        }
    }

    private void OnDateChanged(string value)
    {
        if (nowMode)
            return;

        bool validDate = DateTime.TryParseExact(
            value,
            "dd-MM-yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate
        );

        if (!validDate)
        {
            Debug.LogWarning("Tarih formatı yanlış. Örnek: 29-04-2026");
            UpdateInputTexts();
            return;
        }

        selectedLocalDateTime = new DateTime(
            parsedDate.Year,
            parsedDate.Month,
            parsedDate.Day,
            selectedLocalDateTime.Hour,
            selectedLocalDateTime.Minute,
            selectedLocalDateTime.Second
        );

        UpdateInputTexts();
        Debug.Log("Seçilen tarih-saat: " + selectedLocalDateTime);
    }

    private void OnTimeChanged(string value)
    {
        if (nowMode)
            return;

        bool validTime = DateTime.TryParseExact(
            value,
            "HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedTime
        );

        if (!validTime)
        {
            Debug.LogWarning("Saat formatı yanlış. Örnek: 20:05:30");
            UpdateInputTexts();
            return;
        }

        selectedLocalDateTime = new DateTime(
            selectedLocalDateTime.Year,
            selectedLocalDateTime.Month,
            selectedLocalDateTime.Day,
            parsedTime.Hour,
            parsedTime.Minute,
            parsedTime.Second
        );

        UpdateInputTexts();
        Debug.Log("Seçilen tarih-saat: " + selectedLocalDateTime);
    }

    private void UpdateVisualState()
    {
        if (dateInput != null)
        {
            dateInput.interactable = !nowMode;
        }

        if (timeInput != null)
        {
            timeInput.interactable = !nowMode;
        }

        if (nowButton != null)
        {
            Image buttonImage = nowButton.GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.color = nowMode ? nowActiveColor : nowInactiveColor;
            }

            TMP_Text buttonText = nowButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.color = nowMode ? activeTextColor : inactiveTextColor;
            }
        }
    }

    private bool IsAnyInputFocused()
    {
        bool dateFocused = dateInput != null && dateInput.isFocused;
        bool timeFocused = timeInput != null && timeInput.isFocused;

        return dateFocused || timeFocused;
    }

    public void ForceNow()
    {
        nowMode = true;

        if (speedController != null)
        {
            speedController.ResetToNormalSpeed();
        }

        SetToNow();
        UpdateVisualState();
    }

    public void SetManualDateTime(DateTime dateTime)
    {
        nowMode = false;
        selectedLocalDateTime = dateTime;
        UpdateInputTexts();
        UpdateVisualState();
    }
}