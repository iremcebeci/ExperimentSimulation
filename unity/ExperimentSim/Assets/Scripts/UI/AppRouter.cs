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

        var sidebar = GetComponent<DashboardSidebarController>();
        if (sidebar != null) sidebar.enabled = false;
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

    public void ShowDashboardByRole(string role)
    {
        string r = (role ?? "").Trim().ToLowerInvariant();
        Debug.Log("ROLE => " + role);

        if (r.Contains("admin") || r.Contains("yönetici") || r.Contains("yonetici"))
            LoadDashboard(adminDashboardUxml);
        else if (r.Contains("contentcreator") || r.Contains("content creator") || r.Contains("içerik") || r.Contains("icerik"))
            LoadDashboard(contentCreatorDashboardUxml);
        else if (r.Contains("teacher") || r.Contains("öğretmen") || r.Contains("ogretmen"))
            LoadDashboard(teacherDashboardUxml);
        else if (r.Contains("student") || r.Contains("öğrenci") || r.Contains("ogrenci"))
            LoadDashboard(studentDashboardUxml);
        else
            LoadDashboard(independentDashboardUxml);
    }

    private void LoadDashboard(string dashboardUxmlName)
    {
        ResetView();

        SetWindowMode(true, 0, 0, resizable: false);

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

        var admin = GetComponent<AdminDashboardController>();
        if (admin == null) admin = gameObject.AddComponent<AdminDashboardController>();
        admin.enabled = true;
        admin.Bind(this, dashboardInstance);

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
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreen = false;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }
}