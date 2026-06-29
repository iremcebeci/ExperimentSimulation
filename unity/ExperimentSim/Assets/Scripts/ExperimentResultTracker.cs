using UnityEngine;

public class ExperimentResultTracker : MonoBehaviour
{
    public static ExperimentResultTracker Instance;

    public int correctCount;
    public int wrongCount;
    public int totalQuestionCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddSingleResult(bool isCorrect)
    {
        totalQuestionCount++;

        if (isCorrect)
            correctCount++;
        else
            wrongCount++;
    }

    public void AddMultipleResults(int correct, int wrong)
    {
        correctCount += Mathf.Max(0, correct);
        wrongCount += Mathf.Max(0, wrong);
        totalQuestionCount = correctCount + wrongCount;
    }

    public int GetScore()
    {
        if (totalQuestionCount <= 0)
            return 0;

        return Mathf.RoundToInt((float)correctCount / totalQuestionCount * 100f);
    }
}