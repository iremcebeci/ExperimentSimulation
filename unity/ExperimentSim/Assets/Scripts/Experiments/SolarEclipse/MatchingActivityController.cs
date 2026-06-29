using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class MatchingActivityController : MonoBehaviour
{
    [Header("Placeholder Alanları")]
    public MatchPlaceholder[] placeholders;

    [Header("Butonlar")]
    public Button checkButton;
    public Button continueButton;

    [Header("Genel Feedback Görselleri")]
    public GameObject correctFeedback;
    public GameObject wrongFeedback;

    [Header("Satır Satır Doğru İkonları")]
    public GameObject[] correctIcons;

    [Header("Satır Satır Yanlış Grupları")]
    public GameObject[] wrongAnswerGroups;

    [Header("Yanlış Cevapta Gösterilecek Doğru Cevap Yazıları")]
    public TMP_Text[] correctAnswerTexts;

    [Header("Öğretmen Sonuç Sayfasında Görünecek Soru Metinleri")]
    public string[] questionTexts =
    {
        "Tam tutulma sırasında Ay'ın tamamen girdiği gölge bölgesi hangisidir?",
        "Tutulmaların ışık kaynağı olan gök cismi hangisidir?",
        "Dünya'nın Güneş ile Ay arasına girmesiyle oluşan tutulma hangisidir?",
        "Ay'ın Güneş ile Dünya arasına girmesiyle oluşan tutulma hangisidir?",
        "Güneş tutulmasında Güneş'in önüne geçen gök cismi hangisidir?",
        "Tam Ay tutulmasında Ay hangi renkte görünebilir?",
        "Parçalı tutulma ile ilişkili gölge bölgesi hangisidir?",
        "Ay tutulmasında Güneş ile Ay arasında bulunan gök cismi hangisidir?"
    };

    [Header("Doğru Cevap Metinleri")]
    public string[] correctAnswerMessages =
    {
        "Doğru cevap: Tam Gölge",
        "Doğru cevap: Güneş",
        "Doğru cevap: Ay Tutulması",
        "Doğru cevap: Güneş Tutulması",
        "Doğru cevap: Ay",
        "Doğru cevap: Bakır / Kırmızı Görünüm",
        "Doğru cevap: Yarı Gölge",
        "Doğru cevap: Dünya"
    };

    [Header("Ses")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    private bool answersSaved = false;

    private void Awake()
    {
        if (checkButton != null)
        {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(CheckAnswers);
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        ResetActivity();
        PlayClip(introClip);
    }

    private void ResetActivity()
    {
        answersSaved = false;

        if (correctFeedback != null)
            correctFeedback.SetActive(false);

        if (wrongFeedback != null)
            wrongFeedback.SetActive(false);

        SetArrayActive(correctIcons, false);
        SetArrayActive(wrongAnswerGroups, false);

        if (correctAnswerTexts != null)
        {
            foreach (TMP_Text text in correctAnswerTexts)
            {
                if (text != null)
                    text.text = "";
            }
        }

        if (checkButton != null)
        {
            checkButton.gameObject.SetActive(true);
            checkButton.interactable = true;
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.interactable = true;
        }
    }

    public void CheckAnswers()
    {
        if (placeholders == null || placeholders.Length == 0)
        {
            Debug.LogWarning("Placeholder listesi boş.");
            return;
        }

        bool allCorrect = true;

        int correctCount = 0;
        int wrongCount = 0;

        for (int i = 0; i < placeholders.Length; i++)
        {
            bool isCorrect = false;

            if (placeholders[i] != null)
                isCorrect = placeholders[i].IsCorrect();

            if (isCorrect)
            {
                correctCount++;
            }
            else
            {
                wrongCount++;
                allCorrect = false;
            }

            if (!answersSaved)
            {
                AssignmentResultSubmitter.AddAnswer(
                    GetQuestionText(i),
                    GetStudentAnswerText(placeholders[i]),
                    GetCorrectAnswerText(i),
                    isCorrect
                );
            }

            ShowRowResult(i, isCorrect);
        }

        answersSaved = true;

        if (ExperimentResultTracker.Instance != null)
        {
            ExperimentResultTracker.Instance.AddMultipleResults(correctCount, wrongCount);
        }
        else
        {
            Debug.LogWarning("ExperimentResultTracker sahnede bulunamadı.");
        }

        Debug.Log("Etkinlik 2 sonucu | Doğru: " + correctCount + " | Yanlış: " + wrongCount);

        if (correctFeedback != null)
            correctFeedback.SetActive(allCorrect);

        if (wrongFeedback != null)
            wrongFeedback.SetActive(!allCorrect);

        if (checkButton != null)
            checkButton.gameObject.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        if (allCorrect)
            PlayClip(correctClip);
        else
            PlayClip(wrongClip);
    }

    private void ShowRowResult(int index, bool isCorrect)
    {
        if (correctIcons != null && index < correctIcons.Length && correctIcons[index] != null)
            correctIcons[index].SetActive(isCorrect);

        if (wrongAnswerGroups != null && index < wrongAnswerGroups.Length && wrongAnswerGroups[index] != null)
            wrongAnswerGroups[index].SetActive(!isCorrect);

        if (!isCorrect && correctAnswerTexts != null && index < correctAnswerTexts.Length && correctAnswerTexts[index] != null)
        {
            if (correctAnswerMessages != null && index < correctAnswerMessages.Length)
                correctAnswerTexts[index].text = correctAnswerMessages[index];
        }
    }

    private string GetQuestionText(int index)
    {
        if (questionTexts != null && index >= 0 && index < questionTexts.Length && !string.IsNullOrWhiteSpace(questionTexts[index]))
            return questionTexts[index];

        return "Eşleştirme sorusu " + (index + 1);
    }

    private string GetStudentAnswerText(MatchPlaceholder placeholder)
    {
        if (placeholder == null)
            return "Boş";

        MatchCard card = placeholder.CurrentCard;

        if (card == null)
            card = placeholder.GetComponentInChildren<MatchCard>();

        if (card == null)
            return "Boş";

        TMP_Text cardText = card.GetComponentInChildren<TMP_Text>();

        if (cardText != null && !string.IsNullOrWhiteSpace(cardText.text))
            return cardText.text;

        if (!string.IsNullOrWhiteSpace(card.answerId))
            return card.answerId;

        return "Boş";
    }

    private string GetCorrectAnswerText(int index)
    {
        if (correctAnswerMessages != null && index >= 0 && index < correctAnswerMessages.Length && !string.IsNullOrWhiteSpace(correctAnswerMessages[index]))
        {
            return correctAnswerMessages[index]
                .Replace("Doğru cevap:", "")
                .Trim();
        }

        if (placeholders != null && index >= 0 && index < placeholders.Length && placeholders[index] != null)
            return placeholders[index].expectedAnswerId;

        return "-";
    }

    private void SetArrayActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource bağlı değil.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("AudioClip bağlı değil.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}