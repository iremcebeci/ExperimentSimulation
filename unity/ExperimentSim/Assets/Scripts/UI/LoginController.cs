using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;

public class LoginController : MonoBehaviour
{
    [SerializeField] private string loginPath = "/api/Auth/login";

    private AppRouter router;
    private VisualElement view;

    private TextField emailTf;
    private TextField passwordTf;
    private Button loginBtn;
    private Button toSignupBtn;

    private Label statusLabel;

    public void Bind(AppRouter router, VisualElement loginView)
    {
        this.router = router;
        this.view = loginView;

        emailTf = view.Q<TextField>("email") ?? view.Q<TextField>("Email");
        passwordTf = view.Q<TextField>("password") ?? view.Q<TextField>("Password");
        loginBtn = view.Q<Button>("loginBtn");
        toSignupBtn = view.Q<Button>("toSignupBtn");

        if (loginBtn == null) Debug.LogError("loginBtn not found. Add name=\"loginBtn\" to your login button.");
        if (emailTf == null) Debug.LogWarning("Email TextField not found (name=\"email\").");
        if (passwordTf == null) Debug.LogWarning("Password TextField not found (name=\"password\").");

        if (passwordTf != null)
        {
            passwordTf.isPasswordField = true;
            passwordTf.maskChar = '•';
        }

        statusLabel = new Label("");
        statusLabel.name = "login-status";
        statusLabel.style.marginTop = 8;
        statusLabel.style.whiteSpace = WhiteSpace.Normal;

        if (loginBtn != null && loginBtn.parent != null)
            loginBtn.parent.Add(statusLabel);
        else
            view.Add(statusLabel);

        if (toSignupBtn != null) toSignupBtn.clicked += () => router.ShowRegister();
        if (loginBtn != null) loginBtn.clicked += OnLoginClicked;
    }

    private void OnLoginClicked()
    {
        statusLabel.text = "";

        string email = emailTf?.value?.Trim() ?? "";
        string pass = passwordTf?.value ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
        {
            statusLabel.text = "Email ve şifre zorunlu.";
            return;
        }

        SetInteractable(false);
        statusLabel.text = "Giriş yapılıyor...";

        var req = new LoginRequest { Email = email, Password = pass };
        StartCoroutine(LoginCoroutine(req));
    }

    private void SetInteractable(bool enabled)
    {
        if (loginBtn != null) loginBtn.SetEnabled(enabled);
        if (toSignupBtn != null) toSignupBtn.SetEnabled(enabled);
    }

