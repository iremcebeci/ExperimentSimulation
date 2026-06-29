using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject dateTimePanel;
    public GameObject speedPanel;
    public GameObject cameraPanel;
    public GameObject infoCardPanel;

    [Header("Buttons")]
    public Button dateTimeButton;
    public Button speedButton;
    public Button cameraButton;
    public Button infoHamburgerButton;

    [Header("Button Background Colors")]
    public Color closedButtonColor = new Color(0.278f, 0.333f, 0.412f, 1f); // #475569
    public Color openButtonColor = new Color(0.22f, 0.741f, 0.973f, 1f);    // #38BDF8

    [Header("Button Text Colors")]
    public Color closedTextColor = Color.white;
    public Color openTextColor = new Color(0.008f, 0.024f, 0.09f, 1f);      // #020617

    [Header("Start State")]
    public bool startDateTimeOpen = true;
    public bool startSpeedOpen = true;
    public bool startCameraOpen = false;
    public bool startInfoCardOpen = false;

    void Start()
    {
        SetPanelState(dateTimePanel, startDateTimeOpen);
        SetPanelState(speedPanel, startSpeedOpen);
        SetPanelState(cameraPanel, startCameraOpen);
        SetPanelState(infoCardPanel, startInfoCardOpen);

        PrepareButton(dateTimeButton);
        PrepareButton(speedButton);
        PrepareButton(cameraButton);
        PrepareButton(infoHamburgerButton);

        UpdateAllButtonVisuals();
    }

    public void ToggleDateTimePanel()
    {
        TogglePanel(dateTimePanel);
        UpdateAllButtonVisuals();
    }

    public void ToggleSpeedPanel()
    {
        TogglePanel(speedPanel);
        UpdateAllButtonVisuals();
    }

    public void ToggleCameraPanel()
    {
        TogglePanel(cameraPanel);
        UpdateAllButtonVisuals();
    }

    public void ToggleInfoCardPanel()
    {
        TogglePanel(infoCardPanel);
        UpdateAllButtonVisuals();
    }

    private void TogglePanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("Panel atanmadı.");
            return;
        }

        panel.SetActive(!panel.activeSelf);
    }

    private void SetPanelState(GameObject panel, bool isOpen)
    {
        if (panel != null)
        {
            panel.SetActive(isOpen);
        }
    }

    private void PrepareButton(Button button)
    {
        if (button == null)
            return;

        button.transition = Selectable.Transition.None;
    }

    public void UpdateAllButtonVisuals()
    {
        UpdateButtonVisual(dateTimeButton, dateTimePanel);
        UpdateButtonVisual(speedButton, speedPanel);
        UpdateButtonVisual(cameraButton, cameraPanel);
        UpdateButtonVisual(infoHamburgerButton, infoCardPanel);
    }

    private void UpdateButtonVisual(Button button, GameObject panel)
    {
        if (button == null || panel == null)
            return;

        bool isOpen = panel.activeSelf;

        Color backgroundColor = isOpen ? openButtonColor : closedButtonColor;
        Color textColor = isOpen ? openTextColor : closedTextColor;

        button.transition = Selectable.Transition.None;

        Graphic buttonGraphic = button.targetGraphic;

        if (buttonGraphic == null)
        {
            buttonGraphic = button.GetComponent<Graphic>();
            button.targetGraphic = buttonGraphic;
        }

        if (buttonGraphic != null)
        {
            buttonGraphic.color = backgroundColor;
        }

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            text.color = textColor;
        }
    }
}