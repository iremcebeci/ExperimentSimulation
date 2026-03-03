using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class AdminDashboardController : MonoBehaviour
{
    private AppRouter router;

    private VisualElement root;
    private VisualElement mainContent;


    private ScrollView usersList;
    private Label usersStatus;
    private Button refreshUsersBtn;
    private TextField userSearchTf;


    private TextField addNameTf, addSurnameTf, addEmailTf, addPasswordTf;
    private DropdownField addRoleDd;
    private Toggle addIsActiveTg;
    private Button addSaveBtn, addClearBtn;
    private Label addStatusLabel;

    private List<UserRow> cachedUsers = new();


    private ScrollView rolesList;
    private Label rolesStatusLabel;
    private Label rolesActionLabel;

    private Button rolesRefreshBtn;
    private Button rolesAddBtn;
    private Button rolesAssignBtn;

    private TextField rolesNewNameTf;
    private TextField rolesNewDescTf;

    private DropdownField rolesUserDd;
    private DropdownField rolesRoleDd;

    private List<RoleRow> cachedRoles = new();
    private List<UserLite> cachedUsersLite = new();

    [SerializeField] private string userPath = "/api/User";
    [SerializeField] private string rolesPath = "/api/Role";


    public void Bind(AppRouter router, VisualElement dashboardView)
    {
        this.router = router;
        root = dashboardView;

        mainContent = root.Q<VisualElement>("MainContent");
        if (mainContent == null)
        {
            Debug.LogError("MainContent bulunamadı. AdminDashboard.uxml içine name=\"MainContent\" ekle.");
            return;
        }


        root.Q<Button>("HomeBtn")?.RegisterCallback<ClickEvent>(_ => ShowPage("HomePage"));
        root.Q<Button>("AddUserBtn")?.RegisterCallback<ClickEvent>(_ => ShowPage("AddUserPage"));


        root.Q<Button>("ListUsersBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            ShowPage("ListUsersPage");
            StartCoroutine(FetchUsers());
        });


        root.Q<Button>("RolesBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            ShowPage("RolesPage");
            StartCoroutine(FetchRolesAndUsers());
        });

        root.Q<Button>("PermissionsBtn")?.RegisterCallback<ClickEvent>(_ => ShowPage("PermissionsPage"));
        root.Q<Button>("DatabaseOpsBtn")?.RegisterCallback<ClickEvent>(_ => ShowPage("DatabaseOpsPage"));
        root.Q<Button>("SystemBtn")?.RegisterCallback<ClickEvent>(_ => ShowPage("SystemPage"));
        root.Q<Button>("StartSimulationBtn")?.RegisterCallback<ClickEvent>(_ => ShowPage("StartSimulationPage"));


        usersList = root.Q<ScrollView>("UsersList");
        usersStatus = root.Q<Label>("UsersStatusLabel");
        refreshUsersBtn = root.Q<Button>("RefreshUsersBtn");
        userSearchTf = root.Q<TextField>("UserSearchTf");


        addNameTf = root.Q<TextField>("AddUser_NameTf");
        addSurnameTf = root.Q<TextField>("AddUser_SurnameTf");
        addEmailTf = root.Q<TextField>("AddUser_EmailTf");
        addPasswordTf = root.Q<TextField>("AddUser_PasswordTf");

        addRoleDd = root.Q<DropdownField>("AddUser_RoleDd");
        addIsActiveTg = root.Q<Toggle>("AddUser_IsActiveTg");

        addSaveBtn = root.Q<Button>("AddUser_SaveBtn");
        addClearBtn = root.Q<Button>("AddUser_ClearBtn");

        addStatusLabel = root.Q<Label>("AddUser_StatusLabel");


        if (addRoleDd != null)
        {
            addRoleDd.choices = new List<string> {
                "Öğrenci","Öğretmen","Bağımsız Kullanıcı","İçerik Üreticisi","Yönetici"
            };
            addRoleDd.value = "Öğrenci";
        }

        if (addSaveBtn != null)
            addSaveBtn.clicked += () => StartCoroutine(AddUser());

        if (addClearBtn != null)
            addClearBtn.clicked += ClearAddUserForm;


        if (refreshUsersBtn != null)
            refreshUsersBtn.clicked += () => StartCoroutine(FetchUsers());

        if (userSearchTf != null)
            userSearchTf.RegisterValueChangedCallback(_ => RenderUsersFiltered());


        rolesList = root.Q<ScrollView>("Roles_List");
        rolesStatusLabel = root.Q<Label>("Roles_StatusLabel");
        rolesActionLabel = root.Q<Label>("Roles_ActionLabel");

        rolesRefreshBtn = root.Q<Button>("Roles_RefreshBtn");
        rolesAddBtn = root.Q<Button>("Roles_AddBtn");
        rolesAssignBtn = root.Q<Button>("Roles_AssignBtn");

        rolesNewNameTf = root.Q<TextField>("Roles_NewNameTf");
        rolesNewDescTf = root.Q<TextField>("Roles_NewDescTf");

        rolesUserDd = root.Q<DropdownField>("Roles_UserDd");
        rolesRoleDd = root.Q<DropdownField>("Roles_RoleDd");

        if (rolesRefreshBtn != null)
            rolesRefreshBtn.clicked += () => StartCoroutine(FetchRolesAndUsers());

        if (rolesAddBtn != null)
            rolesAddBtn.clicked += () => StartCoroutine(AddRole());

        if (rolesAssignBtn != null)
            rolesAssignBtn.clicked += () => StartCoroutine(AssignRoleToUser());


        ShowPage("HomePage");
    }

    private void ShowPage(string pageName)
    {
        foreach (var child in mainContent.Children())
            child.RemoveFromClassList("active");

        var page = mainContent.Q<VisualElement>(pageName);
        if (page == null)
        {
            Debug.LogError($"Sayfa bulunamadı: {pageName}");
            return;
        }

        page.AddToClassList("active");
    }



    private IEnumerator FetchUsers()
    {
        if (router == null)
        {
            if (usersStatus != null) usersStatus.text = "Router yok (ApiBaseUrl).";
            yield break;
        }

        string url = router.ApiBaseUrl + userPath;
        Debug.Log("[Users] GET => " + url);

        if (usersStatus != null) usersStatus.text = "Yükleniyor...";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError($"[Users] FAILED {(int)req.responseCode} => {err}");
            if (usersStatus != null) usersStatus.text = $"Hata: {req.responseCode}\n{err}";
            yield break;
        }

        string json = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        cachedUsers = JsonArrayHelper.FromJson<UserRow>(json);

        RenderUsersFiltered();

        if (usersStatus != null) usersStatus.text = $"Toplam: {cachedUsers.Count}";
    }

    private void RenderUsersFiltered()
    {
        if (usersList == null) return;

        usersList.Clear();

        string q = userSearchTf != null ? (userSearchTf.value ?? "").Trim().ToLowerInvariant() : "";

        foreach (var u in cachedUsers)
        {
            if (!string.IsNullOrEmpty(q))
            {
                string hay = $"{u.Name} {u.Surname} {u.Email} {u.RoleName}".ToLowerInvariant();
                if (!hay.Contains(q)) continue;
            }

            usersList.Add(BuildUserItem(u));
        }
    }

    private VisualElement BuildUserItem(UserRow u)
    {
        var row = new VisualElement();
        row.AddToClassList("user-row");

        var title = new Label($"{u.Name} {u.Surname}");
        title.AddToClassList("user-title");

        var sub = new Label($"{u.Email}  •  {u.RoleName}  •  {(u.IsActive ? "Active" : "Passive")}");
        sub.AddToClassList("user-sub");

        row.Add(title);
        row.Add(sub);

        return row;
    }

    [System.Serializable]
    private class UserRow
    {
        public int Id;
        public string Name;
        public string Surname;
        public string Email;
        public int RoleId;
        public string RoleName;
        public bool IsActive;
        public string CreatedAt;
        public string LastLogin;
    }


    public static class JsonArrayHelper
    {
        [System.Serializable]
        private class Wrapper<T> { public T[] Items; }

        public static List<T> FromJson<T>(string jsonArray)
        {
            string wrapped = "{\"Items\":" + jsonArray + "}";
            var w = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return (w != null && w.Items != null) ? new List<T>(w.Items) : new List<T>();
        }
    }



    private void ClearAddUserForm()
    {
        if (addNameTf != null) addNameTf.value = "";
        if (addSurnameTf != null) addSurnameTf.value = "";
        if (addEmailTf != null) addEmailTf.value = "";
        if (addPasswordTf != null) addPasswordTf.value = "";
        if (addRoleDd != null) addRoleDd.value = "Öğrenci";
        if (addIsActiveTg != null) addIsActiveTg.value = true;
        if (addStatusLabel != null) addStatusLabel.text = "";
    }

    private IEnumerator AddUser()
    {
        if (router == null)
        {
            if (addStatusLabel != null) addStatusLabel.text = "Router yok (ApiBaseUrl).";
            yield break;
        }

        string name = addNameTf?.value?.Trim() ?? "";
        string surname = addSurnameTf?.value?.Trim() ?? "";
        string email = addEmailTf?.value?.Trim() ?? "";
        string password = addPasswordTf?.value ?? "";
        bool isActive = addIsActiveTg != null && addIsActiveTg.value;

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(surname) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            if (addStatusLabel != null) addStatusLabel.text = "Ad, soyad, email ve şifre zorunlu.";
            yield break;
        }

        int roleId = RoleNameToId(addRoleDd != null ? addRoleDd.value : "Öğrenci");

        var reqObj = new CreateUserRequest
        {
            Name = name,
            Surname = surname,
            Email = email,
            Password = password,
            RoleId = roleId,
            IsActive = isActive
        };

        string json = JsonUtility.ToJson(reqObj);
        string url = router.ApiBaseUrl + userPath;

        if (addStatusLabel != null) addStatusLabel.text = "Kaydediliyor...";

        using var uwr = new UnityWebRequest(url, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        bool ok = uwr.result == UnityWebRequest.Result.Success &&
                  uwr.responseCode >= 200 && uwr.responseCode < 300;

        string resp = uwr.downloadHandler != null ? uwr.downloadHandler.text : "";

        if (!ok)
        {
            Debug.LogError($"[AddUser] FAILED {(int)uwr.responseCode} => {resp}");
            if (addStatusLabel != null) addStatusLabel.text = $"Kayıt başarısız ({uwr.responseCode})\n{resp}";
            yield break;
        }

        if (addStatusLabel != null) addStatusLabel.text = "✅ Kullanıcı eklendi!";
        ClearAddUserForm();
    }

    [System.Serializable]
    private class CreateUserRequest
    {
        public string Name;
        public string Surname;
        public string Email;
        public string Password;
        public int RoleId;
        public bool IsActive;
    }

    private int RoleNameToId(string roleName)
    {
        return roleName switch
        {
            "Öğrenci" => 1,
            "Öğretmen" => 2,
            "Bağımsız Kullanıcı" => 3,
            "İçerik Üreticisi" => 4,
            "Yönetici" => 5,
            _ => 1
        };
    }

    private string Sha256(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        var sb = new System.Text.StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
            sb.Append(hash[i].ToString("x2"));
        return sb.ToString();
    }



    private IEnumerator FetchRolesAndUsers()
    {
        if (router == null) yield break;

        if (rolesStatusLabel != null) rolesStatusLabel.text = "Yükleniyor...";
        if (rolesActionLabel != null) rolesActionLabel.text = "";


        yield return FetchRoles();


        yield return FetchUsersLite();

        RenderRoles();
        PopulateRoleDropdown();
        PopulateUserDropdown();

        if (rolesStatusLabel != null)
            rolesStatusLabel.text = $"Roller: {cachedRoles.Count} • Kullanıcılar: {cachedUsersLite.Count}";
    }

    private IEnumerator FetchRoles()
    {
        string url = router.ApiBaseUrl + rolesPath;
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError($"[Roles] GET failed {(int)req.responseCode} => {err}");
            cachedRoles = new List<RoleRow>();
            yield break;
        }

        string json = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        cachedRoles = JsonArrayHelper.FromJson<RoleRow>(json);
    }

    private IEnumerator FetchUsersLite()
    {
        string url = router.ApiBaseUrl + userPath;
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError($"[UsersLite] GET failed {(int)req.responseCode} => {err}");
            cachedUsersLite = new List<UserLite>();
            yield break;
        }

        string json = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var full = JsonArrayHelper.FromJson<UserRow>(json);

        cachedUsersLite = new List<UserLite>();
        foreach (var u in full)
            cachedUsersLite.Add(new UserLite { Id = u.Id, Display = $"{u.Name} {u.Surname}" });
    }

    private void RenderRoles()
    {
        if (rolesList == null) return;

        rolesList.Clear();

        foreach (var r in cachedRoles)
        {
            var row = new VisualElement();
            row.AddToClassList("role-row");

            var name = new Label($"{r.Name} (#{r.Id})");
            name.AddToClassList("role-name");

            var desc = new Label(string.IsNullOrEmpty(r.Description) ? "-" : r.Description);
            desc.AddToClassList("role-desc");

            row.Add(name);
            row.Add(desc);

            rolesList.Add(row);
        }
    }

    private void PopulateRoleDropdown()
    {
        if (rolesRoleDd == null) return;

        var choices = new List<string>();
        foreach (var r in cachedRoles)
            choices.Add($"{r.Id} - {r.Name}");

        rolesRoleDd.choices = choices;
        rolesRoleDd.value = choices.Count > 0 ? choices[0] : "";
    }

    private void PopulateUserDropdown()
    {
        if (rolesUserDd == null) return;

        var choices = new List<string>();
        foreach (var u in cachedUsersLite)
            choices.Add($"{u.Id} - {u.Display}");

        rolesUserDd.choices = choices;
        rolesUserDd.value = choices.Count > 0 ? choices[0] : "";
    }

    private IEnumerator AddRole()
    {
        if (router == null) yield break;

        string name = rolesNewNameTf?.value?.Trim() ?? "";
        string desc = rolesNewDescTf?.value?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            if (rolesActionLabel != null) rolesActionLabel.text = "Rol adı zorunlu.";
            yield break;
        }

        var payload = new RoleCreateRequest { Name = name, Description = desc };
        string json = JsonUtility.ToJson(payload);

        string url = router.ApiBaseUrl + rolesPath;

        if (rolesActionLabel != null) rolesActionLabel.text = "Kaydediliyor...";

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError($"[Roles] POST failed {(int)req.responseCode} => {err}");
            if (rolesActionLabel != null) rolesActionLabel.text = $"Rol eklenemedi ({req.responseCode})";
            yield break;
        }

        if (rolesActionLabel != null) rolesActionLabel.text = "Rol eklendi!";
        if (rolesNewNameTf != null) rolesNewNameTf.value = "";
        if (rolesNewDescTf != null) rolesNewDescTf.value = "";

        StartCoroutine(FetchRolesAndUsers());
    }

    private IEnumerator AssignRoleToUser()
    {
        if (router == null) yield break;
        if (rolesUserDd == null || rolesRoleDd == null) yield break;

        int userId = ParseLeadingInt(rolesUserDd.value);
        int roleId = ParseLeadingInt(rolesRoleDd.value);

        if (userId <= 0 || roleId <= 0)
        {
            if (rolesActionLabel != null) rolesActionLabel.text = "Kullanıcı veya rol seçimi hatalı.";
            yield break;
        }

        if (rolesActionLabel != null) rolesActionLabel.text = "Rol atanıyor...";

        string url = router.ApiBaseUrl + $"/api/User/{userId}/role";
        string json = JsonUtility.ToJson(new AssignRoleBody { RoleId = roleId });

        using var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        string resp = req.downloadHandler != null ? req.downloadHandler.text : "";

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AssignRole] FAILED {(int)req.responseCode} => {resp}");
            if (rolesActionLabel != null) rolesActionLabel.text = $"Rol atanamadı ({req.responseCode})\n{resp}";
            yield break;
        }

        if (rolesActionLabel != null) rolesActionLabel.text = "✅ Rol atandı!";
        StartCoroutine(FetchRolesAndUsers());
    }

    [System.Serializable]
    private class AssignRoleBody
    {
        public int RoleId;
    }

    private int ParseLeadingInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int dash = s.IndexOf('-');
        string part = dash > 0 ? s.Substring(0, dash).Trim() : s.Trim();
        int.TryParse(part, out int v);
        return v;
    }

    [System.Serializable]
    private class RoleRow
    {
        public int Id;
        public string Name;
        public string Description;
    }

    [System.Serializable]
    private class RoleCreateRequest
    {
        public string Name;
        public string Description;
    }

    [System.Serializable]
    private class UserLite
    {
        public int Id;
        public string Display;
    }


    [System.Serializable]
    private class UserUpdateRequest
    {
        public int Id;
        public string Name;
        public string Surname;
        public string Email;
        public string Password;
        public int RoleId;
        public bool IsActive;


        public object Role;
    }
}