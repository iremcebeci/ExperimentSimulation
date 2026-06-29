using UnityEngine;

public static class AssignmentSession
{
    public static bool IsAssignmentMode;

    public static int AssignmentId;
    public static int ExperimentId;

    public static string AssignmentTitle;
    public static string ExperimentName;
    public static string SceneName;

    public static string AccessToken;

    private const string TokenKey = "AccessToken";

    public static void StartAssignment(
        int assignmentId,
        int experimentId,
        string assignmentTitle,
        string experimentName,
        string sceneName,
        string accessToken
    )
    {
        IsAssignmentMode = true;
        AssignmentId = assignmentId;
        ExperimentId = experimentId;
        AssignmentTitle = assignmentTitle;
        ExperimentName = experimentName;
        SceneName = sceneName;
        AccessToken = accessToken;

        // Token kaybolmasın diye PlayerPrefs'e de yazıyoruz.
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            PlayerPrefs.SetString(TokenKey, accessToken);
            PlayerPrefs.Save();
        }
    }

    public static string GetToken()
    {
        if (!string.IsNullOrWhiteSpace(AccessToken))
            return AccessToken;

        return PlayerPrefs.GetString(TokenKey, "");
    }

    // Deney / ödev bilgilerini temizler ama kullanıcı oturumunu silmez.
    public static void ClearAssignmentOnly()
    {
        IsAssignmentMode = false;
        AssignmentId = 0;
        ExperimentId = 0;
        AssignmentTitle = "";
        ExperimentName = "";
        SceneName = "";
    }

    // Bunu sadece gerçekten logout yaparken kullan.
    public static void ClearAll()
    {
        ClearAssignmentOnly();

        AccessToken = "";
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.Save();
    }
}