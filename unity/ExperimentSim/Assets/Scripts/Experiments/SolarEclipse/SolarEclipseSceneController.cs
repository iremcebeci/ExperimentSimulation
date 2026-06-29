using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolarEclipseSceneController : MonoBehaviour
{
    private enum EclipseState
    {
        Intro,
        NarrationRunning,
        Finished
    }

    [Header("Main Objects")]
    public Camera eclipseCamera;
    public Transform sun;
    public Transform earth;
    public Transform moon;

    [Header("Moon Visual")]
    [Tooltip("Moon ve Moon_Lod objelerindeki Mesh Renderer'ları buraya ekle.")]
    public Renderer[] moonRenderers;

    [Tooltip("Moon altındaki Glow objesini buraya ver.")]
    public GameObject moonGlowObject;

    public Color normalMoonColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color partialMoonColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color eclipseMoonColor = new Color(0.02f, 0.02f, 0.02f, 1f);

    [Header("Camera Points")]
    public Transform overviewCameraPoint;
    public Transform earthObserverPoint;

    [Header("Light")]
    public Light sunLight;
    public float normalLightIntensity = 2.5f;
    public float partialLightIntensity = 1.3f;
    public float eclipseLightIntensity = 0.25f;

    [Header("Narration Audio")]
    public AudioSource narrationAudio;

    [Header("UI")]
    public Button restartButton;
    public TMP_Text restartButtonText;
    public TMP_Text subtitleText;
    public TMP_Text infoText;

    [Header("Partial Eclipse Marker")]
    [Tooltip("Ok + yazı grubunu buraya ver. Başta kapalı olacak.")]
    public GameObject partialEclipseMarker;

    [Tooltip("Marker içindeki yazı. Boş bırakılırsa sorun olmaz.")]
    public TMP_Text partialEclipseMarkerText;

    [Header("Full Eclipse Marker")]
    [Tooltip("Tam tutulma anında çıkacak ok + yazı grubu.")]
    public GameObject fullEclipseMarker;

    [Tooltip("Marker içindeki yazı.")]
    public TMP_Text fullEclipseMarkerText;

    [Header("Camera Animation")]
    public float introDuration = 4f;
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Audio Timeline Seconds")]
    [Tooltip("Ay'ın Güneş'e doğru hareket etmeye başlayacağı ses zamanı.")]
    public float moonMoveStartTime = 13f;

    [Tooltip("Ay'ın Güneş'i kısmen kapatıp duracağı ses zamanı.")]
    public float partialStopTime = 19f;

    [Tooltip("Parçalı Güneş tutulması cümlesinin bittiği ses zamanı.")]
    public float partialSentenceEndTime = 24f;

    [Tooltip("Ay'ın Güneş'in merkezini tamamen kapattığı ses zamanı.")]
    public float fullEclipseReachTime = 27f;

    [Tooltip("Tam tutulma açıklaması bittikten sonra Ay'ın yoluna devam edeceği ses zamanı.")]
    public float fullEclipseSentenceEndTime = 37f;

    [Header("Moon Eclipse Animation")]
    [Tooltip("Ay kameradan ne kadar uzakta Güneş önüne yerleşsin.")]
    public float moonDistanceFromCamera = 7f;

    [Tooltip("Ay Güneş'in önünden geçerken yatayda ne kadar yol alsın.")]
    public float moonTravelWidth = 4f;

    [Tooltip("Parçalı tutulmada Ay merkezden ne kadar uzakta dursun. Büyük değer = Güneş'i daha az kapatır.")]
    public float partialOffsetFromCenter = 0.85f;

    public float moonExitDuration = 3f;

    [Header("Moon Size During Eclipse")]
    public bool scaleMoonDuringEclipse = true;
    public Vector3 eclipseMoonScale = new Vector3(1f, 1f, 1f);

    [Header("Look Settings")]
    public bool cameraAlwaysLooksAtSun = true;

    private EclipseState state;

    private Vector3 originalMoonPosition;
    private Quaternion originalMoonRotation;
    private Vector3 originalMoonScale;

    private Vector3 moonStartPosition;
    private Vector3 moonPartialPosition;
    private Vector3 moonCoverPosition;
    private Vector3 moonEndPosition;

    private Coroutine activeRoutine;

    private Material[] moonMaterialInstances;
    private Color[] originalMoonColors;

    void Awake()
    {
        SaveOriginalValues();
        SetupMoonMaterials();
        BindButton();
    }

    void Start()
    {
        RestartWholeSimulation();
    }

    void LateUpdate()
    {
        if (cameraAlwaysLooksAtSun && eclipseCamera != null && sun != null)
        {
            eclipseCamera.transform.LookAt(sun.position);
        }
    }

    private void SaveOriginalValues()
    {
        if (moon != null)
        {
            originalMoonPosition = moon.position;
            originalMoonRotation = moon.rotation;
            originalMoonScale = moon.localScale;
        }

        if (sunLight != null)
        {
            normalLightIntensity = sunLight.intensity;
        }
    }

    private void SetupMoonMaterials()
    {
        if (moonRenderers == null || moonRenderers.Length == 0)
        {
            Debug.LogWarning("Moon Renderers boş. Ay karartma çalışmayabilir.");
            return;
        }

        moonMaterialInstances = new Material[moonRenderers.Length];
        originalMoonColors = new Color[moonRenderers.Length];

        for (int i = 0; i < moonRenderers.Length; i++)
        {
            if (moonRenderers[i] == null)
                continue;

            moonMaterialInstances[i] = moonRenderers[i].material;

            if (moonMaterialInstances[i].HasProperty("_BaseColor"))
            {
                originalMoonColors[i] = moonMaterialInstances[i].GetColor("_BaseColor");
            }
            else if (moonMaterialInstances[i].HasProperty("_Color"))
            {
                originalMoonColors[i] = moonMaterialInstances[i].GetColor("_Color");
            }
            else
            {
                originalMoonColors[i] = normalMoonColor;
            }
        }
    }

    private void BindButton()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartWholeSimulation);
            restartButton.onClick.AddListener(RestartWholeSimulation);
        }
    }

    public void RestartWholeSimulation()
    {
        StopActiveRoutine();
        ResetObjects();
        activeRoutine = StartCoroutine(MainTimelineRoutine());
    }

    private IEnumerator MainTimelineRoutine()
    {
        state = EclipseState.Intro;

        SetRestartButton(false, "Hazırlanıyor...");
        SetInfoText("Güneş tutulması incelemesine Dünya’dan bakarak başlıyoruz.");

        if (narrationAudio != null)
        {
            narrationAudio.Stop();
            narrationAudio.time = 0f;
            narrationAudio.Play();
        }

        yield return IntroCameraRoutine();

        CalculateMoonEclipsePositions();
        PlaceMoonAtStartPosition();

        state = EclipseState.NarrationRunning;

        SetInfoText("Şu anda gözlem noktamız Dünya üzerinde. Normal durumda Güneş gökyüzünde parlak görünür.");

        yield return WaitUntilNarrationTime(moonMoveStartTime);

        SetSubtitle("Ay, Güneş’in önüne doğru ilerledikçe Güneş’in bir bölümü kapanır.");
        SetInfoText("Ay, Güneş’in önüne doğru ilerliyor.");

        float moveToPartialDuration = Mathf.Max(0.1f, partialStopTime - moonMoveStartTime);

        yield return MoveMoonLightAndColorRoutine(
            moonStartPosition,
            moonPartialPosition,
            normalLightIntensity,
            partialLightIntensity,
            normalMoonColor,
            partialMoonColor,
            moveToPartialDuration
        );

        ShowPartialMarker(true);

        SetSubtitle("Bu durum parçalı Güneş tutulması olarak gözlemlenir.");
        SetInfoText("Parçalı Güneş Tutulması: Ay, Güneş’in tamamını değil bir bölümünü kapatır.");

        yield return WaitUntilNarrationTime(partialSentenceEndTime);

        ShowPartialMarker(false);

        SetSubtitle("Ay, Güneş’in merkezini tamamen kapattığında ise tam Güneş tutulması meydana gelir.");
        SetInfoText("Ay şimdi Güneş’in merkezini tamamen kapatacak.");

        float moveToFullDuration = Mathf.Max(0.1f, fullEclipseReachTime - partialSentenceEndTime);

        yield return MoveMoonLightAndColorRoutine(
            moonPartialPosition,
            moonCoverPosition,
            partialLightIntensity,
            eclipseLightIntensity,
            partialMoonColor,
            eclipseMoonColor,
            moveToFullDuration
        );

        ShowFullMarker(true);

        SetSubtitle("Bu anda Dünya üzerindeki bazı bölgelerde ışık azalır ve kısa süreli karanlık bir görünüm oluşur.");
        SetInfoText("Tam Güneş Tutulması: Ay, Güneş’in merkezini kapattı ve ışık azaldı.");

        

        yield return WaitUntilNarrationTime(fullEclipseSentenceEndTime);

        ShowFullMarker(false);

        SetSubtitle("");
        SetInfoText("Ay yörüngesinde ilerlemeye devam eder ve Güneş yeniden görünmeye başlar.");

        yield return MoveMoonLightAndColorRoutine(
            moonCoverPosition,
            moonEndPosition,
            eclipseLightIntensity,
            normalLightIntensity,
            eclipseMoonColor,
            normalMoonColor,
            moonExitDuration
        );

        state = EclipseState.Finished;

        SetInfoText("Simülasyon tamamlandı. Ay, Güneş’in önünden geçti ve ışık tekrar normale döndü.");
        SetRestartButton(true, "Baştan Başlat");
    }

    private IEnumerator IntroCameraRoutine()
    {
        if (eclipseCamera == null || overviewCameraPoint == null || earthObserverPoint == null)
        {
            Debug.LogWarning("Kamera veya kamera noktaları eksik.");
            yield break;
        }

        eclipseCamera.enabled = true;

        float elapsed = 0f;

        Vector3 startPosition = overviewCameraPoint.position;
        Quaternion startRotation = overviewCameraPoint.rotation;

        Vector3 targetPosition = earthObserverPoint.position;
        Quaternion targetRotation = GetLookRotationToSun(targetPosition, earthObserverPoint.rotation);

        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / introDuration);
            float smoothT = cameraMoveCurve.Evaluate(t);

            eclipseCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            eclipseCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);

            yield return null;
        }

        eclipseCamera.transform.position = targetPosition;
        eclipseCamera.transform.rotation = targetRotation;
    }

    private IEnumerator WaitUntilNarrationTime(float targetTime)
    {
        if (narrationAudio == null)
        {
            yield return new WaitForSeconds(targetTime);
            yield break;
        }

        while (narrationAudio.isPlaying && narrationAudio.time < targetTime)
        {
            yield return null;
        }
    }

    private IEnumerator MoveMoonLightAndColorRoutine(
        Vector3 startPosition,
        Vector3 targetPosition,
        float startLight,
        float targetLight,
        Color startMoonColor,
        Color targetMoonColor,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (moon != null)
            {
                moon.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            }

            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(startLight, targetLight, smoothT);
            }

            SetMoonColor(Color.Lerp(startMoonColor, targetMoonColor, smoothT));

            yield return null;
        }

        if (moon != null)
        {
            moon.position = targetPosition;
        }

        if (sunLight != null)
        {
            sunLight.intensity = targetLight;
        }

        SetMoonColor(targetMoonColor);
    }

    private void CalculateMoonEclipsePositions()
    {
        if (eclipseCamera == null || sun == null)
            return;

        Vector3 cameraPosition = eclipseCamera.transform.position;

        Vector3 directionToSun = (sun.position - cameraPosition).normalized;
        Vector3 cameraRight = eclipseCamera.transform.right;

        moonCoverPosition = cameraPosition + directionToSun * moonDistanceFromCamera;

        moonStartPosition = moonCoverPosition - cameraRight * moonTravelWidth;

        moonPartialPosition = moonCoverPosition - cameraRight * partialOffsetFromCenter;

        moonEndPosition = moonCoverPosition + cameraRight * moonTravelWidth;
    }

    private void PlaceMoonAtStartPosition()
    {
        if (moon == null)
            return;

        moon.position = moonStartPosition;
        moon.rotation = originalMoonRotation;

        if (scaleMoonDuringEclipse)
        {
            moon.localScale = eclipseMoonScale;
        }

        SetMoonColor(normalMoonColor);

        if (moonGlowObject != null)
        {
            moonGlowObject.SetActive(false);
        }
    }

    private void ResetObjects()
    {
        state = EclipseState.Intro;

        if (moon != null)
        {
            moon.position = originalMoonPosition;
            moon.rotation = originalMoonRotation;
            moon.localScale = originalMoonScale;
        }

        if (sunLight != null)
        {
            sunLight.intensity = normalLightIntensity;
        }

        if (eclipseCamera != null && overviewCameraPoint != null)
        {
            eclipseCamera.transform.position = overviewCameraPoint.position;
            eclipseCamera.transform.rotation = overviewCameraPoint.rotation;
        }

        SetMoonColor(normalMoonColor);

        if (moonGlowObject != null)
        {
            moonGlowObject.SetActive(false);
        }

        ShowPartialMarker(false);
        ShowFullMarker(false);

        SetRestartButton(false, "Hazırlanıyor...");
        SetSubtitle("");
        SetInfoText("");
    }

    private void SetMoonColor(Color color)
    {
        if (moonMaterialInstances == null || moonMaterialInstances.Length == 0)
            return;

        foreach (Material mat in moonMaterialInstances)
        {
            if (mat == null)
                continue;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private Quaternion GetLookRotationToSun(Vector3 fromPosition, Quaternion fallbackRotation)
    {
        if (sun == null)
            return fallbackRotation;

        Vector3 direction = sun.position - fromPosition;

        if (direction.sqrMagnitude < 0.0001f)
            return fallbackRotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void ShowPartialMarker(bool show)
    {
        if (partialEclipseMarker != null)
        {
            partialEclipseMarker.SetActive(show);
        }

        if (partialEclipseMarkerText != null)
        {
            partialEclipseMarkerText.text = "Parçalı Güneş Tutulması";
        }
    }

    private void StopActiveRoutine()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    private void SetRestartButton(bool interactable, string text)
    {
        if (restartButton != null)
        {
            restartButton.interactable = interactable;
        }

        if (restartButtonText != null)
        {
            restartButtonText.text = text;
        }
    }

    private void SetSubtitle(string text)
    {
        if (subtitleText != null)
        {
            subtitleText.text = text;
        }
    }

    private void SetInfoText(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    private void ShowFullMarker(bool show)
    {
        if (fullEclipseMarker != null)
        {
            fullEclipseMarker.SetActive(show);
        }

        if (fullEclipseMarkerText != null)
        {
            fullEclipseMarkerText.text = "Tam Güneş Tutulması";
        }
    }
}