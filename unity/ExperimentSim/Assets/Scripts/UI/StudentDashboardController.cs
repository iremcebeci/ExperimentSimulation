using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
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
    [SerializeField] private string userPath = "/api/User";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";
    [SerializeField] private string experimentsByGradeLessonPath = "/api/Experiment/by-grade-lesson";
    [SerializeField] private string joinClassPath = "/api/Class/join"; // backend'ine göre değişebilir
    [SerializeField] private string classActivityStudentPathTemplate = "/api/Class/{classId}/activity/student";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string calendarCategoriesPath = "/api/Calendar/categories";
    [SerializeField] private string calendarEventsPath = "/api/Calendar/events";

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
    private VisualElement settingsModal;
    private Button settingsModalCloseBtn;
    private Button settingsCancelBtn;
    private Button settingsSaveProfileBtn;
    private TextField settingsNameInput;
    private TextField settingsSurnameInput;
    private TextField settingsEmailInput;
    private TextField settingsPhoneInput;
    private Label settingsStatusLabel;
    private SettingsUserUpdatePayloadDto settingsUserSnapshot;
    private ProfileMeDto profileMe;
    private DashboardNotificationCenter notificationCenter;
    private readonly List<RoleChangeNotificationDto> roleChangeNotificationItems = new();
    private Coroutine sessionHeartbeatRoutine;
    private readonly CultureInfo trCulture = new("tr-TR");

    // Calendar Page
    private VisualElement calendarPage;
    private Button calendarBtn;
    private Button calAddEventBtn;
    private Button calRefreshBtn;
    private Button calPrevBtn;
    private Button calNextBtn;
    private Button calTodayBtn;
    private Button calViewDayBtn;
    private Button calViewWeekBtn;
    private Button calViewMonthBtn;
    private Button calViewAgendaBtn;
    private Button calMiniPrevBtn;
    private Button calMiniNextBtn;
    private Button calAddCategoryBtn;
    private Button calQuickCreateBtn;
    private Button calQuickCategoryBtn;
    private Label calToolbarMonthLabel;
    private Label calMiniMonthLabel;
    private Label calDayHeaderLabel;
    private Label calCategoryEmptyLabel;
    private TextField calSearchInput;
    private DropdownField calFilterDropdown;
    private VisualElement calMiniGrid;
    private VisualElement calCategoryList;
    private VisualElement calMonthGrid;
    private VisualElement calWeekHeader;
    private VisualElement calWeekBody;
    private VisualElement calDayTimeCol;
    private VisualElement calDayEventsCol;
    private VisualElement calAgendaContent;
    private ScrollView calWeekView;
    private ScrollView calDayView;
    private ScrollView calAgendaView;
    private VisualElement calMonthView;
    private VisualElement calAddModal;
    private Button calAddModalCloseBtn;
    private Button calAddCancelBtn;
    private Button calSaveEventBtn;
    private TextField calAddTitleInput;
    private DropdownField calAddTypeDropdown;
    private TextField calAddDateInput;
    private TextField calAddStartInput;
    private TextField calAddEndInput;
    private DropdownField calAddClassDropdown;
    private TextField calAddDescInput;
    private VisualElement calCategoryModal;
    private Button calCategoryModalCloseBtn;
    private Button calCategoryCancelBtn;
    private Button calSaveCategoryBtn;
    private TextField calCategoryNameInput;
    private TextField calCategoryColorInput;
    private Button calTextColorWhiteBtn;
    private Button calTextColorBlackBtn;
    private VisualElement calDetailModal;
    private Button calDetailCloseBtn;
    private Button calDetailFooterCloseBtn;
    private Button calDetailEditBtn;
    private Button calDetailDeleteBtn;
    private Label calDetailTitleLabel;
    private Label calDetailTypeLabel;
    private Label calDetailDateLabel;
    private Label calDetailTimeLabel;
    private Label calDetailLocationLabel;
    private Label calDetailDescLabel;
    private readonly List<CalendarEventItem> calendarEvents = new();
    private readonly List<CalendarCategoryItem> calendarCategories = new();
    private int calCurrentYear;
    private int calCurrentMonth;
    private DateTime? calSelectedDate;
    private DateTime? calWeekStartDate;
    private DateTime? calDayViewDate;
    private string calCurrentView = "month";
    private string calActiveFilter = "all";
    private string calSearchQuery = "";
    private CalendarEventItem calCurrentDetailEvent;
    private int? calEditingEventId;
    private readonly List<Button> calCategoryPresetColorButtons = new();
    private string calSelectedPresetColor = "";
    private string calSelectedTextColor = "#ffffff";
    private static readonly string[] CalendarDefaultCategoryColors =
    {
        "#2e86c1",
        "#16a085",
        "#27ae60",
        "#f39c12",
        "#e74c3c",
        "#8e44ad",
        "#34495e"
    };

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
        BindSettingsModal();
        BindCalendarPage();
        BindNotifications();

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

        root.Q<Button>("AccountBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ProfileBtn");
            ShowPage("ProfilePage");
            StartCoroutine(LoadProfilePageData());
        });

        root.Q<Button>("SettingsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            OpenSettingsModal();
        });

        root.Q<Button>("StartSimulationBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("StartSimulationBtn");
            ShowPage("StartSimulationPage");
        });
    }

    private void BindNotifications()
    {
        notificationCenter = new DashboardNotificationCenter(
            root,
            BuildNotificationItems,
            HandleNotificationSelected,
            () => $"student-{router?.CurrentUserId ?? 0}");
        notificationCenter.Bind("NotificationsBtn");
        notificationCenter.RefreshBadge();
    }

    private void RefreshNotificationsBadge()
    {
        notificationCenter?.RefreshBadge();
    }

    private List<DashboardNotificationCenter.NotificationItem> BuildNotificationItems()
    {
        var list = new List<DashboardNotificationCenter.NotificationItem>();
        var now = DateTime.Now;

        foreach (var assignment in assignmentItems ?? Array.Empty<AssignmentDto>())
        {
            if (assignment == null)
                continue;

            if (TryParseLocalDateTime(assignment.CreatedAt, out var createdAt))
            {
                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"student-new-assignment-{assignment.Id}",
                    Title = "Yeni Ödev",
                    Message = $"{SafeText(assignment.Title)} ödevi eklendi.",
                    Timestamp = createdAt,
                    TargetPage = "AssignmentPage",
                    TargetMenuButton = "AssignmentBtn",
                    IsUnread = createdAt >= now.AddDays(-7)
                });
            }

            var dueAt = GetAssignmentDueAt(assignment);
            if (!dueAt.HasValue)
                continue;

            string status = GetHomeworkStatus(assignment);
            bool isCompleted = string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase);
            bool isUpcoming = dueAt.Value >= now && dueAt.Value <= now.AddDays(3);

            if (isUpcoming && !isCompleted)
            {
                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"student-upcoming-assignment-{assignment.Id}",
                    Title = "Yaklaşan Teslim",
                    Message = $"{SafeText(assignment.Title)} için son tarih: {dueAt.Value.ToString("dd MMM yyyy HH:mm", trCulture)}",
                    Timestamp = dueAt.Value,
                    TargetPage = "AssignmentPage",
                    TargetMenuButton = "AssignmentBtn",
                    IsUnread = true
                });
            }
        }

        foreach (var activity in personalActivityItems ?? Array.Empty<ClassActivityDto>())
        {
            if (activity == null)
                continue;

            if (!string.Equals(activity.Type, "JoinApproved", StringComparison.OrdinalIgnoreCase))
                continue;

            var occurredAt = ParseActivityDate(activity.OccurredAt);
            if (occurredAt == DateTime.MinValue)
                occurredAt = now;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"student-join-approved-{activity.ActivityId}",
                Title = "Sınıf Katılım Onayı",
                Message = string.IsNullOrWhiteSpace(activity.Description)
                    ? "Sınıfa katılma isteğiniz onaylandı."
                    : activity.Description,
                Timestamp = occurredAt,
                TargetPage = "ClassesPage",
                TargetMenuButton = "ClassBtn",
                IsUnread = occurredAt >= now.AddDays(-7)
            });
        }

        foreach (var roleChange in roleChangeNotificationItems)
        {
            if (roleChange == null)
                continue;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = roleChange.Id,
                Title = "Rol Güncellemesi",
                Message = roleChange.Message,
                Timestamp = roleChange.Timestamp,
                TargetPage = "ProfilePage",
                TargetMenuButton = "ProfileBtn",
                IsUnread = roleChange.Timestamp >= now.AddDays(-7)
            });
        }

        return list
            .Where(x => x.Timestamp != DateTime.MinValue)
            .OrderByDescending(x => x.Timestamp)
            .Take(200)
            .ToList();
    }

    private void HandleNotificationSelected(DashboardNotificationCenter.NotificationItem item)
    {
        if (item == null)
            return;

        if (!string.IsNullOrWhiteSpace(item.TargetMenuButton))
            SetMenuActive(item.TargetMenuButton);

        if (!string.IsNullOrWhiteSpace(item.TargetPage))
            ShowPage(item.TargetPage);

        if (string.Equals(item.TargetPage, "AssignmentPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(FetchMyAssignments());
        else if (string.Equals(item.TargetPage, "ClassesPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(FetchMyClasses());
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
        RefreshCalendarClassDropdown();

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

    private void BindSettingsModal()
    {
        settingsModal = root.Q<VisualElement>("SettingsModal");
        if (settingsModal == null)
            return;

        settingsModalCloseBtn = root.Q<Button>("SettingsModalCloseBtn");
        settingsCancelBtn = root.Q<Button>("SettingsCancelBtn");
        settingsSaveProfileBtn = root.Q<Button>("SettingsSaveProfileBtn");
        settingsNameInput = root.Q<TextField>("SettingsNameInput");
        settingsSurnameInput = root.Q<TextField>("SettingsSurnameInput");
        settingsEmailInput = root.Q<TextField>("SettingsEmailInput");
        settingsPhoneInput = root.Q<TextField>("SettingsPhoneInput");
        settingsStatusLabel = root.Q<Label>("SettingsStatusLabel");

        ShowSettingsStatus(string.Empty);

        if (settingsModalCloseBtn != null)
            settingsModalCloseBtn.clicked += CloseSettingsModal;
        if (settingsCancelBtn != null)
            settingsCancelBtn.clicked += CloseSettingsModal;
        if (settingsSaveProfileBtn != null)
            settingsSaveProfileBtn.clicked += () => StartCoroutine(SaveSettingsProfile());

        settingsModal.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == settingsModal)
                CloseSettingsModal();
        });
    }

    private void OpenSettingsModal()
    {
        if (settingsModal == null)
            return;

        StartCoroutine(OpenSettingsModalRoutine());
    }

    private IEnumerator OpenSettingsModalRoutine()
    {
        ShowSettingsStatus(string.Empty);

        if (profileMe == null)
            yield return StartCoroutine(LoadProfilePageData());

        yield return StartCoroutine(LoadSettingsUserSnapshot());
        FillSettingsFieldsFromProfile();

        settingsModal.RemoveFromClassList("hidden");
        settingsModal.AddToClassList("open");
    }

    private void CloseSettingsModal()
    {
        if (settingsModal == null)
            return;

        settingsModal.RemoveFromClassList("open");
        settingsModal.AddToClassList("hidden");
    }

    private void FillSettingsFieldsFromProfile()
    {
        string name = profileMe != null ? profileMe.name : router?.CurrentName;
        string surname = profileMe != null ? profileMe.surname : router?.CurrentSurname;
        string email = profileMe != null ? profileMe.email : string.Empty;
        string phone = settingsUserSnapshot != null ? settingsUserSnapshot.Phone : string.Empty;

        if (settingsNameInput != null)
            settingsNameInput.value = name ?? string.Empty;
        if (settingsSurnameInput != null)
            settingsSurnameInput.value = surname ?? string.Empty;
        if (settingsEmailInput != null)
            settingsEmailInput.value = email ?? string.Empty;
        if (settingsPhoneInput != null)
            settingsPhoneInput.value = phone ?? string.Empty;
    }

    private IEnumerator LoadSettingsUserSnapshot()
    {
        if (router == null)
            yield break;

        string detailUrl = router.ApiBaseUrl + userPath + "/" + router.CurrentUserId;
        using var detailReq = AuthedGet(detailUrl);
        yield return detailReq.SendWebRequest();

        if (detailReq.result != UnityWebRequest.Result.Success)
        {
            settingsUserSnapshot = null;
            yield break;
        }

        string detailRaw = detailReq.downloadHandler != null ? detailReq.downloadHandler.text : "{}";
        settingsUserSnapshot = JsonUtility.FromJson<SettingsUserUpdatePayloadDto>(detailRaw);
    }

    private IEnumerator SaveSettingsProfile()
    {
        if (router == null)
            yield break;

        string name = (settingsNameInput?.value ?? string.Empty).Trim();
        string surname = (settingsSurnameInput?.value ?? string.Empty).Trim();
        string email = (settingsEmailInput?.value ?? string.Empty).Trim();
        string phone = (settingsPhoneInput?.value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname) || string.IsNullOrWhiteSpace(email))
        {
            ShowSettingsStatus("Ad, soyad ve e-posta zorunludur.", isError: true);
            yield break;
        }

        settingsSaveProfileBtn?.SetEnabled(false);
        ShowSettingsStatus("Profil kaydediliyor...");

        if (settingsUserSnapshot == null || settingsUserSnapshot.Id <= 0)
            yield return StartCoroutine(LoadSettingsUserSnapshot());

        if (settingsUserSnapshot == null || settingsUserSnapshot.Id <= 0)
        {
            ShowSettingsStatus("Kullanıcı bilgileri alınamadı.", isError: true);
            settingsSaveProfileBtn?.SetEnabled(true);
            yield break;
        }

        var payload = settingsUserSnapshot;
        payload.Name = name;
        payload.Surname = surname;
        payload.Email = email;
        payload.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;

        string updateUrl = router.ApiBaseUrl + userPath;
        string body = JsonUtility.ToJson(payload);
        using var updateReq = new UnityWebRequest(updateUrl, "PUT");
        updateReq.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        updateReq.downloadHandler = new DownloadHandlerBuffer();
        updateReq.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(router.AccessToken))
            updateReq.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return updateReq.SendWebRequest();

        if (updateReq.result != UnityWebRequest.Result.Success)
        {
            ShowSettingsStatus(ReadApiMessage(updateReq.downloadHandler?.text, "Profil güncellenemedi."), isError: true);
            settingsSaveProfileBtn?.SetEnabled(true);
            yield break;
        }

        router.SetSession(router.CurrentUserId, router.AccessToken, name, surname, router.CurrentRoleId, router.CurrentRoleName);
        yield return StartCoroutine(LoadProfilePageData());
        yield return StartCoroutine(LoadSettingsUserSnapshot());
        FillSettingsFieldsFromProfile();
        ShowSettingsStatus("Profil bilgileri güncellendi.", isSuccess: true);
        settingsSaveProfileBtn?.SetEnabled(true);
    }

    private string ReadApiMessage(string raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        try
        {
            var dto = JsonUtility.FromJson<SettingsApiMessageDto>(raw);
            if (dto != null && !string.IsNullOrWhiteSpace(dto.message))
                return dto.message;
        }
        catch { }

        return fallback;
    }

    private void ShowSettingsStatus(string message, bool isError = false, bool isSuccess = false)
    {
        if (settingsStatusLabel == null)
            return;

        settingsStatusLabel.RemoveFromClassList("error");
        settingsStatusLabel.RemoveFromClassList("success");

        if (string.IsNullOrWhiteSpace(message))
        {
            settingsStatusLabel.text = string.Empty;
            settingsStatusLabel.AddToClassList("hidden");
            return;
        }

        settingsStatusLabel.text = message;
        settingsStatusLabel.RemoveFromClassList("hidden");

        if (isError)
            settingsStatusLabel.AddToClassList("error");
        else if (isSuccess)
            settingsStatusLabel.AddToClassList("success");
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
                profileMe = me;
                TrackRoleChangeNotification(me);
            }
            else
            {
                Debug.LogError($"[STUDENT PROFILE] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            }
        }

        ApplyProfileIdentity(me);
        BuildProfileStatsCards(me);
    }

    private void TrackRoleChangeNotification(ProfileMeDto me)
    {
        if (router == null || me == null)
            return;

        string newRole = string.IsNullOrWhiteSpace(me.roleName) ? "Bilinmeyen Rol" : me.roleName.Trim();
        string snapshotKey = GetRoleSnapshotKey();
        string previousRole = PlayerPrefs.GetString(snapshotKey, string.Empty);

        if (string.IsNullOrWhiteSpace(previousRole))
        {
            PlayerPrefs.SetString(snapshotKey, newRole);
            return;
        }

        if (string.Equals(previousRole, newRole, StringComparison.OrdinalIgnoreCase))
            return;

        roleChangeNotificationItems.Add(new RoleChangeNotificationDto
        {
            Id = $"student-role-change-{router.CurrentUserId}-{DateTime.UtcNow.Ticks}",
            Message = $"Rolünüz {previousRole} rolünden {newRole} rolüne güncellendi.",
            Timestamp = DateTime.Now
        });

        PlayerPrefs.SetString(snapshotKey, newRole);
    }

    private string GetRoleSnapshotKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"student-role-snapshot-{userId}";
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

        int completedAssignments = classAssignments.Count(a =>
            a != null &&
            (a.IsCompleted || a.TotalQuestionCount > 0)
        );

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

        // Sadece gerçekten sonuç kaydedildiyse tamamlandı say.
        if (a.IsCompleted || a.TotalQuestionCount > 0)
            return "Tamamlandı";

        if (!DateTime.TryParse(a.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return "Başlanmadı";

        DateTime start = parsed.ToLocalTime().Date;
        DateTime today = DateTime.Today;

        if (today < start)
            return "Başlanmadı";

        // Süresi geçmiş olsa bile sonuç yoksa tamamlandı değildir.
        return "Devam Ediyor";
    }

    private VisualElement BuildHomeworkNotStartedCard(string title, string lesson, string desc, AssignmentDto a)
    {
        var card = new VisualElement();
        card.AddToClassList("hw-card");
        card.AddToClassList("not-started-border");

        var top = new VisualElement();
        top.AddToClassList("hw-card-top");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("hw-card-title");

        top.Add(titleLabel);

        var subject = new Label(lesson);
        subject.AddToClassList("hw-card-subject");

        var description = new Label(desc);
        description.AddToClassList("hw-card-desc");

        var due = BuildHomeworkDue(a, overdue: false);

        card.Add(top);
        card.Add(subject);
        card.Add(description);
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

       

        top.Add(t);
 
        var subject = new Label(lesson);
        subject.AddToClassList("hw-card-subject");

        var check = new Label("✓ Tamamlandı");
        check.AddToClassList("hw-card-check");

        int percent = GetHomeworkProgressPercent(a);
        percent = Mathf.Clamp(percent, 0, 100);

        string resultSummary;

        if (a != null && a.TotalQuestionCount > 0)
        {
            resultSummary = $"{a.CorrectCount} doğru / {a.WrongCount} yanlış • %{percent}";
        }
        else
        {
            resultSummary = $"Sonuç bulunamadı • %{percent}";
        }

        var resultText = new Label(resultSummary);
        resultText.AddToClassList("hw-card-result");

        var progress = new VisualElement();
        progress.AddToClassList("hw-card-progress");

        var track = new VisualElement();
        track.AddToClassList("hw-card-track");

        var fill = new VisualElement();
        fill.AddToClassList("hw-card-fill");
        fill.AddToClassList("done");

        // Önemli: Burada piksel değil yüzde veriyoruz.
        fill.style.width = new Length(percent, LengthUnit.Percent);

        track.Add(fill);
        progress.Add(track);

        card.Add(top);
        card.Add(subject);
        card.Add(check);
        card.Add(resultText);
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
        if (a == null)
            return 0;

        // Öğrenci deneyi bitirdiyse progress artık başarı puanı olsun.
        if (a.IsCompleted || a.TotalQuestionCount > 0)
            return Mathf.Clamp(a.Score, 0, 100);

        if (!DateTime.TryParse(a.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return a.IsActive ? 0 : 100;

        DateTime start = parsed.ToLocalTime().Date;
        DateTime today = DateTime.Today;
        int duration = a.DurationDays <= 0 ? 1 : a.DurationDays;

        int elapsed = (today - start).Days + 1;
        float ratio = Mathf.Clamp01(elapsed / (float)duration);

        return Mathf.RoundToInt(ratio * 100f);
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

        if (assignmentItems == null || assignmentItems.Length == 0)
            yield return StartCoroutine(FetchMyAssignments());

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

        var classes = lastItems ?? Array.Empty<MyClassDto>();

        foreach (var c in classes)
        {
            if (c == null)
                continue;

            if (string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                continue;

            string lesson = NormalizeProgressLessonDisplay(c.LessonName);

            if (string.IsNullOrWhiteSpace(lesson))
                continue;

            if (!progressLessonTabs.Contains(lesson))
                progressLessonTabs.Add(lesson);
        }

        if (progressLessonTabs.Count == 0)
        {
            progressLessonTabs.Add("Fen Bilimleri");
        }
    }

    private string NormalizeProgressLessonDisplay(string lessonName)
    {
        if (string.IsNullOrWhiteSpace(lessonName))
            return "";

        string lesson = lessonName.Trim();

        if (string.Equals(lesson, "Fen", StringComparison.OrdinalIgnoreCase))
            return "Fen Bilimleri";

        return lesson;
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

        string grade = ResolveGradeForProgressLesson(lessonDisplayName);
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

    private string ResolveGradeForProgressLesson(string lessonDisplayName)
    {
        string apiLesson = MapProgressLessonToApiLesson(lessonDisplayName);

        var classes = lastItems ?? Array.Empty<MyClassDto>();

        foreach (var c in classes)
        {
            if (c == null)
                continue;

            if (string.Equals(c.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                continue;

            string classLesson = c.LessonName ?? "";

            if (string.Equals(classLesson, apiLesson, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeProgressLessonDisplay(classLesson), lessonDisplayName, StringComparison.OrdinalIgnoreCase))
            {
                return c.GradeLevel ?? "";
            }
        }

        return ResolveActiveClassGradeLevel();
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

        var viewItems = BuildProgressViewItems();

        string search = progressSearchField != null ? (progressSearchField.value ?? "") : "";
        string q = search.Trim().ToLowerInvariant();

        string statusFilter = progressStatusDropdown != null
            ? (progressStatusDropdown.value ?? "Tüm Durumlar")
            : "Tüm Durumlar";

        string difficultyFilter = progressDifficultyDropdown != null
            ? (progressDifficultyDropdown.value ?? "Tüm Zorluklar")
            : "Tüm Zorluklar";

        var filteredItems = new List<ProgressExperimentViewItem>();

        foreach (var item in viewItems)
        {
            if (item == null)
                continue;

            if (!string.IsNullOrWhiteSpace(q))
            {
                string haystack = $"{item.Title} {item.UnitName} {item.LessonName} {item.Status}".ToLowerInvariant();

                if (!haystack.Contains(q))
                    continue;
            }

            if (!string.Equals(statusFilter, "Tüm Durumlar", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(difficultyFilter, "Tüm Zorluklar", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.Difficulty, difficultyFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            filteredItems.Add(item);
        }

        int notStartedCount = 0;
        int inProgressCount = 0;
        int completedCount = 0;

        foreach (var item in filteredItems)
        {
            if (item.Status == "Başlanmadı")
            {
                notStartedContent?.Add(BuildProgressExperimentCard(item));
                notStartedCount++;
            }
            else if (item.Status == "Devam Ediyor")
            {
                inProgressContent?.Add(BuildProgressExperimentCard(item));
                inProgressCount++;
            }
            else
            {
                completedContent?.Add(BuildProgressExperimentCard(item));
                completedCount++;
            }
        }

        if (notStartedCount == 0)
            notStartedContent?.Add(BuildProgressEmptyCard("Başlanmadı deney bulunmuyor."));

        if (inProgressCount == 0)
            inProgressContent?.Add(BuildProgressEmptyCard("Henüz devam eden deney bulunmuyor."));

        if (completedCount == 0)
            completedContent?.Add(BuildProgressEmptyCard("Henüz tamamlanan deney bulunmuyor."));

        if (progressNotStartedCountLabel != null) progressNotStartedCountLabel.text = notStartedCount.ToString();
        if (progressInProgressCountLabel != null) progressInProgressCountLabel.text = inProgressCount.ToString();
        if (progressCompletedCountLabel != null) progressCompletedCountLabel.text = completedCount.ToString();

        int totalSubjectExperiments = viewItems.Count;
        int completedSubjectExperiments = viewItems.Count(x => x.Status == "Tamamlandı");

        int subjectPercent = totalSubjectExperiments > 0
            ? Mathf.RoundToInt((completedSubjectExperiments / (float)totalSubjectExperiments) * 100f)
            : 0;

        int totalAssignments = assignmentItems != null ? assignmentItems.Length : 0;
        int completedAssignments = assignmentItems != null
            ? assignmentItems.Count(a => a != null && GetHomeworkStatus(a) == "Tamamlandı")
            : 0;

        int overallPercent = totalAssignments > 0
            ? Mathf.RoundToInt((completedAssignments / (float)totalAssignments) * 100f)
            : subjectPercent;

        if (overallPercentLabel != null)
            overallPercentLabel.text = "%" + Mathf.Clamp(overallPercent, 0, 100);

        if (subjectPercentLabel != null)
            subjectPercentLabel.text = "%" + Mathf.Clamp(subjectPercent, 0, 100);

        if (subjectProgressLabel != null)
        {
            subjectProgressLabel.text = string.IsNullOrWhiteSpace(progressSelectedLesson)
                ? "Ders İlerlemesi"
                : progressSelectedLesson + " İlerlemesi";
        }

        if (subjectSubLabel != null)
        {
            subjectSubLabel.text = $"{completedSubjectExperiments}/{totalSubjectExperiments} deney tamamlandı";
        }

        if (overallFillBar != null)
            overallFillBar.style.width = new Length(Mathf.Clamp(overallPercent, 0, 100), LengthUnit.Percent);

        if (subjectFillBar != null)
            subjectFillBar.style.width = new Length(Mathf.Clamp(subjectPercent, 0, 100), LengthUnit.Percent);
    }

    private List<ProgressExperimentViewItem> BuildProgressViewItems()
    {
        var result = new List<ProgressExperimentViewItem>();

        var experiments = progressExperimentItems ?? Array.Empty<ExperimentDto>();
        var assignments = assignmentItems ?? Array.Empty<AssignmentDto>();

        foreach (var exp in experiments)
        {
            if (exp == null)
                continue;

            AssignmentDto relatedAssignment = FindRelatedAssignmentForExperiment(exp, assignments);

            string status = "Başlanmadı";
            int score = 0;

            if (relatedAssignment != null)
            {
                status = GetHomeworkStatus(relatedAssignment);
                score = Mathf.Clamp(relatedAssignment.Score, 0, 100);
            }

            result.Add(new ProgressExperimentViewItem
            {
                Experiment = exp,
                Assignment = relatedAssignment,
                Title = string.IsNullOrWhiteSpace(exp.ExperimentName) ? "-" : exp.ExperimentName,
                UnitName = string.IsNullOrWhiteSpace(exp.UnitName) ? "-" : exp.UnitName,
                LessonName = string.IsNullOrWhiteSpace(exp.LessonName) ? "-" : NormalizeProgressLessonDisplay(exp.LessonName),
                Difficulty = GetProgressDifficulty(exp),
                Status = status,
                Score = score
            });
        }

        return result;
    }

    private AssignmentDto FindRelatedAssignmentForExperiment(ExperimentDto exp, AssignmentDto[] assignments)
    {
        if (exp == null || assignments == null)
            return null;

        AssignmentDto best = null;

        foreach (var a in assignments)
        {
            if (a == null)
                continue;

            if (a.ExperimentId != exp.Id)
                continue;

            if (best == null)
            {
                best = a;
                continue;
            }

            bool aCompleted = GetHomeworkStatus(a) == "Tamamlandı";
            bool bestCompleted = GetHomeworkStatus(best) == "Tamamlandı";

            if (aCompleted && !bestCompleted)
            {
                best = a;
                continue;
            }

            DateTime? aDate = GetAssignmentDueAt(a);
            DateTime? bestDate = GetAssignmentDueAt(best);

            if (aDate.HasValue && bestDate.HasValue && aDate.Value > bestDate.Value)
                best = a;
        }

        return best;
    }

    private string GetProgressDifficulty(ExperimentDto exp)
    {
        int hash = Mathf.Abs((exp.ExperimentName ?? "").GetHashCode());
        int mod = hash % 3;
        if (mod == 0) return "Kolay";
        if (mod == 1) return "Orta";
        return "Zor";
    }

    private VisualElement BuildProgressExperimentCard(ProgressExperimentViewItem item)
    {
        var card = new VisualElement();
        card.AddToClassList("exp-card");

        if (item.Status == "Başlanmadı")
            card.AddToClassList("not-started-border");
        else if (item.Status == "Devam Ediyor")
            card.AddToClassList("in-progress-border");
        else
            card.AddToClassList("completed-border");

        var top = new VisualElement();
        top.AddToClassList("exp-card-top");

        var titleLabel = new Label(item.Title);
        titleLabel.AddToClassList("exp-card-title");

        var badge = new Label(item.Difficulty);
        badge.AddToClassList("exp-card-badge");

        if (string.Equals(item.Difficulty, "Kolay", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("easy");
        else if (string.Equals(item.Difficulty, "Orta", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("medium");
        else
            badge.AddToClassList("hard");

        top.Add(titleLabel);
        top.Add(badge);

        var unit = new Label(item.UnitName);
        unit.AddToClassList("exp-card-desc");

        var statusRow = new VisualElement();
        statusRow.AddToClassList("hw-card-progress-row");

        var statusText = new Label(item.Status);
        statusText.AddToClassList("hw-card-progress-text");

        var valueText = new Label(item.Status == "Tamamlandı" ? $"%{item.Score}" : "-");
        valueText.AddToClassList("hw-card-progress-value");

        statusRow.Add(statusText);
        statusRow.Add(valueText);

        card.Add(top);
        card.Add(unit);
        card.Add(statusRow);

        if (item.Assignment != null && item.Status != "Tamamlandı")
        {
            var startBtn = new Button { text = item.Status == "Başlanmadı" ? "Başlat" : "Devam Et" };
            startBtn.AddToClassList("hw-card-action-btn");
            startBtn.clicked += () =>
            {
                OpenAssignmentExperiment(item.Assignment);
            };

            card.Add(startBtn);
        }
        else if (item.Status == "Tamamlandı")
        {
            var completedLabel = new Label("Tamamlandı");
            completedLabel.AddToClassList("hw-card-completed-label");
            card.Add(completedLabel);
        }
        else
        {
            var noAssignmentLabel = new Label("Henüz ödev olarak atanmadı.");
            noAssignmentLabel.AddToClassList("exp-card-desc");
            card.Add(noAssignmentLabel);
        }

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
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassActivityListWrapper>("{\"items\":" + raw + "}");
        personalActivityItems = wrapped != null && wrapped.items != null
            ? wrapped.items
            : Array.Empty<ClassActivityDto>();

        BuildPersonalActivityFeed();
        RefreshNotificationsBadge();
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
            RefreshNotificationsBadge();
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
        RefreshNotificationsBadge();
    }

    private void OpenAssignmentExperiment(AssignmentDto assignment)
    {
        if (assignment == null)
            return;

        string sceneName = ExperimentSceneResolver.ResolveSceneName(
            assignment.ExperimentId,
            assignment.ExperimentName
        );

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Bu deney için sahne bulunamadı: " + assignment.ExperimentName);
            return;
        }

        AssignmentSession.StartAssignment(
    assignment.Id,
    assignment.ExperimentId,
    assignment.Title,
    assignment.ExperimentName,
    sceneName,
    router.AccessToken
);

        SceneManager.LoadScene(sceneName);
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
            if (a == null)
                continue;

            if (a.ClassId != currentSelectedClass.Id)
                continue;

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

            assignmentsCardsRow.Add(BuildAssignmentCard(a));
            rendered++;
        }

        if (rendered == 0)
            assignmentsCardsRow.Add(new Label("Filtreye uygun ödev bulunamadı."));
    }


    private VisualElement BuildAssignmentCard(AssignmentDto assignment)
    {
        string title = string.IsNullOrWhiteSpace(assignment.Title) ? "-" : assignment.Title;
        string unit = string.IsNullOrWhiteSpace(assignment.ExperimentName) ? "-" : assignment.ExperimentName;
        string difficulty = "Başlangıç Seviyesi";
        string dayCount = GetRemainingDaysText(assignment);

        string incomplete = assignment.WrongCount.ToString();
        string complete = assignment.CorrectCount.ToString();
        int percent = Mathf.Clamp(assignment.Score, 0, 100);

        var card = BuildAssignmentCard(title, unit, difficulty, dayCount, incomplete, complete, percent);

        var detailBtn = card.Q<Button>();

        if (detailBtn != null)
        {
            detailBtn.text = assignment.IsCompleted ? "Tekrar Dene" : "Deneye Başla";
            detailBtn.clicked += () => OpenAssignmentExperiment(assignment);
        }

        return card;
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
        progressFill.style.width = Length.Percent(Mathf.Clamp(percent, 0, 100));

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

    // ---------------- CALENDAR ----------------

    #region Calendar

    [System.Serializable]
    private class CalendarEventItem
    {
        public int Id;
        public string Title;
        public string Type;
        public string Date;       // yyyy-MM-dd
        public string Start;      // HH:mm
        public string End;        // HH:mm
        public string Location;
        public string RelatedClass;
        public string Desc;
    }

    [System.Serializable]
    private class CalendarCategoryItem
    {
        public int Id;
        public string Type;
        public string Label;
        public string Color;
        public string TextColor;
    }

    [System.Serializable]
    private class CalendarCategoryListWrapper
    {
        public CalendarCategoryItem[] items;
    }

    [System.Serializable]
    private class CalendarEventListWrapper
    {
        public CalendarEventItem[] items;
    }

    [System.Serializable]
    private class CalendarCreateCategoryRequest
    {
        public string Label;
        public string Color;
        public string TextColor;
    }

    [System.Serializable]
    private class CalendarUpsertEventRequest
    {
        public string Title;
        public string Type;
        public string Date;
        public string Start;
        public string End;
        public string Location;
        public string RelatedClass;
        public string Desc;
    }

    private void BindCalendarPage()
    {
        calendarPage = root.Q<VisualElement>("CalendarPage");
        if (calendarPage == null)
            return;

        calendarBtn = root.Q<Button>("CalendarBtn");

        calAddEventBtn = root.Q<Button>("CalAddEventBtn");
        calRefreshBtn = root.Q<Button>("CalRefreshBtn");
        calPrevBtn = root.Q<Button>("CalPrevBtn");
        calNextBtn = root.Q<Button>("CalNextBtn");
        calTodayBtn = root.Q<Button>("CalTodayBtn");

        calViewDayBtn = root.Q<Button>("CalViewDayBtn");
        calViewWeekBtn = root.Q<Button>("CalViewWeekBtn");
        calViewMonthBtn = root.Q<Button>("CalViewMonthBtn");
        calViewAgendaBtn = root.Q<Button>("CalViewAgendaBtn");

        calMiniPrevBtn = root.Q<Button>("CalMiniPrevBtn");
        calMiniNextBtn = root.Q<Button>("CalMiniNextBtn");
        calAddCategoryBtn = root.Q<Button>("CalAddCategoryBtn");
        calQuickCreateBtn = root.Q<Button>("CalQuickCreateBtn");
        calQuickCategoryBtn = root.Q<Button>("CalQuickCategoryBtn");

        calToolbarMonthLabel = root.Q<Label>("CalToolbarMonthLabel");
        calMiniMonthLabel = root.Q<Label>("CalMiniMonthLabel");
        calDayHeaderLabel = root.Q<Label>("CalDayHeaderLabel");
        calCategoryEmptyLabel = root.Q<Label>("CalCategoryEmptyLabel");

        calSearchInput = root.Q<TextField>("CalSearchInput");
        calFilterDropdown = root.Q<DropdownField>("CalFilterDropdown");

        calMiniGrid = root.Q<VisualElement>("CalMiniGrid");
        calCategoryList = root.Q<VisualElement>("CalCategoryList");
        calMonthGrid = root.Q<VisualElement>("CalMonthGrid");
        calWeekHeader = root.Q<VisualElement>("CalWeekHeader");
        calWeekBody = root.Q<VisualElement>("CalWeekBody");
        calDayTimeCol = root.Q<VisualElement>("CalDayTimeCol");
        calDayEventsCol = root.Q<VisualElement>("CalDayEventsCol");
        calAgendaContent = root.Q<VisualElement>("CalAgendaContent");

        calMonthView = root.Q<VisualElement>("CalMonthView");
        calWeekView = root.Q<ScrollView>("CalWeekView");
        calDayView = root.Q<ScrollView>("CalDayView");
        calAgendaView = root.Q<ScrollView>("CalAgendaView");

        calAddModal = root.Q<VisualElement>("CalAddModal");
        calAddModalCloseBtn = root.Q<Button>("CalAddModalCloseBtn");
        calAddCancelBtn = root.Q<Button>("CalAddCancelBtn");
        calSaveEventBtn = root.Q<Button>("CalSaveEventBtn");
        calAddTitleInput = root.Q<TextField>("CalAddTitleInput");
        calAddTypeDropdown = root.Q<DropdownField>("CalAddTypeDropdown");
        calAddDateInput = root.Q<TextField>("CalAddDateInput");
        calAddStartInput = root.Q<TextField>("CalAddStartInput");
        calAddEndInput = root.Q<TextField>("CalAddEndInput");
        calAddClassDropdown = root.Q<DropdownField>("CalAddClassDropdown");
        calAddDescInput = root.Q<TextField>("CalAddDescInput");

        calCategoryModal = root.Q<VisualElement>("CalCategoryModal");
        calCategoryModalCloseBtn = root.Q<Button>("CalCategoryModalCloseBtn");
        calCategoryCancelBtn = root.Q<Button>("CalCategoryCancelBtn");
        calSaveCategoryBtn = root.Q<Button>("CalSaveCategoryBtn");
        calCategoryNameInput = root.Q<TextField>("CalCategoryNameInput");
        calCategoryColorInput = root.Q<TextField>("CalCategoryColorInput");
        calTextColorWhiteBtn = root.Q<Button>("CalTextColorWhiteBtn");
        calTextColorBlackBtn = root.Q<Button>("CalTextColorBlackBtn");
        BindCalendarPresetColorButtons();
        BindCalendarTextColorButtons();

        calDetailModal = root.Q<VisualElement>("CalDetailModal");
        calDetailCloseBtn = root.Q<Button>("CalDetailCloseBtn");
        calDetailFooterCloseBtn = root.Q<Button>("CalDetailFooterCloseBtn");
        calDetailEditBtn = root.Q<Button>("CalDetailEditBtn");
        calDetailDeleteBtn = root.Q<Button>("CalDetailDeleteBtn");
        calDetailTitleLabel = root.Q<Label>("CalDetailTitleLabel");
        calDetailTypeLabel = root.Q<Label>("CalDetailTypeLabel");
        calDetailDateLabel = root.Q<Label>("CalDetailDateLabel");
        calDetailTimeLabel = root.Q<Label>("CalDetailTimeLabel");
        calDetailLocationLabel = root.Q<Label>("CalDetailLocationLabel");
        calDetailDescLabel = root.Q<Label>("CalDetailDescLabel");

        calCurrentYear = DateTime.Today.Year;
        calCurrentMonth = DateTime.Today.Month - 1;
        calSelectedDate = DateTime.Today;
        calDayViewDate = DateTime.Today;

        RefreshCalendarClassDropdown();
        StartCoroutine(LoadCalendarData(false));

        calAddEventBtn.clicked += () => OpenCalendarAddModal();
        calRefreshBtn.clicked += () => StartCoroutine(LoadCalendarData(true));
        calPrevBtn.clicked += () => NavigateCalendar(-1);
        calNextBtn.clicked += () => NavigateCalendar(1);
        calTodayBtn.clicked += GoCalendarToday;

        calViewDayBtn.clicked += () => SwitchCalendarView("day");
        calViewWeekBtn.clicked += () => SwitchCalendarView("week");
        calViewMonthBtn.clicked += () => SwitchCalendarView("month");
        calViewAgendaBtn.clicked += () => SwitchCalendarView("agenda");

        calMiniPrevBtn.clicked += () =>
        {
            calCurrentMonth--;
            if (calCurrentMonth < 0) { calCurrentMonth = 11; calCurrentYear--; }
            RenderCalendarAll();
        };

        calMiniNextBtn.clicked += () =>
        {
            calCurrentMonth++;
            if (calCurrentMonth > 11) { calCurrentMonth = 0; calCurrentYear++; }
            RenderCalendarAll();
        };

        calAddCategoryBtn.clicked += OpenCalendarCategoryModal;
        calQuickCreateBtn.clicked += () => OpenCalendarAddModal();
        calQuickCategoryBtn.clicked += OpenCalendarCategoryModal;

        calAddModalCloseBtn.clicked += CloseCalendarAddModal;
        calAddCancelBtn.clicked += CloseCalendarAddModal;
        calSaveEventBtn.clicked += SaveCalendarEvent;

        calCategoryModalCloseBtn.clicked += CloseCalendarCategoryModal;
        calCategoryCancelBtn.clicked += CloseCalendarCategoryModal;
        calSaveCategoryBtn.clicked += SaveCalendarCategory;

        calDetailCloseBtn.clicked += CloseCalendarDetailModal;
        calDetailFooterCloseBtn.clicked += CloseCalendarDetailModal;
        calDetailEditBtn.clicked += EditCalendarDetailEvent;
        calDetailDeleteBtn.clicked += DeleteCalendarDetailEvent;

        if (calSearchInput != null)
        {
            calSearchInput.RegisterValueChangedCallback(evt =>
            {
                calSearchQuery = evt.newValue ?? "";
                RenderCalendarAll();
            });
        }

        if (calFilterDropdown != null)
        {
            calFilterDropdown.RegisterValueChangedCallback(evt =>
            {
                calActiveFilter = string.IsNullOrWhiteSpace(evt.newValue) || evt.newValue == "Tüm Etkinlikler"
                    ? "all"
                    : evt.newValue;
                RenderCalendarAll();
            });
        }

        if (calAddModal != null)
        {
            calAddModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == calAddModal)
                    CloseCalendarAddModal();
            });
        }

        if (calCategoryModal != null)
        {
            calCategoryModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == calCategoryModal)
                    CloseCalendarCategoryModal();
            });
        }

        if (calDetailModal != null)
        {
            calDetailModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == calDetailModal)
                    CloseCalendarDetailModal();
            });
        }

        RenderCalendarAll();
    }

    private IEnumerator LoadCalendarData(bool forceRender)
    {
        if (router == null)
            yield break;

        yield return StartCoroutine(FetchCalendarCategories());
        yield return StartCoroutine(FetchCalendarEvents());

        RefreshCalendarTypeDropdown();
        RefreshCalendarFilterDropdown();

        if (forceRender)
            RenderCalendarAll();
    }

    private IEnumerator FetchCalendarCategories()
    {
        if (router == null)
            yield break;

        string url = BuildCalendarCategoriesUrl();
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CALENDAR] CATEGORY FETCH FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            calendarCategories.Clear();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<CalendarCategoryListWrapper>("{\"items\":" + raw + "}");
        calendarCategories.Clear();
        if (wrapped?.items != null)
            calendarCategories.AddRange(wrapped.items);
    }

    private IEnumerator FetchCalendarEvents()
    {
        if (router == null)
            yield break;

        string url = BuildCalendarEventsUrl();
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CALENDAR] EVENT FETCH FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            calendarEvents.Clear();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<CalendarEventListWrapper>("{\"items\":" + raw + "}");
        calendarEvents.Clear();
        if (wrapped?.items != null)
            calendarEvents.AddRange(wrapped.items);
    }

    private string BuildCalendarCategoriesUrl()
    {
        return router.ApiBaseUrl + calendarCategoriesPath;
    }

    private string BuildCalendarEventsUrl()
    {
        return router.ApiBaseUrl + calendarEventsPath;
    }

    private void RefreshCalendarClassDropdown()
    {
        if (calAddClassDropdown == null)
            return;

        var choices = new List<string> { "Kişisel" };

        if (lastItems != null)
        {
            foreach (var item in lastItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                string lesson = string.IsNullOrWhiteSpace(item.LessonName) ? "" : $" {item.LessonName}";
                choices.Add($"{item.Name}{lesson}");
            }
        }

        calAddClassDropdown.choices = choices;
        calAddClassDropdown.index = 0;
    }

    private void RefreshCalendarTypeDropdown()
    {
        if (calAddTypeDropdown == null)
            return;

        var types = new List<string>();
        foreach (var cat in calendarCategories)
            types.Add(cat.Type);

        if (types.Count == 0)
            types.Add("Önce kategori ekleyin");

        calAddTypeDropdown.choices = types;
        calAddTypeDropdown.index = types.Count > 0 ? 0 : -1;
    }

    private void RefreshCalendarFilterDropdown()
    {
        if (calFilterDropdown == null)
            return;

        var choices = new List<string> { "Tüm Etkinlikler" };
        foreach (var cat in calendarCategories)
            choices.Add(cat.Type);

        calFilterDropdown.choices = choices;

        if (calActiveFilter == "all")
            calFilterDropdown.value = "Tüm Etkinlikler";
        else if (choices.Contains(calActiveFilter))
            calFilterDropdown.value = calActiveFilter;
        else
            calFilterDropdown.value = "Tüm Etkinlikler";
    }

    private void SwitchCalendarView(string view)
    {
        calCurrentView = view;

        SetCalendarViewButtonActive(calViewDayBtn, view == "day");
        SetCalendarViewButtonActive(calViewWeekBtn, view == "week");
        SetCalendarViewButtonActive(calViewMonthBtn, view == "month");
        SetCalendarViewButtonActive(calViewAgendaBtn, view == "agenda");

        if (calMonthView != null) calMonthView.style.display = view == "month" ? DisplayStyle.Flex : DisplayStyle.None;
        if (calWeekView != null) calWeekView.style.display = view == "week" ? DisplayStyle.Flex : DisplayStyle.None;
        if (calDayView != null) calDayView.style.display = view == "day" ? DisplayStyle.Flex : DisplayStyle.None;
        if (calAgendaView != null) calAgendaView.style.display = view == "agenda" ? DisplayStyle.Flex : DisplayStyle.None;

        RenderCalendarAll();
    }

    private void SetCalendarViewButtonActive(Button btn, bool active)
    {
        if (btn == null) return;
        if (active) btn.AddToClassList("active");
        else btn.RemoveFromClassList("active");
    }

    private void NavigateCalendar(int dir)
    {
        if (calCurrentView == "month" || calCurrentView == "agenda")
        {
            calCurrentMonth += dir;
            if (calCurrentMonth > 11) { calCurrentMonth = 0; calCurrentYear++; }
            if (calCurrentMonth < 0) { calCurrentMonth = 11; calCurrentYear--; }
        }
        else if (calCurrentView == "week")
        {
            if (!calWeekStartDate.HasValue)
                calWeekStartDate = GetCalendarWeekStart(calSelectedDate ?? DateTime.Today);

            calWeekStartDate = calWeekStartDate.Value.AddDays(dir * 7);
            calCurrentYear = calWeekStartDate.Value.Year;
            calCurrentMonth = calWeekStartDate.Value.Month - 1;
        }
        else if (calCurrentView == "day")
        {
            if (!calDayViewDate.HasValue)
                calDayViewDate = calSelectedDate ?? DateTime.Today;

            calDayViewDate = calDayViewDate.Value.AddDays(dir);
            calCurrentYear = calDayViewDate.Value.Year;
            calCurrentMonth = calDayViewDate.Value.Month - 1;
        }

        RenderCalendarAll();
    }

    private void GoCalendarToday()
    {
        var today = DateTime.Today;
        calCurrentYear = today.Year;
        calCurrentMonth = today.Month - 1;
        calSelectedDate = today;
        calDayViewDate = today;
        calWeekStartDate = GetCalendarWeekStart(today);
        RenderCalendarAll();
    }

    private void RenderCalendarAll()
    {
        RenderCalendarToolbar();
        RenderCalendarMini();
        RenderCalendarCategories();

        switch (calCurrentView)
        {
            case "week":
                RenderCalendarWeekView();
                break;
            case "day":
                RenderCalendarDayView();
                break;
            case "agenda":
                RenderCalendarAgendaView();
                break;
            default:
                RenderCalendarMonthView();
                break;
        }
    }

    private void RenderCalendarToolbar()
    {
        if (calToolbarMonthLabel == null)
            return;

        var monthDate = new DateTime(calCurrentYear, calCurrentMonth + 1, 1);
        calToolbarMonthLabel.text = monthDate.ToString("MMMM yyyy", trCulture);
    }

    private void RenderCalendarMini()
    {
        if (calMiniGrid == null)
            return;

        calMiniGrid.Clear();

        var monthDate = new DateTime(calCurrentYear, calCurrentMonth + 1, 1);
        if (calMiniMonthLabel != null)
            calMiniMonthLabel.text = monthDate.ToString("MMMM yyyy", trCulture);

        string[] weekdays = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        foreach (var day in weekdays)
        {
            var lbl = new Label(day);
            lbl.AddToClassList("cal-mini-day-label");
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            calMiniGrid.Add(lbl);
        }

        int startOffset = ((int)monthDate.DayOfWeek + 6) % 7;
        var gridStart = monthDate.AddDays(-startOffset);

        for (int i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var btn = new Button();
            btn.text = date.Day.ToString();
            btn.AddToClassList("cal-mini-day");

            if (date.Month != monthDate.Month)
                btn.AddToClassList("outside");

            if (date.Date == DateTime.Today)
                btn.AddToClassList("today");

            if (calSelectedDate.HasValue && date.Date == calSelectedDate.Value.Date)
                btn.AddToClassList("selected");

            btn.clicked += () =>
            {
                calSelectedDate = date;
                calDayViewDate = date;
                calCurrentYear = date.Year;
                calCurrentMonth = date.Month - 1;
                RenderCalendarAll();
            };

            calMiniGrid.Add(btn);
        }
    }

    private void RenderCalendarCategories()
    {
        if (calCategoryList == null)
            return;

        calCategoryList.Clear();

        if (calCategoryEmptyLabel != null)
            calCategoryEmptyLabel.style.display = calendarCategories.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        foreach (var cat in calendarCategories)
        {
            var btn = new Button();
            btn.text = string.Empty;
            btn.AddToClassList("cal-cat-chip");

            var content = new VisualElement();
            content.AddToClassList("cal-cat-chip-content");

            var dot = new VisualElement();
            dot.AddToClassList("cal-cat-chip-dot");
            dot.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(cat.Type));

            var label = new Label(cat.Label ?? "-");
            label.AddToClassList("cal-cat-chip-label");

            content.Add(dot);
            content.Add(label);
            btn.Add(content);

            if (calActiveFilter == cat.Type)
                btn.AddToClassList("active");

            btn.clicked += () =>
            {
                calActiveFilter = calActiveFilter == cat.Type ? "all" : cat.Type;
                RefreshCalendarFilterDropdown();
                RenderCalendarAll();
            };

            calCategoryList.Add(btn);
        }
    }

    private List<CalendarEventItem> GetFilteredCalendarEvents()
    {
        var list = new List<CalendarEventItem>();

        foreach (var ev in calendarEvents)
        {
            if (ev == null) continue;

            if (calActiveFilter != "all" && !string.Equals(ev.Type, calActiveFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(calSearchQuery))
            {
                string hay = $"{ev.Title} {ev.Location} {ev.Desc}".ToLowerInvariant();
                if (!hay.Contains(calSearchQuery.ToLowerInvariant()))
                    continue;
            }

            list.Add(ev);
        }

        return list
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Start)
            .ToList();
    }

    private void RenderCalendarMonthView()
    {
        if (calMonthGrid == null)
            return;

        calMonthGrid.Clear();

        var monthDate = new DateTime(calCurrentYear, calCurrentMonth + 1, 1);
        int startOffset = ((int)monthDate.DayOfWeek + 6) % 7;
        var gridStart = monthDate.AddDays(-startOffset);

        for (int i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var cell = new VisualElement();
            cell.AddToClassList("cal-day-cell");

            if (date.Date == DateTime.Today)
                cell.AddToClassList("today");

            if (calSelectedDate.HasValue && date.Date == calSelectedDate.Value.Date)
                cell.AddToClassList("selected");

            var number = new Label(date.Day.ToString());
            number.AddToClassList("cal-day-number");
            number.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (date.Month != monthDate.Month)
                number.AddToClassList("outside");
            if (date.Date == DateTime.Today)
                number.AddToClassList("today");

            cell.Add(number);

            var eventsWrap = new VisualElement();
            eventsWrap.AddToClassList("cal-day-events");

            var dayEvents = GetFilteredCalendarEvents()
                .Where(x => x.Date == date.ToString("yyyy-MM-dd"))
                .Take(3)
                .ToList();

            foreach (var ev in dayEvents)
            {
                var chip = new Button();
                chip.text = ev.Title;
                chip.AddToClassList("cal-event-chip");
                chip.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(ev.Type));
                chip.style.color = ParseHexColor(GetCalendarTypeTextColor(ev.Type));
                chip.clicked += () => OpenCalendarDetailModal(ev);
                eventsWrap.Add(chip);
            }

            int totalCount = GetFilteredCalendarEvents().Count(x => x.Date == date.ToString("yyyy-MM-dd"));
            if (totalCount > 3)
            {
                var more = new Button();
                more.text = $"+ {totalCount - 3} daha";
                more.AddToClassList("cal-more-link");
                more.clicked += () =>
                {
                    calSelectedDate = date;
                    calDayViewDate = date;
                    SwitchCalendarView("day");
                };
                eventsWrap.Add(more);
            }

            cell.Add(eventsWrap);

            cell.RegisterCallback<ClickEvent>(_ =>
            {
                calSelectedDate = date;
                calDayViewDate = date;
                RenderCalendarAll();
            });

            calMonthGrid.Add(cell);
        }
    }

    private void RenderCalendarWeekView()
    {
        if (calWeekHeader == null || calWeekBody == null)
            return;

        calWeekHeader.Clear();
        calWeekBody.Clear();

        if (!calWeekStartDate.HasValue)
            calWeekStartDate = GetCalendarWeekStart(calSelectedDate ?? DateTime.Today);

        var placeholder = new Label("");
        placeholder.AddToClassList("cal-week-header-cell");
        placeholder.AddToClassList("cal-week-time-placeholder");
        calWeekHeader.Add(placeholder);

        var weekStart = calWeekStartDate.Value;
        for (int i = 0; i < 7; i++)
        {
            var d = weekStart.AddDays(i);
            var headerCell = new Label($"{GetWeekDayShort(i)} {d.Day}");
            headerCell.AddToClassList("cal-week-header-cell");
            headerCell.style.unityTextAlign = TextAnchor.MiddleCenter;
            calWeekHeader.Add(headerCell);
        }

        var timeCol = new VisualElement();
        timeCol.AddToClassList("cal-week-time-col");
        for (int h = 7; h <= 20; h++)
        {
            var t = new Label($"{h:00}:00");
            t.AddToClassList("cal-week-time-slot");
            timeCol.Add(t);
        }
        calWeekBody.Add(timeCol);

        for (int i = 0; i < 7; i++)
        {
            var d = weekStart.AddDays(i);
            var dayCol = new VisualElement();
            dayCol.AddToClassList("cal-week-day-col");

            for (int h = 7; h <= 20; h++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("cal-week-slot");
                dayCol.Add(slot);
            }

            foreach (var ev in GetFilteredCalendarEvents().Where(x => x.Date == d.ToString("yyyy-MM-dd")))
            {
                var block = new VisualElement();
                block.AddToClassList("cal-week-event");
                block.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(ev.Type));
                block.style.color = ParseHexColor(GetCalendarTypeTextColor(ev.Type));

                int startHour = ParseHour(ev.Start);
                int startMinute = ParseMinute(ev.Start);
                int endHour = ParseHour(ev.End);
                int endMinute = ParseMinute(ev.End);

                float top = (startHour - 7) * 50f + (startMinute / 60f) * 50f;
                float height = Mathf.Max(20f, (((endHour - startHour) * 60f + (endMinute - startMinute)) / 60f) * 50f);

                block.style.top = top;
                block.style.height = height;

                block.Add(new Label(ev.Title) { name = "WeekEventTitle" });
                block.Add(new Label($"{ev.Start} - {ev.End}") { name = "WeekEventTime" });

                block.RegisterCallback<ClickEvent>(_ => OpenCalendarDetailModal(ev));
                dayCol.Add(block);
            }

            calWeekBody.Add(dayCol);
        }
    }

    private void RenderCalendarDayView()
    {
        if (calDayHeaderLabel == null || calDayTimeCol == null || calDayEventsCol == null)
            return;

        var day = calDayViewDate ?? calSelectedDate ?? DateTime.Today;
        calDayHeaderLabel.text = day.ToString("dddd, dd MMMM yyyy", trCulture);

        calDayTimeCol.Clear();
        calDayEventsCol.Clear();

        for (int h = 7; h <= 20; h++)
        {
            var time = new Label($"{h:00}:00");
            time.AddToClassList("cal-day-time-slot");
            calDayTimeCol.Add(time);

            var slot = new VisualElement();
            slot.AddToClassList("cal-day-slot");
            calDayEventsCol.Add(slot);
        }

        foreach (var ev in GetFilteredCalendarEvents().Where(x => x.Date == day.ToString("yyyy-MM-dd")))
        {
            var block = new VisualElement();
            block.AddToClassList("cal-day-event");
            block.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(ev.Type));
            block.style.color = ParseHexColor(GetCalendarTypeTextColor(ev.Type));

            int startHour = ParseHour(ev.Start);
            int startMinute = ParseMinute(ev.Start);
            int endHour = ParseHour(ev.End);
            int endMinute = ParseMinute(ev.End);

            float top = (startHour - 7) * 56f + (startMinute / 60f) * 56f;
            float height = Mathf.Max(28f, (((endHour - startHour) * 60f + (endMinute - startMinute)) / 60f) * 56f);

            block.style.top = top;
            block.style.height = height;

            var title = new Label(ev.Title);
            title.AddToClassList("cal-day-event-title");
            var time = new Label($"{ev.Start} - {ev.End}");
            time.AddToClassList("cal-day-event-time");
            var loc = new Label(string.IsNullOrWhiteSpace(ev.Location) ? "-" : ev.Location);
            loc.AddToClassList("cal-day-event-loc");

            block.Add(title);
            block.Add(time);
            block.Add(loc);

            block.RegisterCallback<ClickEvent>(_ => OpenCalendarDetailModal(ev));
            calDayEventsCol.Add(block);
        }
    }

    private void RenderCalendarAgendaView()
    {
        if (calAgendaContent == null)
            return;

        calAgendaContent.Clear();

        var grouped = GetFilteredCalendarEvents()
            .GroupBy(x => x.Date)
            .OrderBy(x => x.Key);

        foreach (var group in grouped)
        {
            var grp = new VisualElement();
            grp.AddToClassList("cal-agenda-date-group");

            DateTime date;
            DateTime.TryParse(group.Key, out date);

            var lbl = new Label(date == default ? group.Key : date.ToString("dddd, dd MMMM yyyy", trCulture));
            lbl.AddToClassList("cal-agenda-date-label");
            grp.Add(lbl);

            foreach (var ev in group)
            {
                var item = new VisualElement();
                item.AddToClassList("cal-agenda-item");

                var dot = new VisualElement();
                dot.AddToClassList("cal-agenda-dot");
                dot.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(ev.Type));

                var info = new VisualElement();
                info.AddToClassList("cal-agenda-info");

                var title = new Label(ev.Title);
                title.AddToClassList("cal-agenda-title");

                var meta = new Label($"{ev.Start} - {ev.End}" + (string.IsNullOrWhiteSpace(ev.Location) ? "" : $" · {ev.Location}"));
                meta.AddToClassList("cal-agenda-meta");

                info.Add(title);
                info.Add(meta);

                var badge = new Label(GetCalendarTypeLabel(ev.Type));
                badge.AddToClassList("cal-agenda-badge");
                badge.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(ev.Type));
                badge.style.color = ParseHexColor(GetCalendarTypeTextColor(ev.Type));

                item.Add(dot);
                item.Add(info);
                item.Add(badge);

                item.RegisterCallback<ClickEvent>(_ => OpenCalendarDetailModal(ev));
                grp.Add(item);
            }

            calAgendaContent.Add(grp);
        }
    }

    private void OpenCalendarAddModal(bool editMode = false)
    {
        if (calAddModal == null) return;

        RefreshCalendarClassDropdown();
        if (lastItems == null)
            StartCoroutine(FetchMyClasses());

        if (!editMode)
        {
            calEditingEventId = null;

            if (calAddTitleInput != null) calAddTitleInput.value = string.Empty;
            if (calAddDescInput != null) calAddDescInput.value = string.Empty;

            if (calAddTypeDropdown != null && calAddTypeDropdown.choices != null && calAddTypeDropdown.choices.Count > 0)
                calAddTypeDropdown.index = 0;

            if (calAddClassDropdown != null)
            {
                if (calAddClassDropdown.choices != null && calAddClassDropdown.choices.Contains("Kişisel"))
                    calAddClassDropdown.value = "Kişisel";
                else if (calAddClassDropdown.choices != null && calAddClassDropdown.choices.Count > 0)
                    calAddClassDropdown.index = 0;
            }

            if (calAddDateInput != null)
                calAddDateInput.value = (calSelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd");

            if (calAddStartInput != null) calAddStartInput.value = "09:00";
            if (calAddEndInput != null) calAddEndInput.value = "10:00";
        }

        calAddModal.RemoveFromClassList("hidden");
    }

    private void CloseCalendarAddModal()
    {
        if (calAddModal == null) return;
        calAddModal.AddToClassList("hidden");
    }

    private void OpenCalendarCategoryModal()
    {
        if (calCategoryNameInput != null)
            calCategoryNameInput.value = "";

        calSelectedPresetColor = "";
        RefreshCalendarPresetSelectionUI();

        if (calCategoryColorInput != null)
            calCategoryColorInput.value = "";

        calSelectedTextColor = "#ffffff";
        RefreshCalendarTextColorSelectionUI();

        calCategoryModal?.RemoveFromClassList("hidden");
    }

    private void CloseCalendarCategoryModal()
    {
        calCategoryModal?.AddToClassList("hidden");
    }

    private void OpenCalendarDetailModal(CalendarEventItem ev)
    {
        if (ev == null || calDetailModal == null) return;

        calCurrentDetailEvent = ev;
        calDetailModal.RemoveFromClassList("hidden");

        if (calDetailTitleLabel != null) calDetailTitleLabel.text = ev.Title;
        if (calDetailTypeLabel != null)
        {
            calDetailTypeLabel.text = GetCalendarTypeLabel(ev.Type);
            calDetailTypeLabel.style.backgroundColor = ParseHexColor(GetCalendarTypeColor(ev.Type));
            calDetailTypeLabel.style.color = ParseHexColor(GetCalendarTypeTextColor(ev.Type));
        }

        if (calDetailDateLabel != null) calDetailDateLabel.text = ev.Date;
        if (calDetailTimeLabel != null) calDetailTimeLabel.text = $"{ev.Start} - {ev.End}";
        if (calDetailLocationLabel != null)
            calDetailLocationLabel.text = string.IsNullOrWhiteSpace(ev.RelatedClass)
                ? "Kişisel"
                : ev.RelatedClass;
        if (calDetailDescLabel != null) calDetailDescLabel.text = string.IsNullOrWhiteSpace(ev.Desc) ? "-" : ev.Desc;
    }

    private void CloseCalendarDetailModal()
    {
        calDetailModal?.AddToClassList("hidden");
        calCurrentDetailEvent = null;
    }

    private void SaveCalendarEvent()
    {
        string title = calAddTitleInput?.value?.Trim() ?? "";
        string type = calAddTypeDropdown?.value ?? "";
        string date = calAddDateInput?.value?.Trim() ?? "";
        string start = calAddStartInput?.value?.Trim() ?? "09:00";
        string end = calAddEndInput?.value?.Trim() ?? "10:00";
        string relatedClass = calAddClassDropdown?.value ?? "";
        string desc = calAddDescInput?.value?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(relatedClass) || relatedClass == "Seçiniz")
            relatedClass = "Kişisel";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(date))
        {
            Debug.LogWarning("[CALENDAR] Başlık ve tarih zorunlu.");
            return;
        }

        if (calendarCategories.Count == 0 || string.IsNullOrWhiteSpace(type) || type == "Önce kategori ekleyin")
        {
            Debug.LogWarning("[CALENDAR] Etkinlik eklemeden önce kategori eklemelisiniz.");
            return;
        }

        var dto = new CalendarUpsertEventRequest
        {
            Title = title,
            Type = type,
            Date = date,
            Start = string.IsNullOrWhiteSpace(start) ? "09:00" : start,
            End = string.IsNullOrWhiteSpace(end) ? "10:00" : end,
            Location = string.Empty,
            RelatedClass = relatedClass,
            Desc = desc
        };

        if (calEditingEventId.HasValue)
            StartCoroutine(UpdateCalendarEvent(calEditingEventId.Value, dto));
        else
            StartCoroutine(CreateCalendarEvent(dto));
    }

    private void SaveCalendarCategory()
    {
        string label = calCategoryNameInput?.value?.Trim() ?? "";
        string color = ResolveCalendarCategoryColor();
        string textColor = ResolveCalendarCategoryTextColor(color);

        if (string.IsNullOrWhiteSpace(label))
        {
            Debug.LogWarning("[CALENDAR] Kategori adı boş olamaz.");
            return;
        }

        StartCoroutine(CreateCalendarCategory(new CalendarCreateCategoryRequest
        {
            Label = label,
            Color = color,
            TextColor = textColor
        }));
    }

    private string ResolveCalendarCategoryColor()
    {
        string manual = calCategoryColorInput?.value?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(manual) && UnityEngine.ColorUtility.TryParseHtmlString(manual, out _))
            return manual;

        if (!string.IsNullOrWhiteSpace(calSelectedPresetColor) && UnityEngine.ColorUtility.TryParseHtmlString(calSelectedPresetColor, out _))
            return calSelectedPresetColor;

        return CalendarDefaultCategoryColors[0];
    }

    private string ResolveCalendarCategoryTextColor(string backgroundColor)
    {
        if (!string.IsNullOrWhiteSpace(calSelectedTextColor) && UnityEngine.ColorUtility.TryParseHtmlString(calSelectedTextColor, out _))
            return calSelectedTextColor;

        return AutoTextColorFor(backgroundColor);
    }

    private void BindCalendarPresetColorButtons()
    {
        calCategoryPresetColorButtons.Clear();

        for (int i = 0; i < CalendarDefaultCategoryColors.Length; i++)
        {
            int index = i;
            string buttonName = $"CalPresetColor{(i + 1).ToString("00")}";
            var button = root.Q<Button>(buttonName);
            if (button == null)
                continue;

            string hex = CalendarDefaultCategoryColors[i];
            button.clicked += () => SelectCalendarPresetColor(index, hex);
            calCategoryPresetColorButtons.Add(button);
        }
    }

    private void BindCalendarTextColorButtons()
    {
        if (calTextColorWhiteBtn != null)
            calTextColorWhiteBtn.clicked += () => SelectCalendarTextColor("#ffffff");

        if (calTextColorBlackBtn != null)
            calTextColorBlackBtn.clicked += () => SelectCalendarTextColor("#111111");
    }

    private void SelectCalendarTextColor(string hex)
    {
        calSelectedTextColor = hex;
        RefreshCalendarTextColorSelectionUI();
    }

    private void RefreshCalendarTextColorSelectionUI()
    {
        StyleTextColorButton(calTextColorWhiteBtn, string.Equals(calSelectedTextColor, "#ffffff", StringComparison.OrdinalIgnoreCase));
        StyleTextColorButton(calTextColorBlackBtn, string.Equals(calSelectedTextColor, "#111111", StringComparison.OrdinalIgnoreCase));
    }

    private void StyleTextColorButton(Button button, bool selected)
    {
        if (button == null)
            return;

        button.style.borderLeftWidth = selected ? 2 : 1;
        button.style.borderRightWidth = selected ? 2 : 1;
        button.style.borderTopWidth = selected ? 2 : 1;
        button.style.borderBottomWidth = selected ? 2 : 1;
        button.style.borderLeftColor = selected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
        button.style.borderRightColor = selected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
        button.style.borderTopColor = selected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
        button.style.borderBottomColor = selected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
    }

    private void SelectCalendarPresetColor(int index, string hex)
    {
        calSelectedPresetColor = hex;
        if (calCategoryColorInput != null)
            calCategoryColorInput.value = hex;

        RefreshCalendarPresetSelectionUI(index);
    }

    private void RefreshCalendarPresetSelectionUI(int selectedIndex = -1)
    {
        for (int i = 0; i < calCategoryPresetColorButtons.Count; i++)
        {
            var btn = calCategoryPresetColorButtons[i];
            if (btn == null)
                continue;

            bool isSelected = i == selectedIndex;
            btn.style.borderLeftWidth = isSelected ? 2 : 1;
            btn.style.borderRightWidth = isSelected ? 2 : 1;
            btn.style.borderTopWidth = isSelected ? 2 : 1;
            btn.style.borderBottomWidth = isSelected ? 2 : 1;
            btn.style.borderLeftColor = isSelected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
            btn.style.borderRightColor = isSelected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
            btn.style.borderTopColor = isSelected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
            btn.style.borderBottomColor = isSelected ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.79f, 0.79f, 0.79f, 1f);
        }
    }

    private void DeleteCalendarDetailEvent()
    {
        if (calCurrentDetailEvent == null)
            return;

        StartCoroutine(DeleteCalendarEvent(calCurrentDetailEvent.Id));
    }

    private void EditCalendarDetailEvent()
    {
        if (calCurrentDetailEvent == null)
            return;

        var ev = calCurrentDetailEvent;
        CloseCalendarDetailModal();
        OpenCalendarAddModal(true);
        calEditingEventId = ev.Id;

        if (calAddTitleInput != null) calAddTitleInput.value = ev.Title;
        if (calAddTypeDropdown != null) calAddTypeDropdown.value = ev.Type;
        if (calAddDateInput != null) calAddDateInput.value = ev.Date;
        if (calAddStartInput != null) calAddStartInput.value = ev.Start;
        if (calAddEndInput != null) calAddEndInput.value = ev.End;
        if (calAddClassDropdown != null)
            calAddClassDropdown.value = string.IsNullOrWhiteSpace(ev.RelatedClass) ? "Kişisel" : ev.RelatedClass;
        if (calAddDescInput != null) calAddDescInput.value = ev.Desc;
    }

    private IEnumerator CreateCalendarCategory(CalendarCreateCategoryRequest dto)
    {
        if (router == null)
            yield break;

        string url = BuildCalendarCategoriesUrl();
        string json = JsonUtility.ToJson(dto);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CALENDAR] CATEGORY CREATE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        CloseCalendarCategoryModal();
        yield return StartCoroutine(LoadCalendarData(false));
        RenderCalendarAll();
    }

    private IEnumerator CreateCalendarEvent(CalendarUpsertEventRequest dto)
    {
        if (router == null)
            yield break;

        string url = BuildCalendarEventsUrl();
        string json = JsonUtility.ToJson(dto);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CALENDAR] EVENT CREATE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        calEditingEventId = null;
        CloseCalendarAddModal();
        yield return StartCoroutine(FetchCalendarEvents());
        RenderCalendarAll();
    }

    private IEnumerator UpdateCalendarEvent(int eventId, CalendarUpsertEventRequest dto)
    {
        if (router == null)
            yield break;

        string url = BuildCalendarEventsUrl() + "/" + eventId;
        string json = JsonUtility.ToJson(dto);

        using var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CALENDAR] EVENT UPDATE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        calEditingEventId = null;
        CloseCalendarAddModal();
        yield return StartCoroutine(FetchCalendarEvents());
        RenderCalendarAll();
    }

    private IEnumerator DeleteCalendarEvent(int eventId)
    {
        if (router == null)
            yield break;

        string url = BuildCalendarEventsUrl() + "/" + eventId;
        using var req = new UnityWebRequest(url, "DELETE");
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CALENDAR] EVENT DELETE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        CloseCalendarDetailModal();
        yield return StartCoroutine(FetchCalendarEvents());
        RenderCalendarAll();
    }

    private DateTime GetCalendarWeekStart(DateTime date)
    {
        int diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    private string GetWeekDayShort(int index)
    {
        string[] days = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        return days[Mathf.Clamp(index, 0, days.Length - 1)];
    }

    private string GetCalendarTypeColor(string type)
    {
        var cat = calendarCategories.FirstOrDefault(x => x.Type == type);
        return cat != null && !string.IsNullOrWhiteSpace(cat.Color) ? cat.Color : "#7f8c8d";
    }

    private string GetCalendarTypeTextColor(string type)
    {
        var cat = calendarCategories.FirstOrDefault(x => x.Type == type);
        if (cat != null && !string.IsNullOrWhiteSpace(cat.TextColor) && UnityEngine.ColorUtility.TryParseHtmlString(cat.TextColor, out _))
            return cat.TextColor;

        return AutoTextColorFor(GetCalendarTypeColor(type));
    }

    private string GetCalendarTypeLabel(string type)
    {
        var cat = calendarCategories.FirstOrDefault(x => x.Type == type);
        return cat != null && !string.IsNullOrWhiteSpace(cat.Label) ? cat.Label : "Kategori";
    }

    private string AutoTextColorFor(string backgroundHex)
    {
        if (!UnityEngine.ColorUtility.TryParseHtmlString(backgroundHex, out var bg))
            return "#ffffff";

        float luminance = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
        return luminance > 0.62f ? "#111111" : "#ffffff";
    }

    private int ParseHour(string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return 9;
        var parts = time.Split(':');
        return parts.Length > 0 && int.TryParse(parts[0], out var h) ? h : 9;
    }

    private int ParseMinute(string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return 0;
        var parts = time.Split(':');
        return parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
    }

    private Color ParseHexColor(string hex)
    {
        if (UnityEngine.ColorUtility.TryParseHtmlString(hex, out var c))
            return c;

        return new Color32(127, 140, 141, 255);
    }

    #endregion

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

    private class ProgressExperimentViewItem
    {
        public ExperimentDto Experiment;
        public AssignmentDto Assignment;

        public string Title;
        public string UnitName;
        public string LessonName;
        public string Difficulty;
        public string Status;
        public int Score;
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

        public int CorrectCount;
        public int WrongCount;
        public int TotalQuestionCount;
        public int Score;
        public bool IsCompleted;
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
    private class SettingsApiMessageDto
    {
        public string message;
    }

    [Serializable]
    private class SettingsUserUpdatePayloadDto
    {
        public int Id;
        public string Name;
        public string Surname;
        public string Email;
        public string PasswordHash;
        public string PasswordSalt;
        public int RoleId;
        public string CreatedAt;
        public string LastLogin;
        public bool IsActive;
        public string ProfilePictureUrl;
        public string Phone;
        public string BirthDate;
    }

    [Serializable]
    private class RoleChangeNotificationDto
    {
        public string Id;
        public string Message;
        public DateTime Timestamp;
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