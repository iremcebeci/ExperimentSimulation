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


        // Şifre alanlarını gizleme kodu

        if (passwordTf != null)
        {
            passwordTf.isPasswordField = true;
            passwordTf.maskChar = '•';
        }

        if (password2Tf != null)
        {
            password2Tf.isPasswordField = true;
            password2Tf.maskChar = '•';
        }


        // Doğum tarihi alanı için formatlama

        if (birthDateTf != null)
        {
            birthDateTf.isDelayed = false;
            birthDateTf.maxLength = 10;

            birthDateTf.RegisterCallback<KeyDownEvent>(_ =>
            {
                birthDateTf.selectIndex = birthDateTf.cursorIndex;
            });

            birthDateTf.RegisterValueChangedCallback(evt =>
            {
                string raw = evt.newValue ?? "";
                string formatted = FormatBirthDate(raw);

                if (formatted == raw) return;

                int digitCount = CountDigitsUpToCaret(raw, birthDateTf.cursorIndex);
                int desiredCaret = CaretIndexForDateDigits(digitCount, formatted);

                birthDateTf.SetValueWithoutNotify(formatted);

                birthDateTf.schedule.Execute(() =>
                {
                    birthDateTf.cursorIndex = desiredCaret;
                    birthDateTf.selectIndex = desiredCaret;
                });
            });
        }


        // Telefon alanı için formatlama

        if (phoneTf != null)
        {
            phoneTf.isDelayed = false;
            phoneTf.maxLength = 17;

            phoneTf.RegisterCallback<KeyDownEvent>(_ =>
            {
                phoneTf.selectIndex = phoneTf.cursorIndex;
            });

            phoneTf.RegisterValueChangedCallback(evt =>
            {
                string raw = evt.newValue ?? "";

                int digitCount = CountDigitsUpToCaret(raw, phoneTf.cursorIndex);

                string formatted = FormatTrPhone(raw);

                if (formatted == raw) return;

                int desiredCaret = CaretIndexAfterNthDigit(formatted, digitCount);

                phoneTf.SetValueWithoutNotify(formatted);

                phoneTf.schedule.Execute(() =>
                {
                    phoneTf.cursorIndex = desiredCaret;
                    phoneTf.selectIndex = desiredCaret;
                });
            });
        }



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

    // Tarih formatlama için ek fonksiyonlar

    private static string FormatBirthDate(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        var d = new StringBuilder(8);
        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                d.Append(c);
                if (d.Length == 8) break;
            }
        }

        if (d.Length <= 2) return d.ToString();

        if (d.Length <= 4)
            return d.ToString(0, 2) + "." + d.ToString(2, d.Length - 2);

        return d.ToString(0, 2) + "." + d.ToString(2, 2) + "." + d.ToString(4, d.Length - 4);
    }

    private static int CountDigitsUpToCaret(string s, int caret)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        caret = Mathf.Clamp(caret, 0, s.Length);

        int count = 0;
        for (int i = 0; i < caret; i++)
            if (char.IsDigit(s[i])) count++;

        return count;
    }

    private static int CaretIndexForDateDigits(int digits, string formatted)
    {
        int idx = digits;
        if (digits > 2) idx += 1;
        if (digits > 4) idx += 1;

        return Mathf.Clamp(idx, 0, formatted.Length);
    }


    // Telefon formatlama için ek fonksiyon

    private static string FormatTrPhone(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // sadece rakamları al (max 11: 0 + 10)
        var digits = new StringBuilder(11);
        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                digits.Append(c);
                if (digits.Length == 11) break;
            }
        }

        // +90 ile başladıysa (90xxxxxxxxxx) -> 0xxxxxxxxxx
        if (digits.Length >= 2 && digits[0] == '9' && digits[1] == '0')
        {
            digits.Remove(0, 2);
            digits.Insert(0, '0');
            if (digits.Length > 11) digits.Length = 11;
        }

        // 5 ile başladıysa 0 ekle
        if (digits.Length > 0 && digits[0] == '5')
        {
            digits.Insert(0, '0');
            if (digits.Length > 11) digits.Length = 11;
        }

        int n = digits.Length;
        if (n == 0) return "";

        // Kademeli format: 0 (5xx) xxx xx xx
        var sb = new StringBuilder(16);

        // 0
        sb.Append(digits[0]);
        if (n == 1) return sb.ToString();

        sb.Append(" (");

        // 5xx
        sb.Append(digits[1]);
        if (n == 2) return sb.ToString();
        sb.Append(digits[2]);
        if (n == 3) return sb.ToString();
        sb.Append(digits[3]);
        if (n == 4) { sb.Append(") "); return sb.ToString(); }

        sb.Append(") ");

        // xxx
        sb.Append(digits[4]);
        if (n == 5) return sb.ToString();
        sb.Append(digits[5]);
        if (n == 6) return sb.ToString();
        sb.Append(digits[6]);
        if (n == 7) { sb.Append(" "); return sb.ToString(); }

        sb.Append(" ");

        // xx
        sb.Append(digits[7]);
        if (n == 8) return sb.ToString();
        sb.Append(digits[8]);
        if (n == 9) { sb.Append(" "); return sb.ToString(); }

        sb.Append(" ");

        // xx
        sb.Append(digits[9]);
        if (n == 10) return sb.ToString();
        sb.Append(digits[10]); // n==11

        return sb.ToString();
    }

    private static int CaretIndexForPhoneDigits(int digits, string formatted)
    {
        // digits = kaç rakam (0..11)
        // formatted hedef: "0 (5xx) xxx xx xx"

        // Digit->string index eşlemesi (tam format için):
        // d1 '0'              -> idx 1
        // d2 after "0 ("       -> idx 4
        // d3                   -> idx 5
        // d4                   -> idx 6
        // d5 after ") "        -> idx 9
        // d6                   -> idx 10
        // d7                   -> idx 11
        // d8 after space       -> idx 13
        // d9                   -> idx 14
        // d10 after space      -> idx 16
        // d11                  -> idx 17
        //
        // Ama string uzunluğu 16 olduğu için, biz "karakterin sağı" mantığıyla clamp yapacağız.

        int idx = digits switch
        {
            0 => 0,
            1 => 1,   // "0|"
            2 => 4,   // "0 (5|"
            3 => 5,   // "0 (53|"
            4 => 6,   // "0 (532|"
            5 => 9,   // "0 (532) 1|"
            6 => 10,  // "0 (532) 12|"
            7 => 11,  // "0 (532) 123|"
            8 => 13,  // "0 (532) 123 4|"
            9 => 14,  // "0 (532) 123 45|"
            10 => 16, // "0 (532) 123 45 6|"  (clamp)
            _ => 16   // 11 digit: "0 (532) 123 45 67|" (clamp)
        };

        return Mathf.Clamp(idx, 0, formatted.Length);
    }

    private static int CaretIndexAfterNthDigit(string formatted, int digitCount)
    {
        if (string.IsNullOrEmpty(formatted) || digitCount <= 0) return 0;

        int seen = 0;
        for (int i = 0; i < formatted.Length; i++)
        {
            if (char.IsDigit(formatted[i]))
            {
                seen++;
                if (seen == digitCount)
                    return i + 1;
            }
        }
        return formatted.Length;
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