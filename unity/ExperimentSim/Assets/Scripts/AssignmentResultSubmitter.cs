using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AssignmentResultSubmitter : MonoBehaviour
{
    [Header("API")]
    public string apiBaseUrl = "http://localhost:5156";
    public string submitPath = "/api/AssignmentResult";

    [Header("Return")]
    public string dashboardSceneName = "DashboardScene";

    private bool isSubmitting = false;

    private static readonly List<AssignmentAnswerRequest> pendingAnswers = new List<AssignmentAnswerRequest>();

    public static void ClearAnswers()
    {
        pendingAnswers.Clear();
    }

    public static void AddAnswer(string questionText, string studentAnswer, string correctAnswer, bool isCorrect)
    {
        pendingAnswers.Add(new AssignmentAnswerRequest
        {
            questionText = string.IsNullOrWhiteSpace(questionText) ? "-" : questionText,
            studentAnswer = string.IsNullOrWhiteSpace(studentAnswer) ? "-" : studentAnswer,
            correctAnswer = string.IsNullOrWhiteSpace(correctAnswer) ? "-" : correctAnswer,
            isCorrect = isCorrect
        });

        Debug.Log($"[ASSIGNMENT ANSWER ADDED] Question: {questionText} | Student: {studentAnswer} | Correct: {correctAnswer} | IsCorrect: {isCorrect}");
    }

    public void SubmitResultAndReturn()
    {
        if (isSubmitting)
            return;

        StartCoroutine(SubmitAndReturnRoutine());
    }

    private IEnumerator SubmitAndReturnRoutine()
    {
        isSubmitting = true;

        if (!AssignmentSession.IsAssignmentMode)
        {
            Debug.Log("Ödev modu değil. Dashboarda dönülüyor.");
            ReturnToDashboard();
            yield break;
        }

        if (ExperimentResultTracker.Instance == null)
        {
            Debug.LogWarning("ExperimentResultTracker bulunamadı. Dashboarda dönülüyor.");
            ReturnToDashboard();
            yield break;
        }

        var tracker = ExperimentResultTracker.Instance;

        int totalQuestionCount = tracker.totalQuestionCount;

        if (totalQuestionCount <= 0)
            totalQuestionCount = tracker.correctCount + tracker.wrongCount;

        var payload = new AssignmentResultRequest
        {
            assignmentId = AssignmentSession.AssignmentId,
            correctCount = tracker.correctCount,
            wrongCount = tracker.wrongCount,
            totalQuestionCount = totalQuestionCount,
            score = tracker.GetScore(),
            answers = pendingAnswers.ToArray()
        };

        Debug.Log($"[ASSIGNMENT ANSWERS COUNT] {pendingAnswers.Count}");

        string json = JsonUtility.ToJson(payload);
        string url = apiBaseUrl + submitPath;

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        string token = AssignmentSession.GetToken();

        if (!string.IsNullOrWhiteSpace(token))
            req.SetRequestHeader("Authorization", "Bearer " + token);

        Debug.Log("[ASSIGNMENT RESULT] POST => " + json);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[ASSIGNMENT RESULT] FAILED: " + req.responseCode + " => " + req.downloadHandler.text);
        }
        else
        {
            Debug.Log("[ASSIGNMENT RESULT] OK: " + req.downloadHandler.text);
        }

        ReturnToDashboard();
    }

    private void ReturnToDashboard()
    {
        ClearAnswers();

        AssignmentSession.ClearAssignmentOnly();

        SceneManager.LoadScene(dashboardSceneName);
    }

    [Serializable]
    private class AssignmentResultRequest
    {
        public int assignmentId;
        public int correctCount;
        public int wrongCount;
        public int totalQuestionCount;
        public int score;
        public AssignmentAnswerRequest[] answers;
    }

    [Serializable]
    private class AssignmentAnswerRequest
    {
        public string questionText;
        public string studentAnswer;
        public string correctAnswer;
        public bool isCorrect;
    }
}