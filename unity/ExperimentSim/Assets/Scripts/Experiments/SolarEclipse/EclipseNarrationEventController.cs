using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EclipseNarrationEventController : MonoBehaviour
{
    [Header("Narration")]
    public AudioSource narrationAudio;

    public bool autoStartTimeline = true;

    [Tooltip("Ses dosyasını bu script başlatacaksa açık kalsın.")]
    public bool controlAudioPlayback = true;

    public bool restartAudioFromZero = true;

    [Tooltip("AudioSource çalışmazsa zaman yine ilerlesin.")]
    public bool useFallbackTimerIfAudioNotPlaying = true;

    [Header("Debug")]
    public float currentAudioTime;
    public bool debugLogs = true;

    [Header("Line Highlight Settings")]
    public Color highlightColor = new Color(0.2f, 0.65f, 1f, 1f);
    public float widthMultiplier = 2.3f;
    public float minHighlightWidth = 0.05f;
    public float transitionDuration = 0.25f;

    [Header("1 - Gölge Alanı Oluşur")]
    public float shadowAreaStartTime = 10f;
    public float shadowAreaEndTime = 13f;
    public LineRenderer[] shadowAreaLines;

    [Header("2 - Tam Gölge")]
    public float fullShadowStartTime = 20f;
    public float fullShadowEndTime = 27f;
    public LineRenderer[] fullShadowLines;
    public GameObject fullShadowMarker;
    public TMP_Text fullShadowLabelText;
    public string fullShadowLabel = "Tam Gölge (Tam Tutulma)";

    [Header("3 - Yarı Gölge / Parçalı")]
    public float partialShadowStartTime = 28f;
    public float partialShadowEndTime = 35f;
    public LineRenderer[] partialShadowLines;
    public GameObject partialShadowMarker;
    public TMP_Text partialShadowLabelText;
    public string partialShadowLabel = "Yarı Gölge (Parçalı Tutulma)";

    [Header("Marker Behaviour")]
    public bool keepMarkersVisibleAfterCue = true;

    [Header("Continue Button")]
    public Button continueButton;
    public TMP_Text continueButtonText;
    public string continueButtonLabel = "Devam Et";
    public bool showContinueWhenAudioEnds = true;
    public float continueButtonShowTime = 40f;

    private Coroutine timelineRoutine;
    private float fallbackTimer;
    private bool timelineStarted;

    private readonly List<LineOriginalState> originalLineStates = new List<LineOriginalState>();

    private class LineOriginalState
    {
        public LineRenderer line;
        public float startWidth;
        public float endWidth;
        public Color startColor;
        public Color endColor;
        public Material materialInstance;
        public Color materialColor;
    }

    private void Awake()
    {
        CacheOriginalLineStates();
        ResetVisuals();
    }

    private void OnEnable()
    {
        if (autoStartTimeline)
        {
            PlayTimeline();
        }
    }

    private void OnDisable()
    {
        if (timelineRoutine != null)
        {
            StopCoroutine(timelineRoutine);
            timelineRoutine = null;
        }
    }

    private void Update()
    {
        if (!timelineStarted)
            return;

        fallbackTimer += Time.deltaTime;

        if (narrationAudio != null && narrationAudio.isPlaying)
        {
            currentAudioTime = narrationAudio.time;
        }
        else
        {
            currentAudioTime = fallbackTimer;
        }
    }

    public void PlayTimeline()
    {
        if (timelineRoutine != null)
        {
            StopCoroutine(timelineRoutine);
        }

        timelineRoutine = StartCoroutine(TimelineRoutine());
    }

    private IEnumerator TimelineRoutine()
    {
        timelineStarted = true;
        fallbackTimer = 0f;

        CacheOriginalLineStates();
        ResetVisuals();

        if (debugLogs)
            Debug.Log("EclipseNarrationEventController başladı.");

        if (narrationAudio != null && controlAudioPlayback)
        {
            if (restartAudioFromZero)
            {
                narrationAudio.Stop();
                narrationAudio.time = 0f;
            }

            narrationAudio.Play();

            if (debugLogs)
                Debug.Log("Ses başlatıldı. Clip var mı: " + (narrationAudio.clip != null));
        }
        else
        {
            if (debugLogs)
                Debug.LogWarning("Narration Audio yok veya Control Audio Playback kapalı. Fallback timer kullanılacak.");
        }

        yield return RunCue(
            "Gölge Alanı",
            shadowAreaStartTime,
            shadowAreaEndTime,
            shadowAreaLines,
            null,
            null,
            ""
        );

        yield return RunCue(
            "Tam Gölge",
            fullShadowStartTime,
            fullShadowEndTime,
            fullShadowLines,
            fullShadowMarker,
            fullShadowLabelText,
            fullShadowLabel
        );

        yield return RunCue(
            "Yarı Gölge",
            partialShadowStartTime,
            partialShadowEndTime,
            partialShadowLines,
            partialShadowMarker,
            partialShadowLabelText,
            partialShadowLabel
        );

        if (showContinueWhenAudioEnds && narrationAudio != null && narrationAudio.clip != null && narrationAudio.isPlaying)
        {
            while (narrationAudio.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return WaitUntilTimelineTime(continueButtonShowTime);
        }

        ShowContinueButton(true);

        if (debugLogs)
            Debug.Log("Timeline bitti. Devam Et butonu gösterildi.");
    }

    private IEnumerator RunCue(
        string cueName,
        float startTime,
        float endTime,
        LineRenderer[] lines,
        GameObject marker,
        TMP_Text labelText,
        string labelValue)
    {
        yield return WaitUntilTimelineTime(startTime);

        if (debugLogs)
            Debug.Log(cueName + " başladı. Time: " + currentAudioTime.ToString("0.00"));

        SetMarker(marker, labelText, labelValue, true);

        float duration = Mathf.Max(0.1f, endTime - startTime);

        yield return HighlightLinesForDuration(lines, duration);

        if (!keepMarkersVisibleAfterCue)
        {
            SetMarker(marker, labelText, labelValue, false);
        }

        if (debugLogs)
            Debug.Log(cueName + " bitti. Time: " + currentAudioTime.ToString("0.00"));
    }

    private IEnumerator WaitUntilTimelineTime(float targetTime)
    {
        targetTime = Mathf.Max(0f, targetTime);

        while (GetTimelineTime() < targetTime)
        {
            yield return null;
        }
    }

    private float GetTimelineTime()
    {
        if (narrationAudio != null && narrationAudio.isPlaying)
        {
            return narrationAudio.time;
        }

        if (useFallbackTimerIfAudioNotPlaying)
        {
            return fallbackTimer;
        }

        if (narrationAudio != null)
        {
            return narrationAudio.time;
        }

        return fallbackTimer;
    }

    private IEnumerator HighlightLinesForDuration(LineRenderer[] lines, float duration)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Line listesi boş.");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float fadeDuration = Mathf.Min(transitionDuration, duration * 0.5f);
        float holdDuration = Mathf.Max(0f, duration - fadeDuration * 2f);

        yield return AnimateLines(lines, 0f, 1f, fadeDuration);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        yield return AnimateLines(lines, 1f, 0f, fadeDuration);

        RestoreLines(lines);
    }

    private IEnumerator AnimateLines(LineRenderer[] lines, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            ApplyLineHighlightAmount(lines, to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float amount = Mathf.Lerp(from, to, smoothT);

            ApplyLineHighlightAmount(lines, amount);

            yield return null;
        }

        ApplyLineHighlightAmount(lines, to);
    }

    private void ApplyLineHighlightAmount(LineRenderer[] lines, float amount)
    {
        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;

            LineOriginalState state = GetOriginalState(line);

            if (state == null)
                continue;

            float targetStartWidth = Mathf.Max(state.startWidth * widthMultiplier, minHighlightWidth);
            float targetEndWidth = Mathf.Max(state.endWidth * widthMultiplier, minHighlightWidth);

            line.startWidth = Mathf.Lerp(state.startWidth, targetStartWidth, amount);
            line.endWidth = Mathf.Lerp(state.endWidth, targetEndWidth, amount);

            Color startColor = Color.Lerp(state.startColor, highlightColor, amount);
            Color endColor = Color.Lerp(state.endColor, highlightColor, amount);

            line.startColor = startColor;
            line.endColor = endColor;

            if (state.materialInstance != null)
            {
                Color matColor = Color.Lerp(state.materialColor, highlightColor, amount);
                SetMaterialColor(state.materialInstance, matColor);
            }
        }
    }

    private void RestoreLines(LineRenderer[] lines)
    {
        if (lines == null)
            return;

        foreach (LineRenderer line in lines)
        {
            if (line == null)
                continue;

            LineOriginalState state = GetOriginalState(line);

            if (state == null)
                continue;

            line.startWidth = state.startWidth;
            line.endWidth = state.endWidth;
            line.startColor = state.startColor;
            line.endColor = state.endColor;

            if (state.materialInstance != null)
            {
                SetMaterialColor(state.materialInstance, state.materialColor);
            }
        }
    }

    private void CacheOriginalLineStates()
    {
        originalLineStates.Clear();

        RegisterLines(shadowAreaLines);
        RegisterLines(fullShadowLines);
        RegisterLines(partialShadowLines);
    }

    private void RegisterLines(LineRenderer[] lines)
    {
        if (lines == null)
            return;

        foreach (LineRenderer line in lines)
        {
            RegisterLine(line);
        }
    }

    private void RegisterLine(LineRenderer line)
    {
        if (line == null)
            return;

        if (GetOriginalState(line) != null)
            return;

        if (line.sharedMaterial == null)
        {
            line.material = CreateDefaultLineMaterial();
        }
        else
        {
            line.material = new Material(line.sharedMaterial);
        }

        LineOriginalState state = new LineOriginalState
        {
            line = line,
            startWidth = line.startWidth,
            endWidth = line.endWidth,
            startColor = line.startColor,
            endColor = line.endColor,
            materialInstance = line.material,
            materialColor = GetMaterialColor(line.material)
        };

        originalLineStates.Add(state);
    }

    private LineOriginalState GetOriginalState(LineRenderer line)
    {
        for (int i = 0; i < originalLineStates.Count; i++)
        {
            if (originalLineStates[i].line == line)
                return originalLineStates[i];
        }

        return null;
    }

    private Material CreateDefaultLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        Material mat = new Material(shader);
        SetMaterialColor(mat, Color.white);

        return mat;
    }

    private Color GetMaterialColor(Material mat)
    {
        if (mat == null)
            return Color.white;

        if (mat.HasProperty("_BaseColor"))
            return mat.GetColor("_BaseColor");

        if (mat.HasProperty("_Color"))
            return mat.GetColor("_Color");

        return Color.white;
    }

    private void SetMaterialColor(Material mat, Color color)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);
    }

    private void SetMarker(GameObject marker, TMP_Text labelText, string labelValue, bool show)
    {
        if (marker != null)
            marker.SetActive(show);

        if (labelText != null && !string.IsNullOrWhiteSpace(labelValue))
            labelText.text = labelValue;
    }

    private void ShowContinueButton(bool show)
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(show);
            continueButton.interactable = show;
        }

        if (continueButtonText != null)
            continueButtonText.text = continueButtonLabel;
    }

    private void ResetVisuals()
    {
        RestoreLines(shadowAreaLines);
        RestoreLines(fullShadowLines);
        RestoreLines(partialShadowLines);

        SetMarker(fullShadowMarker, fullShadowLabelText, fullShadowLabel, false);
        SetMarker(partialShadowMarker, partialShadowLabelText, partialShadowLabel, false);

        ShowContinueButton(false);
    }

    [ContextMenu("Test Shadow Area Now")]
    private void TestShadowAreaNow()
    {
        StartCoroutine(HighlightLinesForDuration(shadowAreaLines, 2f));
    }

    [ContextMenu("Test Full Shadow Marker Now")]
    private void TestFullShadowMarkerNow()
    {
        SetMarker(fullShadowMarker, fullShadowLabelText, fullShadowLabel, true);
    }

    [ContextMenu("Test Partial Shadow Marker Now")]
    private void TestPartialShadowMarkerNow()
    {
        SetMarker(partialShadowMarker, partialShadowLabelText, partialShadowLabel, true);
    }
}