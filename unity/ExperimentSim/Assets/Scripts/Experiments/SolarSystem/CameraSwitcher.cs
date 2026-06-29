using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCamera;
    public Camera topViewCamera;
    public Camera earthViewCamera;
    public Camera sunViewCamera;
    public Camera moonViewCamera;
    public Camera solarEclipseCamera;

    [Header("Camera Buttons")]
    public Button mainCameraButton;
    public Button topViewButton;
    public Button earthViewButton;
    public Button sunViewButton;
    public Button moonViewButton;
    public Button solarEclipseCameraButton;

    [Header("Start Settings")]
    public bool switchToMainOnStart = true;

    [Header("Active Camera Text")]
    public TMP_Text activeCameraText;

    [Header("Info Card")]
    public CelestialInfoCard infoCard;

    void Start()
    {
        if (switchToMainOnStart)
        {
            SwitchToMainCamera();
        }
    }

    public void SwitchToMainCamera()
    {
        ActivateCamera(mainCamera, mainCameraButton, "Genel Kamera");

        if (infoCard != null)
            infoCard.HideAll();
    }

    public void SwitchToTopView()
    {
        ActivateCamera(topViewCamera, topViewButton, "Üstten Görünüm");

        if (infoCard != null)
            infoCard.HideAll();
    }

    public void SwitchToEarthView()
    {
        ActivateCamera(earthViewCamera, earthViewButton, "Dünya Kamerası");

        if (infoCard != null)
            infoCard.OpenEarthInfo();
    }

    public void SwitchToSunView()
    {
        ActivateCamera(sunViewCamera, sunViewButton, "Güneş Kamerası");

        if (infoCard != null)
            infoCard.OpenSunInfo();
    }

    public void SwitchToMoonView()
    {
        ActivateCamera(moonViewCamera, moonViewButton, "Ay Kamerası");

        if (infoCard != null)
            infoCard.OpenMoonInfo();
    }

    public void SwitchToSolarEclipseView()
    {
        ActivateCamera(solarEclipseCamera, solarEclipseCameraButton, "Güneş Tutulması Kamerası");

        if (infoCard != null)
            infoCard.HideAll();
    }

    private void ActivateCamera(Camera selectedCamera, Button selectedButton, string cameraName)
    {
        if (selectedCamera == null)
        {
            Debug.LogWarning("Seçilen kamera boş: " + cameraName);
            return;
        }

        DisableCamera(mainCamera);
        DisableCamera(topViewCamera);
        DisableCamera(earthViewCamera);
        DisableCamera(sunViewCamera);
        DisableCamera(moonViewCamera);
        DisableCamera(solarEclipseCamera);

        selectedCamera.enabled = true;

        AudioListener listener = selectedCamera.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = true;

        UpdateActiveButton(selectedButton);
        UpdateActiveCameraText(cameraName);

        Debug.Log("Aktif kamera: " + cameraName);
    }

    private void DisableCamera(Camera cam)
    {
        if (cam == null)
            return;

        cam.enabled = false;

        AudioListener listener = cam.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = false;
    }

    private void UpdateActiveButton(Button activeButton)
    {
        SetButtonActive(mainCameraButton, false);
        SetButtonActive(topViewButton, false);
        SetButtonActive(earthViewButton, false);
        SetButtonActive(sunViewButton, false);
        SetButtonActive(moonViewButton, false);
        SetButtonActive(solarEclipseCameraButton, false);

        SetButtonActive(activeButton, true);
    }

    private void SetButtonActive(Button button, bool active)
    {
        if (button == null)
            return;

        UIStylizedButton style = button.GetComponent<UIStylizedButton>();

        if (style != null)
        {
            style.SetActiveState(active);
        }
    }

    private void UpdateActiveCameraText(string cameraName)
    {
        if (activeCameraText != null)
        {
            activeCameraText.text = cameraName;
        }
    }
}