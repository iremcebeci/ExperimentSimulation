using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SubtitleLine
{
    [TextArea(2, 4)]
    public string text;
    public float startTime;
    public float endTime;
}

[RequireComponent(typeof(AudioSource))]
public class StepAudioPlayer : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Subtitle UI")]
    public TMP_Text subtitleText;

    [Header("Continue Button")]
    public Button continueButton;

    [Header("Subtitles")]
    public SubtitleLine[] subtitles;

    private Coroutine subtitleRoutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        PlayAudioWithSubtitles();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (subtitleRoutine != null)
        {
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        ClearSubtitle();

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    public void PlayAudioWithSubtitles()
    {
        if (audioSource == null)
            return;

        if (subtitleRoutine != null)
            StopCoroutine(subtitleRoutine);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.Play();

        subtitleRoutine = StartCoroutine(SubtitleRoutine());
    }

    private IEnumerator SubtitleRoutine()
    {
        ClearSubtitle();

        while (audioSource != null && audioSource.isPlaying)
        {
            float currentTime = audioSource.time;
            string currentSubtitle = "";

            for (int i = 0; i < subtitles.Length; i++)
            {
                if (currentTime >= subtitles[i].startTime && currentTime <= subtitles[i].endTime)
                {
                    currentSubtitle = subtitles[i].text;
                    break;
                }
            }

            if (subtitleText != null)
                subtitleText.text = currentSubtitle;

            yield return null;
        }

        ClearSubtitle();

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    private void ClearSubtitle()
    {
        if (subtitleText != null)
            subtitleText.text = "";
    }
}