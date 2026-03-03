using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class RegisterController : MonoBehaviour
{
    [Header("API Endpoint")]
    [SerializeField] private string registerPath = "/api/User";

    [Header("Default Role")]
    [Tooltip("1=Student, 2=Teacher, 3=Independent, 4=ContentCreator, 5=Admin")]
    [SerializeField] private int defaultRoleId = 1;

    private AppRouter router;
    private VisualElement view;

    private TextField firstNameTf, lastNameTf, emailTf, birthDateTf, phoneTf, classCodeTf, passwordTf, password2Tf;
    private Button submitBtn, goLoginBtn;

    private Label statusLabel;

    public void Bind(AppRouter router, VisualElement registerView)
    {
        this.router = router;
        this.view = registerView;

        goLoginBtn = view.Q<Button>("go-login");
        submitBtn = view.Q<Button>("signup-submit");

        firstNameTf = view.Q<TextField>("firstName");
        lastNameTf = view.Q<TextField>("lastName");
        emailTf = view.Q<TextField>("email");
        birthDateTf = view.Q<TextField>("birthDate");
        phoneTf = view.Q<TextField>("phone");
        classCodeTf = view.Q<TextField>("classCode");
        passwordTf = view.Q<TextField>("password");
        password2Tf = view.Q<TextField>("password2");

        if (submitBtn == null) Debug.LogError("signup-submit button not found in Register.uxml (name=\"signup-submit\")");
        if (goLoginBtn == null) Debug.LogWarning("go-login button not found in Register.uxml (name=\"go-login\")");


        statusLabel = view.Q<Label>("register-status");
        if (statusLabel == null)
        {
            statusLabel = new Label("");
            statusLabel.name = "register-status";
            statusLabel.style.marginTop = 8;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            statusLabel.style.maxWidth = Length.Percent(100);

            if (submitBtn != null && submitBtn.parent != null)
                submitBtn.parent.Add(statusLabel);
            else
                view.Add(statusLabel);
        }
        else
        {
            statusLabel.text = "";
        }

        if (goLoginBtn != null)
        {
            goLoginBtn.clicked -= OnGoLoginClicked;
            goLoginBtn.clicked += OnGoLoginClicked;
        }

        if (submitBtn != null)
        {
            submitBtn.clicked -= OnSubmitClicked;
            submitBtn.clicked += OnSubmitClicked;
        }
    }

    private void OnGoLoginClicked()
    {
        router?.ShowLogin();
    }

    private void OnSubmitClicked()
    {
        statusLabel.text = "";

        string firstName = firstNameTf?.value?.Trim() ?? "";
        string lastName = lastNameTf?.value?.Trim() ?? "";
        string email = emailTf?.value?.Trim() ?? "";
        string birthRaw = birthDateTf?.value?.Trim() ?? "";
        string phone = phoneTf?.value?.Trim() ?? "";
        string classCode = classCodeTf?.value?.Trim() ?? "";
        string pass1 = passwordTf?.value ?? "";
        string pass2 = password2Tf?.value ?? "";

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(pass1) ||
            string.IsNullOrWhiteSpace(pass2))
        {
            statusLabel.text = "Lütfen zorunlu alanları doldurun.";
            return;
        }

        if (!email.Contains("@"))
        {
            statusLabel.text = "E-posta formatı geçersiz.";
            return;
        }

        if (pass1.Length < 6)
        {
            statusLabel.text = "Şifre en az 6 karakter olmalı.";
            return;
        }

        if (pass1 != pass2)
        {
            statusLabel.text = "Şifreler eşleşmiyor.";
            return;
        }

        string birthIso = NormalizeBirthDate(birthRaw);

        var req = new CreateUserRequest
        {
            Name = firstName,
            Surname = lastName,
            Email = email,
            Password = pass1,
            RoleId = defaultRoleId,
            IsActive = true,
            Phone = phone,
            ClassCode = classCode,
            BirthDate = birthIso
        };

        SetInteractable(false);
        statusLabel.text = "Kayıt yapılıyor...";

        StartCoroutine(RegisterCoroutine(req));
    }

    private void SetInteractable(bool enabled)
    {
        if (submitBtn != null) submitBtn.SetEnabled(enabled);
        if (goLoginBtn != null) goLoginBtn.SetEnabled(enabled);
    }

    private IEnumerator RegisterCoroutine(CreateUserRequest req)
    {
        if (router == null)
        {
            statusLabel.text = "Router bulunamadı (AppRouter).";
            SetInteractable(true);
            yield break;
        }

        string url = router.ApiBaseUrl + registerPath;
        string json = JsonUtility.ToJson(req);
        byte[] body = Encoding.UTF8.GetBytes(json);

        Debug.Log("REGISTER URL => " + url);
        Debug.Log("REGISTER JSON => " + json);

        using var uwr = new UnityWebRequest(url, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(body);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        bool ok = uwr.result == UnityWebRequest.Result.Success &&
                  (uwr.responseCode >= 200 && uwr.responseCode < 300);

        if (ok)
        {
            statusLabel.text = "Kayıt başarılı! Giriş ekranına yönlendiriliyorsunuz...";
            SetInteractable(true);
            router.ShowLogin();
            yield break;
        }

        string serverMsg = uwr.downloadHandler != null ? uwr.downloadHandler.text : "";
        Debug.LogError($"REGISTER FAILED {(int)uwr.responseCode} => {serverMsg}");
        statusLabel.text = $"Kayıt başarısız. ({uwr.responseCode})\n{serverMsg}";

        SetInteractable(true);
    }

    private static string NormalizeBirthDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (DateTime.TryParseExact(
                raw,
                new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            return dt.ToString("yyyy-MM-dd");
        }

        if (DateTime.TryParse(raw, out var dt2))
            return dt2.ToString("yyyy-MM-dd");

        return raw;
    }

    [Serializable]
    private class CreateUserRequest
    {
        public string Name;
        public string Surname;
        public string Email;

        public string Password;

        public int RoleId;
        public bool IsActive;

        public string Phone;
        public string ClassCode;
        public string BirthDate;
    }
}