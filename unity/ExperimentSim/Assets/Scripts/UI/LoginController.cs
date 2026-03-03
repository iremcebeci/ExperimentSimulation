using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

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


        // Şifre alanlarını gizleme kodu

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

        PlayerPrefs.SetString("auth_user", serverMsg);
        PlayerPrefs.Save();

        statusLabel.text = "Giriş başarılı!";

        SetInteractable(true);

        string role = ExtractRole(serverMsg);
        router.ShowDashboardByRole(role);
    }

    [System.Serializable]
    private class LoginRequest
    {
        public string Email;
        public string Password;
    }

    private string ExtractRole(string json)
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
        int i = json.IndexOf(pattern);
        if (i < 0) return null;

        i = json.IndexOf(':', i);
        if (i < 0) return null;
        i++;


        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;


        if (i >= json.Length || json[i] != '\"') return null;
        i++;

        int j = json.IndexOf('\"', i);
        if (j < 0) return null;

        return json.Substring(i, j - i);
    }

    private int? ExtractJsonInt(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

        string pattern = $"\"{key}\"";
        int i = json.IndexOf(pattern);
        if (i < 0) return null;

        i = json.IndexOf(':', i);
        if (i < 0) return null;
        i++;

        while (i < json.Length && (json[i] == ' ')) i++;

        int j = i;
        while (j < json.Length && (char.IsDigit(json[j]) || json[j] == '-')) j++;

        if (j <= i) return null;

        if (int.TryParse(json.Substring(i, j - i), out int val))
            return val;

        return null;
    }
}