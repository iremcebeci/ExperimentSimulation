using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObservationQuizPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject observationPanel;
    public Button observationButton;
    public Button closeButton;

    [Header("Soru 1 - Dünya")]
    public TMP_InputField earthInput;
    public TMP_Text earthFeedback;

    [Header("Soru 2 - Ay")]
    public TMP_InputField moonInput;
    public TMP_Text moonFeedback;

    [Header("Soru 3 - Güneş Tutulmaları")]
    public TMP_InputField solarInput1;
    public TMP_InputField solarInput2;
    public TMP_Text solarFeedback;

    [Header("Soru 4 - Ay Tutulmaları")]
    public TMP_InputField lunarInput1;
    public TMP_InputField lunarInput2;
    public TMP_Text lunarFeedback;

    [Header("Genel")]
    public TMP_Text generalFeedback;
    public Button checkButton;
    public Button clearButton;

    private bool observationAnswersSaved = false;
    private bool observationScoreSaved = false;

    private void Start()
    {
        if (observationPanel != null)
            observationPanel.SetActive(false);

        if (observationButton != null)
        {
            observationButton.onClick.RemoveAllListeners();
            observationButton.onClick.AddListener(TogglePanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (checkButton != null)
        {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(CheckAnswers);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(ClearInputs);
        }

        ClearFeedbacks();
    }

    private void TogglePanel()
    {
        if (observationPanel == null)
            return;

        observationPanel.SetActive(!observationPanel.activeSelf);
    }

    private void ClosePanel()
    {
        if (observationPanel != null)
            observationPanel.SetActive(false);
    }

    private void CheckAnswers()
    {
        string earthAnswer = earthInput != null ? earthInput.text : "";
        string moonAnswer = moonInput != null ? moonInput.text : "";

        string solarAnswer1 = solarInput1 != null ? solarInput1.text : "";
        string solarAnswer2 = solarInput2 != null ? solarInput2.text : "";

        string lunarAnswer1 = lunarInput1 != null ? lunarInput1.text : "";
        string lunarAnswer2 = lunarInput2 != null ? lunarInput2.text : "";

        bool earthCorrect = IsSameWord(earthAnswer, "gunes");
        bool moonCorrect = IsSameWord(moonAnswer, "dunya");

        bool solarCorrect = AreTwoDatesCorrect(
            solarAnswer1,
            solarAnswer2,
            "17-02-2026",
            "12-08-2026"
        );

        bool lunarCorrect = AreTwoDatesCorrect(
            lunarAnswer1,
            lunarAnswer2,
            "03-03-2026",
            "28-08-2026"
        );

        SetFeedback(earthFeedback, earthCorrect, "Doğru cevap: Güneş");
        SetFeedback(moonFeedback, moonCorrect, "Doğru cevap: Dünya");
        SetFeedback(solarFeedback, solarCorrect, "Doğru cevaplar: 17-02-2026 ve 12-08-2026");
        SetFeedback(lunarFeedback, lunarCorrect, "Doğru cevaplar: 03-03-2026 ve 28-08-2026");

        bool allCorrect = earthCorrect && moonCorrect && solarCorrect && lunarCorrect;

        if (generalFeedback != null)
        {
            generalFeedback.text = allCorrect
                ? "Tebrikler, tüm gözlem soruları doğru."
                : "Bazı cevaplar hatalı veya eksik. Tekrar kontrol et.";

            generalFeedback.color = allCorrect
                ? new Color32(70, 190, 110, 255)
                : new Color32(230, 150, 60, 255);
        }

        SaveObservationAnswersOnce(
            earthAnswer,
            moonAnswer,
            solarAnswer1,
            solarAnswer2,
            lunarAnswer1,
            lunarAnswer2,
            earthCorrect,
            moonCorrect,
            solarCorrect,
            lunarCorrect
        );

        SaveScoreOnce(earthCorrect, moonCorrect, solarCorrect, lunarCorrect);
    }

    private void SaveObservationAnswersOnce(
        string earthAnswer,
        string moonAnswer,
        string solarAnswer1,
        string solarAnswer2,
        string lunarAnswer1,
        string lunarAnswer2,
        bool earthCorrect,
        bool moonCorrect,
        bool solarCorrect,
        bool lunarCorrect)
    {
        if (observationAnswersSaved)
            return;

        observationAnswersSaved = true;

        AssignmentResultSubmitter.AddAnswer(
            "Dünya hangi gök cisminin etrafında döner?",
            earthAnswer,
            "Güneş",
            earthCorrect
        );

        AssignmentResultSubmitter.AddAnswer(
            "Ay hangi gök cisminin etrafında döner?",
            moonAnswer,
            "Dünya",
            moonCorrect
        );

        AssignmentResultSubmitter.AddAnswer(
            "2026 yılında gerçekleşecek güneş tutulmalarını bulunuz.",
            solarAnswer1 + ", " + solarAnswer2,
            "17-02-2026, 12-08-2026",
            solarCorrect
        );

        AssignmentResultSubmitter.AddAnswer(
            "2026 yılında gerçekleşecek ay tutulmalarını bulunuz.",
            lunarAnswer1 + ", " + lunarAnswer2,
            "03-03-2026, 28-08-2026",
            lunarCorrect
        );

        Debug.Log("[OBSERVATION QUIZ] Gözlem cevapları kaydedildi.");
    }

    private void SaveScoreOnce(
        bool earthCorrect,
        bool moonCorrect,
        bool solarCorrect,
        bool lunarCorrect)
    {
        if (observationScoreSaved)
            return;

        observationScoreSaved = true;

        int correctCount = 0;
        int wrongCount = 0;

        if (earthCorrect) correctCount++; else wrongCount++;
        if (moonCorrect) correctCount++; else wrongCount++;
        if (solarCorrect) correctCount++; else wrongCount++;
        if (lunarCorrect) correctCount++; else wrongCount++;

        if (ExperimentResultTracker.Instance != null)
        {
            ExperimentResultTracker.Instance.AddMultipleResults(correctCount, wrongCount);
        }

        Debug.Log($"[OBSERVATION QUIZ SCORE] Doğru: {correctCount} | Yanlış: {wrongCount}");
    }

    private void SetFeedback(TMP_Text feedback, bool correct, string correctAnswer)
    {
        if (feedback == null)
            return;

        if (correct)
        {
            feedback.text = "✓ Doğru";
            feedback.color = new Color32(70, 190, 110, 255);
        }
        else
        {
            feedback.text = "✕ Yanlış / Eksik. " + correctAnswer;
            feedback.color = new Color32(220, 80, 80, 255);
        }
    }

    private bool IsSameWord(string input, string expectedNormalized)
    {
        string normalized = NormalizeTurkish(input);
        return normalized == expectedNormalized;
    }

    private string NormalizeTurkish(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string text = value.Trim().ToLowerInvariant();

        text = text.Replace("ı", "i");
        text = text.Replace("İ", "i");
        text = text.Replace("ğ", "g");
        text = text.Replace("ü", "u");
        text = text.Replace("ş", "s");
        text = text.Replace("ö", "o");
        text = text.Replace("ç", "c");

        return text;
    }

    private bool AreTwoDatesCorrect(string input1, string input2, string correctDate1, string correctDate2)
    {
        string a = NormalizeDateText(input1);
        string b = NormalizeDateText(input2);

        string c1 = NormalizeDateText(correctDate1);
        string c2 = NormalizeDateText(correctDate2);

        bool normalOrder = a == c1 && b == c2;
        bool reverseOrder = a == c2 && b == c1;

        return normalOrder || reverseOrder;
    }

    private string NormalizeDateText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value
            .Trim()
            .Replace(".", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("_", "-")
            .Replace(" ", "");
    }

    private void ClearInputs()
    {
        if (earthInput != null) earthInput.text = "";
        if (moonInput != null) moonInput.text = "";

        if (solarInput1 != null) solarInput1.text = "";
        if (solarInput2 != null) solarInput2.text = "";

        if (lunarInput1 != null) lunarInput1.text = "";
        if (lunarInput2 != null) lunarInput2.text = "";

        ClearFeedbacks();

        observationAnswersSaved = false;
        observationScoreSaved = false;
    }

    private void ClearFeedbacks()
    {
        if (earthFeedback != null) earthFeedback.text = "";
        if (moonFeedback != null) moonFeedback.text = "";
        if (solarFeedback != null) solarFeedback.text = "";
        if (lunarFeedback != null) lunarFeedback.text = "";
        if (generalFeedback != null) generalFeedback.text = "";
    }
}