    private IEnumerator LoginCoroutine(LoginRequest req)
    {
        if (router == null)
        {
            statusLabel.text = "Router yok.";
            yield break;
        }

        string url = router.ApiBaseUrl + loginPath;
        string json = JsonUtility.ToJson(req);
        byte[] body = Encoding.UTF8.GetBytes(json);

        Debug.Log("LOGIN URL => " + url);
        Debug.Log("LOGIN JSON => " + json);

        using var uwr = new UnityWebRequest(url, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(body);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        bool ok = uwr.result == UnityWebRequest.Result.Success &&
                  (uwr.responseCode >= 200 && uwr.responseCode < 300);

        string serverMsg = uwr.downloadHandler != null ? uwr.downloadHandler.text : "";

        if (!ok)
        {
            Debug.LogError($"LOGIN FAILED {(int)uwr.responseCode} => {serverMsg}");
            statusLabel.text = $"Giriş başarısız. ({uwr.responseCode})\n{serverMsg}";
            SetInteractable(true);
            yield break;
        }

        Debug.Log("LOGIN OK => " + serverMsg);

        string token =
            ExtractJsonString(serverMsg, "accessToken") ??
            ExtractJsonString(serverMsg, "access_token") ??
            ExtractJsonString(serverMsg, "token") ??
            ExtractJsonString(serverMsg, "Token") ??
            ExtractNestedObjectString(serverMsg, "data", "accessToken") ??
            ExtractNestedObjectString(serverMsg, "data", "token") ??
            "";

        int userId =
            ExtractJsonInt(serverMsg, "userId") ??
            ExtractJsonInt(serverMsg, "id") ??
            ExtractJsonInt(serverMsg, "Id") ??
            ExtractNestedObjectInt(serverMsg, "user", "id") ??
            ExtractNestedObjectInt(serverMsg, "User", "Id") ??
            ExtractNestedObjectInt(serverMsg, "data", "id") ??
            ExtractNestedObjectInt(serverMsg, "data", "userId") ??
            0;

        string name =
            ExtractJsonString(serverMsg, "name") ??
            ExtractJsonString(serverMsg, "Name") ??
            ExtractNestedObjectString(serverMsg, "user", "name") ??
            ExtractNestedObjectString(serverMsg, "user", "Name") ??
            "";

        string surname =
            ExtractJsonString(serverMsg, "surname") ??
            ExtractJsonString(serverMsg, "Surname") ??
            ExtractNestedObjectString(serverMsg, "user", "surname") ??
            ExtractNestedObjectString(serverMsg, "user", "Surname") ??
            "";

        int roleId =
            ExtractJsonInt(serverMsg, "roleId") ??
            ExtractJsonInt(serverMsg, "RoleId") ??
            ExtractNestedObjectInt(serverMsg, "user", "roleId") ??
            ExtractNestedObjectInt(serverMsg, "user", "RoleId") ??
            0;

        string roleName =
            ExtractJsonString(serverMsg, "roleName") ??
            ExtractJsonString(serverMsg, "RoleName") ??
            ExtractNestedObjectString(serverMsg, "user", "roleName") ??
            ExtractNestedObjectString(serverMsg, "user", "RoleName") ??
            ExtractNestedObjectString(serverMsg, "role", "name") ??
            ExtractNestedObjectString(serverMsg, "Role", "Name") ??
            ExtractRoleFallback(serverMsg);


        router.SetSession(userId, token, name, surname, roleId, roleName);

        Debug.Log($"[SESSION] userId={router.CurrentUserId} token={(string.IsNullOrEmpty(router.AccessToken) ? "EMPTY" : "OK")} role={router.CurrentRoleName}");

        if (string.IsNullOrEmpty(token))
            Debug.LogWarning("LOGIN response token içermiyor (accessToken/token). Auth login response'u kontrol et.");

        if (userId <= 0)
            Debug.LogWarning("LOGIN response user id içermiyor (id/userId/user.id). Response'u kontrol et.");

        PlayerPrefs.SetString("auth_user", serverMsg);
        PlayerPrefs.Save();

        statusLabel.text = "Giriş başarılı!";
        SetInteractable(true);

        Debug.Log($"ROLE DEBUG => Id:{roleId}, Name:{roleName}");
        router.ShowDashboardByRole(roleName, roleId);

    }

    [System.Serializable]
    private class LoginRequest
    {
        public string Email;
        public string Password;
    }


    private string ExtractRoleFallback(string json)
    {
        string role =
            ExtractJsonString(json, "RoleName") ??
            ExtractJsonString(json, "roleName") ??
            ExtractJsonString(json, "Role") ??
            ExtractJsonString(json, "role");

        if (!string.IsNullOrEmpty(role))
            return role;

        int roleId =
            ExtractJsonInt(json, "RoleId") ??
            ExtractJsonInt(json, "roleId") ??
            0;

        return roleId switch
        {
            1 => "Student",
            2 => "Teacher",
            3 => "Independent",
            4 => "ContentCreator",
            5 => "Admin",
            _ => "Independent"
        };
    }







    private string ExtractJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

        string pattern = $"\"{key}\"";
        int i = json.IndexOf(pattern, StringComparison.Ordinal);
        if (i < 0) return null;

        i = json.IndexOf(':', i);
        if (i < 0) return null;
        i++;

        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length) return null;

        if (json[i] != '\"') return null;
        i++;

        int j = json.IndexOf('\"', i);
        if (j < 0) return null;

        return json.Substring(i, j - i);
    }

    private int? ExtractJsonInt(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

        string pattern = $"\"{key}\"";
        int i = json.IndexOf(pattern, StringComparison.Ordinal);
        if (i < 0) return null;

        i = json.IndexOf(':', i);
        if (i < 0) return null;
        i++;

        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

        int j = i;
        while (j < json.Length && (char.IsDigit(json[j]) || json[j] == '-')) j++;

        if (j <= i) return null;

        return int.TryParse(json.Substring(i, j - i), out int val) ? val : null;
    }

    private string ExtractNestedObjectString(string json, string objectKey, string innerKey)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(objectKey) || string.IsNullOrEmpty(innerKey))
            return null;

        int i = json.IndexOf($"\"{objectKey}\"", StringComparison.Ordinal);
        if (i < 0) return null;

        i = json.IndexOf('{', i);
        if (i < 0) return null;

        return ExtractJsonString(json.Substring(i), innerKey);
    }

    private int? ExtractNestedObjectInt(string json, string objectKey, string innerKey)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(objectKey) || string.IsNullOrEmpty(innerKey))
            return null;

        int i = json.IndexOf($"\"{objectKey}\"", StringComparison.Ordinal);
        if (i < 0) return null;

        i = json.IndexOf('{', i);
        if (i < 0) return null;

        return ExtractJsonInt(json.Substring(i), innerKey);
    }
}