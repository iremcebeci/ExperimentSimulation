using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AppRouter : MonoBehaviour
{
    [Header("API Base URL (DEV)")]
    [Tooltip("Örn: http://localhost:5156")]
    public string apiBaseUrl = "http://localhost:5156";
    public string ApiBaseUrl => apiBaseUrl;



    [Header("Resources UXML Names (no extension)")]
    public string loginUxml = "Login";
    public string registerUxml = "Register";

    [Header("Dashboards (Resources UXML Names)")]
    public string adminDashboardUxml = "AdminDashboard";
    public string independentDashboardUxml = "IndependentUserDashboard";
    public string contentCreatorDashboardUxml = "ContentCreatorDashboard";
    public string teacherDashboardUxml = "TeacherDashboard";
    public string studentDashboardUxml = "StudentDashboard";

    [Header("Window Settings")]
    [Range(0.5f, 0.9f)]
    [Tooltip("Login/Register penceresi ekranın yüzde kaçını kaplayacak (0.5 = %50, 0.8 = %80)")]
    public float loginScreenPercentage = 0.75f;

    [Tooltip("Login/Register penceresi için tercih edilen aspect ratio (16:9 veya 16:10)")]
    public AspectRatio preferredAspectRatio = AspectRatio.SixteenByNine;

    [Header("Session (Auth)")]
    [SerializeField] private int currentUserId;
    [SerializeField] private string accessToken;
    [SerializeField] private int currentRoleId;
    [SerializeField] private string currentRoleName;
    [SerializeField] private string currentName;
    [SerializeField] private string currentSurname;

    public int CurrentUserId => currentUserId;
    public string AccessToken => accessToken;
    public int CurrentRoleId => currentRoleId;
    public string CurrentRoleName => currentRoleName;
    public string CurrentName => currentName;
    public string CurrentSurname => currentSurname;

    public void SetSession(int userId, string token, string name, string surname, int roleId, string roleName)
    {
        currentUserId = userId;
        accessToken = token;

        currentName = name;
        currentSurname = surname;

        currentRoleId = roleId;
        currentRoleName = roleName;
    }

    public void ClearSession()
    {
        currentUserId = 0;
        accessToken = "";

        currentName = "";
        currentSurname = "";

        currentRoleId = 0;
        currentRoleName = "";
    }

    public enum AspectRatio
    {
        SixteenByNine,
        SixteenByTen
    }

    private UIDocument doc;
    private VisualElement root;

    private void Awake()
    {
        doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;
    }

    private void Start()
    {
        ShowLogin();
    }

    private void ResetView()
    {
        root.Clear();

        DisableAllViewControllers();
    }

    private void DisableAllViewControllers()
    {
        var login = GetComponent<LoginController>();
        if (login != null) login.enabled = false;

        var reg = GetComponent<RegisterController>();
        if (reg != null) reg.enabled = false;

        var admin = GetComponent<AdminDashboardController>();
        if (admin != null) admin.enabled = false;

        var teacher = GetComponent<TeacherDashboardController>();
        if (teacher != null) teacher.enabled = false;

        var general = GetComponent<GeneralDashboardController>();
        if (general != null) general.enabled = false;

        var sidebar = GetComponent<DashboardSidebarController>();
        if (sidebar != null) sidebar.enabled = false;

        var header = GetComponent<DashboardsHeaderController>();
        if (header != null) header.enabled = false;

        var student = GetComponent<StudentDashboardController>();
        if (student != null) student.enabled = false;

        var independentUser = GetComponent<IndependentUserDashboardController>();
        if (independentUser != null) independentUser.enabled = false;

        var contentCreator = GetComponent<ContentCreatorDashboardController>();
        if (contentCreator != null) contentCreator.enabled = false;
    }

    // giriş yap

    public void ShowLogin()
    {
        ResetView();

        (int width, int height) = CalculateLoginWindowSize();
        SetWindowMode(false, width, height, resizable: false);

        var uxml = Resources.Load<VisualTreeAsset>(loginUxml);
        if (uxml == null)
        {
            Debug.LogError($"Login UXML not found in Resources: {loginUxml}.uxml");
            return;
        }

        TemplateContainer loginInstance = uxml.CloneTree();
        loginInstance.style.flexGrow = 1;
        root.Add(loginInstance);

        var controller = GetComponent<LoginController>();
        if (controller == null) controller = gameObject.AddComponent<LoginController>();
        controller.enabled = true;
        controller.Bind(this, loginInstance);

        var toSignupBtn = loginInstance.Q<Button>("toSignupBtn");
        if (toSignupBtn != null)
        {
            toSignupBtn.clicked -= ShowRegister;
            toSignupBtn.clicked += ShowRegister;
        }
    }

    // kayıt ol

    public void ShowRegister()
    {
        ResetView();

        (int width, int height) = CalculateLoginWindowSize();
        SetWindowMode(false, width, height, resizable: false);

        var uxml = Resources.Load<VisualTreeAsset>(registerUxml);
        if (uxml == null)
        {
            Debug.LogError($"Register UXML not found in Resources: {registerUxml}.uxml");
            return;
        }

        TemplateContainer registerInstance = uxml.CloneTree();
        registerInstance.style.flexGrow = 1;
        root.Add(registerInstance);

        var controller = GetComponent<RegisterController>();
        if (controller == null) controller = gameObject.AddComponent<RegisterController>();
        controller.enabled = true;
        controller.Bind(this, registerInstance);
    }

    public void ShowDashboard()
    {
        LoadDashboard(adminDashboardUxml);
    }

    public void ShowDashboardByRole(string role, int roleId = 0)
    {
        string r = (role ?? "").Trim().ToLowerInvariant();
        Debug.Log($"ROLE => {role}, roleId => {roleId}");

        if (roleId == 5)
            LoadDashboard(adminDashboardUxml);
        else if (roleId == 4)
            LoadDashboard(contentCreatorDashboardUxml);
        else if (roleId == 3)
            LoadDashboard(independentDashboardUxml);
        else if (roleId == 2)
            LoadDashboard(teacherDashboardUxml);
        else if (roleId == 1)
            LoadDashboard(studentDashboardUxml);
        else if (r.Contains("admin") || r.Contains("yönetici") || r.Contains("yonetici"))
            LoadDashboard(adminDashboardUxml);
        else if (r.Contains("contentcreator") || r.Contains("content creator") || r.Contains("içerik") || r.Contains("icerik"))
            LoadDashboard(contentCreatorDashboardUxml);
        else if (r.Contains("teacher") || r.Contains("öğretmen") || r.Contains("ogretmen"))
            LoadDashboard(teacherDashboardUxml);
        else if (r.Contains("student") || r.Contains("öğrenci") || r.Contains("ogrenci"))
            LoadDashboard(studentDashboardUxml);
        else if (r.Contains("independent") || r.Contains("bağımsız") || r.Contains("bagimsiz"))
            LoadDashboard(independentDashboardUxml);
        else
            LoadDashboard(independentDashboardUxml);
    }

    private void LoadDashboard(string dashboardUxmlName)
    {
        ResetView();

        SetWindowMode(true, Screen.currentResolution.width, Screen.currentResolution.height, resizable: false);

        var uxml = Resources.Load<VisualTreeAsset>(dashboardUxmlName);
        if (uxml == null)
        {
            Debug.LogError($"Dashboard UXML not found in Resources: {dashboardUxmlName}.uxml");
            return;
        }

        TemplateContainer dashboardInstance = uxml.CloneTree();
        dashboardInstance.style.flexGrow = 1;
        root.Add(dashboardInstance);

        var sidebarCtrl = GetComponent<DashboardSidebarController>();
        if (sidebarCtrl == null) sidebarCtrl = gameObject.AddComponent<DashboardSidebarController>();
        sidebarCtrl.enabled = true;
        sidebarCtrl.Bind(this, dashboardInstance);

        var header = GetComponent<DashboardsHeaderController>();
        if (header == null) header = gameObject.AddComponent<DashboardsHeaderController>();
        header.enabled = true;
        header.Bind(this, dashboardInstance);

        if (dashboardUxmlName == adminDashboardUxml)
        {
            var admin = GetComponent<AdminDashboardController>();
            if (admin == null) admin = gameObject.AddComponent<AdminDashboardController>();
            admin.enabled = true;
            admin.Bind(this, dashboardInstance);
        }
        else if (dashboardUxmlName == teacherDashboardUxml)
        {
            var teacher = GetComponent<TeacherDashboardController>();
            if (teacher == null) teacher = gameObject.AddComponent<TeacherDashboardController>();
            teacher.enabled = true;
            teacher.Bind(this, dashboardInstance);
        }
        else if (dashboardUxmlName == studentDashboardUxml)
        {
            var student = GetComponent<StudentDashboardController>();
            if (student == null) student = gameObject.AddComponent<StudentDashboardController>();
            student.enabled = true;
            student.Bind(this, dashboardInstance);
        }
        else if (dashboardUxmlName == independentDashboardUxml)
        {
            var independent = GetComponent<IndependentUserDashboardController>();
            if (independent == null) independent = gameObject.AddComponent<IndependentUserDashboardController>();
            independent.enabled = true;
            independent.Bind(this, dashboardInstance);
        }
        else if (dashboardUxmlName == contentCreatorDashboardUxml)
        {
            var contentCreator = GetComponent<ContentCreatorDashboardController>();
            if (contentCreator == null) contentCreator = gameObject.AddComponent<ContentCreatorDashboardController>();
            contentCreator.enabled = true;
            contentCreator.Bind(this, dashboardInstance);
        }
        else
        {
            var general = GetComponent<GeneralDashboardController>();
            if (general == null) general = gameObject.AddComponent<GeneralDashboardController>();
            general.enabled = true;
            general.Bind(this, dashboardInstance);
        }

        Debug.Log($"Loaded dashboard: {dashboardUxmlName}");
    }

    private (int width, int height) CalculateLoginWindowSize()
    {
        int screenWidth = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        int targetWidth = Mathf.RoundToInt(screenWidth * loginScreenPercentage);

        int targetHeight = preferredAspectRatio switch
        {
            AspectRatio.SixteenByNine => Mathf.RoundToInt(targetWidth * 9f / 16f),
            AspectRatio.SixteenByTen => Mathf.RoundToInt(targetWidth * 10f / 16f),
            _ => Mathf.RoundToInt(targetWidth * 9f / 16f)
        };

        targetWidth = Mathf.Max(targetWidth, 1920);
        targetHeight = Mathf.Max(targetHeight, 1080);

        targetWidth = Mathf.Min(targetWidth, 1920);
        targetHeight = Mathf.Min(targetHeight, 1080);

        Debug.Log($"Login window size calculated: {targetWidth}x{targetHeight} (Screen: {screenWidth}x{screenHeight}, Ratio: {preferredAspectRatio})");
        return (targetWidth, targetHeight);
    }

    private void SetWindowMode(bool fullscreen, int width, int height, bool resizable)
    {
        if (fullscreen)
        {
            int w = Display.main.systemWidth;
            int h = Display.main.systemHeight;

            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }
}