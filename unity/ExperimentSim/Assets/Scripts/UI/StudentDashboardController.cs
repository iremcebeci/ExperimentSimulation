using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class StudentDashboardController : MonoBehaviour
{
    private AppRouter router;
    private VisualElement root;
    private VisualElement mainContent;

    [Header("Controllers")]
    [SerializeField] private DashboardSidebarController sidebarController;
    [SerializeField] private DashboardsHeaderController headerController;

    [Header("API Paths")]
    [SerializeField] private string myClassesPath = "/api/Class/my";
    [SerializeField] private string myAssignmentsPath = "/api/Assignment/my";
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";
    [SerializeField] private string experimentsByGradeLessonPath = "/api/Experiment/by-grade-lesson";
    [SerializeField] private string joinClassPath = "/api/Class/join"; // backend'ine göre değişebilir
    [SerializeField] private string classActivityStudentPathTemplate = "/api/Class/{classId}/activity/student";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";

    // Home
    private Label welcomeUsernameLabel;
    private VisualElement studentHomePage;
    private Label homeActiveAssignmentsValueLabel;
    private Label homeCompletedAssignmentsValueLabel;
    private Label homeJoinedClassesValueLabel;
    private Label homeUpcomingDeadlinesValueLabel;
    private ScrollView homeSummaryScroll;
    private Label homeChartPeakInfoLabel;
    private readonly List<VisualElement> homeChartBars = new();
    private readonly List<Label> homeChartValueLabels = new();
    private readonly float[] homeWeeklyHours = new float[7];

    // Stats
    private Label activeClassCountLabel, totalClassCountLabel;
    private Label completedAssignmentsCountLabel, totalAssignmentsCountLabel;
    private Label bestClassNameLabel, bestClassMessageLabel;
    private Label lastAssignmentDueDateLabel, lastAssignmentNameLabel;
    private Label cgCompletionPercentLabel;
    private Label cgCompletionDoneLabel;
    private Label cgCompletionRemainingLabel;
    private VisualElement cgCompletionDonut;
    private readonly List<Label> classGeneralChartValueLabels = new();
    private readonly List<VisualElement> classGeneralChartFillBars = new();
    private readonly Dictionary<string, Label> classGeneralChartValueByDay = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VisualElement> classGeneralChartFillByDay = new(StringComparer.OrdinalIgnoreCase);

    // Classes
    private ScrollView classesScroll;
    private VisualElement classesRows;

    // Join Class Modal
    private Button joinClassBtn;
    private VisualElement joinClassModal;      // uxml: addClassModal
    private Button joinClassModalCloseBtn;     // AddClassModalCloseBtn
    private Button joinClassCancelBtn;         // AddJoinClassCancelBtn
    private Button saveJoinClassBtn;           // saveJoinClassBtn
    private TextField classCodeInput;          // classCodeInput

    private bool modalBackdropBound;

    // Filters
    private TextField searchInput;
    private Toggle includeInactiveToggle;

    private bool filtersBound;
    private string currentSearch = "";
    private bool includeInactive = false;

    private MyClassDto[] lastItems;

    // Class Details Tabs
    private Button cdTabGeneralBtn;
    private Button cdTabAssignmentsBtn;
    private Button cdTabActivityBtn;

    // Class Details Contents
    private VisualElement classDetailsGeneralContent;
    private VisualElement classDetailsAssignmentsContent;
    private VisualElement classDetailsActivityContent;
    private VisualElement classDetailsActivityFeed;
    private VisualElement assignmentsCardsRow;
    private Label cdClassNameLabel;
    private Label cdTeacherNameLabel;
    private Label cdStudentCountLabel;
    private Label cdAssignmentCountLabel;
    private Label cdSuccessRateLabel;
    private Label cdCreatedDateLabel;
    private Label cdClassCodeLabel;
    private Label cdStatusLabel;

    private Button assignmentFilterAllBtn;
    private Button assignmentFilterActiveBtn;
    private Button assignmentFilterPassiveBtn;
    private Button assignmentFilterCompletedBtn;
    private Button assignmentFilterIncompleteBtn;
    private TextField assignmentSearchInput;
    private string assignmentFilterMode = "all";
    private string assignmentSearchQuery = "";

    private MyClassDto currentSelectedClass;
    private ClassActivityDto[] currentActivityItems;
    private AssignmentDto[] assignmentItems;

    // Assignment Page (Odevlerim)
    private VisualElement assignmentPage;
    private TextField hwSearchField;
    private DropdownField hwFilterSubjectDropdown;
    private DropdownField hwFilterStatusDropdown;
    private ScrollView hwNotStartedCards;
    private ScrollView hwInProgressCards;
    private ScrollView hwCompletedCards;
    private Label hwNotStartedCountLabel;
    private Label hwInProgressCountLabel;
    private Label hwCompletedCountLabel;

    // Progress Page
    private VisualElement progressPageContent;
    private VisualElement progressTabsBar;
    private ScrollView progressNotStartedScroll;
    private ScrollView progressInProgressScroll;
    private ScrollView progressCompletedScroll;
    private Label progressNotStartedCountLabel;
    private Label progressInProgressCountLabel;
    private Label progressCompletedCountLabel;
    private Label overallPercentLabel;
    private VisualElement overallFillBar;
    private Label subjectProgressLabel;
    private Label subjectPercentLabel;
    private VisualElement subjectFillBar;
    private Label subjectSubLabel;
    private TextField progressSearchField;
    private DropdownField progressStatusDropdown;
    private DropdownField progressDifficultyDropdown;
    private string progressSelectedLesson;
    private List<string> progressLessonTabs = new();
    private ExperimentDto[] progressExperimentItems;

    // Personal Activity Page
    private VisualElement personalActivityPage;
    private VisualElement personalActivityFeed;
    private Button actTabAllBtn;
    private Button actTabExperimentBtn;
    private Button actTabAssignmentBtn;
    private Button actTabProgressBtn;
    private Button actTabParticipationBtn;
    private TextField personalActivitySearchInput;
    private DropdownField actDateFilterDropdown;
    private ClassActivityDto[] personalActivityItems;
    private string personalActivityFilterMode = "all";
    private string personalActivitySearchQuery = "";

    // Profile Page
    private VisualElement profilePage;
    private Label profileAvatarLabel;
    private Label profileNameLabel;
    private Label profileRoleLabel;
    private Label profileStatusLabel;
    private Label profileRegistryNoLabel;
    private Label profileMailLabel;
    private Label profileJoinDateLabel;
    private Label profileLastLoginLabel;
    private VisualElement profileStatsGrid;
    private Button profileHomeBtn;
    private Button profileClassesBtn;
    private Button profileLogoutBtn;
    private Coroutine sessionHeartbeatRoutine;

    public void Bind(AppRouter router, VisualElement studentView)
    {
        this.router = router;
        root = studentView;

        if (root == null)
        {
            Debug.LogError("[StudentDashboardController] root null.");
            return;
        }

        mainContent = root.Q<VisualElement>("MainContent");
        if (mainContent == null)
        {
            Debug.LogError("[StudentDashboardController] MainContent not found (name=\"MainContent\").");
            return;
        }

        // Home
        welcomeUsernameLabel = root.Q<Label>("WelcomeUsernameLabel");
        var welcomeMessageLabel = root.Q<Label>("WelcomeMessageLabel");

        // Stats
        activeClassCountLabel = root.Q<Label>("ActiveClassCountLabel");
        totalClassCountLabel = root.Q<Label>("TotalClassCountLabel");
        completedAssignmentsCountLabel = root.Q<Label>("CompletedAssignmentsCountLabel");
        totalAssignmentsCountLabel = root.Q<Label>("TotalAssignmentsCountLabel");
        bestClassNameLabel = root.Q<Label>("BestClassNameLabel");
        bestClassMessageLabel = root.Q<Label>("BestClassMessageLabel");
        lastAssignmentDueDateLabel = root.Q<Label>("LastAssignmentDueDateLabel");
        lastAssignmentNameLabel = root.Q<Label>("LastAssignmentNameLabel");

        // Classes page
        classesScroll = root.Q<ScrollView>("ClassesScroll");
        classesRows = root.Q<VisualElement>("ClassesRows");

        // Sidebar
        sidebarController?.Bind(router, root);

        // Header
        if (headerController != null)
        {
            headerController.OnUserLoaded -= HandleHeaderUserLoaded;
            headerController.OnUserLoaded += HandleHeaderUserLoaded;
            headerController.Bind(router, root);
        }

        if (welcomeUsernameLabel != null)
            welcomeUsernameLabel.text = $"Merhaba, {router.CurrentName} {router.CurrentSurname}!";

        if (welcomeMessageLabel != null)
            welcomeMessageLabel.text = WelcomeText.BuildRoleMessage(router.CurrentRoleName);

        // Filters UI
        searchInput = root.Q<TextField>("searchInput");
        includeInactiveToggle = root.Q<Toggle>("includeInactive");

        if (includeInactiveToggle != null)
        {
            includeInactiveToggle.SetValueWithoutNotify(false);
            includeInactive = false;
        }

        BindFilters();
        BindJoinClassModal();
        BindHomePage();
        BindMenuButtons();
        BindClassDetailsTabs();
        BindHomeworkPage();
        BindProgressPage();
        BindPersonalActivityPage();
        BindProfilePage();

        ShowPage("HomePage");
        SetMenuActive("HomeBtn");

        if (sessionHeartbeatRoutine != null)
            StopCoroutine(sessionHeartbeatRoutine);
        sessionHeartbeatRoutine = StartCoroutine(SessionHeartbeatLoop());

        StartCoroutine(RefreshHomeDashboardData(forceRefresh: true));
    }

    private void HandleHeaderUserLoaded()
    {
        if (welcomeUsernameLabel != null)
            welcomeUsernameLabel.text = $"Merhaba, {router.CurrentName} {router.CurrentSurname}!";
    }

    private void BindHomePage()
    {
        studentHomePage = root.Q<VisualElement>("StudentHomePage");
        if (studentHomePage == null)
            return;

        homeActiveAssignmentsValueLabel = studentHomePage.Q<Label>("TcTotalClassValueLabel");
        homeCompletedAssignmentsValueLabel = studentHomePage.Q<Label>("TcTotalStudentValueLabel");
        homeJoinedClassesValueLabel = studentHomePage.Q<Label>("TcActiveAssignmentValueLabel");
        homeUpcomingDeadlinesValueLabel = studentHomePage.Q<Label>("TcCompletedAssignmentValueLabel");

        homeSummaryScroll = studentHomePage.Q<ScrollView>("TcSummaryScroll");
        homeChartPeakInfoLabel = studentHomePage.Q<Label>("TcChartPeakInfoLabel");

        homeChartBars.Clear();
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarMon"));
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarTue"));
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarWed"));
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarThu"));
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarFri"));
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarSat"));
        homeChartBars.Add(studentHomePage.Q<VisualElement>("TcBarSun"));

        homeChartValueLabels.Clear();
        homeChartValueLabels.AddRange(studentHomePage.Query<Label>(className: "cg-bar-value").ToList());
    }

    private IEnumerator RefreshHomeDashboardData(bool forceRefresh)
    {
        if (router == null)
            yield break;

        if (forceRefresh || lastItems == null)
            yield return StartCoroutine(FetchMyClasses());

        if (forceRefresh || assignmentItems == null)
            yield return StartCoroutine(FetchMyAssignments());

        yield return StartCoroutine(FetchWeeklySessionHours());

        ApplyHomeDashboardMetrics();
    }

    private void ApplyHomeDashboardMetrics()
    {
        var classes = (lastItems ?? Array.Empty<MyClassDto>())
            .Where(c => c != null && !string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var assignments = (assignmentItems ?? Array.Empty<AssignmentDto>())
            .Where(a => a != null)
            .ToArray();

        int activeAssignments = assignments.Count(a => a.IsActive);
        int completedAssignments = assignments.Count(a => string.Equals(GetHomeworkStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
        int joinedClasses = classes.Length;
        int upcomingDeadlines = assignments.Count(a =>
        {
            if (!a.IsActive)
                return false;

            var due = GetAssignmentDueAt(a);
            if (!due.HasValue)
                return false;

            int remaining = (due.Value.Date - DateTime.Today).Days;
            return remaining <= 3;
        });

        if (homeActiveAssignmentsValueLabel != null) homeActiveAssignmentsValueLabel.text = activeAssignments.ToString();
        if (homeCompletedAssignmentsValueLabel != null) homeCompletedAssignmentsValueLabel.text = completedAssignments.ToString();
        if (homeJoinedClassesValueLabel != null) homeJoinedClassesValueLabel.text = joinedClasses.ToString();
        if (homeUpcomingDeadlinesValueLabel != null) homeUpcomingDeadlinesValueLabel.text = upcomingDeadlines.ToString();

        var nearestDue = assignments
            .Where(a => !string.Equals(GetHomeworkStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            .Select(a => new { item = a, due = GetAssignmentDueAt(a) })
            .Where(x => x.due.HasValue)
            .OrderBy(x => x.due.Value)
            .FirstOrDefault();

        if (nearestDue == null)
        {
            SetHomeSummaryItem(0, "-", "Aktif teslim bekleyen ödev yok");
        }
        else
        {
            int remaining = Mathf.Max((nearestDue.due.Value.Date - DateTime.Today).Days, 0);
            string remainText = remaining == 0 ? "bugün teslim" : remaining == 1 ? "1 gün kaldı" : $"{remaining} gün kaldı";
            SetHomeSummaryItem(0, SafeText(nearestDue.item.Title), $"{nearestDue.due.Value:dd MMM HH:mm} | {remainText}");
        }

        var latestCompleted = assignments
            .Where(a => string.Equals(GetHomeworkStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            .Select(a => new { item = a, due = GetAssignmentDueAt(a) })
            .Where(x => x.due.HasValue)
            .OrderByDescending(x => x.due.Value)
            .FirstOrDefault();

        if (latestCompleted == null)
        {
            SetHomeSummaryItem(1, "-", "Henüz tamamlanan ödev yok");
        }
        else
        {
            SetHomeSummaryItem(1, SafeText(latestCompleted.item.Title), latestCompleted.due.Value.ToString("dd MMM HH:mm"));
        }

        var classByAssignment = assignments
            .Where(a => a.ClassId > 0)
            .GroupBy(a => new { a.ClassId, name = SafeText(a.ClassName) })
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (classByAssignment != null)
        {
            var classInfo = classes.FirstOrDefault(c => c.Id == classByAssignment.Key.ClassId);
            string detail = classInfo != null && !string.IsNullOrWhiteSpace(classInfo.LessonName)
                ? classInfo.LessonName
                : $"Toplam {classByAssignment.Count()} ödev";
            SetHomeSummaryItem(2, classByAssignment.Key.name, detail);
        }
        else
        {
            var fallbackClass = classes.OrderByDescending(c => c.AssignmentCount).FirstOrDefault();
            if (fallbackClass == null)
                SetHomeSummaryItem(2, "-", "Sınıf verisi yok");
            else
                SetHomeSummaryItem(2, SafeText(fallbackClass.Name), SafeText(fallbackClass.LessonName));
        }

        ApplyHomeChartValues(homeWeeklyHours);
    }

    private void SetHomeSummaryItem(int index, string title, string detail)
    {
        if (homeSummaryScroll == null)
            return;

        var items = homeSummaryScroll.Query<VisualElement>(className: "tc-summary-item").ToList();
        if (index < 0 || index >= items.Count)
            return;

        var labels = items[index].Query<Label>().ToList();
        if (labels.Count > 1)
            labels[1].text = SafeText(title);
        if (labels.Count > 2)
            labels[2].text = SafeText(detail);
    }

    private void ApplyHomeChartValues(float[] weekly)
    {
        if (weekly == null || weekly.Length == 0)
            return;

        float peak = weekly.Max();
        int peakIndex = Array.IndexOf(weekly, peak);
        string[] dayNames = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

        for (int i = 0; i < weekly.Length; i++)
        {
            if (i < homeChartValueLabels.Count && homeChartValueLabels[i] != null)
                homeChartValueLabels[i].text = weekly[i] <= 0f ? "0" : weekly[i].ToString("0.0");

            if (i < homeChartBars.Count && homeChartBars[i] != null)
            {
                float ratio = peak > 0f ? weekly[i] / peak : 0f;
                float px = 14f + (ratio * 96f);
                homeChartBars[i].style.height = px;
            }
        }

        if (homeChartPeakInfoLabel != null)
            homeChartPeakInfoLabel.text = peak <= 0f ? "Tepe: -" : $"Tepe: {peak:0.0} saat ({dayNames[Mathf.Clamp(peakIndex, 0, dayNames.Length - 1)]})";
    }

    private IEnumerator FetchWeeklySessionHours()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + sessionWeeklyHoursPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        for (int i = 0; i < homeWeeklyHours.Length; i++)
            homeWeeklyHours[i] = 0f;

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        var dto = JsonUtility.FromJson<WeeklySessionHoursDto>(raw);
        if (dto?.items == null)
            yield break;

        foreach (var item in dto.items)
        {
            if (item == null) continue;
            if (item.dayIndex < 0 || item.dayIndex >= homeWeeklyHours.Length) continue;
            homeWeeklyHours[item.dayIndex] = Mathf.Max(0f, item.hours);
        }
    }

    private DateTime? GetAssignmentDueAt(AssignmentDto assignment)
    {
        if (!TryParseLocalDateTime(assignment?.StartDate, out var start))
            return null;

        int duration = Mathf.Max(assignment.DurationDays, 1);
        return start.Date.AddDays(duration).AddSeconds(-1);
    }

    private int GetRemainingDaysToDue(AssignmentDto assignment)
    {
        var due = GetAssignmentDueAt(assignment);
        if (!due.HasValue)
            return 0;

        return (due.Value.Date - DateTime.Today).Days;
    }

    private bool TryParseLocalDateTime(string raw, out DateTime parsed)
    {
        parsed = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var iso))
        {
            parsed = iso.ToLocalTime();
            return true;
        }

        if (DateTime.TryParse(raw, out var dt))
        {
            parsed = dt;
            return true;
        }

        return false;
    }

    private string SafeText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    // -------------------------
    // Menu buttons
    // -------------------------
    private void BindMenuButtons()
    {
        root.Q<Button>("HomeBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("HomeBtn");
            ShowPage("HomePage");
            StartCoroutine(RefreshHomeDashboardData(forceRefresh: true));
        });

        root.Q<Button>("ClassBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ClassBtn");
            ShowPage("ClassesPage");
            StartCoroutine(FetchMyClasses());
            StartCoroutine(FetchMyAssignments());
        });

        root.Q<Button>("AssignmentBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("AssignmentBtn");
            ShowPage("AssignmentPage");
            if (lastItems == null || lastItems.Length == 0)
                StartCoroutine(FetchMyClasses());
            StartCoroutine(FetchMyAssignments());
        });

        root.Q<Button>("ProgressBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ProgressBtn");
            ShowPage("ProgressPage");
            StartCoroutine(LoadProgressPageData());
        });

        root.Q<Button>("CalendarBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("CalendarBtn");
            ShowPage("CalendarPage");
        });

        root.Q<Button>("EmailBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("EmailBtn");
            ShowPage("EmailPage");
        });

        root.Q<Button>("ActivityBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ActivityBtn");
            ShowPage("ActivityPage");
            StartCoroutine(FetchPersonalActivity());
        });

        root.Q<Button>("ProfileBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ProfileBtn");
            ShowPage("ProfilePage");
            StartCoroutine(LoadProfilePageData());
        });

        root.Q<Button>("StartSimulationBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("StartSimulationBtn");
            ShowPage("StartSimulationPage");
        });
    }

    // -------------------------
    // Filters
    // -------------------------
    private void BindFilters()
    {
        if (filtersBound) return;
        filtersBound = true;

        if (searchInput != null)
        {
            searchInput.RegisterValueChangedCallback(evt =>
            {
                currentSearch = evt.newValue ?? "";
                ApplyFiltersAndRender();
            });
        }

        if (includeInactiveToggle != null)
        {
            includeInactiveToggle.RegisterValueChangedCallback(evt =>
            {
                includeInactive = evt.newValue;
                ApplyFiltersAndRender();
            });
        }
    }

    // -------------------------
    // Join modal bindings
    // -------------------------
    private void BindJoinClassModal()
    {
        joinClassBtn = root.Q<Button>("JoinClassBtn");

        joinClassModal = root.Q<VisualElement>("addClassModal");
        joinClassModalCloseBtn = root.Q<Button>("AddClassModalCloseBtn");
        joinClassCancelBtn = root.Q<Button>("AddJoinClassCancelBtn");
        saveJoinClassBtn = root.Q<Button>("saveJoinClassBtn");
        classCodeInput = root.Q<TextField>("classCodeInput");

        SetJoinModalOpen(false);

        if (joinClassBtn == null)
            Debug.LogError("[StudentDashboardController] JoinClassBtn not found (name=\"JoinClassBtn\").");
        else
        {
            joinClassBtn.clicked -= OnJoinClassClicked;
            joinClassBtn.clicked += OnJoinClassClicked;
        }

        if (joinClassModalCloseBtn != null)
        {
            joinClassModalCloseBtn.clicked -= OnJoinModalCloseClicked;
            joinClassModalCloseBtn.clicked += OnJoinModalCloseClicked;
        }

        if (joinClassCancelBtn != null)
        {
            joinClassCancelBtn.clicked -= OnJoinModalCloseClicked;
            joinClassCancelBtn.clicked += OnJoinModalCloseClicked;
        }

        if (saveJoinClassBtn != null)
        {
            saveJoinClassBtn.clicked -= OnSaveJoinClassClicked;
            saveJoinClassBtn.clicked += OnSaveJoinClassClicked;
        }

        if (joinClassModal != null && !modalBackdropBound)
        {
            modalBackdropBound = true;
            joinClassModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == joinClassModal)
                    SetJoinModalOpen(false);
            });
        }
    }

    private void OnJoinClassClicked()
    {
        if (classCodeInput != null) classCodeInput.value = "";
        SetJoinModalOpen(true);
    }

    private void OnJoinModalCloseClicked()
    {
        SetJoinModalOpen(false);
    }

    private void OnSaveJoinClassClicked()
    {
        string code = classCodeInput != null ? (classCodeInput.value ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(code))
        {
            Debug.LogWarning("[JOIN CLASS] Sınıf kodu boş olamaz.");
            return;
        }

        StartCoroutine(JoinClass(code));
    }

    private void SetJoinModalOpen(bool open)
    {
        if (joinClassModal == null) return;

        if (open) joinClassModal.AddToClassList("open");
        else joinClassModal.RemoveFromClassList("open");
    }

    // -------------------------
    // API: list classes
    // -------------------------
    private IEnumerator FetchMyClasses()
    {
        if (router == null) yield break;
        if (classesRows == null)
        {
            Debug.LogError("[StudentDashboardController] ClassesRows not found (name=\"ClassesRows\").");
            yield break;
        }

        string url = router.ApiBaseUrl + myClassesPath;

        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CLASSES] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        Debug.Log("[CLASSES] OK => " + raw);

        var wrapped = JsonUtility.FromJson<ClassListWrapper>("{\"items\":" + raw + "}");
        var items = wrapped != null ? wrapped.items : null;

        lastItems = items;

        int totalCount = items != null ? items.Length : 0;
        int activeCount = 0;

        if (items != null)
        {
            foreach (var c in items)
                if (c != null && c.IsActive) activeCount++;
        }

        if (activeClassCountLabel != null) activeClassCountLabel.text = activeCount.ToString();
        if (totalClassCountLabel != null) totalClassCountLabel.text = totalCount.ToString();
        RefreshClassStatisticsCards();
        RefreshClassDetailsGeneralMetrics();

        ApplyHomeDashboardMetrics();

        if (currentSelectedClass != null && items != null)
        {
            var refreshed = Array.Find(items, x => x != null && x.Id == currentSelectedClass.Id);
            if (refreshed != null)
                currentSelectedClass = refreshed;
        }

        ApplyFiltersAndRender();
        BuildHomeworkBoard();
        PopulateClassDetailsHeader(currentSelectedClass);
    }

    // -------------------------
    // API: join class
    // -------------------------
    private IEnumerator JoinClass(string classCode)
    {
        if (router == null) yield break;

        string url = router.ApiBaseUrl + joinClassPath;

        var payload = new JoinClassRequest
        {
            ClassCode = classCode
        };

        string json = JsonUtility.ToJson(payload);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JOIN CLASS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        Debug.Log("[JOIN CLASS] OK => " + (req.downloadHandler?.text ?? ""));

        var responseText = req.downloadHandler?.text ?? "";
        if (!string.IsNullOrWhiteSpace(responseText))
            Debug.Log("[JOIN CLASS] Sunucu yanıtı: " + responseText);

        SetJoinModalOpen(false);
        StartCoroutine(FetchMyClasses());
    }

    // -------------------------
    // Render
    // -------------------------
    private void RenderClasses(MyClassDto[] items)
    {
        if (classesRows == null) return;

        classesRows.Clear();

        if (items == null || items.Length == 0)
        {
            var empty = new Label("Hiç sınıf bulunamadı.");
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.paddingTop = 12;
            classesRows.Add(empty);
            return;
        }

        foreach (var c in items)
            if (c != null) classesRows.Add(BuildClassRow(c));
    }

    private VisualElement BuildClassRow(MyClassDto c)
    {
        var row = new VisualElement();
        row.AddToClassList("class-row");
        row.AddToClassList("class-card");

        row.Add(BuildColLabel("col class-name", string.IsNullOrWhiteSpace(c.Name) ? "-" : c.Name));
        row.Add(BuildColLabel("col class-lesson", string.IsNullOrWhiteSpace(c.LessonName) ? "-" : c.LessonName));

        var codeCol = new VisualElement();
        codeCol.AddToClassList("col");
        codeCol.AddToClassList("class-code");

        var codeLabel = new Label(string.IsNullOrWhiteSpace(c.Code) ? "-" : c.Code);
        codeLabel.AddToClassList("code-text");

        var copyBtn = new Button();
        copyBtn.AddToClassList("copy-btn");
        copyBtn.text = "";

        copyBtn.clicked += () => CopyCodeWithFeedback(copyBtn, c.Code);

        var copyIcon = new VisualElement();
        copyIcon.AddToClassList("icon");
        copyIcon.AddToClassList("icon-copy");
        copyBtn.Add(copyIcon);

        codeCol.Add(codeLabel);
        codeCol.Add(copyBtn);
        row.Add(codeCol);

        row.Add(BuildColLabel("col class-student", c.StudentCount.ToString()));
        row.Add(BuildColLabel("col class-assignment", c.AssignmentCount.ToString()));
        row.Add(BuildColLabel("col class-success", $"%{c.SuccessRatePercent}"));

        var statusCol = new VisualElement();
        statusCol.AddToClassList("col");
        statusCol.AddToClassList("class-status");

        bool isPending = string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        var badge = new Label(isPending ? "Beklemede" : (c.IsActive ? "Aktif" : "Pasif"));
        badge.AddToClassList("badge");
        if (!isPending && c.IsActive) badge.AddToClassList("active");

        statusCol.Add(badge);
        row.Add(statusCol);

        var actionsCol = new VisualElement();
        actionsCol.AddToClassList("col");
        actionsCol.AddToClassList("class-actions");

        var goBtn = new Button(() =>
        {
            Debug.Log("Sınıfa git: " + c.Id);
            currentSelectedClass = c;
            PopulateClassDetailsHeader(currentSelectedClass);

            SetMenuActive("ClassBtn");
            ShowPage("ClassDetailsPage");
            ShowClassDetailsTab("general");

            StartCoroutine(OpenClassDetailsNextFrame());
        });
        goBtn.AddToClassList("go-class-btn");
        goBtn.text = "Git";
        if (isPending)
        {
            goBtn.SetEnabled(false);
            goBtn.AddToClassList("disabled");
        }

        actionsCol.Add(goBtn);
        row.Add(actionsCol);

        return row;
    }

    private void CopyCodeWithFeedback(Button btn, string code)
    {
        code ??= "";
        GUIUtility.systemCopyBuffer = code;

        btn.AddToClassList("copied");
        StartCoroutine(RemoveCopiedClass(btn, 1f));
    }

    private IEnumerator RemoveCopiedClass(VisualElement el, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (el != null) el.RemoveFromClassList("copied");
    }

    private VisualElement BuildColLabel(string classList, string text)
    {
        var col = new VisualElement();
        foreach (var cls in classList.Split(' '))
            if (!string.IsNullOrWhiteSpace(cls)) col.AddToClassList(cls);

        col.Add(new Label(text));
        return col;
    }

    private void ApplyFiltersAndRender()
    {
        if (lastItems == null)
        {
            RenderClasses(null);
            return;
        }

        var filtered = lastItems;

        if (!includeInactive)
            filtered = Array.FindAll(filtered, c => c != null && c.IsActive);

        var q = (currentSearch ?? "").Trim();
        if (!string.IsNullOrEmpty(q))
        {
            var qLower = q.ToLowerInvariant();

            filtered = Array.FindAll(filtered, c =>
            {
                if (c == null) return false;

                string name = (c.Name ?? "").ToLowerInvariant();
                string lesson = (c.LessonName ?? "").ToLowerInvariant();
                string code = (c.Code ?? "").ToLowerInvariant();

                return name.Contains(qLower) || lesson.Contains(qLower) || code.Contains(qLower);
            });
        }

        RenderClasses(filtered);
    }


    private void ShowPage(string pageName)
    {
        foreach (var child in mainContent.Children())
            child.RemoveFromClassList("active");

        var page = mainContent.Q<VisualElement>(pageName);
        if (page == null)
        {
            Debug.LogError($"[StudentDashboardController] Page not found: {pageName}");
            return;
        }

        page.AddToClassList("active");

        if (pageName == "StartSimulationPage")
            mainContent.AddToClassList("hide-topbar");
        else
            mainContent.RemoveFromClassList("hide-topbar");
    }

    private void SetMenuActive(string activeButtonName)
    {
        var names = new[] { "HomeBtn", "ClassBtn", "StartSimulationBtn", "AssignmentBtn", "ProgressBtn", "CalendarBtn", "EmailBtn", "ActivityBtn", "ProfileBtn" };

        foreach (var n in names)
            root.Q<Button>(n)?.RemoveFromClassList("active");

        root.Q<Button>(activeButtonName)?.AddToClassList("active");
    }

    private void BindProfilePage()
    {
        profilePage = root.Q<VisualElement>("ProfilePage");
        if (profilePage == null)
            return;

        profileAvatarLabel = profilePage.Q<Label>("TeacherAvatarLabel");
        profileNameLabel = profilePage.Q<Label>("TeacherNameLabel");
        profileRoleLabel = profilePage.Q<Label>("TeacherRoleLabel");
        profileStatusLabel = profilePage.Q<Label>("TeacherStatusLabel");
        profileRegistryNoLabel = profilePage.Q<Label>("TeacherRegistryNoLabel");
        profileMailLabel = profilePage.Q<Label>("TeacherMailLabel");
        profileJoinDateLabel = profilePage.Q<Label>("TeacherJoinDateLabel");
        profileLastLoginLabel = profilePage.Q<Label>("TeacherLastLoginLabel");
        profileStatsGrid = profilePage.Q<VisualElement>(className: "teacher-stats-grid");

        var quickActions = profilePage.Q<VisualElement>(className: "teacher-quick-actions");
        if (quickActions != null)
        {
            var buttons = quickActions.Query<Button>().ToList();
            if (buttons.Count > 0) profileHomeBtn = buttons[0];
            if (buttons.Count > 1) profileClassesBtn = buttons[1];
            if (buttons.Count > 2) profileLogoutBtn = buttons[2];

            if (profileHomeBtn != null)
            {
                profileHomeBtn.clicked -= OnProfileHomeClicked;
                profileHomeBtn.clicked += OnProfileHomeClicked;
            }

            if (profileClassesBtn != null)
            {
                profileClassesBtn.clicked -= OnProfileClassesClicked;
                profileClassesBtn.clicked += OnProfileClassesClicked;
            }

            if (profileLogoutBtn != null)
            {
                profileLogoutBtn.clicked -= OnProfileLogoutClicked;
                profileLogoutBtn.clicked += OnProfileLogoutClicked;
            }
        }
    }

    private void OnProfileHomeClicked()
    {
        SetMenuActive("HomeBtn");
        ShowPage("HomePage");
        StartCoroutine(RefreshHomeDashboardData(forceRefresh: true));
    }

    private void OnProfileClassesClicked()
    {
        SetMenuActive("ClassBtn");
        ShowPage("ClassesPage");
        StartCoroutine(FetchMyClasses());
        StartCoroutine(FetchMyAssignments());
    }

    private void OnProfileLogoutClicked()
    {
        StartCoroutine(EndSessionAndLogout());
    }

    private IEnumerator LoadProfilePageData()
    {
        if (router == null)
            yield break;

        if (lastItems == null)
            yield return StartCoroutine(FetchMyClasses());

        if (assignmentItems == null)
            yield return StartCoroutine(FetchMyAssignments());

        ProfileMeDto me = null;
        string url = router.ApiBaseUrl + myProfilePath;

        using (var req = AuthedGet(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
                me = JsonUtility.FromJson<ProfileMeDto>(raw);
            }
            else
            {
                Debug.LogError($"[STUDENT PROFILE] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            }
        }

        ApplyProfileIdentity(me);
        BuildProfileStatsCards(me);
    }

    private void ApplyProfileIdentity(ProfileMeDto me)
    {
        string name = me != null && !string.IsNullOrWhiteSpace(me.name) ? me.name : (router?.CurrentName ?? "");
        string surname = me != null && !string.IsNullOrWhiteSpace(me.surname) ? me.surname : (router?.CurrentSurname ?? "");
        string fullName = $"{name} {surname}".Trim();

        if (profileNameLabel != null)
            profileNameLabel.text = string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;

        if (profileAvatarLabel != null)
            profileAvatarLabel.text = BuildInitialsFromName(fullName);

        if (profileRoleLabel != null)
            profileRoleLabel.text = !string.IsNullOrWhiteSpace(me?.roleName)
                ? me.roleName
                : (router?.CurrentRoleName ?? "Öğrenci");

        if (profileStatusLabel != null)
            profileStatusLabel.text = me != null && me.isActive ? "Aktif" : "Pasif";

        if (profileRegistryNoLabel != null)
        {
            int registryId = me != null && me.id > 0 ? me.id : (router != null ? router.CurrentUserId : 0);
            profileRegistryNoLabel.text = registryId > 0 ? registryId.ToString("D5") : "-";
        }

        if (profileMailLabel != null)
            profileMailLabel.text = !string.IsNullOrWhiteSpace(me?.email) ? me.email : "-";

        if (profileJoinDateLabel != null)
            profileJoinDateLabel.text = FormatDateTr(me?.createdAt);

        if (profileLastLoginLabel != null)
            profileLastLoginLabel.text = FormatDateTr(me?.lastLogin);
    }

    private void BuildProfileStatsCards(ProfileMeDto me)
    {
        if (profileStatsGrid == null)
            return;

        profileStatsGrid.Clear();

        var approvedClasses = (lastItems ?? Array.Empty<MyClassDto>())
            .Where(c => c != null && !string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        int activeClassCount = approvedClasses.Count(c => c.IsActive);

        var assignments = assignmentItems ?? Array.Empty<AssignmentDto>();
        int assignmentTotal = assignments.Length;
        int assignmentCompleted = assignments.Count(a => a != null && !a.IsActive);

        int averageSuccess = 0;
        if (approvedClasses.Length > 0)
            averageSuccess = Mathf.RoundToInt((float)approvedClasses.Average(c => Mathf.Max(c.SuccessRatePercent, 0)));

        int attendanceRate = approvedClasses.Length == 0
            ? 0
            : Mathf.RoundToInt((activeClassCount / (float)approvedClasses.Length) * 100f);

        int activityStreak = Math.Max(me?.currentActiveStreakDays ?? 0, 0);

        int classSize = approvedClasses.Length > 0
            ? Mathf.Max(approvedClasses.Max(c => Mathf.Max(c.StudentCount, 0)), 1)
            : 1;
        string classRankingValue = $"1/{classSize}";
        string classRankingChange = classSize <= 1
            ? "Sınıfta tek öğrenci"
            : "Sınıf mevcuduna göre";

        int activeDays = Math.Max(me?.totalActiveDays ?? 0, 0);
        float totalActiveHours = Mathf.Max(me?.totalActiveHours ?? 0f, 0f);

        int completedExperiments = 0;

        profileStatsGrid.Add(BuildProfileStatCard($"{assignmentCompleted}/{assignmentTotal}", "Tamamlanan Ödev", assignmentTotal > 0 ? $"%{Mathf.RoundToInt((assignmentCompleted / (float)assignmentTotal) * 100f)} tamamlandı" : "Henüz ödev yok", true));
        profileStatsGrid.Add(BuildProfileStatCard(completedExperiments.ToString(), "Tamamlanan Deney", "İlerleme verisi geldikçe artar"));
        profileStatsGrid.Add(BuildProfileStatCard($"%{averageSuccess}", "Ortalama Başarı", averageSuccess >= 70 ? "İyi gidiyor" : "Geliştirilebilir", true));
        profileStatsGrid.Add(BuildProfileStatCard($"%{attendanceRate}", "Devam Oranı", attendanceRate >= 80 ? "Düzenli katılım" : "Katılımı artır", false));
        profileStatsGrid.Add(BuildProfileStatCard(activityStreak.ToString(), "Aktif Gün Serisi", "Üst üste giriş yapılan gün", false));
        profileStatsGrid.Add(BuildProfileStatCard(classRankingValue, "Sınıf Sıralaması", classRankingChange, false));
        profileStatsGrid.Add(BuildProfileStatCard(activeClassCount.ToString(), "Aktif Sınıf", "Onaylı sınıflar", false));
        profileStatsGrid.Add(BuildProfileStatCard(activeDays.ToString(), "Aktif Toplam Gün", $"Toplam süre: {totalActiveHours:0.0} saat", false));
    }

    private IEnumerator SessionHeartbeatLoop()
    {
        while (true)
        {
            if (router != null && !string.IsNullOrEmpty(router.AccessToken))
            {
                using var req = AuthedPost(router.ApiBaseUrl + sessionHeartbeatPath);
                yield return req.SendWebRequest();
            }

            yield return new WaitForSeconds(120f);
        }
    }

    private IEnumerator EndSessionAndLogout()
    {
        if (router != null && !string.IsNullOrEmpty(router.AccessToken))
        {
            using var req = AuthedPost(router.ApiBaseUrl + sessionEndPath);
            yield return req.SendWebRequest();
        }

        router?.ClearSession();
        router?.ShowLogin();
    }

    private void OnDisable()
    {
        if (sessionHeartbeatRoutine != null)
        {
            StopCoroutine(sessionHeartbeatRoutine);
            sessionHeartbeatRoutine = null;
        }
    }

    private VisualElement BuildProfileStatCard(string value, string label, string change, bool left = false)
    {
        var card = new VisualElement();
        card.AddToClassList("teacher-stat-card");
        if (left)
            card.AddToClassList("teacher-stat-card-left");

        var valueLabel = new Label(string.IsNullOrWhiteSpace(value) ? "0" : value);
        valueLabel.AddToClassList("stat-value");

        var titleLabel = new Label(string.IsNullOrWhiteSpace(label) ? "-" : label);
        titleLabel.AddToClassList("stat-label");

        var changeLabel = new Label(string.IsNullOrWhiteSpace(change) ? "-" : change);
        changeLabel.AddToClassList("stat-change");

        card.Add(valueLabel);
        card.Add(titleLabel);
        card.Add(changeLabel);
        return card;
    }

    private string FormatDateTr(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "-";

        var trCulture = new System.Globalization.CultureInfo("tr-TR");
        if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var iso))
            return iso.ToLocalTime().ToString("dd MMMM yyyy", trCulture);

        if (DateTime.TryParse(raw, out var dt))
            return dt.ToString("dd MMMM yyyy", trCulture);

        return "-";
    }

    private void BindClassDetailsTabs()
    {
        cdTabGeneralBtn = root.Q<Button>("cdTabGeneralBtn");
        cdTabAssignmentsBtn = root.Q<Button>("cdTabAssignmentsBtn");
        cdTabActivityBtn = root.Q<Button>("cdTabActivityBtn");

        classDetailsGeneralContent = root.Q<VisualElement>("ClassDetailsGeneralContent");
        classDetailsAssignmentsContent = root.Q<VisualElement>("ClassDetailsAssignmentsContent");
        classDetailsActivityContent = root.Q<VisualElement>("ClassDetailsActivityContent");
        cgCompletionPercentLabel = root.Q<Label>("CgCompletionPercentLabel");
        cgCompletionDoneLabel = root.Q<Label>("CgCompletionDoneLabel");
        cgCompletionRemainingLabel = root.Q<Label>("CgCompletionRemainingLabel");
        cgCompletionDonut = classDetailsGeneralContent != null
            ? classDetailsGeneralContent.Q<VisualElement>(className: "cg-donut")
            : null;
        cdClassNameLabel = root.Q<Label>("CdClassNameLabel");
        cdTeacherNameLabel = root.Q<Label>("CdTeacherNameLabel");
        cdStudentCountLabel = root.Q<Label>("CdStudentCountLabel");
        cdAssignmentCountLabel = root.Q<Label>("CdAssignmentCountLabel");
        cdSuccessRateLabel = root.Q<Label>("CdSuccessRateLabel");
        cdCreatedDateLabel = root.Q<Label>("CdCreatedDateLabel");
        cdClassCodeLabel = root.Q<Label>("CdClassCodeLabel");
        cdStatusLabel = root.Q<Label>("CdStatusLabel");
        assignmentsCardsRow = classDetailsAssignmentsContent != null
            ? classDetailsAssignmentsContent.Q<VisualElement>(className: "table-assignment-cards")
            : null;

        classGeneralChartValueLabels.Clear();
        classGeneralChartFillBars.Clear();
        classGeneralChartValueByDay.Clear();
        classGeneralChartFillByDay.Clear();
        if (classDetailsGeneralContent != null)
        {
            classGeneralChartValueLabels.AddRange(classDetailsGeneralContent.Query<Label>(className: "cg-bar-value").ToList());
            classGeneralChartFillBars.AddRange(classDetailsGeneralContent.Query<VisualElement>(className: "cg-bar-fill").ToList());

            var barItems = classDetailsGeneralContent.Query<VisualElement>(className: "cg-bar-item").ToList();
            foreach (var barItem in barItems)
            {
                if (barItem == null) continue;
                var dayLabel = barItem.Q<Label>(className: "cg-bar-label");
                var valueLabel = barItem.Q<Label>(className: "cg-bar-value");
                var fill = barItem.Q<VisualElement>(className: "cg-bar-fill");
                string dayKey = NormalizeChartDayKey(dayLabel != null ? dayLabel.text : null);
                if (string.IsNullOrWhiteSpace(dayKey)) continue;
                if (valueLabel != null) classGeneralChartValueByDay[dayKey] = valueLabel;
                if (fill != null) classGeneralChartFillByDay[dayKey] = fill;
            }
        }

        assignmentFilterAllBtn = root.Q<Button>("AssignmentFilterAllBtn");
        assignmentFilterActiveBtn = root.Q<Button>("AssignmentFilterActiveBtn");
        assignmentFilterPassiveBtn = root.Q<Button>("AssignmentFilterPassiveBtn");
        assignmentFilterCompletedBtn = root.Q<Button>("AssignmentFilterCompletedBtn");
        assignmentFilterIncompleteBtn = root.Q<Button>("AssignmentFilterIncompleteBtn");
        assignmentSearchInput = root.Q<TextField>("AssignmentSearchInput");

        var activityScroll = root.Q<ScrollView>("ActivityFeedScroll");
        classDetailsActivityFeed = activityScroll != null ? activityScroll.contentContainer : null;

        cdTabGeneralBtn?.RegisterCallback<ClickEvent>(_ => ShowClassDetailsTab("general"));
        cdTabAssignmentsBtn?.RegisterCallback<ClickEvent>(_ => ShowClassDetailsTab("assignments"));
        cdTabActivityBtn?.RegisterCallback<ClickEvent>(_ => ShowClassDetailsTab("activity"));
        BindAssignmentFilters();

        HideElement(classDetailsGeneralContent);
        HideElement(classDetailsAssignmentsContent);
        HideElement(classDetailsActivityContent);
    }

    private void PopulateClassDetailsHeader(MyClassDto c)
    {
        if (c == null)
            return;

        if (cdClassNameLabel != null)
            cdClassNameLabel.text = string.IsNullOrWhiteSpace(c.Name) ? "-" : c.Name;

        if (cdTeacherNameLabel != null)
            cdTeacherNameLabel.text = string.IsNullOrWhiteSpace(c.TeacherName) ? "-" : c.TeacherName;

        if (cdStudentCountLabel != null)
            cdStudentCountLabel.text = Mathf.Max(c.StudentCount, 0).ToString();

        int assignmentCount = c.AssignmentCount;
        if (assignmentItems != null)
            assignmentCount = assignmentItems.Count(a => a != null && a.ClassId == c.Id);

        if (cdAssignmentCountLabel != null)
            cdAssignmentCountLabel.text = Mathf.Max(assignmentCount, 0).ToString();

        if (cdSuccessRateLabel != null)
            cdSuccessRateLabel.text = "%" + Mathf.Max(c.SuccessRatePercent, 0);

        if (cdCreatedDateLabel != null)
            cdCreatedDateLabel.text = FormatDisplayDate(c.JoinedAt, c.CreatedAt);

        if (cdClassCodeLabel != null)
            cdClassCodeLabel.text = string.IsNullOrWhiteSpace(c.Code) ? "-" : c.Code;

        if (cdStatusLabel != null)
        {
            bool isPending = string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase);
            cdStatusLabel.text = isPending ? "Beklemede" : (c.IsActive ? "Aktif" : "Pasif");
        }
    }

    private string FormatDisplayDate(string joinedAtRaw, string createdAtRaw)
    {
        var trCulture = new System.Globalization.CultureInfo("tr-TR");

        if (DateTime.TryParse(joinedAtRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var joinedIso))
            return joinedIso.ToLocalTime().ToString("dd MMMM yyyy", trCulture);

        if (DateTime.TryParse(joinedAtRaw, out var joined))
            return joined.ToString("dd MMMM yyyy", trCulture);

        if (DateTime.TryParse(createdAtRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdIso))
            return createdIso.ToLocalTime().ToString("dd MMMM yyyy", trCulture);

        if (DateTime.TryParse(createdAtRaw, out var created))
            return created.ToString("dd MMMM yyyy", trCulture);

        return "-";
    }

    private void ShowClassDetailsTab(string tabName)
    {
        // önce tüm tab butonlarından active kaldır
        cdTabGeneralBtn?.RemoveFromClassList("active");
        cdTabAssignmentsBtn?.RemoveFromClassList("active");
        cdTabActivityBtn?.RemoveFromClassList("active");

        // önce tüm içerikleri kapat
        HideElement(classDetailsGeneralContent);
        HideElement(classDetailsAssignmentsContent);
        HideElement(classDetailsActivityContent);

        // seçileni aç
        switch (tabName)
        {
            case "general":
                cdTabGeneralBtn?.AddToClassList("active");
                ShowElement(classDetailsGeneralContent);
                RefreshClassDetailsGeneralMetrics();
                StartCoroutine(FetchClassActivityForStudent());
                break;


            case "assignments":
                cdTabAssignmentsBtn?.AddToClassList("active");
                ShowElement(classDetailsAssignmentsContent);
                StartCoroutine(FetchMyAssignments());
                BuildStudentAssignmentCards();
                break;

            case "activity":
                cdTabActivityBtn?.AddToClassList("active");
                ShowElement(classDetailsActivityContent);
                StartCoroutine(FetchClassActivityForStudent());
                break;


            default:
                cdTabGeneralBtn?.AddToClassList("active");
                ShowElement(classDetailsGeneralContent);
                RefreshClassDetailsGeneralMetrics();
                StartCoroutine(FetchClassActivityForStudent());
                break;
        }
    }

    private void RefreshClassDetailsGeneralMetrics()
    {
        int classId = currentSelectedClass != null ? currentSelectedClass.Id : 0;
        var classAssignments = (assignmentItems ?? Array.Empty<AssignmentDto>())
            .Where(a => a != null && a.ClassId == classId)
            .ToArray();

        int totalAssignments = classAssignments.Length;
        int completedAssignments = classAssignments.Count(a => string.Equals(GetHomeworkStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
        int completionPercent = totalAssignments > 0
            ? Mathf.RoundToInt((completedAssignments / (float)totalAssignments) * 100f)
            : 0;

        if (cgCompletionPercentLabel != null)
            cgCompletionPercentLabel.text = $"%{completionPercent}";
        if (cgCompletionDoneLabel != null)
            cgCompletionDoneLabel.text = $"%{completionPercent} tamamlandı";
        if (cgCompletionRemainingLabel != null)
            cgCompletionRemainingLabel.text = $"%{Mathf.Clamp(100 - completionPercent, 0, 100)} eksik";

        ApplyCompletionDonutStyle(cgCompletionDonut, completionPercent);

        var buckets = new float[7];
        DateTime weekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        DateTime weekEnd = weekStart.AddDays(6);
        if (currentActivityItems != null && currentActivityItems.Length > 0)
        {
            foreach (var item in currentActivityItems)
            {
                if (item == null) continue;
                var dt = ParseActivityDate(item.OccurredAt);
                if (dt == DateTime.MinValue) continue;
                if (dt.Date < weekStart || dt.Date > weekEnd) continue;
                int idx = (dt.Date - weekStart).Days;
                buckets[idx] += 1f;
            }
        }
        else
        {
            foreach (var assignment in classAssignments)
            {
                if (!TryParseLocalDateTime(assignment?.StartDate, out var dt))
                    continue;
                if (dt.Date < weekStart || dt.Date > weekEnd) continue;
                int idx = (dt.Date - weekStart).Days;
                buckets[idx] += 1f;
            }
        }

        ApplyGeneralChartBuckets(buckets);
    }

    private void ApplyGeneralChartBuckets(float[] buckets)
    {
        if (buckets == null || buckets.Length < 7)
            return;

        string[] dayOrder = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cts", "Paz" };
        float peak = buckets.Max();

        for (int i = 0; i < dayOrder.Length; i++)
        {
            string key = dayOrder[i];
            if (classGeneralChartValueByDay.TryGetValue(key, out var valueLabel) && valueLabel != null)
                valueLabel.text = Mathf.RoundToInt(buckets[i]).ToString();

            if (classGeneralChartFillByDay.TryGetValue(key, out var fill) && fill != null)
            {
                float ratio = peak > 0f ? buckets[i] / peak : 0f;
                fill.style.height = 14f + (ratio * 96f);
            }
        }
    }

    private string NormalizeChartDayKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string text = raw.Trim();
        return text switch
        {
            "Cmt" => "Cts",
            _ => text
        };
    }

    private void ApplyCompletionDonutStyle(VisualElement donut, int percent)
    {
        if (donut == null)
            return;

        int clamped = Mathf.Clamp(percent, 0, 100);
        int activeSides = clamped <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(clamped / 25f), 0, 4);

        Color inactive = new Color32(223, 231, 247, 255);
        Color active = clamped >= 75
            ? new Color32(67, 163, 109, 255)
            : clamped >= 40
                ? new Color32(217, 164, 65, 255)
                : new Color32(217, 83, 79, 255);

        donut.style.borderTopColor = activeSides >= 1 ? active : inactive;
        donut.style.borderRightColor = activeSides >= 2 ? active : inactive;
        donut.style.borderBottomColor = activeSides >= 3 ? active : inactive;
        donut.style.borderLeftColor = activeSides >= 4 ? active : inactive;
    }

    private void ShowElement(VisualElement el)
    {
        if (el == null) return;
        el.style.display = DisplayStyle.Flex;
    }

    private void HideElement(VisualElement el)
    {
        if (el == null) return;
        el.style.display = DisplayStyle.None;
    }

    private void BindAssignmentFilters()
    {
        if (assignmentFilterAllBtn != null)
            assignmentFilterAllBtn.clicked += () => { assignmentFilterMode = "all"; SetAssignmentFilterActive(assignmentFilterAllBtn); BuildStudentAssignmentCards(); };
        if (assignmentFilterActiveBtn != null)
            assignmentFilterActiveBtn.clicked += () => { assignmentFilterMode = "active"; SetAssignmentFilterActive(assignmentFilterActiveBtn); BuildStudentAssignmentCards(); };
        if (assignmentFilterPassiveBtn != null)
            assignmentFilterPassiveBtn.clicked += () => { assignmentFilterMode = "passive"; SetAssignmentFilterActive(assignmentFilterPassiveBtn); BuildStudentAssignmentCards(); };
        if (assignmentFilterCompletedBtn != null)
            assignmentFilterCompletedBtn.clicked += () => { assignmentFilterMode = "completed"; SetAssignmentFilterActive(assignmentFilterCompletedBtn); BuildStudentAssignmentCards(); };
        if (assignmentFilterIncompleteBtn != null)
            assignmentFilterIncompleteBtn.clicked += () => { assignmentFilterMode = "incomplete"; SetAssignmentFilterActive(assignmentFilterIncompleteBtn); BuildStudentAssignmentCards(); };

        SetAssignmentFilterActive(assignmentFilterAllBtn);

        if (assignmentSearchInput != null)
        {
            assignmentSearchInput.RegisterValueChangedCallback(evt =>
            {
                assignmentSearchQuery = evt.newValue ?? "";
                BuildStudentAssignmentCards();
            });
        }
    }

    private void SetAssignmentFilterActive(Button activeButton)
    {
        assignmentFilterAllBtn?.RemoveFromClassList("active");
        assignmentFilterActiveBtn?.RemoveFromClassList("active");
        assignmentFilterPassiveBtn?.RemoveFromClassList("active");
        assignmentFilterCompletedBtn?.RemoveFromClassList("active");
        assignmentFilterIncompleteBtn?.RemoveFromClassList("active");
        activeButton?.AddToClassList("active");
    }

    private void BindHomeworkPage()
    {
        assignmentPage = root.Q<VisualElement>("AssignmentPage");
        if (assignmentPage == null)
            return;

        hwSearchField = assignmentPage.Q<TextField>("HwSearchField");
        hwFilterSubjectDropdown = assignmentPage.Q<DropdownField>("HwFilterSubjectDropdown");
        hwFilterStatusDropdown = assignmentPage.Q<DropdownField>("HwFilterStatusDropdown");
        hwNotStartedCards = assignmentPage.Q<ScrollView>("HwNotStartedCards");
        hwInProgressCards = assignmentPage.Q<ScrollView>("HwInProgressCards");
        hwCompletedCards = assignmentPage.Q<ScrollView>("HwCompletedCards");

        var counts = assignmentPage.Query<Label>(className: "hw-col-count").ToList();
        if (counts.Count >= 3)
        {
            hwNotStartedCountLabel = counts[0];
            hwInProgressCountLabel = counts[1];
            hwCompletedCountLabel = counts[2];
        }

        if (hwSearchField != null)
            hwSearchField.RegisterValueChangedCallback(_ => BuildHomeworkBoard());

        if (hwFilterSubjectDropdown != null)
            hwFilterSubjectDropdown.RegisterValueChangedCallback(_ => BuildHomeworkBoard());

        if (hwFilterStatusDropdown != null)
            hwFilterStatusDropdown.RegisterValueChangedCallback(_ => BuildHomeworkBoard());
    }

    private void BuildHomeworkBoard()
    {
        var ns = hwNotStartedCards != null ? hwNotStartedCards.contentContainer : null;
        var ip = hwInProgressCards != null ? hwInProgressCards.contentContainer : null;
        var cp = hwCompletedCards != null ? hwCompletedCards.contentContainer : null;

        ns?.Clear();
        ip?.Clear();
        cp?.Clear();

        RefreshHomeworkSubjectChoices();

        if (assignmentItems == null || assignmentItems.Length == 0)
        {
            ns?.Add(BuildHomeworkEmptyCard("Ödev bulunamadı."));
            if (hwNotStartedCountLabel != null) hwNotStartedCountLabel.text = "0";
            if (hwInProgressCountLabel != null) hwInProgressCountLabel.text = "0";
            if (hwCompletedCountLabel != null) hwCompletedCountLabel.text = "0";
            return;
        }

        string q = ((hwSearchField != null ? hwSearchField.value : "") ?? "").Trim().ToLowerInvariant();
        string subject = hwFilterSubjectDropdown != null ? (hwFilterSubjectDropdown.value ?? "Tüm Dersler") : "Tüm Dersler";
        string statusFilter = hwFilterStatusDropdown != null ? (hwFilterStatusDropdown.value ?? "Tüm Durumlar") : "Tüm Durumlar";

        int nsCount = 0;
        int ipCount = 0;
        int cpCount = 0;

        foreach (var a in assignmentItems)
        {
            if (a == null) continue;

            string lesson = ResolveAssignmentLesson(a);
            string experiment = a.ExperimentName ?? "-";
            string title = a.Title ?? "-";
            string desc = string.IsNullOrWhiteSpace(experiment) ? "-" : experiment;

            if (!string.IsNullOrWhiteSpace(q))
            {
                string haystack = $"{title} {experiment} {lesson}".ToLowerInvariant();
                if (!haystack.Contains(q))
                    continue;
            }

            if (!string.Equals(subject, "Tüm Dersler", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(lesson, subject, StringComparison.OrdinalIgnoreCase))
                continue;

            string status = GetHomeworkStatus(a);
            if (!string.Equals(statusFilter, "Tüm Durumlar", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, statusFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (status == "Başlanmadı")
            {
                ns?.Add(BuildHomeworkNotStartedCard(title, lesson, desc, a));
                nsCount++;
            }
            else if (status == "Devam Ediyor")
            {
                ip?.Add(BuildHomeworkInProgressCard(title, lesson, desc, a));
                ipCount++;
            }
            else
            {
                cp?.Add(BuildHomeworkCompletedCard(title, lesson, desc, a));
                cpCount++;
            }
        }

        if (nsCount == 0) ns?.Add(BuildHomeworkEmptyCard("Başlanmadı ödev yok."));
        if (ipCount == 0) ip?.Add(BuildHomeworkEmptyCard("Devam eden ödev yok."));
        if (cpCount == 0) cp?.Add(BuildHomeworkEmptyCard("Tamamlanan ödev yok."));

        if (hwNotStartedCountLabel != null) hwNotStartedCountLabel.text = nsCount.ToString();
        if (hwInProgressCountLabel != null) hwInProgressCountLabel.text = ipCount.ToString();
        if (hwCompletedCountLabel != null) hwCompletedCountLabel.text = cpCount.ToString();
    }

    private void RefreshHomeworkSubjectChoices()
    {
        if (hwFilterSubjectDropdown == null)
            return;

        var choices = new List<string> { "Tüm Dersler" };
        if (lastItems != null)
        {
            foreach (var c in lastItems)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.LessonName))
                    continue;
                if (!choices.Contains(c.LessonName))
                    choices.Add(c.LessonName);
            }
        }

        string current = hwFilterSubjectDropdown.value;
        hwFilterSubjectDropdown.choices = choices;
        if (!choices.Contains(current))
            hwFilterSubjectDropdown.SetValueWithoutNotify("Tüm Dersler");
    }

    private string ResolveAssignmentLesson(AssignmentDto a)
    {
        if (a == null || lastItems == null)
            return "-";

        foreach (var c in lastItems)
        {
            if (c == null) continue;
            if (c.Id != a.ClassId) continue;
            return string.IsNullOrWhiteSpace(c.LessonName) ? "-" : c.LessonName;
        }

        return "-";
    }

    private string GetHomeworkStatus(AssignmentDto a)
    {
        if (a == null)
            return "Başlanmadı";

        if (!DateTime.TryParse(a.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return a.IsActive ? "Başlanmadı" : "Tamamlandı";

        DateTime start = parsed.ToLocalTime().Date;
        DateTime today = DateTime.Today;
        int duration = a.DurationDays <= 0 ? 1 : a.DurationDays;
        DateTime endExclusive = start.AddDays(duration);

        if (today < start)
            return "Başlanmadı";

        if (!a.IsActive || today >= endExclusive)
            return "Tamamlandı";

        return "Devam Ediyor";
    }

    private VisualElement BuildHomeworkNotStartedCard(string title, string lesson, string desc, AssignmentDto a)
    {
        var card = new VisualElement();
        card.AddToClassList("hw-card");
        card.AddToClassList("not-started-border");

        var top = new VisualElement();
        top.AddToClassList("hw-card-top");
        top.Add(new Label(title) { name = "hwTitle" });
        top.Q<Label>("hwTitle").AddToClassList("hw-card-title");

        var pr = new Label(GetHomeworkPriority(a.DurationDays));
        pr.AddToClassList("hw-priority");
        if (pr.text == "Yüksek") pr.AddToClassList("high");
        else if (pr.text == "Orta") pr.AddToClassList("medium");
        else pr.AddToClassList("low");
        top.Add(pr);

        var subject = new Label(lesson);
        subject.AddToClassList("hw-card-subject");
        var d = new Label(desc);
        d.AddToClassList("hw-card-desc");

        var due = BuildHomeworkDue(a, overdue: false);

        card.Add(top);
        card.Add(subject);
        card.Add(d);
        card.Add(due);
        return card;
    }

    private VisualElement BuildHomeworkInProgressCard(string title, string lesson, string desc, AssignmentDto a)
    {
        var card = new VisualElement();
        card.AddToClassList("hw-card");
        card.AddToClassList("in-progress-border");

        var top = new VisualElement();
        top.AddToClassList("hw-card-top");
        var t = new Label(title);
        t.AddToClassList("hw-card-title");
        var pr = new Label(GetHomeworkPriority(a.DurationDays));
        pr.AddToClassList("hw-priority");
        if (pr.text == "Yüksek") pr.AddToClassList("high");
        else if (pr.text == "Orta") pr.AddToClassList("medium");
        else pr.AddToClassList("low");
        top.Add(t);
        top.Add(pr);

        var subject = new Label(lesson);
        subject.AddToClassList("hw-card-subject");
        var d = new Label(desc);
        d.AddToClassList("hw-card-desc");

        int pct = GetHomeworkProgressPercent(a);
        var progress = new VisualElement();
        progress.AddToClassList("hw-card-progress");
        var row = new VisualElement();
        row.AddToClassList("hw-card-progress-row");
        var txt = new Label("İlerleme");
        txt.AddToClassList("hw-card-progress-text");
        var val = new Label($"%{pct}");
        val.AddToClassList("hw-card-progress-val");
        row.Add(txt);
        row.Add(val);
        var track = new VisualElement();
        track.AddToClassList("hw-card-track");
        var fill = new VisualElement();
        fill.AddToClassList("hw-card-fill");
        fill.style.width = new Length(pct, LengthUnit.Percent);
        track.Add(fill);
        progress.Add(row);
        progress.Add(track);

        var due = BuildHomeworkDue(a, overdue: false);

        card.Add(top);
        card.Add(subject);
        card.Add(d);
        card.Add(progress);
        card.Add(due);
        return card;
    }

    private VisualElement BuildHomeworkCompletedCard(string title, string lesson, string desc, AssignmentDto a)
    {
        var card = new VisualElement();
        card.AddToClassList("hw-card");
        card.AddToClassList("completed-border");

        var top = new VisualElement();
        top.AddToClassList("hw-card-top");
        var t = new Label(title);
        t.AddToClassList("hw-card-title");
        var pr = new Label(GetHomeworkPriority(a.DurationDays));
        pr.AddToClassList("hw-priority");
        if (pr.text == "Yüksek") pr.AddToClassList("high");
        else if (pr.text == "Orta") pr.AddToClassList("medium");
        else pr.AddToClassList("low");
        top.Add(t);
        top.Add(pr);

        var subject = new Label(lesson);
        subject.AddToClassList("hw-card-subject");
        var check = new Label("✓ Tamamlandı");
        check.AddToClassList("hw-card-check");

        var progress = new VisualElement();
        progress.AddToClassList("hw-card-progress");
        var track = new VisualElement();
        track.AddToClassList("hw-card-track");
        var fill = new VisualElement();
        fill.AddToClassList("hw-card-fill");
        fill.AddToClassList("done");
        fill.style.width = new Length(100, LengthUnit.Percent);
        track.Add(fill);
        progress.Add(track);

        card.Add(top);
        card.Add(subject);
        card.Add(check);
        card.Add(progress);
        return card;
    }

    private VisualElement BuildHomeworkDue(AssignmentDto a, bool overdue)
    {
        var dueWrap = new VisualElement();
        dueWrap.AddToClassList("hw-card-due");
        if (overdue) dueWrap.AddToClassList("overdue");

        var icon = new VisualElement();
        icon.AddToClassList("hw-due-icon");
        if (overdue) icon.AddToClassList("overdue");

        DateTime dueDate = DateTime.Today;
        if (DateTime.TryParse(a.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            dueDate = parsed.ToLocalTime().Date.AddDays(Mathf.Max(a.DurationDays, 1) - 1);

        var dueText = new Label("Son Teslim: " + dueDate.ToString("dd MMMM yyyy"));
        dueText.AddToClassList("hw-card-due-text");
        if (overdue) dueText.AddToClassList("overdue");

        dueWrap.Add(icon);
        dueWrap.Add(dueText);
        return dueWrap;
    }

    private string GetHomeworkPriority(int durationDays)
    {
        if (durationDays <= 2) return "Yüksek";
        if (durationDays <= 5) return "Orta";
        return "Düşük";
    }

    private int GetHomeworkProgressPercent(AssignmentDto a)
    {
        if (a == null) return 0;
        if (!DateTime.TryParse(a.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return 0;

        DateTime start = parsed.ToLocalTime().Date;
        int duration = Mathf.Max(a.DurationDays, 1);
        DateTime today = DateTime.Today;

        if (today <= start) return 0;

        int elapsed = (today - start).Days;
        int percent = Mathf.RoundToInt((elapsed / (float)duration) * 100f);
        return Mathf.Clamp(percent, 0, 99);
    }

    private VisualElement BuildHomeworkEmptyCard(string text)
    {
        var card = new VisualElement();
        card.AddToClassList("hw-card");
        var label = new Label(text);
        label.AddToClassList("hw-card-desc");
        card.Add(label);
        return card;
    }

    private void BindProgressPage()
    {
        progressPageContent = root.Q<VisualElement>("ProgressPageContent");
        if (progressPageContent == null)
            return;

        progressTabsBar = progressPageContent.Q<VisualElement>(className: "progress-tabs-bar");
        progressNotStartedScroll = progressPageContent.Q<ScrollView>("ProgressNotStartedScroll");
        progressInProgressScroll = progressPageContent.Q<ScrollView>("ProgressInProgressScroll");
        progressCompletedScroll = progressPageContent.Q<ScrollView>("ProgressCompletedScroll");

        var countLabels = progressPageContent.Query<Label>(className: "progress-column-count").ToList();
        if (countLabels.Count >= 3)
        {
            progressNotStartedCountLabel = countLabels[0];
            progressInProgressCountLabel = countLabels[1];
            progressCompletedCountLabel = countLabels[2];
        }

        overallPercentLabel = progressPageContent.Q<Label>("OverallPercentLabel");
        overallFillBar = progressPageContent.Q<VisualElement>("OverallFillBar");
        subjectProgressLabel = progressPageContent.Q<Label>("SubjectProgressLabel");
        subjectPercentLabel = progressPageContent.Q<Label>("SubjectPercentLabel");
        subjectFillBar = progressPageContent.Q<VisualElement>("SubjectFillBar");
        subjectSubLabel = progressPageContent.Q<Label>("SubjectSubLabel");

        progressSearchField = progressPageContent.Q<TextField>("ProgressSearchField");
        progressStatusDropdown = progressPageContent.Q<DropdownField>("ProgressStatusDropdown");
        progressDifficultyDropdown = progressPageContent.Q<DropdownField>("ProgressDifficultyDropdown");

        if (progressSearchField != null)
            progressSearchField.RegisterValueChangedCallback(_ => RenderProgressColumns());
        if (progressStatusDropdown != null)
            progressStatusDropdown.RegisterValueChangedCallback(_ => RenderProgressColumns());
        if (progressDifficultyDropdown != null)
            progressDifficultyDropdown.RegisterValueChangedCallback(_ => RenderProgressColumns());
    }

    private IEnumerator LoadProgressPageData()
    {
        if (lastItems == null || lastItems.Length == 0)
            yield return StartCoroutine(FetchMyClasses());

        SetupProgressLessonsFromActiveClass();
        BuildProgressLessonTabs();

        if (progressLessonTabs == null || progressLessonTabs.Count == 0)
        {
            progressExperimentItems = Array.Empty<ExperimentDto>();
            RenderProgressColumns();
            yield break;
        }

        if (string.IsNullOrWhiteSpace(progressSelectedLesson) || !progressLessonTabs.Contains(progressSelectedLesson))
            progressSelectedLesson = progressLessonTabs[0];

        yield return StartCoroutine(FetchProgressExperiments(progressSelectedLesson));
    }

    private void SetupProgressLessonsFromActiveClass()
    {
        progressLessonTabs = new List<string>();

        string grade = ResolveActiveClassGradeLevel();
        bool isFiveToEight = IsGradeInRange(grade, 5, 8);

        if (isFiveToEight)
        {
            progressLessonTabs.Add("Fen Bilimleri");
            progressLessonTabs.Add("Matematik");
        }
        else
        {
            progressLessonTabs.Add("Fizik");
            progressLessonTabs.Add("Kimya");
            progressLessonTabs.Add("Biyoloji");
            progressLessonTabs.Add("Matematik");
        }
    }

    private string ResolveActiveClassGradeLevel()
    {
        if (lastItems == null || lastItems.Length == 0)
            return "";

        foreach (var c in lastItems)
        {
            if (c == null) continue;
            if (!c.IsActive) continue;
            if (string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase)) continue;
            return c.GradeLevel ?? "";
        }

        foreach (var c in lastItems)
        {
            if (c == null) continue;
            if (string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase)) continue;
            return c.GradeLevel ?? "";
        }

        return "";
    }

    private bool IsGradeInRange(string gradeText, int min, int max)
    {
        if (string.IsNullOrWhiteSpace(gradeText))
            return false;

        if (!int.TryParse(gradeText.Trim(), out int grade))
            return false;

        return grade >= min && grade <= max;
    }

    private void BuildProgressLessonTabs()
    {
        if (progressTabsBar == null)
            return;

        progressTabsBar.Clear();

        if (progressLessonTabs == null || progressLessonTabs.Count == 0)
            return;

        foreach (var lesson in progressLessonTabs)
        {
            var btn = new Button();
            btn.AddToClassList("progress-tab");
            if (lesson == progressSelectedLesson)
                btn.AddToClassList("active");

            var icon = new Label(GetProgressLessonIcon(lesson));
            icon.AddToClassList("progress-tab-icon");

            var txt = new Label(lesson);
            txt.AddToClassList("progress-tab-text");

            btn.Add(icon);
            btn.Add(txt);

            string capturedLesson = lesson;
            btn.clicked += () =>
            {
                progressSelectedLesson = capturedLesson;
                BuildProgressLessonTabs();
                StartCoroutine(FetchProgressExperiments(capturedLesson));
            };

            progressTabsBar.Add(btn);
        }
    }

    private string GetProgressLessonIcon(string lesson)
    {
        if (string.Equals(lesson, "Matematik", StringComparison.OrdinalIgnoreCase)) return "📐";
        if (string.Equals(lesson, "Fizik", StringComparison.OrdinalIgnoreCase)) return "⚛";
        if (string.Equals(lesson, "Kimya", StringComparison.OrdinalIgnoreCase)) return "🧪";
        if (string.Equals(lesson, "Biyoloji", StringComparison.OrdinalIgnoreCase)) return "🧬";
        return "🔬";
    }

    private IEnumerator FetchProgressExperiments(string lessonDisplayName)
    {
        if (router == null)
            yield break;

        string grade = ResolveActiveClassGradeLevel();
        string lessonApi = MapProgressLessonToApiLesson(lessonDisplayName);

        string url = router.ApiBaseUrl
            + experimentsByGradeLessonPath
            + "?gradeLevel=" + UnityWebRequest.EscapeURL(grade ?? "")
            + "&lessonName=" + UnityWebRequest.EscapeURL(lessonApi ?? "");

        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[PROGRESS EXPERIMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            progressExperimentItems = Array.Empty<ExperimentDto>();
            RenderProgressColumns();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ExperimentListWrapper>("{\"items\":" + raw + "}");
        progressExperimentItems = wrapped != null && wrapped.items != null
            ? wrapped.items
            : Array.Empty<ExperimentDto>();

        RenderProgressColumns();
    }

    private string MapProgressLessonToApiLesson(string lessonDisplayName)
    {
        if (string.Equals(lessonDisplayName, "Fen Bilimleri", StringComparison.OrdinalIgnoreCase))
            return "Fen";

        return lessonDisplayName;
    }

    private void RenderProgressColumns()
    {
        var notStartedContent = progressNotStartedScroll != null ? progressNotStartedScroll.contentContainer : null;
        var inProgressContent = progressInProgressScroll != null ? progressInProgressScroll.contentContainer : null;
        var completedContent = progressCompletedScroll != null ? progressCompletedScroll.contentContainer : null;

        notStartedContent?.Clear();
        inProgressContent?.Clear();
        completedContent?.Clear();

        var experiments = progressExperimentItems ?? Array.Empty<ExperimentDto>();

        string search = (progressSearchField != null ? progressSearchField.value : "") ?? "";
        string q = search.Trim().ToLowerInvariant();
        string status = progressStatusDropdown != null ? (progressStatusDropdown.value ?? "Tüm Durumlar") : "Tüm Durumlar";
        string difficulty = progressDifficultyDropdown != null ? (progressDifficultyDropdown.value ?? "Tüm Zorluklar") : "Tüm Zorluklar";

        int notStartedCount = 0;

        bool statusAllowsNotStarted = status == "Tüm Durumlar" || status == "Başlanmadı";
        if (statusAllowsNotStarted)
        {
            foreach (var exp in experiments)
            {
                if (exp == null) continue;

                string name = exp.ExperimentName ?? "-";
                string desc = string.IsNullOrWhiteSpace(exp.UnitName) ? "Deney açıklaması bulunmuyor." : exp.UnitName;
                string level = GetProgressDifficulty(exp);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    string haystack = $"{name} {desc}".ToLowerInvariant();
                    if (!haystack.Contains(q))
                        continue;
                }

                if (!string.Equals(difficulty, "Tüm Zorluklar", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(level, difficulty, StringComparison.OrdinalIgnoreCase))
                    continue;

                notStartedContent?.Add(BuildProgressNotStartedCard(name, desc, level));
                notStartedCount++;
            }
        }

        if (notStartedCount == 0)
        {
            string emptyText = string.Equals(progressSelectedLesson, "Matematik", StringComparison.OrdinalIgnoreCase)
                ? "Deney bulunamadı."
                : "Bu filtre için deney bulunamadı.";
            notStartedContent?.Add(BuildProgressEmptyCard(emptyText));
        }

        inProgressContent?.Add(BuildProgressEmptyCard("Henüz devam eden deney bulunmuyor."));
        completedContent?.Add(BuildProgressEmptyCard("Henüz tamamlanan deney bulunmuyor."));

        if (progressNotStartedCountLabel != null) progressNotStartedCountLabel.text = notStartedCount.ToString();
        if (progressInProgressCountLabel != null) progressInProgressCountLabel.text = "0";
        if (progressCompletedCountLabel != null) progressCompletedCountLabel.text = "0";

        if (overallPercentLabel != null) overallPercentLabel.text = "%0";
        if (subjectPercentLabel != null) subjectPercentLabel.text = "%0";
        if (subjectProgressLabel != null)
            subjectProgressLabel.text = string.IsNullOrWhiteSpace(progressSelectedLesson)
                ? "Ders İlerlemesi"
                : progressSelectedLesson + " İlerlemesi";
        if (subjectSubLabel != null)
            subjectSubLabel.text = $"{notStartedCount} deney başlanmadı";

        if (overallFillBar != null)
            overallFillBar.style.width = new Length(0, LengthUnit.Percent);
        if (subjectFillBar != null)
            subjectFillBar.style.width = new Length(0, LengthUnit.Percent);
    }

    private string GetProgressDifficulty(ExperimentDto exp)
    {
        int hash = Mathf.Abs((exp.ExperimentName ?? "").GetHashCode());
        int mod = hash % 3;
        if (mod == 0) return "Kolay";
        if (mod == 1) return "Orta";
        return "Zor";
    }

    private VisualElement BuildProgressNotStartedCard(string title, string description, string difficulty)
    {
        var card = new VisualElement();
        card.AddToClassList("exp-card");

        var top = new VisualElement();
        top.AddToClassList("exp-card-top");

        var titleLabel = new Label(string.IsNullOrWhiteSpace(title) ? "-" : title);
        titleLabel.AddToClassList("exp-card-title");

        var badge = new Label(difficulty);
        badge.AddToClassList("exp-card-badge");
        if (string.Equals(difficulty, "Kolay", StringComparison.OrdinalIgnoreCase)) badge.AddToClassList("easy");
        else if (string.Equals(difficulty, "Orta", StringComparison.OrdinalIgnoreCase)) badge.AddToClassList("medium");
        else badge.AddToClassList("hard");

        top.Add(titleLabel);
        top.Add(badge);

        var desc = new Label(string.IsNullOrWhiteSpace(description) ? "Deney açıklaması bulunmuyor." : description);
        desc.AddToClassList("exp-card-desc");

        card.Add(top);
        card.Add(desc);
        return card;
    }

    private VisualElement BuildProgressEmptyCard(string text)
    {
        var card = new VisualElement();
        card.AddToClassList("exp-card");

        var label = new Label(text);
        label.AddToClassList("exp-card-desc");

        card.Add(label);
        return card;
    }

    private void BindPersonalActivityPage()
    {
        personalActivityPage = root.Q<VisualElement>("ActivityPage");
        if (personalActivityPage == null)
            return;

        personalActivityFeed = personalActivityPage.Q<VisualElement>("ActFeed");
        actTabAllBtn = personalActivityPage.Q<Button>("ActTabAllBtn");
        actTabExperimentBtn = personalActivityPage.Q<Button>("ActTabExperimentBtn");
        actTabAssignmentBtn = personalActivityPage.Q<Button>("ActTabAssignmentBtn");
        actTabProgressBtn = personalActivityPage.Q<Button>("ActTabProgressBtn");
        actTabParticipationBtn = personalActivityPage.Q<Button>("ActTabParticipationBtn");
        personalActivitySearchInput = personalActivityPage.Q<TextField>("AssignmentSearchInput");
        actDateFilterDropdown = personalActivityPage.Q<DropdownField>("ActDateFilterDropdown");

        if (actTabAllBtn != null)
            actTabAllBtn.clicked += () => { personalActivityFilterMode = "all"; SetPersonalActivityTabActive(actTabAllBtn); BuildPersonalActivityFeed(); };
        if (actTabExperimentBtn != null)
            actTabExperimentBtn.clicked += () => { personalActivityFilterMode = "experiment"; SetPersonalActivityTabActive(actTabExperimentBtn); BuildPersonalActivityFeed(); };
        if (actTabAssignmentBtn != null)
            actTabAssignmentBtn.clicked += () => { personalActivityFilterMode = "assignment"; SetPersonalActivityTabActive(actTabAssignmentBtn); BuildPersonalActivityFeed(); };
        if (actTabProgressBtn != null)
            actTabProgressBtn.clicked += () => { personalActivityFilterMode = "progress"; SetPersonalActivityTabActive(actTabProgressBtn); BuildPersonalActivityFeed(); };
        if (actTabParticipationBtn != null)
            actTabParticipationBtn.clicked += () => { personalActivityFilterMode = "participation"; SetPersonalActivityTabActive(actTabParticipationBtn); BuildPersonalActivityFeed(); };

        if (personalActivitySearchInput != null)
        {
            personalActivitySearchInput.RegisterValueChangedCallback(evt =>
            {
                personalActivitySearchQuery = evt.newValue ?? "";
                BuildPersonalActivityFeed();
            });
        }

        if (actDateFilterDropdown != null)
            actDateFilterDropdown.RegisterValueChangedCallback(_ => BuildPersonalActivityFeed());

        SetPersonalActivityTabActive(actTabAllBtn);
    }

    private void SetPersonalActivityTabActive(Button activeButton)
    {
        actTabAllBtn?.RemoveFromClassList("active");
        actTabExperimentBtn?.RemoveFromClassList("active");
        actTabAssignmentBtn?.RemoveFromClassList("active");
        actTabProgressBtn?.RemoveFromClassList("active");
        actTabParticipationBtn?.RemoveFromClassList("active");
        activeButton?.AddToClassList("active");
    }

    private IEnumerator FetchPersonalActivity()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + personalActivityPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[STUDENT PERSONAL ACTIVITY] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            personalActivityItems = Array.Empty<ClassActivityDto>();
            BuildPersonalActivityFeed();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassActivityListWrapper>("{\"items\":" + raw + "}");
        personalActivityItems = wrapped != null && wrapped.items != null
            ? wrapped.items
            : Array.Empty<ClassActivityDto>();

        BuildPersonalActivityFeed();
    }

    private void BuildPersonalActivityFeed()
    {
        if (personalActivityFeed == null)
            return;

        personalActivityFeed.Clear();

        if (personalActivityItems == null || personalActivityItems.Length == 0)
        {
            personalActivityFeed.Add(new Label("Kişisel aktivite bulunmuyor."));
            return;
        }

        string q = (personalActivitySearchQuery ?? "").Trim().ToLowerInvariant();
        string currentDate = null;

        foreach (var item in personalActivityItems)
        {
            if (item == null)
                continue;

            if (!MatchesPersonalActivityType(item.Type))
                continue;

            var dt = ParseActivityDate(item.OccurredAt);
            if (!MatchesPersonalActivityDateFilter(dt))
                continue;

            if (!string.IsNullOrWhiteSpace(q))
            {
                string haystack = $"{item.Title} {item.Description} {item.ActorName}".ToLowerInvariant();
                if (!haystack.Contains(q))
                    continue;
            }

            string key = dt.ToString("dd MMMM yyyy");
            if (!string.Equals(currentDate, key, StringComparison.Ordinal))
            {
                currentDate = key;
                var divider = new Label(key);
                divider.AddToClassList("act-date-divider");
                personalActivityFeed.Add(divider);
            }

            personalActivityFeed.Add(BuildPersonalActivityItem(item, dt));
        }

        if (personalActivityFeed.childCount == 0)
            personalActivityFeed.Add(new Label("Filtreye uygun kişisel aktivite bulunamadı."));
    }

    private VisualElement BuildPersonalActivityItem(ClassActivityDto item, DateTime occurredAt)
    {
        var row = new VisualElement();
        row.AddToClassList("act-item");

        var avatar = new Label(BuildInitialsFromName(item.ActorName));
        avatar.AddToClassList("act-avatar");

        var body = new VisualElement();
        body.AddToClassList("act-item-body");

        var top = new VisualElement();
        top.AddToClassList("act-item-top");

        var badge = new Label(GetPersonalBadgeText(item.Type));
        badge.AddToClassList("act-type-badge");
        AddPersonalBadgeVariant(badge, item.Type);

        var time = new Label(occurredAt.ToString("HH:mm"));
        time.AddToClassList("act-item-time");

        top.Add(badge);
        top.Add(time);

        var title = new Label(string.IsNullOrWhiteSpace(item.Title) ? "Aktivite" : item.Title);
        title.AddToClassList("act-item-title");

        var desc = new Label(string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description);
        desc.AddToClassList("act-item-desc");

        body.Add(top);
        body.Add(title);
        body.Add(desc);

        var actionBtn = new Button { text = "Görüntüle" };
        actionBtn.AddToClassList("act-item-btn");

        row.Add(avatar);
        row.Add(body);
        row.Add(actionBtn);

        return row;
    }

    private bool MatchesPersonalActivityType(string type)
    {
        return personalActivityFilterMode switch
        {
            "experiment" => string.Equals(type, "ClassCreated", StringComparison.OrdinalIgnoreCase),
            "assignment" => string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase),
            "progress" => string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase),
            "participation" => string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private bool MatchesPersonalActivityDateFilter(DateTime dt)
    {
        if (actDateFilterDropdown == null)
            return true;

        string selected = actDateFilterDropdown.value ?? "Tüm Zamanlar";
        var today = DateTime.Today;

        return selected switch
        {
            "Bugün" => dt.Date == today,
            "Bu Hafta" => dt.Date >= today.AddDays(-6),
            "Bu Ay" => dt.Year == today.Year && dt.Month == today.Month,
            _ => true
        };
    }

    private DateTime ParseActivityDate(string raw)
    {
        if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var iso))
            return iso.ToLocalTime();
        if (DateTime.TryParse(raw, out var dt))
            return dt;
        return DateTime.MinValue;
    }

    private string BuildInitialsFromName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, 1).ToUpper();

        string first = parts[0].Substring(0, 1).ToUpper();
        string second = parts[parts.Length - 1].Substring(0, 1).ToUpper();
        return first + second;
    }

    private string GetPersonalBadgeText(string type)
    {
        if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase)) return "Ödev";
        if (string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase)) return "Katılım";
        if (string.Equals(type, "ClassCreated", StringComparison.OrdinalIgnoreCase)) return "Sınıf";
        return "Aktivite";
    }

    private void AddPersonalBadgeVariant(Label badge, string type)
    {
        if (badge == null)
            return;

        if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("submitted");
        else if (string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("achievement");
        else
            badge.AddToClassList("in-progress");
    }

    private IEnumerator FetchMyAssignments()
    {
        if (router == null) yield break;

        string url = router.ApiBaseUrl + myAssignmentsPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[STUDENT ASSIGNMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            assignmentItems = Array.Empty<AssignmentDto>();
            RefreshClassStatisticsCards();
            RefreshClassDetailsGeneralMetrics();
            BuildStudentAssignmentCards();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<AssignmentListWrapper>("{\"items\":" + raw + "}");
        assignmentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<AssignmentDto>();
        RefreshClassStatisticsCards();
        RefreshClassDetailsGeneralMetrics();

        BuildStudentAssignmentCards();
        BuildHomeworkBoard();
        PopulateClassDetailsHeader(currentSelectedClass);
        ApplyHomeDashboardMetrics();
    }

    private void RefreshClassStatisticsCards()
    {
        var classes = (lastItems ?? Array.Empty<MyClassDto>())
            .Where(c => c != null && !string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var assignments = (assignmentItems ?? Array.Empty<AssignmentDto>())
            .Where(a => a != null)
            .ToArray();

        int completedAssignments = assignments.Count(a => string.Equals(GetHomeworkStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
        int totalAssignments = assignments.Length;

        if (completedAssignmentsCountLabel != null)
            completedAssignmentsCountLabel.text = completedAssignments.ToString();
        if (totalAssignmentsCountLabel != null)
            totalAssignmentsCountLabel.text = totalAssignments.ToString();

        var bestClass = classes
            .OrderByDescending(c => Mathf.Clamp(c.SuccessRatePercent, 0, 100))
            .ThenByDescending(c => Mathf.Max(c.AssignmentCount, 0))
            .FirstOrDefault();

        if (bestClassNameLabel != null)
            bestClassNameLabel.text = bestClass != null ? SafeText(bestClass.Name) : "-";
        if (bestClassMessageLabel != null)
            bestClassMessageLabel.text = "Tebrikler!";

        var nearestDue = assignments
            .Where(a => !string.Equals(GetHomeworkStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            .Select(a => new { item = a, due = GetAssignmentDueAt(a) })
            .Where(x => x.due.HasValue)
            .OrderBy(x => x.due.Value)
            .FirstOrDefault();

        if (lastAssignmentDueDateLabel != null)
            lastAssignmentDueDateLabel.text = nearestDue != null
                ? $"{Mathf.Max((nearestDue.due.Value.Date - DateTime.Today).Days, 0)} gün"
                : "-";

        if (lastAssignmentNameLabel != null)
            lastAssignmentNameLabel.text = nearestDue != null
                ? SafeText(nearestDue.item.Title)
                : "-";
    }

    private void BuildStudentAssignmentCards()
    {
        if (assignmentsCardsRow == null)
            return;

        assignmentsCardsRow.Clear();

        if (currentSelectedClass == null || assignmentItems == null)
        {
            assignmentsCardsRow.Add(new Label("Ödev bulunamadı."));
            return;
        }

        string q = (assignmentSearchQuery ?? "").Trim().ToLowerInvariant();
        int rendered = 0;

        foreach (var a in assignmentItems)
        {
            if (a == null) continue;
            if (a.ClassId != currentSelectedClass.Id) continue;

            bool include = assignmentFilterMode switch
            {
                "active" => a.IsActive,
                "passive" => !a.IsActive,
                "completed" => !a.IsActive,
                "incomplete" => a.IsActive,
                _ => true
            };

            if (!include)
                continue;

            if (!string.IsNullOrWhiteSpace(q))
            {
                string title = (a.Title ?? "").ToLowerInvariant();
                string experiment = (a.ExperimentName ?? "").ToLowerInvariant();
                if (!title.Contains(q) && !experiment.Contains(q))
                    continue;
            }

            assignmentsCardsRow.Add(BuildAssignmentCard(
                string.IsNullOrWhiteSpace(a.Title) ? "-" : a.Title,
                string.IsNullOrWhiteSpace(a.ExperimentName) ? "-" : a.ExperimentName,
                "Başlangıç Seviyesi",
                GetRemainingDaysText(a),
                "0",
                "0",
                0
            ));
            rendered++;
        }

        if (rendered == 0)
            assignmentsCardsRow.Add(new Label("Filtreye uygun ödev bulunamadı."));
    }

    private VisualElement BuildAssignmentCard(string title, string unit, string difficulty, string dayCount, string incomplete, string complete, int percent)
    {
        var card = new VisualElement();
        card.AddToClassList("table-assignment-card");

        var firstRow = new VisualElement();
        firstRow.AddToClassList("ta-row");
        firstRow.AddToClassList("ta-first");

        var icon = new VisualElement();
        icon.AddToClassList("ta-icon");

        var iconCircle = new Label();
        iconCircle.AddToClassList("ass-icon");
        iconCircle.AddToClassList("ta-icon-label");

        var info = new VisualElement();
        info.AddToClassList("ta-info");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("ta-title");

        var unitLabel = new Label(unit);
        unitLabel.AddToClassList("ta-unit");

        info.Add(titleLabel);
        info.Add(unitLabel);
        icon.Add(iconCircle);

        firstRow.Add(icon);
        firstRow.Add(info);

        var difficultyRow = new VisualElement();
        difficultyRow.AddToClassList("ta-row");

        var diffLabel = new Label(difficulty);
        diffLabel.AddToClassList("ta-difficulty");
        difficultyRow.Add(diffLabel);

        var statusRow = new VisualElement();
        statusRow.AddToClassList("ta-row");
        statusRow.AddToClassList("ta-bar");

        var statusBar = new VisualElement();
        statusBar.AddToClassList("ta-status-bar");

        statusBar.Add(BuildStatusMini("ass-calendar-icon", dayCount));
        statusBar.Add(BuildStatusMini("ass-fail-icon", incomplete));
        statusBar.Add(BuildStatusMini("ass-ok-icon", complete, true));

        statusRow.Add(statusBar);

        var progressRow = new VisualElement();
        progressRow.AddToClassList("ta-row");
        progressRow.AddToClassList("ta-bar");

        var progressWrap = new VisualElement();
        progressWrap.AddToClassList("complete-bar");

        var progressFill = new VisualElement();
        progressFill.AddToClassList("complete-bar-fill");
        progressFill.style.width = percent;

        progressWrap.Add(progressFill);
        progressRow.Add(progressWrap);

        var lastRow = new VisualElement();
        lastRow.AddToClassList("ta-row");
        lastRow.AddToClassList("ta-last");

        var comp = new Label($"Tamamlandı : %{percent}");
        comp.AddToClassList("ta-comp");

        var detailBtn = new Button();
        detailBtn.text = "Sonuçları Gör";
        detailBtn.AddToClassList("ta-go-details");

        lastRow.Add(comp);
        lastRow.Add(detailBtn);

        card.Add(firstRow);
        card.Add(difficultyRow);
        card.Add(statusRow);
        card.Add(progressRow);
        card.Add(lastRow);

        return card;
    }

    private VisualElement BuildStatusMini(string iconClass, string value, bool noBorder = false)
    {
        var box = new VisualElement();
        box.AddToClassList("ts");
        if (noBorder)
            box.AddToClassList("ts-no-border");

        var icon = new Label();
        icon.AddToClassList("ts-icon-label");
        if (!string.IsNullOrWhiteSpace(iconClass))
            icon.AddToClassList(iconClass);

        var content = new Label(value);
        content.AddToClassList("ts-content");

        box.Add(icon);
        box.Add(content);

        return box;
    }

    private string GetRemainingDaysText(AssignmentDto assignment)
    {
        if (assignment == null)
            return "0";

        if (!DateTime.TryParse(assignment.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedStart))
            return "0";

        var start = parsedStart.ToLocalTime().Date;
        int duration = assignment.DurationDays <= 0 ? 1 : assignment.DurationDays;
        var endExclusive = start.AddDays(duration);
        int remainingDays = (endExclusive - DateTime.Today).Days;
        return Mathf.Max(remainingDays, 0).ToString();
    }

    private IEnumerator OpenClassDetailsNextFrame()
{
    yield return null; // 1 frame bekle
    ShowClassDetailsTab("general");
}

    private IEnumerator FetchClassActivityForStudent()
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildClassActivityStudentUrl(currentSelectedClass.Id);
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[STUDENT CLASS ACTIVITY] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            currentActivityItems = Array.Empty<ClassActivityDto>();
            RefreshClassDetailsGeneralMetrics();
            BuildStudentClassActivityFeed();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassActivityListWrapper>("{\"items\":" + raw + "}");
        currentActivityItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ClassActivityDto>();
        RefreshClassDetailsGeneralMetrics();

        BuildStudentClassActivityFeed();
    }

    private void BuildStudentClassActivityFeed()
    {
        if (classDetailsActivityFeed == null) return;
        classDetailsActivityFeed.Clear();

        if (currentActivityItems == null || currentActivityItems.Length == 0)
        {
            classDetailsActivityFeed.Add(new Label("Bu sınıf için görüntülenecek aktivite bulunmuyor."));
            return;
        }

        foreach (var item in currentActivityItems)
        {
            if (item == null) continue;

            var card = new VisualElement();
            card.AddToClassList("activity-item");

            var title = new Label(string.IsNullOrWhiteSpace(item.Title) ? "Aktivite" : item.Title);
            title.AddToClassList("activity-username");

            var actor = new Label(string.IsNullOrWhiteSpace(item.ActorName) ? "-" : item.ActorName);
            actor.AddToClassList("activity-time");

            var text = new Label(string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description);
            text.AddToClassList("activity-content");

            card.Add(title);
            card.Add(actor);
            card.Add(text);

            if (item.Comments != null)
            {
                foreach (var c in item.Comments)
                {
                    if (c == null) continue;
                    if (!string.Equals(c.UserRole, "Teacher", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var commentWrap = new VisualElement();
                    commentWrap.AddToClassList("activity-comments");

                    var author = new Label(string.IsNullOrWhiteSpace(c.UserName) ? "Öğretmen" : c.UserName);
                    author.AddToClassList("comment-author");

                    var commentText = new Label(string.IsNullOrWhiteSpace(c.Text) ? "-" : c.Text);
                    commentText.AddToClassList("comment-text");

                    commentWrap.Add(author);
                    commentWrap.Add(commentText);
                    card.Add(commentWrap);
                }
            }

            classDetailsActivityFeed.Add(card);
        }
    }

    private string BuildClassActivityStudentUrl(int classId)
    {
        string path = classActivityStudentPathTemplate ?? "/api/Class/{classId}/activity/student";
        return router.ApiBaseUrl + path.Replace("{classId}", classId.ToString());
    }

    private UnityWebRequest AuthedGet(string url)
    {
        var req = UnityWebRequest.Get(url);

        if (!string.IsNullOrEmpty(router?.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        return req;
    }

    private UnityWebRequest AuthedPost(string url)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router?.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        return req;
    }

    [Serializable]
    public class MyClassDto
    {
        public int Id;
        public string Code;
        public string Name;
        public string TeacherName;
        public string GradeLevel;
        public string LessonName;
        public bool IsActive;
        public string CreatedAt;
        public string JoinedAt;
        public int StudentCount;
        public int AssignmentCount;
        public int SuccessRatePercent;
        public string Status;
    }

    [Serializable]
    private class ClassListWrapper
    {
        public MyClassDto[] items;
    }

    [Serializable]
    private class JoinClassRequest
    {
        public string ClassCode;
    }

    [Serializable]
    private class ClassActivityDto
    {
        public string ActivityId;
        public string Type;
        public string Title;
        public string Description;
        public string ActorName;
        public string ActorRole;
        public string OccurredAt;
        public ActivityCommentDto[] Comments;
    }

    [Serializable]
    private class ActivityCommentDto
    {
        public int UserId;
        public string UserName;
        public string UserRole;
        public string Text;
        public string CreatedAt;
    }

    [Serializable]
    private class ClassActivityListWrapper
    {
        public ClassActivityDto[] items;
    }

    [Serializable]
    private class AssignmentDto
    {
        public int Id;
        public string Title;
        public int ClassId;
        public string ClassName;
        public bool IsActive;
        public int ExperimentId;
        public string ExperimentName;
        public string StartDate;
        public int DurationDays;
        public string CreatedAt;
    }

    [Serializable]
    private class AssignmentListWrapper
    {
        public AssignmentDto[] items;
    }

    [Serializable]
    private class ProfileMeDto
    {
        public int id;
        public string name;
        public string surname;
        public string email;
        public string roleName;
        public string createdAt;
        public string lastLogin;
        public bool isActive;
        public int totalActiveDays;
        public float totalActiveHours;
        public int currentActiveStreakDays;
    }

    [Serializable]
    private class WeeklySessionHoursDto
    {
        public WeeklySessionDayDto[] items;
    }

    [Serializable]
    private class WeeklySessionDayDto
    {
        public int dayIndex;
        public string dayLabel;
        public float hours;
    }

    [Serializable]
    private class ExperimentDto
    {
        public int Id;
        public string GradeLevel;
        public string LessonName;
        public string UnitName;
        public string ExperimentName;
        public bool IsActive;
        public string CreatedAt;
    }

    [Serializable]
    private class ExperimentListWrapper
    {
        public ExperimentDto[] items;
    }
}