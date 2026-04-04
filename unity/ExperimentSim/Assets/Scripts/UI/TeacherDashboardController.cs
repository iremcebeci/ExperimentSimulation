using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class TeacherDashboardController : MonoBehaviour
{
    private AppRouter router;
    private VisualElement root;
    private VisualElement mainContent;

    [Header("Controllers")]
    [SerializeField] private DashboardSidebarController sidebarController;
    [SerializeField] private DashboardsHeaderController headerController;


    [Header("API Paths")]
    [SerializeField] private string myClassesPath = "/api/Class/my";
    [SerializeField] private string createClassPath = "/api/Class";
    [SerializeField] private string myAssignmentsPath = "/api/Assignment/my";
    [SerializeField] private string createAssignmentPath = "/api/Assignment";
    [SerializeField] private string experimentsByGradeLessonPath = "/api/Experiment/by-grade-lesson";
    [SerializeField] private string classJoinRequestsPathTemplate = "/api/Class/{classId}/join-requests";
    [SerializeField] private string classStudentsPathTemplate = "/api/Class/{classId}/students";
    [SerializeField] private string classStudentProfilePathTemplate = "/api/Class/{classId}/students/{studentId}/profile";
    [SerializeField] private string classActivityPathTemplate = "/api/Class/{classId}/activity";
    [SerializeField] private string classStatusPathTemplate = "/api/Class/{classId}/status";
    [SerializeField] private string classStudentRemovePathTemplate = "/api/Class/{classId}/students/{studentId}/remove";
    [SerializeField] private string classActivityLikePathTemplate = "/api/Class/{classId}/activity/{activityId}/like";
    [SerializeField] private string classActivityUnlikePathTemplate = "/api/Class/{classId}/activity/{activityId}/unlike";
    [SerializeField] private string classActivityCommentPathTemplate = "/api/Class/{classId}/activity/{activityId}/comments";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";

    private DropdownField assignmentUnitDropdown;
    private DropdownField assignmentExperimentDropdown;

    private ExperimentDto[] experimentItems;
    private readonly Dictionary<string, List<ExperimentDto>> unitToExperiments = new();

    // Home
    private Label welcomeUsernameLabel;
    private VisualElement teacherHomePage;
    private Label homeTotalClassValueLabel;
    private Label homeTotalStudentValueLabel;
    private Label homeActiveAssignmentValueLabel;
    private Label homeCompletedAssignmentValueLabel;
    private ScrollView homeSummaryScroll;
    private Label homeChartPeakInfoLabel;
    private readonly List<VisualElement> homeChartBars = new();
    private readonly List<Label> homeChartValueLabels = new();
    private readonly float[] homeWeeklyHours = new float[7];

    // Stats
    private Label activeClassCountLabel, totalClassCountLabel;
    private Label activeStudentCountLabel, totalStudentCountLabel;
    private Label classSuccessRateLabel, topClassNameLabel;
    private Label latestAssignmentCompletionRateLabel, latestAssignmentDeliverySplitLabel;

    // Classes
    private ScrollView classesScroll;
    private VisualElement classesRows;

    // Add Class Modal
    private Button addClassBtn;
    private VisualElement addClassModal;
    private Button addClassModalCloseBtn;
    private Button addClassCancelBtn;
    private Button saveClassBtn;
    private TextField classNameInput;
    private TextField lessonInput;

    private bool modalBackdropBound;

    // Filters
    private TextField searchInput;
    private Toggle includeInactiveToggle;

    private bool filtersBound;
    private string currentSearch = "";
    private bool includeInactive = false;

    private MyClassDto[] lastItems;

    // ---------------- CLASS DETAILS ----------------
    private VisualElement classDetailsPage;

    private Label cdClassNameLabel;
    private Label cdTeacherNameLabel;
    private Label cdStudentCountLabel;
    private Label cdAssignmentCountLabel;
    private Label cdSuccessRateLabel;
    private Label cdCreatedAtLabel;
    private Label cdClassCodeLabel;
    private Label cdStatusLabel;

    private Button copyClassCodeBtn;
    private Button toggleClassStatusBtn;

    private Button cdTabGeneralBtn;
    private Button cdTabStudentsBtn;
    private Button cdTabAssignmentsBtn;
    private Button cdTabActivityBtn;
    private Button cdTabRequestsBtn;

    private VisualElement classDetailsRows;
    private MyClassDto currentSelectedClass;

    private VisualElement classDetailsGeneralContent;
    private VisualElement classDetailsStudentsContent;
    private VisualElement classDetailsAssignmentsContent;
    private VisualElement classDetailsActivityContent;
    private VisualElement classDetailsRequestsContent;
    private Label cgCompletionPercentLabel;
    private Label cgCompletionDoneLabel;
    private Label cgCompletionRemainingLabel;
    private VisualElement cgCompletionDonut;
    private readonly List<Label> classGeneralChartValueLabels = new();
    private readonly List<VisualElement> classGeneralChartFillBars = new();
    private readonly Dictionary<string, Label> classGeneralChartValueByDay = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VisualElement> classGeneralChartFillByDay = new(StringComparer.OrdinalIgnoreCase);

    private VisualElement studentsRows;
    private VisualElement assignmentsCardsRow;
    private VisualElement activityFeed;
    private VisualElement requestList;

    private Label pendingRequestCountLabel;
    private TextField requestSearchInput;

    private Button assignmentFilterAllBtn;
    private Button assignmentFilterActiveBtn;
    private Button assignmentFilterPassiveBtn;
    private Button assignmentFilterCompletedBtn;
    private Button assignmentFilterIncompleteBtn;
    private TextField assignmentSearchInput;
    private string assignmentFilterMode = "all";
    private string assignmentSearchQuery = "";

    private Button activityFilterAllBtn;
    private Button activityFilterExperimentBtn;
    private Button activityFilterParticipationBtn;
    private TextField activitySearchInput;
    private string activityFilterMode = "all";
    private string activitySearchQuery = "";

    private DropdownField gradeLevelDropdown;
    private JoinRequestDto[] currentRequestItems;
    private ClassStudentDto[] currentStudentItems;
    private ClassActivityDto[] currentActivityItems;

    private VisualElement studentFilePage;
    private Button studentFileBackBtn;

    private Label sfStudentNameLabel;
    private Label sfStudentClassLabel;
    private Label sfStudentAvatarLabel;
    private Label sfStudentNoLabel;
    private Label sfStudentJoinDateLabel;
    private Label sfStudentLastLoginLabel;
    private Label sfStudentEmailLabel;
    private Label sfStudentPerformancePercentLabel;
    private Label sfStudentCompletedAssignmentsLabel;
    private Label sfStudentCompletedExperimentsLabel;
    private Label sfStudentParticipationLabel;
    private Label sfStudentStreakLabel;

    private ScrollView sfAssignmentsHistoryScroll;
    private ScrollView sfExperimentsHistoryScroll;
    private int selectedStudentId;

    // ------------------------------------------------

    // ---------------- ADD ASSIGNMENT ----------------
    private Button assignmentPastBtn, assignmentNextBtn;
    private VisualElement timelineDays, timelineContents;
    private Label addAssignmentInfoLabel;

    private VisualElement assignmentModal;
    private Button closeAssignmentModalBtn, cancelAssignmentBtn, saveAssignmentBtn;

    private TextField assignmentTitleField, assignmentStartField;
    private DropdownField assignmentClassDropdown;
    private DropdownField assignmentLessonDropdown;
    private IntegerField assignmentDurationField;

    private VisualElement assignmentDetailsModal;
    private Button assignmentDetailsCloseBtn;
    private Label assignmentDetailsTitle;
    private Label assignmentDetailsClass;
    private Label assignmentDetailsLesson;
    private Label assignmentDetailsStart;
    private Label assignmentDetailsDuration;
    private Label assignmentDetailsExperiment;

    private VisualElement selectedTimelineCell;
    private int selectedTimelineRow = -1;
    private int selectedTimelineDay = -1;

    private DateTime visibleStartDate;
    private const int AssignmentDayCount = 14;
    private const int AssignmentRowCount = 8;

    private readonly CultureInfo trCulture = new CultureInfo("tr-TR");
    private AssignmentDto[] assignmentItems;
    private readonly Dictionary<string, int> classNameToId = new();
    private readonly Dictionary<string, string> classNameToLesson = new();
    private readonly Dictionary<string, string> classNameToGrade = new();
    // ------------------------------------------------
    // ---------------- CALENDAR ----------------
    private VisualElement calendarPage;

    private Button calAddEventBtn;
    private Button calTodayBtnTop;
    private Button calExportBtn;
    private Button calRefreshBtn;

    private Button calPrevBtn;
    private Button calNextBtn;
    private Button calMiniPrevBtn;
    private Button calMiniNextBtn;

    private Button calMonthViewBtn;
    private Button calWeekViewBtn;
    private Button calDayViewBtn;
    private Button calAgendaViewBtn;

    private TextField calSearchInput;
    private DropdownField calFilterDropdown;

    private Label calCurrentMonthLabel;
    private Label calMiniMonthLabel;

    private VisualElement calMiniGrid;
    private VisualElement calUpcomingList;
    private VisualElement calMonthGrid;

    private VisualElement calMonthView;
    private VisualElement calWeekView;
    private VisualElement calDayView;
    private VisualElement calAgendaView;

    private VisualElement calWeekHeader;
    private VisualElement calWeekBody;
    private VisualElement calDayHeader;
    private VisualElement calDayBody;

    private ScrollView calAgendaList;
    private VisualElement calAgendaItems;

    private VisualElement calEventModal;
    private Button calCloseModalBtn;
    private Button calCancelEventBtn;
    private Button calSaveEventBtn;
    private Button calDeleteEventBtn;

    private Label calModalTitleLabel;
    private TextField calEventTitleInput;
    private DropdownField calEventTypeDropdown;
    private TextField calEventDateInput;
    private TextField calEventTimeInput;
    private TextField calEventDescriptionInput;

    private DateTime calendarCurrentDate = DateTime.Today;
    private string currentCalendarView = "month";

    private readonly List<CalendarEventData> sampleCalendarEvents = new();
    // ------------------------------------------

    // ---------------- PERSONAL ACTIVITY ----------------
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
    // ------------------------------------------

    // ---------------- PROFILE ----------------
    private VisualElement profilePage;
    private Label profileAvatarLabel;
    private Label profileNameLabel;
    private Label profileRoleLabel;
    private Label profileStatusLabel;
    private Label profileMailLabel;
    private Label profileJoinDateLabel;
    private Label profileLastLoginLabel;
    private VisualElement profileStatsGrid;
    private Button teacherNewAssignmentBtn;
    private Button teacherGoClassesBtn;
    private Button teacherLogoutBtn;
    private Coroutine sessionHeartbeatRoutine;
    // ------------------------------------------

    public void Bind(AppRouter router, VisualElement teacherView)
    {
        this.router = router;
        root = teacherView;

        if (root == null)
        {
            Debug.LogError("[TeacherDashboardController] root null.");
            return;
        }

        mainContent = root.Q<VisualElement>("MainContent");
        if (mainContent == null)
        {
            Debug.LogError("[TeacherDashboardController] MainContent not found (name=\"MainContent\").");
            return;
        }

        visibleStartDate = GetStartOfWeek(DateTime.Today);

        // Home
        welcomeUsernameLabel = root.Q<Label>("WelcomeUsernameLabel");
        var welcomeMessageLabel = root.Q<Label>("WelcomeMessageLabel");

        // Stats
        activeClassCountLabel = root.Q<Label>("ActiveClassCountLabel");
        totalClassCountLabel = root.Q<Label>("TotalClassCountLabel");
        activeStudentCountLabel = root.Q<Label>("ActiveStudentCountLabel");
        totalStudentCountLabel = root.Q<Label>("TotalStudentCountLabel");
        classSuccessRateLabel = root.Q<Label>("ClassSuccessRateLabel");
        topClassNameLabel = root.Q<Label>("TopClassNameLabel");
        latestAssignmentCompletionRateLabel = root.Q<Label>("LatestAssignmentCompletionRateLabel");
        latestAssignmentDeliverySplitLabel = root.Q<Label>("LatestAssignmentDeliverySplitLabel");

        // Classes page
        classesScroll = root.Q<ScrollView>("ClassesScroll");
        classesRows = root.Q<VisualElement>("ClassesRows");

        // Class Details page
        BindClassDetailsPage();

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
        BindAddClassModal();
        BindAddAssignmentPage();
        BindHomePage();
        BindFilters();
        BindAddClassModal();
        BindAddAssignmentPage();
        BindCalendarPage();
        BindPersonalActivityPage();
        BindProfilePage();
        BindMenuButtons();

        StartCoroutine(FetchMyClasses());
        StartCoroutine(FetchMyAssignments());


        ShowPage("HomePage");
        SetMenuActive("HomeBtn");

        if (sessionHeartbeatRoutine != null)
            StopCoroutine(sessionHeartbeatRoutine);
        sessionHeartbeatRoutine = StartCoroutine(SessionHeartbeatLoop());

        StartCoroutine(RefreshTeacherHomeDashboardData(forceRefresh: true));
    }

    private void HandleHeaderUserLoaded()
    {
        if (welcomeUsernameLabel != null)
            welcomeUsernameLabel.text = $"Merhaba, {router.CurrentName} {router.CurrentSurname}!";
    }

    private void BindHomePage()
    {
        teacherHomePage = root.Q<VisualElement>("TeacherHomePage");
        if (teacherHomePage == null)
            return;

        homeTotalClassValueLabel = teacherHomePage.Q<Label>("TcTotalClassValueLabel");
        homeTotalStudentValueLabel = teacherHomePage.Q<Label>("TcTotalStudentValueLabel");
        homeActiveAssignmentValueLabel = teacherHomePage.Q<Label>("TcActiveAssignmentValueLabel");
        homeCompletedAssignmentValueLabel = teacherHomePage.Q<Label>("TcCompletedAssignmentValueLabel");

        homeSummaryScroll = teacherHomePage.Q<ScrollView>("TcSummaryScroll");
        homeChartPeakInfoLabel = teacherHomePage.Q<Label>("TcChartPeakInfoLabel");

        homeChartBars.Clear();
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarMon"));
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarTue"));
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarWed"));
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarThu"));
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarFri"));
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarSat"));
        homeChartBars.Add(teacherHomePage.Q<VisualElement>("TcBarSun"));

        homeChartValueLabels.Clear();
        homeChartValueLabels.AddRange(teacherHomePage.Query<Label>(className: "cg-bar-value").ToList());
    }

    private IEnumerator RefreshTeacherHomeDashboardData(bool forceRefresh)
    {
        if (router == null)
            yield break;

        if (forceRefresh || lastItems == null)
            yield return StartCoroutine(FetchMyClasses());

        if (forceRefresh || assignmentItems == null)
            yield return StartCoroutine(FetchMyAssignments());

        yield return StartCoroutine(FetchWeeklySessionHours());

        ApplyTeacherHomeDashboardMetrics();
    }

    private void ApplyTeacherHomeDashboardMetrics()
    {
        var classes = (lastItems ?? Array.Empty<MyClassDto>()).Where(c => c != null).ToArray();
        var assignments = (assignmentItems ?? Array.Empty<AssignmentDto>()).Where(a => a != null).ToArray();

        int totalClasses = classes.Length;
        int totalStudents = classes.Sum(c => Mathf.Max(c.StudentCount, 0));
        int activeAssignments = assignments.Count(a => a.IsActive);
        int completedAssignments = assignments.Count(a => string.Equals(GetTeacherAssignmentStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase));

        if (homeTotalClassValueLabel != null) homeTotalClassValueLabel.text = totalClasses.ToString();
        if (homeTotalStudentValueLabel != null) homeTotalStudentValueLabel.text = totalStudents.ToString();
        if (homeActiveAssignmentValueLabel != null) homeActiveAssignmentValueLabel.text = activeAssignments.ToString();
        if (homeCompletedAssignmentValueLabel != null) homeCompletedAssignmentValueLabel.text = completedAssignments.ToString();

        var mostActiveClass = classes
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.StudentCount)
            .ThenByDescending(c => c.AssignmentCount)
            .FirstOrDefault();

        if (mostActiveClass == null)
        {
            SetTeacherHomeSummaryItem(0, "-", "Aktif sınıf bulunmuyor");
        }
        else
        {
            string detail = $"Öğrenci: {Mathf.Max(mostActiveClass.StudentCount, 0)} • Ödev: {Mathf.Max(mostActiveClass.AssignmentCount, 0)}";
            SetTeacherHomeSummaryItem(0, SafeText(mostActiveClass.Name), detail);
        }

        DateTime weekStart = DateTime.Today.AddDays(-6);
        var topCompletedThisWeek = assignments
            .Select(a => new { item = a, due = GetTeacherAssignmentDueAt(a) })
            .Where(x => x.due.HasValue && x.due.Value.Date >= weekStart && x.due.Value.Date <= DateTime.Today)
            .Where(x => string.Equals(GetTeacherAssignmentStatus(x.item), "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => new { x.item.ClassId, className = SafeText(x.item.ClassName) })
            .Select(g => new { g.Key.className, count = g.Count() })
            .OrderByDescending(x => x.count)
            .FirstOrDefault();

        if (topCompletedThisWeek == null)
        {
            SetTeacherHomeSummaryItem(1, "-", "Bu hafta tamamlanan ödev yok");
        }
        else
        {
            SetTeacherHomeSummaryItem(1, topCompletedThisWeek.className, $"Toplam {topCompletedThisWeek.count} ödev teslimi");
        }

        var latestClass = classes
            .Select(c => new { item = c, created = TryParseDashboardDate(c.JoinedAt, out var dt) ? dt : DateTime.MinValue })
            .OrderByDescending(x => x.created)
            .FirstOrDefault();

        if (latestClass == null)
        {
            SetTeacherHomeSummaryItem(2, "-", "Sınıf verisi yok");
        }
        else
        {
            string createdText = latestClass.created == DateTime.MinValue
                ? "Oluşturulma Tarihi: bilinmiyor"
                : $"Oluşturulma Tarihi: {latestClass.created:dd MMMM yyyy}";
            SetTeacherHomeSummaryItem(2, SafeText(latestClass.item.Name), createdText);
        }

        ApplyTeacherHomeChartValues(homeWeeklyHours);
    }

    private void SetTeacherHomeSummaryItem(int index, string title, string detail)
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

    private void ApplyTeacherHomeChartValues(float[] weekly)
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

    private string GetTeacherAssignmentStatus(AssignmentDto assignment)
    {
        if (assignment == null)
            return "Planlı";

        if (!TryParseDashboardDate(assignment.StartDate, out var startDate))
            return assignment.IsActive ? "Aktif" : "Tamamlandı";

        int duration = Mathf.Max(assignment.DurationDays, 1);
        DateTime endExclusive = startDate.Date.AddDays(duration);

        if (!assignment.IsActive || DateTime.Today >= endExclusive)
            return "Tamamlandı";

        if (DateTime.Today < startDate.Date)
            return "Planlı";

        return "Aktif";
    }

    private DateTime? GetTeacherAssignmentDueAt(AssignmentDto assignment)
    {
        if (!TryParseDashboardDate(assignment?.StartDate, out var startDate))
            return null;

        int duration = Mathf.Max(assignment.DurationDays, 1);
        return startDate.Date.AddDays(duration).AddSeconds(-1);
    }

    private bool TryParseDashboardDate(string raw, out DateTime parsed)
    {
        parsed = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var iso))
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

    // ---------------- ADD CLASS ----------------

    private void BindAddClassModal()
    {
        addClassBtn = root.Q<Button>("AddClassBtn");
        addClassModal = root.Q<VisualElement>("addClassModal");
        addClassModalCloseBtn = root.Q<Button>("AddClassModalCloseBtn");
        addClassCancelBtn = root.Q<Button>("AddClassCancelBtn");
        saveClassBtn = root.Q<Button>("saveClassBtn");

        classNameInput = root.Q<TextField>("classNameInput");
        lessonInput = root.Q<TextField>("lessonInput");
        gradeLevelDropdown = root.Q<DropdownField>("gradeLevelDropdown");

        SetAddClassModalOpen(false);

        if (addClassBtn == null)
            Debug.LogError("[TeacherDashboardController] AddClassBtn not found (name=\"AddClassBtn\").");
        else
        {
            addClassBtn.clicked -= OnAddClassClicked;
            addClassBtn.clicked += OnAddClassClicked;
        }

        if (addClassModalCloseBtn != null)
        {
            addClassModalCloseBtn.clicked -= OnAddClassModalCloseClicked;
            addClassModalCloseBtn.clicked += OnAddClassModalCloseClicked;
        }

        if (addClassCancelBtn != null)
        {
            addClassCancelBtn.clicked -= OnAddClassModalCloseClicked;
            addClassCancelBtn.clicked += OnAddClassModalCloseClicked;
        }

        if (saveClassBtn != null)
        {
            saveClassBtn.clicked -= OnSaveClassClicked;
            saveClassBtn.clicked += OnSaveClassClicked;
        }

        if (gradeLevelDropdown != null)
        {
            gradeLevelDropdown.choices = new List<string>
    {
        "5",
        "6",
        "7",
        "8",
        "9",
        "10",
        "11",
        "12",
        "Üniversite",
        "Diğer"
    };

            gradeLevelDropdown.index = -1;
        }

        if (addClassModal != null && !modalBackdropBound)
        {
            modalBackdropBound = true;
            addClassModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == addClassModal)
                    SetAddClassModalOpen(false);
            });
        }
    }

    private void OnAddClassClicked()
    {
        if (classNameInput != null) classNameInput.value = "";
        if (lessonInput != null) lessonInput.value = "";
        if (gradeLevelDropdown != null) gradeLevelDropdown.index = -1;
        SetAddClassModalOpen(true);
    }

    private void OnAddClassModalCloseClicked()
    {
        SetAddClassModalOpen(false);
    }

    private void OnSaveClassClicked()
    {
        string className = classNameInput != null ? (classNameInput.value ?? "").Trim() : "";
        string lessonName = lessonInput != null ? (lessonInput.value ?? "").Trim() : "";
        string gradeLevel = gradeLevelDropdown != null ? (gradeLevelDropdown.value ?? "").Trim() : "";

        if (string.IsNullOrWhiteSpace(className))
        {
            Debug.LogWarning("[ADD CLASS] Sınıf adı boş olamaz.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gradeLevel))
        {
            Debug.LogWarning("[ADD CLASS] Kademe / sınıf seviyesi seçmelisiniz.");
            return;
        }

        Debug.Log($"[ADD CLASS] name={className} lesson={lessonName}");
        StartCoroutine(CreateClass(className, gradeLevel, lessonName));
    }

    private void SetAddClassModalOpen(bool open)
    {
        if (addClassModal == null) return;

        if (open) addClassModal.AddToClassList("open");
        else addClassModal.RemoveFromClassList("open");
    }

    // ---------------- MENU ----------------

    private void BindMenuButtons()
    {
        root.Q<Button>("HomeBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("HomeBtn");
            ShowPage("HomePage");
            StartCoroutine(RefreshTeacherHomeDashboardData(forceRefresh: true));
        });

        root.Q<Button>("ClassBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("ClassBtn");
            ShowPage("ClassesPage");
            StartCoroutine(FetchMyClasses());
            StartCoroutine(FetchMyAssignments());
        });

        root.Q<Button>("AddAssignmentBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("AddAssignmentBtn");
            ShowPage("AddAssignmentPage");

            if (lastItems == null || lastItems.Length == 0)
                StartCoroutine(FetchMyClasses());

            StartCoroutine(FetchMyAssignments());
        });

        root.Q<Button>("StartSimulationBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("StartSimulationBtn");
            ShowPage("StartSimulationPage");
        });

        root.Q<Button>("CalendarBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("CalendarBtn");
            ShowPage("CalendarPage");
        });

        root.Q<Button>("EmailBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("EmailBtn");
            ShowPage("EmailPage");
        });

        root.Q<Button>("ActivityBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("ActivityBtn");
            ShowPage("ActivityPage");
            StartCoroutine(FetchPersonalActivity());
        });

        root.Q<Button>("ProfileBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("ProfileBtn");
            ShowPage("ProfilePage");
            StartCoroutine(LoadProfilePageData());
        });
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
        profileMailLabel = profilePage.Q<Label>("TeacherMailLabel");
        profileJoinDateLabel = profilePage.Q<Label>("TeacherJoinDateLabel");
        profileLastLoginLabel = profilePage.Q<Label>("TeacherLastLoginLabel");
        profileStatsGrid = profilePage.Q<VisualElement>(className: "teacher-stats-grid");

        teacherNewAssignmentBtn = profilePage.Q<Button>("TeacherNewAssignmentBtn");
        teacherGoClassesBtn = profilePage.Q<Button>("TeacherGoClassesBtn");
        teacherLogoutBtn = profilePage.Q<Button>("TeacherLogoutBtn");

        if (teacherNewAssignmentBtn != null)
        {
            teacherNewAssignmentBtn.clicked -= OnProfileNewAssignmentClicked;
            teacherNewAssignmentBtn.clicked += OnProfileNewAssignmentClicked;
        }

        if (teacherGoClassesBtn != null)
        {
            teacherGoClassesBtn.clicked -= OnProfileClassesClicked;
            teacherGoClassesBtn.clicked += OnProfileClassesClicked;
        }

        if (teacherLogoutBtn != null)
        {
            teacherLogoutBtn.clicked -= OnProfileLogoutClicked;
            teacherLogoutBtn.clicked += OnProfileLogoutClicked;
        }
    }

    private void OnProfileNewAssignmentClicked()
    {
        CloseClassDetailsOverlays();
        SetMenuActive("AddAssignmentBtn");
        ShowPage("AddAssignmentPage");

        if (lastItems == null || lastItems.Length == 0)
            StartCoroutine(FetchMyClasses());

        StartCoroutine(FetchMyAssignments());
    }

    private void OnProfileClassesClicked()
    {
        CloseClassDetailsOverlays();
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
                Debug.LogError($"[TEACHER PROFILE] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
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
                : (router?.CurrentRoleName ?? "Öğretmen");

        if (profileStatusLabel != null)
            profileStatusLabel.text = me != null && me.isActive ? "Aktif" : "Pasif";

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

        var classes = lastItems ?? Array.Empty<MyClassDto>();
        int totalClassCount = classes.Length;
        int activeClassCount = classes.Count(c => c != null && c.IsActive);
        int totalStudents = classes.Where(c => c != null).Sum(c => Mathf.Max(c.StudentCount, 0));
        int activeStudents = classes.Where(c => c != null && c.IsActive).Sum(c => Mathf.Max(c.StudentCount, 0));

        var assignments = assignmentItems ?? Array.Empty<AssignmentDto>();
        int givenAssignments = assignments.Length;

        int averageSuccess = 0;
        var successSource = classes.Where(c => c != null).ToArray();
        if (successSource.Length > 0)
            averageSuccess = Mathf.RoundToInt((float)successSource.Average(c => Mathf.Max(c.SuccessRatePercent, 0)));

        int streakDays = Mathf.Max(me?.currentActiveStreakDays ?? 0, 0);
        int totalActiveDays = Mathf.Max(me?.totalActiveDays ?? 0, 0);
        float totalActiveHours = Mathf.Max(me?.totalActiveHours ?? 0f, 0f);

        profileStatsGrid.Add(BuildProfileStatCard(activeClassCount.ToString(), "Aktif Sınıf", totalClassCount > 0 ? $"Toplam: {totalClassCount}" : "Sınıf yok", true));
        profileStatsGrid.Add(BuildProfileStatCard(totalClassCount.ToString(), "Toplam Sınıf", "Canlı sınıf verisi"));
        profileStatsGrid.Add(BuildProfileStatCard(activeStudents.ToString(), "Aktif Öğrenci", totalStudents > 0 ? $"Toplam: {totalStudents}" : "Öğrenci yok", true));
        profileStatsGrid.Add(BuildProfileStatCard(totalStudents.ToString(), "Toplam Öğrenci", "Canlı öğrenci verisi"));
        profileStatsGrid.Add(BuildProfileStatCard(givenAssignments.ToString(), "Verilen Ödev", "Canlı ödev verisi", true));
        profileStatsGrid.Add(BuildProfileStatCard($"%{averageSuccess}", "Ortalama Öğrenci Başarısı", averageSuccess >= 70 ? "İyi gidiyor" : "Geliştirilebilir"));
        profileStatsGrid.Add(BuildProfileStatCard(streakDays.ToString(), "Aktif Gün Serisi", "Üst üste giriş yapılan gün", true));
        profileStatsGrid.Add(BuildProfileStatCard(totalActiveDays.ToString(), "Toplam Aktif Gün", $"Toplam süre: {totalActiveHours:0.0} saat"));
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

        var tr = new CultureInfo("tr-TR");
        if (DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var iso))
            return iso.ToLocalTime().ToString("dd MMMM yyyy", tr);

        if (DateTime.TryParse(raw, out var dt))
            return dt.ToString("dd MMMM yyyy", tr);

        return "-";
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

    private void ShowPage(string pageName)
    {
        foreach (var child in mainContent.Children())
            child.RemoveFromClassList("active");

        var page = mainContent.Q<VisualElement>(pageName);
        if (page == null)
        {
            Debug.LogError($"[TeacherDashboardController] Page not found: {pageName}");
            return;
        }

        page.AddToClassList("active");

        if (pageName == "CalendarPage")
            RenderCalendar();
    }

    private void SetMenuActive(string activeButtonName)
    {
        var names = new[] { "HomeBtn", "ClassBtn", "AddAssignmentBtn", "StartSimulationBtn", "CalendarBtn", "EmailBtn", "ActivityBtn", "ProfileBtn" };

        foreach (var n in names)
            root.Q<Button>(n)?.RemoveFromClassList("active");

        root.Q<Button>(activeButtonName)?.AddToClassList("active");
    }

    // ---------------- FILTERS ----------------

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

    private void ApplyFiltersAndRender()
    {
        if (lastItems == null)
        {
            RenderClasses(null);
            return;
        }

        var filtered = lastItems;

        if (!includeInactive)
        {
            filtered = Array.FindAll(filtered, c => c != null && c.IsActive);
        }

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

    // ---------------- CLASSES API ----------------

    private IEnumerator FetchMyClasses()
    {
        if (router == null) yield break;
        if (classesRows == null)
        {
            Debug.LogError("[TeacherDashboardController] ClassesRows not found (name=\"ClassesRows\").");
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

        int totalStudents = 0;
        int activeStudents = 0;

        if (items != null)
        {
            foreach (var c in items)
            {
                if (c == null) continue;

                totalStudents += c.StudentCount;

                if (c.IsActive)
                    activeStudents += c.StudentCount;
            }
        }

        if (activeStudentCountLabel != null) activeStudentCountLabel.text = activeStudents.ToString();
        if (totalStudentCountLabel != null) totalStudentCountLabel.text = totalStudents.ToString();

        RefreshClassStatisticsCards();
        RefreshClassDetailsGeneralMetrics();

        ApplyTeacherHomeDashboardMetrics();

        RefreshAssignmentLessonDropdown();
        RefreshAssignmentClassDropdown();
        ApplyFiltersAndRender();
    }

    private void RefreshClassStatisticsCards()
    {
        var classes = (lastItems ?? Array.Empty<MyClassDto>()).Where(c => c != null).ToArray();
        var assignments = (assignmentItems ?? Array.Empty<AssignmentDto>()).Where(a => a != null).ToArray();

        int averageSuccess = classes.Length > 0
            ? Mathf.RoundToInt((float)classes.Average(c => Mathf.Clamp(c.SuccessRatePercent, 0, 100)))
            : 0;

        if (classSuccessRateLabel != null)
            classSuccessRateLabel.text = $"%{averageSuccess}";

        var topClass = classes
            .OrderByDescending(c => Mathf.Clamp(c.SuccessRatePercent, 0, 100))
            .ThenByDescending(c => Mathf.Max(c.StudentCount, 0))
            .FirstOrDefault();

        if (topClassNameLabel != null)
            topClassNameLabel.text = topClass != null ? SafeText(topClass.Name) : "-";

        int completedAssignments = assignments.Count(a => string.Equals(GetTeacherAssignmentStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
        int totalAssignments = assignments.Length;
        int incompleteAssignments = Mathf.Max(totalAssignments - completedAssignments, 0);

        int completionRate = totalAssignments > 0
            ? Mathf.RoundToInt((completedAssignments / (float)totalAssignments) * 100f)
            : 0;

        if (latestAssignmentCompletionRateLabel != null)
            latestAssignmentCompletionRateLabel.text = $"%{completionRate}";

        if (latestAssignmentDeliverySplitLabel != null)
            latestAssignmentDeliverySplitLabel.text = $"{completedAssignments} / {incompleteAssignments}";
    }

    private IEnumerator CreateClass(string className, string gradeLevel, string lessonName)
    {
        if (router == null) yield break;

        string url = router.ApiBaseUrl + createClassPath;

        var payload = new CreateClassRequest
        {
            Name = className,
            GradeLevel = gradeLevel,
            LessonName = string.IsNullOrWhiteSpace(lessonName) ? null : lessonName
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
            Debug.LogError($"[CREATE CLASS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        Debug.Log("[CREATE CLASS] OK => " + (req.downloadHandler?.text ?? ""));

        SetAddClassModalOpen(false);
        StartCoroutine(FetchMyClasses());
    }

    // ---------------- ASSIGNMENTS API ----------------

    private IEnumerator FetchMyAssignments()
    {
        if (router == null) yield break;

        string url = router.ApiBaseUrl + myAssignmentsPath;

        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ASSIGNMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        Debug.Log("[ASSIGNMENTS] OK => " + raw);

        var wrapped = JsonUtility.FromJson<AssignmentListWrapper>("{\"items\":" + raw + "}");
        assignmentItems = wrapped != null ? wrapped.items : null;

        RefreshClassStatisticsCards();

        RenderAssignmentTimeline();

        if (currentSelectedClass != null && cdAssignmentCountLabel != null)
            cdAssignmentCountLabel.text = CountAssignmentsForClass(currentSelectedClass.Id).ToString();

        if (currentSelectedClass != null &&
            classDetailsAssignmentsContent != null &&
            classDetailsAssignmentsContent.resolvedStyle.display != DisplayStyle.None)
        {
            BuildAssignmentCards();
        }

        RefreshClassDetailsGeneralMetrics();

        ApplyTeacherHomeDashboardMetrics();
    }

    private IEnumerator CreateAssignment(string title, int classId, DateTime startDate, int durationDays, int experimentId, string className, string lessonName, string experimentName)
    {
        if (router == null) yield break;

        string url = router.ApiBaseUrl + createAssignmentPath;

        var payload = new CreateAssignmentRequest
        {
            Title = title,
            ClassId = classId,
            StartDate = startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
            DurationDays = durationDays,
            ExperimentId = experimentId
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
            Debug.LogError($"[CREATE ASSIGNMENT] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        Debug.Log("[CREATE ASSIGNMENT] OK => " + (req.downloadHandler?.text ?? ""));

        CloseAssignmentModal();
        StartCoroutine(FetchMyAssignments());
        StartCoroutine(FetchMyClasses());
    }

    // ---------------- RENDER CLASSES ----------------

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

        var badge = new Label(c.IsActive ? "Aktif" : "Pasif");
        badge.AddToClassList("badge");
        if (c.IsActive) badge.AddToClassList("active");

        statusCol.Add(badge);
        row.Add(statusCol);

        var actionsCol = new VisualElement();
        actionsCol.AddToClassList("col");
        actionsCol.AddToClassList("class-actions");

        var goBtn = new Button(() =>
        {
            OpenClassDetails(c);
        });
        goBtn.AddToClassList("go-class-btn");
        goBtn.text = "Git";

        actionsCol.Add(goBtn);
        row.Add(actionsCol);

        return row;
    }

    private VisualElement BuildColLabel(string classList, string text)
    {
        var col = new VisualElement();
        foreach (var cls in classList.Split(' '))
            if (!string.IsNullOrWhiteSpace(cls)) col.AddToClassList(cls);

        col.Add(new Label(text));
        return col;
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

    // ---------------- CLASS DETAILS PAGE ----------------

    private void BindClassDetailsPage()
    {
        classDetailsPage = root.Q<VisualElement>("ClassDetailsPage");

        cdClassNameLabel = root.Q<Label>("CdClassNameLabel");
        cdTeacherNameLabel = root.Q<Label>("CdTeacherNameLabel");
        cdStudentCountLabel = root.Q<Label>("CdStudentCountLabel");
        cdAssignmentCountLabel = root.Q<Label>("CdAssignmentCountLabel");
        cdSuccessRateLabel = root.Q<Label>("CdSuccessRateLabel");
        cdCreatedAtLabel = root.Q<Label>("CdCreatedDateLabel");
        cdClassCodeLabel = root.Q<Label>("CdClassCodeLabel");
        cdStatusLabel = root.Q<Label>("CdStatusLabel");

        copyClassCodeBtn = root.Q<Button>("CdCopyCodeBtn");
        toggleClassStatusBtn = root.Q<Button>("CdToggleStatusBtn");

        cdTabGeneralBtn = root.Q<Button>("cdTabGeneralBtn");
        cdTabStudentsBtn = root.Q<Button>("cdTabStudentsBtn");
        cdTabAssignmentsBtn = root.Q<Button>("cdTabAssignmentsBtn");
        cdTabActivityBtn = root.Q<Button>("cdTabActivityBtn");
        cdTabRequestsBtn = root.Q<Button>("cdTabRequestsBtn");

        classDetailsRows = root.Q<VisualElement>("ClassDetailsRows");
        classDetailsGeneralContent = root.Q<VisualElement>("ClassDetailsGeneralContent");
        classDetailsStudentsContent = root.Q<VisualElement>("ClassDetailsStudentsContent");
        classDetailsAssignmentsContent = root.Q<VisualElement>("ClassDetailsAssignmentsContent");
        classDetailsActivityContent = root.Q<VisualElement>("ClassDetailsActivityContent");
        classDetailsRequestsContent = root.Q<VisualElement>("ClassDetailsRequestsContent");
        cgCompletionPercentLabel = root.Q<Label>("CgCompletionPercentLabel");
        cgCompletionDoneLabel = root.Q<Label>("CgCompletionDoneLabel");
        cgCompletionRemainingLabel = root.Q<Label>("CgCompletionRemainingLabel");
        cgCompletionDonut = classDetailsGeneralContent != null
            ? classDetailsGeneralContent.Q<VisualElement>(className: "cg-donut")
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

        var studentsScroll = root.Q<ScrollView>("StudentsScrollView");
        studentsRows = studentsScroll != null ? studentsScroll.contentContainer : root.Q<VisualElement>("StudentsRows");
        assignmentsCardsRow = root.Q<VisualElement>("AssignmentsCardsRow");
        if (assignmentsCardsRow == null)
            assignmentsCardsRow = classDetailsAssignmentsContent != null
                ? classDetailsAssignmentsContent.Q<VisualElement>(className: "table-assignment-cards")
                : null;
        var activityScroll = root.Q<ScrollView>("ActivityFeedScroll");
        activityFeed = activityScroll != null ? activityScroll.contentContainer : root.Q<VisualElement>("ActivityFeed");

        var requestListScroll = root.Q<ScrollView>("RequestListScroll");
        requestList = requestListScroll != null ? requestListScroll.contentContainer : null;

        pendingRequestCountLabel = classDetailsRequestsContent != null
            ? classDetailsRequestsContent.Q<Label>(className: "request-count")
            : null;
        requestSearchInput = root.Q<TextField>("RequestSearchInput");

        assignmentFilterAllBtn = root.Q<Button>("AssignmentFilterAllBtn");
        assignmentFilterActiveBtn = root.Q<Button>("AssignmentFilterActiveBtn");
        assignmentFilterPassiveBtn = root.Q<Button>("AssignmentFilterPassiveBtn");
        assignmentFilterCompletedBtn = root.Q<Button>("AssignmentFilterCompletedBtn");
        assignmentFilterIncompleteBtn = root.Q<Button>("AssignmentFilterIncompleteBtn");
        assignmentSearchInput = root.Q<TextField>("AssignmentSearchInput");

        activityFilterAllBtn = root.Q<Button>("ActivityFilterAllBtn");
        activityFilterExperimentBtn = root.Q<Button>("ActivityFilterExperimentBtn");
        activityFilterParticipationBtn = root.Q<Button>("ActivityFilterParticipationBtn");
        activitySearchInput = root.Q<TextField>("ActivitySearchInput");

        studentFilePage = root.Q<VisualElement>("StudentFilePage");
        studentFileBackBtn = root.Q<Button>("StudentFileBackBtn");

        sfStudentNameLabel = root.Q<Label>("SfStudentNameLabel");
        sfStudentClassLabel = root.Q<Label>("SfStudentClassLabel");
        sfStudentAvatarLabel = root.Q<Label>("SfStudentAvatarLabel");
        sfStudentNoLabel = root.Q<Label>("SfStudentNoLabel");
        sfStudentJoinDateLabel = root.Q<Label>("SfStudentJoinDateLabel");
        sfStudentLastLoginLabel = root.Q<Label>("SfStudentLastLoginLabel");
        sfStudentEmailLabel = root.Q<Label>("SfStudentEmailLabel");
        sfStudentPerformancePercentLabel = root.Q<Label>("SfStudentPerformancePercentLabel");
        sfStudentCompletedAssignmentsLabel = root.Q<Label>("SfStudentCompletedAssignmentsLabel");
        sfStudentCompletedExperimentsLabel = root.Q<Label>("SfStudentCompletedExperimentsLabel");
        sfStudentParticipationLabel = root.Q<Label>("SfStudentParticipationLabel");
        sfStudentStreakLabel = root.Q<Label>("SfStudentStreakLabel");

        sfAssignmentsHistoryScroll = root.Q<ScrollView>("SfAssignmentsHistoryScroll");
        sfExperimentsHistoryScroll = root.Q<ScrollView>("SfExperimentsHistoryScroll");

        if (copyClassCodeBtn != null)
        {
            copyClassCodeBtn.clicked -= CopySelectedClassCode;
            copyClassCodeBtn.clicked += CopySelectedClassCode;
        }

        if (toggleClassStatusBtn != null)
        {
            toggleClassStatusBtn.clicked -= ToggleSelectedClassStatus;
            toggleClassStatusBtn.clicked += ToggleSelectedClassStatus;
        }

        if (cdTabGeneralBtn != null)
        {
            cdTabGeneralBtn.clicked -= OnGeneralTabClicked;
            cdTabGeneralBtn.clicked += OnGeneralTabClicked;
        }

        if (cdTabStudentsBtn != null)
        {
            cdTabStudentsBtn.clicked -= OnStudentsTabClicked;
            cdTabStudentsBtn.clicked += OnStudentsTabClicked;
        }

        if (cdTabAssignmentsBtn != null)
        {
            cdTabAssignmentsBtn.clicked -= OnAssignmentsTabClicked;
            cdTabAssignmentsBtn.clicked += OnAssignmentsTabClicked;
        }

        if (cdTabActivityBtn != null)
        {
            cdTabActivityBtn.clicked -= OnActivityTabClicked;
            cdTabActivityBtn.clicked += OnActivityTabClicked;
        }

        if (cdTabRequestsBtn != null)
        {
            cdTabRequestsBtn.clicked -= OnRequestsTabClicked;
            cdTabRequestsBtn.clicked += OnRequestsTabClicked;
        }

        if (studentFileBackBtn != null)
        {
            studentFileBackBtn.clicked -= CloseStudentFile;
            studentFileBackBtn.clicked += CloseStudentFile;
        }

        if (requestSearchInput != null)
        {
            requestSearchInput.RegisterValueChangedCallback(evt =>
            {
                assignmentSearchQuery = assignmentSearchQuery ?? "";
                activitySearchQuery = activitySearchQuery ?? "";
                BuildRequestList();
            });
        }

        BindAssignmentFilters();
        BindActivityFilters();
    }

    private void BindAssignmentFilters()
    {
        if (assignmentFilterAllBtn != null)
            assignmentFilterAllBtn.clicked += () => { assignmentFilterMode = "all"; SetAssignmentFilterActive(assignmentFilterAllBtn); BuildAssignmentCards(); };
        if (assignmentFilterActiveBtn != null)
            assignmentFilterActiveBtn.clicked += () => { assignmentFilterMode = "active"; SetAssignmentFilterActive(assignmentFilterActiveBtn); BuildAssignmentCards(); };
        if (assignmentFilterPassiveBtn != null)
            assignmentFilterPassiveBtn.clicked += () => { assignmentFilterMode = "passive"; SetAssignmentFilterActive(assignmentFilterPassiveBtn); BuildAssignmentCards(); };
        if (assignmentFilterCompletedBtn != null)
            assignmentFilterCompletedBtn.clicked += () => { assignmentFilterMode = "completed"; SetAssignmentFilterActive(assignmentFilterCompletedBtn); BuildAssignmentCards(); };
        if (assignmentFilterIncompleteBtn != null)
            assignmentFilterIncompleteBtn.clicked += () => { assignmentFilterMode = "incomplete"; SetAssignmentFilterActive(assignmentFilterIncompleteBtn); BuildAssignmentCards(); };

        SetAssignmentFilterActive(assignmentFilterAllBtn);

        if (assignmentSearchInput != null)
        {
            assignmentSearchInput.RegisterValueChangedCallback(evt =>
            {
                assignmentSearchQuery = evt.newValue ?? "";
                BuildAssignmentCards();
            });
        }
    }

    private void BindActivityFilters()
    {
        if (activityFilterAllBtn != null)
            activityFilterAllBtn.clicked += () => { activityFilterMode = "all"; SetClassActivityFilterActive(activityFilterAllBtn); BuildActivityFeed(); };
        if (activityFilterExperimentBtn != null)
            activityFilterExperimentBtn.clicked += () => { activityFilterMode = "experiment"; SetClassActivityFilterActive(activityFilterExperimentBtn); BuildActivityFeed(); };
        if (activityFilterParticipationBtn != null)
            activityFilterParticipationBtn.clicked += () => { activityFilterMode = "participation"; SetClassActivityFilterActive(activityFilterParticipationBtn); BuildActivityFeed(); };

        SetClassActivityFilterActive(activityFilterAllBtn);

        if (activitySearchInput != null)
        {
            activitySearchInput.RegisterValueChangedCallback(evt =>
            {
                activitySearchQuery = evt.newValue ?? "";
                BuildActivityFeed();
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

    private void SetClassActivityFilterActive(Button activeButton)
    {
        activityFilterAllBtn?.RemoveFromClassList("active");
        activityFilterExperimentBtn?.RemoveFromClassList("active");
        activityFilterParticipationBtn?.RemoveFromClassList("active");
        activeButton?.AddToClassList("active");
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
            Debug.LogError($"[PERSONAL ACTIVITY] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
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
            var empty = new Label("Kişisel aktivite bulunmuyor.");
            empty.AddToClassList("request-meta");
            personalActivityFeed.Add(empty);
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

            string key = dt.ToString("dd MMMM yyyy", trCulture);
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
        {
            var empty = new Label("Filtreye uygun kişisel aktivite bulunamadı.");
            empty.AddToClassList("request-meta");
            personalActivityFeed.Add(empty);
        }
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

        var time = new Label(occurredAt.ToString("HH:mm", trCulture));
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
        if (DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var iso))
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
            return parts[0].Substring(0, 1).ToUpper(trCulture);

        string first = parts[0].Substring(0, 1).ToUpper(trCulture);
        string second = parts[parts.Length - 1].Substring(0, 1).ToUpper(trCulture);
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

    private IEnumerator OpenStudentFile(ClassStudentDto student)
    {
        if (student == null)
            yield break;

        selectedStudentId = student.UserId;
        string studentName = $"{student.Name} {student.Surname}".Trim();
        if (string.IsNullOrWhiteSpace(studentName))
            studentName = "-";

        string schoolNo = student.UserId.ToString();
        string initials = BuildInitials(student.Name, student.Surname);

        SetDisplay(classDetailsPage, false);
        SetDisplay(studentFilePage, true);

        if (sfStudentNameLabel != null)
            sfStudentNameLabel.text = studentName;

        if (sfStudentClassLabel != null)
            sfStudentClassLabel.text = currentSelectedClass != null
                ? $"{currentSelectedClass.Name} {currentSelectedClass.LessonName}"
                : "-";

        if (sfStudentAvatarLabel != null)
            sfStudentAvatarLabel.text = initials;

        if (sfStudentNoLabel != null)
            sfStudentNoLabel.text = schoolNo;

        if (sfStudentJoinDateLabel != null)
            sfStudentJoinDateLabel.text = FormatDate(student.JoinedAt);

        if (sfStudentLastLoginLabel != null)
            sfStudentLastLoginLabel.text = string.Empty;

        if (sfStudentEmailLabel != null)
            sfStudentEmailLabel.text = string.IsNullOrWhiteSpace(student.Email) ? "-" : student.Email;

        if (sfStudentPerformancePercentLabel != null)
            sfStudentPerformancePercentLabel.text = string.Empty;

        if (sfStudentCompletedAssignmentsLabel != null)
            sfStudentCompletedAssignmentsLabel.text = string.Empty;

        if (sfStudentCompletedExperimentsLabel != null)
            sfStudentCompletedExperimentsLabel.text = string.Empty;

        if (sfStudentParticipationLabel != null)
            sfStudentParticipationLabel.text = string.Empty;

        if (sfStudentStreakLabel != null)
            sfStudentStreakLabel.text = string.Empty;

        BuildStudentFileAssignmentHistory(Array.Empty<StudentProfileHistoryItemDto>());
        BuildStudentFileExperimentHistory(Array.Empty<StudentProfileHistoryItemDto>());

        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildClassStudentProfileUrl(currentSelectedClass.Id, student.UserId);
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[STUDENT PROFILE] FETCH FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        var profile = JsonUtility.FromJson<StudentProfileDto>(raw);
        ApplyStudentProfile(profile, student);
    }

    private void CloseStudentFile()
    {
        SetDisplay(studentFilePage, false);
        SetDisplay(classDetailsPage, true);
        SetClassDetailsTab("students");
    }

    private void ApplyStudentProfile(StudentProfileDto profile, ClassStudentDto fallback)
    {
        if (profile == null)
            return;

        string fullName = $"{profile.Name} {profile.Surname}".Trim();
        if (string.IsNullOrWhiteSpace(fullName) && fallback != null)
            fullName = $"{fallback.Name} {fallback.Surname}".Trim();

        if (sfStudentNameLabel != null)
            sfStudentNameLabel.text = string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;

        if (sfStudentAvatarLabel != null)
            sfStudentAvatarLabel.text = BuildInitialsFromName(fullName);

        if (sfStudentNoLabel != null)
            sfStudentNoLabel.text = profile.StudentId > 0
                ? profile.StudentId.ToString()
                : (fallback != null ? fallback.UserId.ToString() : "-");

        if (sfStudentJoinDateLabel != null)
            sfStudentJoinDateLabel.text = FormatDate(profile.JoinedAt);

        if (sfStudentLastLoginLabel != null)
            sfStudentLastLoginLabel.text = FormatDate(profile.LastLogin);

        if (sfStudentEmailLabel != null)
            sfStudentEmailLabel.text = !string.IsNullOrWhiteSpace(profile.Email)
                ? profile.Email
                : (fallback != null ? fallback.Email : "-");

        if (sfStudentPerformancePercentLabel != null)
            sfStudentPerformancePercentLabel.text = $"%{Mathf.Clamp(profile.PerformancePercent, 0, 100)}";

        if (sfStudentCompletedAssignmentsLabel != null)
            sfStudentCompletedAssignmentsLabel.text = $"{Mathf.Max(profile.CompletedAssignments, 0)} / {Mathf.Max(profile.TotalAssignments, 0)}";

        if (sfStudentCompletedExperimentsLabel != null)
            sfStudentCompletedExperimentsLabel.text = Mathf.Max(profile.CompletedExperiments, 0).ToString();

        if (sfStudentParticipationLabel != null)
            sfStudentParticipationLabel.text = string.IsNullOrWhiteSpace(profile.ParticipationLevel) ? "-" : profile.ParticipationLevel;

        if (sfStudentStreakLabel != null)
            sfStudentStreakLabel.text = Mathf.Max(profile.CurrentStreakDays, 0).ToString();

        BuildStudentFileAssignmentHistory(profile.AssignmentHistory ?? Array.Empty<StudentProfileHistoryItemDto>());
        BuildStudentFileExperimentHistory(profile.ExperimentHistory ?? Array.Empty<StudentProfileHistoryItemDto>());
    }

    private void BuildStudentFileAssignmentHistory(StudentProfileHistoryItemDto[] historyItems)
    {
        if (sfAssignmentsHistoryScroll == null) return;

        var content = sfAssignmentsHistoryScroll.contentContainer;
        content.Clear();

        var items = historyItems ?? Array.Empty<StudentProfileHistoryItemDto>();
        if (items.Length == 0)
        {
            content.Add(BuildStudentHistoryItem("Kayıt bulunamadı", "-", "-"));
            return;
        }

        foreach (var item in items)
            content.Add(BuildStudentHistoryItem(item?.Title ?? "-", item?.Value ?? "-", FormatDate(item?.Date)));
    }

    private void BuildStudentFileExperimentHistory(StudentProfileHistoryItemDto[] historyItems)
    {
        if (sfExperimentsHistoryScroll == null) return;

        var content = sfExperimentsHistoryScroll.contentContainer;
        content.Clear();

        var items = historyItems ?? Array.Empty<StudentProfileHistoryItemDto>();
        if (items.Length == 0)
        {
            content.Add(BuildStudentHistoryItem("Kayıt bulunamadı", "-", "-"));
            return;
        }

        foreach (var item in items)
            content.Add(BuildStudentHistoryItem(item?.Title ?? "-", item?.Value ?? "-", FormatDate(item?.Date)));
    }

    private VisualElement BuildStudentHistoryItem(string title, string score, string date)
    {
        var item = new VisualElement();
        item.AddToClassList("history-item");

        var top = new VisualElement();
        top.AddToClassList("history-item-top");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("history-item-title");

        var group = new VisualElement();
        group.AddToClassList("history-item-group");

        var scoreLabel = new Label(score);
        scoreLabel.AddToClassList("history-item-score");

        var pdfBtn = new Button();
        pdfBtn.text = "PDF";
        pdfBtn.AddToClassList("history-item-pdf");

        group.Add(scoreLabel);
        group.Add(pdfBtn);

        top.Add(titleLabel);
        top.Add(group);

        var bottom = new Label(date);
        bottom.AddToClassList("history-item-bottom");

        item.Add(top);
        item.Add(bottom);

        return item;
    }

    private void OpenClassDetails(MyClassDto item)
    {
        if (item == null) return;

        currentSelectedClass = item;

        SetDisplay(studentFilePage, false);
        SetDisplay(classDetailsPage, true);

        ShowPage("ClassDetailsPage");
        SetMenuActive("ClassBtn");

        if (cdClassNameLabel != null)
        {
            string lessonText = string.IsNullOrWhiteSpace(item.LessonName) ? "" : (" " + item.LessonName.ToUpper(trCulture));
            cdClassNameLabel.text = (item.Name ?? "-").ToUpper(trCulture) + lessonText;
        }

        if (cdTeacherNameLabel != null)
            cdTeacherNameLabel.text = $"{router.CurrentName} {router.CurrentSurname}".Trim();

        if (cdStudentCountLabel != null)
            cdStudentCountLabel.text = item.StudentCount.ToString();

        if (cdAssignmentCountLabel != null)
            cdAssignmentCountLabel.text = assignmentItems != null ? CountAssignmentsForClass(item.Id).ToString() : "0";

        if (cdSuccessRateLabel != null)
            cdSuccessRateLabel.text = $"%{Mathf.Max(item.SuccessRatePercent, 0)}";

        if (cdCreatedAtLabel != null)
            cdCreatedAtLabel.text = FormatDate(item.JoinedAt);

        if (cdClassCodeLabel != null)
            cdClassCodeLabel.text = string.IsNullOrWhiteSpace(item.Code) ? "-" : item.Code;

        if (cdStatusLabel != null)
            cdStatusLabel.text = item.IsActive ? "Aktif" : "Pasif";

        if (toggleClassStatusBtn != null)
            toggleClassStatusBtn.text = item.IsActive ? "Pasif Duruma Al" : "Aktif Duruma Al";

        SetClassDetailsTab("general");
    }

    private int CountAssignmentsForClass(int classId)
    {
        if (assignmentItems == null || assignmentItems.Length == 0) return 0;

        int count = 0;
        foreach (var a in assignmentItems)
        {
            if (a != null && a.ClassId == classId && a.IsActive)
                count++;
        }

        return count;
    }

    private void OnGeneralTabClicked() => SetClassDetailsTab("general");
    private void OnStudentsTabClicked() => SetClassDetailsTab("students");
    private void OnAssignmentsTabClicked() => SetClassDetailsTab("assignments");
    private void OnActivityTabClicked() => SetClassDetailsTab("activity");
    private void OnRequestsTabClicked() => SetClassDetailsTab("requests");

    private void SetClassDetailsTab(string tabName)
    {

        SetDisplay(studentFilePage, false);
        SetDisplay(classDetailsPage, true);

        RemoveTabActive(cdTabGeneralBtn);
        RemoveTabActive(cdTabStudentsBtn);
        RemoveTabActive(cdTabAssignmentsBtn);
        RemoveTabActive(cdTabActivityBtn);
        RemoveTabActive(cdTabRequestsBtn);

        SetDisplay(classDetailsGeneralContent, false);
        SetDisplay(classDetailsStudentsContent, false);
        SetDisplay(classDetailsAssignmentsContent, false);
        SetDisplay(classDetailsActivityContent, false);
        SetDisplay(classDetailsRequestsContent, false);
        SetDisplay(studentFilePage, false);

        switch (tabName)
        {
            case "general":
                AddTabActive(cdTabGeneralBtn);
                SetDisplay(classDetailsGeneralContent, true);
                BuildGeneralRows();
                StartCoroutine(FetchClassActivity());
                break;

            case "students":
                AddTabActive(cdTabStudentsBtn);
                SetDisplay(classDetailsStudentsContent, true);
                StartCoroutine(FetchClassStudents());
                break;

            case "assignments":
                AddTabActive(cdTabAssignmentsBtn);
                SetDisplay(classDetailsAssignmentsContent, true);
                StartCoroutine(FetchMyAssignments());
                BuildAssignmentCards();
                break;

            case "activity":
                AddTabActive(cdTabActivityBtn);
                SetDisplay(classDetailsActivityContent, true);
                StartCoroutine(FetchClassActivity());
                break;

            case "requests":
                AddTabActive(cdTabRequestsBtn);
                SetDisplay(classDetailsRequestsContent, true);
                StartCoroutine(FetchJoinRequests());
                break;
        }
    }

    private void CloseClassDetailsOverlays()
    {
        SetDisplay(studentFilePage, false);
        SetDisplay(classDetailsPage, false);
    }

    private void SetDisplay(VisualElement el, bool show)
    {
        if (el == null) return;
        el.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void AddTabActive(Button btn)
    {
        if (btn == null) return;

        if (!btn.ClassListContains("active-tab"))
            btn.AddToClassList("active-tab");

        if (!btn.ClassListContains("active"))
            btn.AddToClassList("active");
    }

    private void RemoveTabActive(Button btn)
    {
        if (btn == null) return;

        if (btn.ClassListContains("active-tab"))
            btn.RemoveFromClassList("active-tab");

        if (btn.ClassListContains("active"))
            btn.RemoveFromClassList("active");
    }

    private void CopySelectedClassCode()
    {
        if (cdClassCodeLabel == null) return;

        GUIUtility.systemCopyBuffer = cdClassCodeLabel.text ?? "";
        Debug.Log("Sınıf kodu kopyalandı: " + (cdClassCodeLabel.text ?? ""));
    }

    private void ToggleSelectedClassStatus()
    {
        if (currentSelectedClass == null) return;

        StartCoroutine(UpdateClassStatus(currentSelectedClass.Id, !currentSelectedClass.IsActive));
    }

    private IEnumerator UpdateClassStatus(int classId, bool isActive)
    {
        if (router == null)
            yield break;

        string url = BuildClassStatusUrl(classId);
        var payload = new UpdateClassStatusRequest { IsActive = isActive };
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
            Debug.LogError($"[CLASS STATUS] UPDATE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        currentSelectedClass.IsActive = isActive;

        if (cdStatusLabel != null)
            cdStatusLabel.text = currentSelectedClass.IsActive ? "Aktif" : "Pasif";

        if (toggleClassStatusBtn != null)
            toggleClassStatusBtn.text = currentSelectedClass.IsActive ? "Pasif Duruma Al" : "Aktif Duruma Al";

        StartCoroutine(FetchMyClasses());
    }

    private void BuildGeneralRows()
    {
        RefreshClassDetailsGeneralMetrics();
    }

    private void RefreshClassDetailsGeneralMetrics()
    {
        int classId = currentSelectedClass != null ? currentSelectedClass.Id : 0;
        var classAssignments = (assignmentItems ?? Array.Empty<AssignmentDto>())
            .Where(a => a != null && a.ClassId == classId)
            .ToArray();

        int totalAssignments = classAssignments.Length;
        int completedAssignments = classAssignments.Count(a => string.Equals(GetTeacherAssignmentStatus(a), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
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
                if (!TryParseDashboardDate(assignment?.StartDate, out var dt))
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

    private void BuildStudentRows()
    {
        if (classDetailsRows == null) return;
        classDetailsRows.Clear();

        int count = currentSelectedClass != null ? Mathf.Max(1, currentSelectedClass.StudentCount) : 1;
        int renderCount = Mathf.Min(count, 10);

        for (int i = 1; i <= renderCount; i++)
        {
            AddInfoRow(
                i.ToString(),
                "Öğrenci " + i,
                "%" + UnityEngine.Random.Range(60, 100),
                UnityEngine.Random.Range(1, 8) + " gün önce",
                "Detay"
            );
        }
    }

    private void BuildAssignmentsRows()
    {
        if (classDetailsRows == null) return;
        classDetailsRows.Clear();

        if (currentSelectedClass == null)
        {
            AddInfoRow("1", "Sınıf seçilmedi", "-", "-", "Aç");
            return;
        }

        if (assignmentItems == null || assignmentItems.Length == 0)
        {
            AddInfoRow("1", "Henüz ödev bulunmuyor", "-", "-", "Aç");
            return;
        }

        int order = 1;
        foreach (var item in assignmentItems)
        {
            if (item == null) continue;
            if (item.ClassId != currentSelectedClass.Id) continue;

            AddInfoRow(
                order.ToString(),
                string.IsNullOrWhiteSpace(item.Title) ? "-" : item.Title,
                item.DurationDays + " gün",
                FormatDate(item.StartDate),
                "Aç"
            );

            order++;
        }

        if (order == 1)
            AddInfoRow("1", "Bu sınıfa ait ödev yok", "-", "-", "Aç");
    }

    private void BuildActivityRows()
    {
        if (classDetailsRows == null) return;
        classDetailsRows.Clear();

        AddInfoRow("1", "Sınıf görüntülendi", "1 kayıt", "Bugün", "İncele");
        AddInfoRow("2", "Öğrenci katılımı", "0 kayıt", "Dün", "İncele");
        AddInfoRow("3", "Ödev hareketleri", "0 kayıt", "Bu hafta", "İncele");
    }

    private void BuildRequestsRows()
    {
        if (classDetailsRows == null) return;
        classDetailsRows.Clear();

        AddInfoRow("1", "Bekleyen istek yok", "-", "-", "Yönet");
    }

    private void AddInfoRow(string no, string student, string success, string lastSign, string actionText)
    {
        if (classDetailsRows == null) return;

        var row = new VisualElement();
        row.AddToClassList("table-row");

        var noLabel = new Label(no);
        noLabel.AddToClassList("td");
        noLabel.AddToClassList("td-no");

        var studentLabel = new Label(student);
        studentLabel.AddToClassList("td");
        studentLabel.AddToClassList("td-student");

        var successLabel = new Label(success);
        successLabel.AddToClassList("td");
        successLabel.AddToClassList("td-success-rate");

        var lastSignLabel = new Label(lastSign);
        lastSignLabel.AddToClassList("td");
        lastSignLabel.AddToClassList("td-last-sign");

        var actionWrap = new VisualElement();
        actionWrap.AddToClassList("td");
        actionWrap.AddToClassList("td-action");

        var actionBtn = new Button();
        actionBtn.text = actionText;
        actionBtn.AddToClassList("row-action-btn");

        actionWrap.Add(actionBtn);

        row.Add(noLabel);
        row.Add(studentLabel);
        row.Add(successLabel);
        row.Add(lastSignLabel);
        row.Add(actionWrap);

        classDetailsRows.Add(row);
    }

    private string FormatDate(string rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
            return "-";

        if (DateTime.TryParse(rawDate, out DateTime dt))
            return dt.ToString("dd MMMM yyyy", trCulture);

        if (DateTime.TryParse(rawDate, null, DateTimeStyles.RoundtripKind, out DateTime isoDt))
            return isoDt.ToLocalTime().ToString("dd MMMM yyyy", trCulture);

        return rawDate;
    }

    private void BuildStudentRowsRich()
    {
        if (studentsRows == null) return;
        studentsRows.Clear();

        var items = currentStudentItems;
        if (items == null || items.Length == 0)
        {
            var empty = new Label("Bu sınıfta henüz onaylı öğrenci bulunmuyor.");
            empty.AddToClassList("request-meta");
            studentsRows.Add(empty);
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            var s = items[i];
            if (s == null) continue;

            string fullName = $"{s.Name} {s.Surname}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = "-";

            string initials = BuildInitials(s.Name, s.Surname);

            studentsRows.Add(BuildStudentRowCard(
                s,
                fullName,
                initials
            ));
        }
    }

    private VisualElement BuildStudentRowCard(ClassStudentDto student, string studentName, string initials)
    {
        var row = new VisualElement();
        row.AddToClassList("table-row");

        var studentCell = new VisualElement();
        studentCell.AddToClassList("td");
        studentCell.AddToClassList("td-student");
        studentCell.AddToClassList("student-cell");

        var avatar = new Label(initials);
        avatar.AddToClassList("student-avatar");

        var info = new VisualElement();
        info.AddToClassList("student-info");

        var name = new Label(studentName);
        name.AddToClassList("student-name");

        info.Add(name);
        studentCell.Add(avatar);
        studentCell.Add(info);

        var noCell = new Label(student != null ? student.UserId.ToString() : "-");
        noCell.AddToClassList("td");
        noCell.AddToClassList("td-no");

        var successCell = new Label("%0");
        successCell.AddToClassList("td");
        successCell.AddToClassList("td-success-rate");

        var lastSignCell = new Label(FormatDate(student != null ? student.JoinedAt : null));
        lastSignCell.AddToClassList("td");
        lastSignCell.AddToClassList("td-last-sign");

        var actionCell = new VisualElement();
        actionCell.AddToClassList("td");
        actionCell.AddToClassList("td-action");

        var profileBtn = new Button();
        profileBtn.text = "Profil";
        profileBtn.AddToClassList("btn-student-profile");

        profileBtn.clicked += () =>
        {
            StartCoroutine(OpenStudentFile(student));
        };

        var kickBtn = new Button();
        kickBtn.text = "Çıkart";
        kickBtn.AddToClassList("btn-kick");
        kickBtn.clicked += () =>
        {
            if (student != null)
                StartCoroutine(RemoveStudentFromClass(student.UserId));
        };

        actionCell.Add(profileBtn);
        actionCell.Add(kickBtn);

        row.Add(studentCell);
        row.Add(noCell);
        row.Add(successCell);
        row.Add(lastSignCell);
        row.Add(actionCell);

        return row;
    }

    private void BuildAssignmentCards()
    {
        if (assignmentsCardsRow == null) return;
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
                string cls = (a.ClassName ?? "").ToLowerInvariant();
                if (!title.Contains(q) && !cls.Contains(q))
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

        if (!DateTime.TryParse(assignment.StartDate, null, DateTimeStyles.RoundtripKind, out var parsedStart))
            return "0";

        var start = parsedStart.ToLocalTime().Date;
        int duration = assignment.DurationDays <= 0 ? 1 : assignment.DurationDays;
        var endExclusive = start.AddDays(duration);
        int remainingDays = (endExclusive - DateTime.Today).Days;
        return Mathf.Max(remainingDays, 0).ToString();
    }

    private void BuildActivityFeed()
    {
        if (activityFeed == null) return;
        activityFeed.Clear();

        var items = currentActivityItems;
        if (items == null || items.Length == 0)
        {
            var empty = new Label("Bu sınıf için henüz aktivite bulunmuyor.");
            empty.AddToClassList("request-meta");
            activityFeed.Add(empty);
            return;
        }

        string currentDateKey = null;
        string q = (activitySearchQuery ?? "").Trim().ToLowerInvariant();
        foreach (var item in items)
        {
            if (item == null) continue;

            bool typeMatches = activityFilterMode switch
            {
                "participation" => string.Equals(item.Type, "JoinApproved", StringComparison.OrdinalIgnoreCase),
                "experiment" => string.Equals(item.Type, "ClassCreated", StringComparison.OrdinalIgnoreCase),
                _ => true
            };

            if (!typeMatches)
                continue;

            if (!string.IsNullOrWhiteSpace(q))
            {
                string haystack = $"{item.Title} {item.Description} {item.ActorName}".ToLowerInvariant();
                if (!haystack.Contains(q))
                    continue;
            }

            string dateKey = FormatDate(item.OccurredAt);
            if (!string.Equals(currentDateKey, dateKey, StringComparison.Ordinal))
            {
                currentDateKey = dateKey;
                activityFeed.Add(BuildActivityDateDivider(dateKey));
            }

            string actor = string.IsNullOrWhiteSpace(item.ActorName) ? "Sistem" : item.ActorName;
            string initials = BuildInitials(actor, "");
            string timeText = FormatDateTime(item.OccurredAt);
            string badgeText = string.Equals(item.Type, "JoinApproved", StringComparison.OrdinalIgnoreCase)
                ? "Katılım"
                : "Sınıf";

            activityFeed.Add(BuildActivityItem(item, initials, actor, timeText, badgeText, item.Description ?? item.Title ?? "-"));
        }
    }

    private VisualElement BuildActivityDateDivider(string text)
    {
        var row = new VisualElement();
        row.AddToClassList("activity-date-divider");

        var label = new Label(text);
        label.AddToClassList("activity-date-divider-label");

        row.Add(label);
        return row;
    }

    private VisualElement BuildActivityItem(ClassActivityDto activity, string initials, string username, string time, string badgeText, string contentText)
    {
        var item = new VisualElement();
        item.AddToClassList("activity-item");

        var top = new VisualElement();
        top.AddToClassList("activity-item-top");

        var user = new VisualElement();
        user.AddToClassList("activity-user");

        var avatar = new Label(initials);
        avatar.AddToClassList("activity-avatar");

        var meta = new VisualElement();
        meta.AddToClassList("activity-user-meta");

        var name = new Label(username);
        name.AddToClassList("activity-username");

        var timeLabel = new Label(time);
        timeLabel.AddToClassList("activity-time");

        meta.Add(name);
        meta.Add(timeLabel);

        user.Add(avatar);
        user.Add(meta);

        var badge = new Label(badgeText);
        badge.AddToClassList("activity-badge");

        top.Add(user);
        top.Add(badge);

        var content = new Label(contentText);
        content.AddToClassList("activity-content");

        VisualElement commentsWrap = null;
        if (activity != null && activity.Comments != null && activity.Comments.Length > 0)
        {
            commentsWrap = new VisualElement();
            commentsWrap.AddToClassList("activity-comments");

            foreach (var c in activity.Comments)
            {
                if (c == null) continue;
                commentsWrap.Add(BuildActivityCommentItem(c));
            }
        }

        var actions = new VisualElement();
        actions.AddToClassList("activity-actions");

        string likeText = activity != null && activity.IsLikedByCurrentUser
            ? $"Beğenildi ({activity.LikesCount})"
            : $"Beğen ({(activity != null ? activity.LikesCount : 0)})";

        var likeBtn = new Button { text = likeText };
        likeBtn.AddToClassList("activity-action-btn");
        if (activity != null && activity.IsLikedByCurrentUser)
            likeBtn.AddToClassList("active");

        likeBtn.clicked += () =>
        {
            if (activity == null || string.IsNullOrWhiteSpace(activity.ActivityId)) return;
            StartCoroutine(ToggleActivityLike(activity));
        };

        var commentBtn = new Button { text = "Yorum Yap" };
        commentBtn.AddToClassList("activity-action-btn");

        var commentBox = new VisualElement();
        commentBox.AddToClassList("activity-comment-box");
        commentBox.style.display = DisplayStyle.None;

        var closeCommentBtn = new Button();
        closeCommentBtn.AddToClassList("comment-close-btn");
        closeCommentBtn.clicked += () =>
        {
            commentBox.style.display = DisplayStyle.None;
        };

        var commentInput = new TextField();
        commentInput.AddToClassList("activity-comment-input");

        var sendCommentBtn = new Button { text = "Gönder" };
        sendCommentBtn.AddToClassList("comment-send-btn");
        sendCommentBtn.clicked += () =>
        {
            if (activity == null || string.IsNullOrWhiteSpace(activity.ActivityId)) return;
            string text = (commentInput.value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            StartCoroutine(AddActivityComment(activity.ActivityId, text));
            commentInput.value = "";
            commentBox.style.display = DisplayStyle.None;
        };

        commentBox.Add(closeCommentBtn);
        commentBox.Add(commentInput);
        commentBox.Add(sendCommentBtn);

        commentBtn.clicked += () =>
        {
            if (activity == null || string.IsNullOrWhiteSpace(activity.ActivityId)) return;
            commentBox.style.display = commentBox.style.display == DisplayStyle.None
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        };

        var seeBtn = new Button { text = "Görüntüle" };
        seeBtn.AddToClassList("activity-action-btn");

        actions.Add(likeBtn);
        actions.Add(commentBtn);
        actions.Add(seeBtn);

        item.Add(top);
        item.Add(content);
        if (commentsWrap != null)
            item.Add(commentsWrap);
        item.Add(actions);
        item.Add(commentBox);

        return item;
    }

    private VisualElement BuildActivityCommentItem(ActivityCommentDto comment)
    {
        var item = new VisualElement();
        item.AddToClassList("activity-comment");

        var author = new Label(comment.UserName ?? "Öğretmen");
        author.AddToClassList("comment-author");

        var text = new Label(comment.Text ?? "-");
        text.AddToClassList("comment-text");

        item.Add(author);
        item.Add(text);
        return item;
    }

    private void BuildRequestList()
    {
        if (requestList == null) return;
        requestList.Clear();

        var items = currentRequestItems;
        string q = (requestSearchInput != null ? requestSearchInput.value : "") ?? "";
        string qLower = q.Trim().ToLowerInvariant();

        int count = 0;
        if (items != null)
        {
            foreach (var it in items)
            {
                if (it == null) continue;
                string haystack = $"{it.Name} {it.Surname} {it.Email}".ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(qLower) || haystack.Contains(qLower))
                    count++;
            }
        }

        if (pendingRequestCountLabel != null)
            pendingRequestCountLabel.text = count > 0 ? $"{count} Bekleyen İstek" : "Bekleyen istek yok";

        if (items == null || items.Length == 0)
        {
            var empty = new Label("Bu sınıf için bekleyen katılma isteği bulunmuyor.");
            empty.AddToClassList("request-meta");
            requestList.Add(empty);
            return;
        }

        foreach (var item in items)
        {
            if (item == null) continue;

            if (!string.IsNullOrWhiteSpace(qLower))
            {
                string haystack = $"{item.Name} {item.Surname} {item.Email}".ToLowerInvariant();
                if (!haystack.Contains(qLower))
                    continue;
            }

            string initials = BuildInitials(item.Name, item.Surname);
            string fullName = $"{item.Name} {item.Surname}".Trim();
            string requestedAt = FormatDate(item.RequestedAt);
            requestList.Add(BuildRequestItem(item, initials, fullName, requestedAt));
        }
    }

    private VisualElement BuildRequestItem(JoinRequestDto reqItem, string initials, string fullName, string requestedAt)
    {
        var item = new VisualElement();
        item.AddToClassList("request-item");

        var user = new VisualElement();
        user.AddToClassList("request-user");

        var avatar = new Label(initials);
        avatar.AddToClassList("request-avatar");

        var info = new VisualElement();
        info.AddToClassList("request-user-info");

        var nameRow = new VisualElement();
        nameRow.AddToClassList("request-name-row");

        var name = new Label(fullName);
        name.AddToClassList("request-name");

        var badge = new Label("Beklemede");
        badge.AddToClassList("request-badge");
        badge.AddToClassList("pending");

        nameRow.Add(name);
        nameRow.Add(badge);

        var meta = new Label($"E-posta: {reqItem.Email}   Talep: {requestedAt}");
        meta.AddToClassList("request-meta");

        info.Add(nameRow);
        info.Add(meta);

        user.Add(avatar);
        user.Add(info);

        var actions = new VisualElement();
        actions.AddToClassList("request-actions");

        var approveBtn = new Button { text = "Onayla" };
        approveBtn.AddToClassList("btn-approve");
        approveBtn.clicked += () =>
        {
            StartCoroutine(ApproveJoinRequest(reqItem.UserId));
        };

        var rejectBtn = new Button { text = "Reddet" };
        rejectBtn.AddToClassList("btn-reject");
        rejectBtn.clicked += () =>
        {
            StartCoroutine(RejectJoinRequest(reqItem.UserId));
        };

        actions.Add(approveBtn);
        actions.Add(rejectBtn);

        item.Add(user);
        item.Add(actions);

        return item;
    }

    private IEnumerator FetchJoinRequests()
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildJoinRequestsUrl(currentSelectedClass.Id);
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JOIN REQUESTS] FETCH FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            currentRequestItems = Array.Empty<JoinRequestDto>();
            BuildRequestList();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<JoinRequestListWrapper>("{\"items\":" + raw + "}");
        currentRequestItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<JoinRequestDto>();

        BuildRequestList();
    }

    private IEnumerator FetchClassStudents()
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildClassStudentsUrl(currentSelectedClass.Id);
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CLASS STUDENTS] FETCH FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            currentStudentItems = Array.Empty<ClassStudentDto>();
            BuildStudentRowsRich();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassStudentListWrapper>("{\"items\":" + raw + "}");
        currentStudentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ClassStudentDto>();

        if (cdStudentCountLabel != null)
            cdStudentCountLabel.text = currentStudentItems.Length.ToString();

        BuildStudentRowsRich();
    }

    private IEnumerator FetchClassActivity()
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildClassActivityUrl(currentSelectedClass.Id);
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CLASS ACTIVITY] FETCH FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            currentActivityItems = Array.Empty<ClassActivityDto>();
            BuildActivityFeed();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassActivityListWrapper>("{\"items\":" + raw + "}");
        currentActivityItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ClassActivityDto>();

        BuildActivityFeed();
        RefreshClassDetailsGeneralMetrics();
    }

    private IEnumerator RemoveStudentFromClass(int studentId)
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildClassStudentRemoveUrl(currentSelectedClass.Id, studentId);
        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CLASS STUDENTS] REMOVE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        StartCoroutine(FetchClassStudents());
        StartCoroutine(FetchMyClasses());
    }

    private IEnumerator ToggleActivityLike(ClassActivityDto activity)
    {
        if (router == null || currentSelectedClass == null || activity == null)
            yield break;

        string url = activity.IsLikedByCurrentUser
            ? BuildClassActivityUnlikeUrl(currentSelectedClass.Id, activity.ActivityId)
            : BuildClassActivityLikeUrl(currentSelectedClass.Id, activity.ActivityId);

        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CLASS ACTIVITY] LIKE TOGGLE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        StartCoroutine(FetchClassActivity());
    }

    private IEnumerator AddActivityComment(string activityId, string text)
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildClassActivityCommentUrl(currentSelectedClass.Id, activityId);
        var payload = JsonUtility.ToJson(new ActivityCommentRequest
        {
            Text = string.IsNullOrWhiteSpace(text) ? "Yorum" : text.Trim()
        });

        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CLASS ACTIVITY] COMMENT FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        StartCoroutine(FetchClassActivity());
    }

    private IEnumerator ApproveJoinRequest(int studentId)
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildJoinRequestsUrl(currentSelectedClass.Id) + "/" + studentId + "/approve";
        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JOIN REQUESTS] APPROVE FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        Debug.Log("[JOIN REQUESTS] APPROVED => " + (req.downloadHandler?.text ?? ""));
        StartCoroutine(FetchJoinRequests());
        StartCoroutine(FetchMyClasses());
    }

    private IEnumerator RejectJoinRequest(int studentId)
    {
        if (router == null || currentSelectedClass == null)
            yield break;

        string url = BuildJoinRequestsUrl(currentSelectedClass.Id) + "/" + studentId + "/reject";
        using var req = new UnityWebRequest(url, "POST");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[JOIN REQUESTS] REJECT FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        Debug.Log("[JOIN REQUESTS] REJECTED => " + (req.downloadHandler?.text ?? ""));
        StartCoroutine(FetchJoinRequests());
    }

    private string BuildJoinRequestsUrl(int classId)
    {
        string path = classJoinRequestsPathTemplate ?? "/api/Class/{classId}/join-requests";
        return router.ApiBaseUrl + path.Replace("{classId}", classId.ToString());
    }

    private string BuildClassStudentsUrl(int classId)
    {
        string path = classStudentsPathTemplate ?? "/api/Class/{classId}/students";
        return router.ApiBaseUrl + path.Replace("{classId}", classId.ToString());
    }

    private string BuildClassStudentProfileUrl(int classId, int studentId)
    {
        string path = classStudentProfilePathTemplate ?? "/api/Class/{classId}/students/{studentId}/profile";
        return router.ApiBaseUrl
            + path.Replace("{classId}", classId.ToString())
                  .Replace("{studentId}", studentId.ToString());
    }

    private string BuildClassStatusUrl(int classId)
    {
        string path = classStatusPathTemplate ?? "/api/Class/{classId}/status";
        return router.ApiBaseUrl + path.Replace("{classId}", classId.ToString());
    }

    private string BuildClassActivityUrl(int classId)
    {
        string path = classActivityPathTemplate ?? "/api/Class/{classId}/activity";
        return router.ApiBaseUrl + path.Replace("{classId}", classId.ToString());
    }

    private string BuildClassStudentRemoveUrl(int classId, int studentId)
    {
        string path = classStudentRemovePathTemplate ?? "/api/Class/{classId}/students/{studentId}/remove";
        return router.ApiBaseUrl
            + path.Replace("{classId}", classId.ToString())
                  .Replace("{studentId}", studentId.ToString());
    }

    private string BuildClassActivityLikeUrl(int classId, string activityId)
    {
        string path = classActivityLikePathTemplate ?? "/api/Class/{classId}/activity/{activityId}/like";
        return router.ApiBaseUrl
            + path.Replace("{classId}", classId.ToString())
                  .Replace("{activityId}", UnityWebRequest.EscapeURL(activityId ?? ""));
    }

    private string BuildClassActivityUnlikeUrl(int classId, string activityId)
    {
        string path = classActivityUnlikePathTemplate ?? "/api/Class/{classId}/activity/{activityId}/unlike";
        return router.ApiBaseUrl
            + path.Replace("{classId}", classId.ToString())
                  .Replace("{activityId}", UnityWebRequest.EscapeURL(activityId ?? ""));
    }

    private string BuildClassActivityCommentUrl(int classId, string activityId)
    {
        string path = classActivityCommentPathTemplate ?? "/api/Class/{classId}/activity/{activityId}/comments";
        return router.ApiBaseUrl
            + path.Replace("{classId}", classId.ToString())
                  .Replace("{activityId}", UnityWebRequest.EscapeURL(activityId ?? ""));
    }

    private string FormatDate(DateTime value)
    {
        return value.ToString("dd MMMM yyyy", trCulture);
    }

    private string FormatDateTime(DateTime value)
    {
        return value.ToString("dd MMMM yyyy HH:mm", trCulture);
    }

    private string FormatDateTime(string rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
            return "-";

        if (DateTime.TryParse(rawDate, out DateTime dt))
            return dt.ToString("dd MMMM yyyy HH:mm", trCulture);

        if (DateTime.TryParse(rawDate, null, DateTimeStyles.RoundtripKind, out DateTime isoDt))
            return isoDt.ToLocalTime().ToString("dd MMMM yyyy HH:mm", trCulture);

        return rawDate;
    }

    private string BuildInitials(string name, string surname)
    {
        string first = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim().Substring(0, 1).ToUpper(trCulture);
        string second = string.IsNullOrWhiteSpace(surname) ? "" : surname.Trim().Substring(0, 1).ToUpper(trCulture);
        return first + second;
    }

    // ---------------- ADD ASSIGNMENT PAGE ----------------

    private void BindAddAssignmentPage()
    {
        assignmentPastBtn = root.Q<Button>("AssignmentPastBtn");
        assignmentNextBtn = root.Q<Button>("AssignmentNextBtn");
        timelineDays = root.Q<VisualElement>("TimelineDays");
        timelineContents = root.Q<VisualElement>("TimelineContents");
        addAssignmentInfoLabel = root.Q<Label>("AddAssignmentInfoLabel");

        assignmentModal = root.Q<VisualElement>("AssignmentModal");
        closeAssignmentModalBtn = root.Q<Button>("CloseAssignmentModalBtn");
        cancelAssignmentBtn = root.Q<Button>("CancelAssignmentBtn");
        saveAssignmentBtn = root.Q<Button>("SaveAssignmentBtn");

        assignmentTitleField = root.Q<TextField>("AssignmentTitleField");
        assignmentClassDropdown = root.Q<DropdownField>("AssignmentClassDropdown");
        assignmentLessonDropdown = root.Q<DropdownField>("AssignmentLessonDropdown");
        assignmentStartField = root.Q<TextField>("AssignmentStartField");
        assignmentDurationField = root.Q<IntegerField>("AssignmentDurationField");
        assignmentUnitDropdown = root.Q<DropdownField>("AssignmentUnitDropdown");
        assignmentExperimentDropdown = root.Q<DropdownField>("AssignmentExperimentDropdown");

        assignmentDetailsModal = root.Q<VisualElement>("AssignmentDetailsModal");
        assignmentDetailsCloseBtn = root.Q<Button>("AssignmentDetailsCloseBtn");
        assignmentDetailsTitle = root.Q<Label>("AssignmentDetailsTitle");
        assignmentDetailsClass = root.Q<Label>("AssignmentDetailsClass");
        assignmentDetailsLesson = root.Q<Label>("AssignmentDetailsLesson");
        assignmentDetailsStart = root.Q<Label>("AssignmentDetailsStart");
        assignmentDetailsDuration = root.Q<Label>("AssignmentDetailsDuration");
        assignmentDetailsExperiment = root.Q<Label>("AssignmentDetailsExperiment");

        if (timelineDays == null || timelineContents == null || assignmentModal == null)
        {
            Debug.LogWarning("[TeacherDashboardController] AddAssignmentPage UI eksik.");
            return;
        }

        if (assignmentPastBtn != null)
        {
            assignmentPastBtn.clicked -= OnAssignmentPastClicked;
            assignmentPastBtn.clicked += OnAssignmentPastClicked;
        }

        if (assignmentNextBtn != null)
        {
            assignmentNextBtn.clicked -= OnAssignmentNextClicked;
            assignmentNextBtn.clicked += OnAssignmentNextClicked;
        }

        if (closeAssignmentModalBtn != null)
        {
            closeAssignmentModalBtn.clicked -= CloseAssignmentModal;
            closeAssignmentModalBtn.clicked += CloseAssignmentModal;
        }

        if (cancelAssignmentBtn != null)
        {
            cancelAssignmentBtn.clicked -= CloseAssignmentModal;
            cancelAssignmentBtn.clicked += CloseAssignmentModal;
        }

        if (saveAssignmentBtn != null)
        {
            saveAssignmentBtn.clicked -= SaveAssignmentToTimeline;
            saveAssignmentBtn.clicked += SaveAssignmentToTimeline;
        }

        if (assignmentClassDropdown != null)
        {
            assignmentClassDropdown.RegisterValueChangedCallback(evt =>
            {
                OnAssignmentClassChanged(evt.newValue);
            });
        }

        if (assignmentLessonDropdown != null)
        {
            assignmentLessonDropdown.RegisterValueChangedCallback(evt =>
            {
                OnAssignmentLessonChanged(evt.newValue);
            });
        }

        if (assignmentUnitDropdown != null)
        {
            assignmentUnitDropdown.RegisterValueChangedCallback(evt =>
            {
                OnAssignmentUnitChanged(evt.newValue);
            });
        }

        assignmentModal.AddToClassList("hidden");
        if (assignmentDetailsModal != null)
            assignmentDetailsModal.AddToClassList("hidden");

        if (assignmentDetailsCloseBtn != null)
        {
            assignmentDetailsCloseBtn.clicked -= CloseAssignmentDetailsModal;
            assignmentDetailsCloseBtn.clicked += CloseAssignmentDetailsModal;
        }

        RefreshAssignmentLessonDropdown();
        RefreshAssignmentClassDropdown();
        RenderAssignmentTimeline();
    }

    private void RefreshAssignmentLessonDropdown()
    {
        if (assignmentLessonDropdown == null) return;

        assignmentLessonDropdown.choices = new List<string> { "Fen", "Fizik", "Kimya", "Biyoloji" };
        assignmentLessonDropdown.index = -1;
    }

    private void RefreshAssignmentClassDropdown(string selectedLesson = null)
    {
        if (assignmentClassDropdown == null) return;

        var choices = new List<string>();
        classNameToId.Clear();
        classNameToLesson.Clear();
        classNameToGrade.Clear();

        if (lastItems != null)
        {
            foreach (var item in lastItems)
            {
                if (item == null) continue;
                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                if (!string.IsNullOrWhiteSpace(selectedLesson) &&
                    !string.Equals(item.LessonName, selectedLesson, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!classNameToId.ContainsKey(item.Name))
                {
                    classNameToId[item.Name] = item.Id;
                    classNameToLesson[item.Name] = item.LessonName ?? "-";
                    classNameToGrade[item.Name] = item.GradeLevel ?? "";
                    choices.Add(item.Name);
                }
            }
        }

        assignmentClassDropdown.choices = choices;
        assignmentClassDropdown.index = -1;
    }

    private void OnAssignmentPastClicked()
    {
        visibleStartDate = visibleStartDate.AddDays(-AssignmentDayCount);
        RenderAssignmentTimeline();
    }

    private void OnAssignmentNextClicked()
    {
        visibleStartDate = visibleStartDate.AddDays(AssignmentDayCount);
        RenderAssignmentTimeline();
    }

    private void RenderAssignmentTimeline()
    {
        if (timelineDays == null || timelineContents == null) return;

        timelineDays.Clear();
        timelineContents.Clear();

        if (addAssignmentInfoLabel != null)
        {
            DateTime visibleEndDate = visibleStartDate.AddDays(AssignmentDayCount - 1);
            addAssignmentInfoLabel.text =
                $"Ödev eklemek için bir güne tıklayın. Gösterilen aralık: {visibleStartDate.ToString("dd MMM yyyy", trCulture)} - {visibleEndDate.ToString("dd MMM yyyy", trCulture)}";
        }

        for (int i = 0; i < AssignmentDayCount; i++)
        {
            var date = visibleStartDate.AddDays(i);

            var dayItem = new VisualElement();
            dayItem.AddToClassList("timeline-day");

            var dateLabel = new Label(date.ToString("dd MMM yyyy", trCulture));
            dateLabel.AddToClassList("timeline-day-date");

            var dayNameLabel = new Label(CapitalizeFirst(trCulture.DateTimeFormat.GetDayName(date.DayOfWeek)));
            dayNameLabel.AddToClassList("timeline-day-name");

            dayItem.Add(dateLabel);
            dayItem.Add(dayNameLabel);

            timelineDays.Add(dayItem);
        }

        for (int rowIndex = 0; rowIndex < AssignmentRowCount; rowIndex++)
        {
            var row = new VisualElement();
            row.AddToClassList("timeline-row");
            row.name = $"AssignmentTimelineRow_{rowIndex}";

            for (int colIndex = 0; colIndex < AssignmentDayCount; colIndex++)
            {
                var col = new VisualElement();
                col.AddToClassList("timeline-col");

                int capturedRow = rowIndex;
                int capturedCol = colIndex;

                col.RegisterCallback<ClickEvent>(_ =>
                {
                    SelectTimelineCell(col, capturedRow, capturedCol);
                });

                row.Add(col);
            }

            timelineContents.Add(row);
        }

        ClearTimelineSelection();
        RenderAssignmentBlocks();
    }

    private void SelectTimelineCell(VisualElement cell, int rowIndex, int dayIndex)
    {
        ClearTimelineSelection();

        selectedTimelineCell = cell;
        selectedTimelineRow = rowIndex;
        selectedTimelineDay = dayIndex;

        selectedTimelineCell.AddToClassList("timeline-col-selected");

        var selectedDate = visibleStartDate.AddDays(dayIndex);
        if (assignmentStartField != null)
            assignmentStartField.value = selectedDate.ToString("dd MMMM yyyy dddd", trCulture);

        OpenAssignmentModal();
    }

    private void OpenAssignmentModal()
    {
        if (assignmentModal == null) return;

        assignmentModal.RemoveFromClassList("hidden");

        if (assignmentTitleField != null) assignmentTitleField.value = "";
        if (assignmentDurationField != null) assignmentDurationField.value = 1;
        if (assignmentClassDropdown != null) assignmentClassDropdown.index = -1;
        if (assignmentLessonDropdown != null) assignmentLessonDropdown.index = -1;
    }

    private void CloseAssignmentModal()
    {
        if (assignmentModal == null) return;

        assignmentModal.AddToClassList("hidden");

        if (assignmentTitleField != null) assignmentTitleField.value = "";
        if (assignmentStartField != null) assignmentStartField.value = "";
        if (assignmentDurationField != null) assignmentDurationField.value = 1;
        if (assignmentClassDropdown != null) assignmentClassDropdown.index = -1;
        if (assignmentLessonDropdown != null) assignmentLessonDropdown.index = -1;

        ClearTimelineSelection();
    }

    private void ClearTimelineSelection()
    {
        if (selectedTimelineCell != null)
            selectedTimelineCell.RemoveFromClassList("timeline-col-selected");

        selectedTimelineCell = null;
        selectedTimelineRow = -1;
        selectedTimelineDay = -1;
    }

    private void SaveAssignmentToTimeline()
    {
        if (selectedTimelineRow < 0 || selectedTimelineDay < 0)
            return;

        string title = assignmentTitleField != null ? (assignmentTitleField.value ?? "").Trim() : "";
        string className = assignmentClassDropdown != null ? (assignmentClassDropdown.value ?? "") : "";
        string lessonName = assignmentLessonDropdown != null ? (assignmentLessonDropdown.value ?? "") : "";
        int duration = assignmentDurationField != null ? Mathf.Clamp(assignmentDurationField.value, 1, AssignmentDayCount) : 1;
        string selectedUnit = assignmentUnitDropdown != null ? (assignmentUnitDropdown.value ?? "") : "";
        string selectedExperiment = assignmentExperimentDropdown != null ? (assignmentExperimentDropdown.value ?? "") : "";

        if (string.IsNullOrWhiteSpace(selectedUnit))
        {
            Debug.LogWarning("[ADD ASSIGNMENT] Ünite seçmelisiniz.");
            assignmentUnitDropdown?.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedExperiment))
        {
            Debug.LogWarning("[ADD ASSIGNMENT] Deney seçmelisiniz.");
            assignmentExperimentDropdown?.Focus();
            return;
        }

        int experimentId = -1;

        if (unitToExperiments.TryGetValue(selectedUnit, out var experimentsInUnit))
        {
            foreach (var exp in experimentsInUnit)
            {
                if (exp != null && exp.ExperimentName == selectedExperiment)
                {
                    experimentId = exp.Id;
                    break;
                }
            }
        }

        if (experimentId <= 0)
        {
            Debug.LogWarning("[ADD ASSIGNMENT] Seçilen deney id bulunamadı.");
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            Debug.LogWarning("[ADD ASSIGNMENT] Ödev başlığı boş olamaz.");
            assignmentTitleField?.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(className))
        {
            Debug.LogWarning("[ADD ASSIGNMENT] Sınıf seçmelisiniz.");
            assignmentClassDropdown?.Focus();
            return;
        }

        if (!classNameToId.TryGetValue(className, out int classId))
        {
            Debug.LogWarning("[ADD ASSIGNMENT] Seçilen sınıf id bulunamadı.");
            return;
        }

        if (string.IsNullOrWhiteSpace(lessonName) && classNameToLesson.TryGetValue(className, out var inferredLesson))
            lessonName = inferredLesson;

        DateTime startDate = visibleStartDate.AddDays(selectedTimelineDay);
        StartCoroutine(CreateAssignment(title, classId, startDate, duration, experimentId, className, lessonName, selectedExperiment));
    }

    private void OpenAssignmentDetailsModal(string title, string className, string lessonName, DateTime startDate, int durationDays, string experimentName)
    {
        if (assignmentDetailsModal == null) return;

        if (assignmentDetailsTitle != null)
            assignmentDetailsTitle.text = string.IsNullOrWhiteSpace(title) ? "-" : title;
        if (assignmentDetailsClass != null)
            assignmentDetailsClass.text = string.IsNullOrWhiteSpace(className) ? "-" : className;
        if (assignmentDetailsLesson != null)
            assignmentDetailsLesson.text = string.IsNullOrWhiteSpace(lessonName) ? "-" : lessonName;
        if (assignmentDetailsStart != null)
            assignmentDetailsStart.text = startDate.ToString("dd MMMM yyyy", trCulture);
        if (assignmentDetailsDuration != null)
            assignmentDetailsDuration.text = durationDays + " gün";
        if (assignmentDetailsExperiment != null)
            assignmentDetailsExperiment.text = string.IsNullOrWhiteSpace(experimentName) ? "-" : experimentName;

        assignmentDetailsModal.RemoveFromClassList("hidden");
    }

    private void CloseAssignmentDetailsModal()
    {
        if (assignmentDetailsModal == null) return;
        assignmentDetailsModal.AddToClassList("hidden");
    }

    private void RenderAssignmentBlocks()
    {
        if (timelineContents == null) return;
        if (assignmentItems == null || assignmentItems.Length == 0) return;

        DateTime visibleEndDate = visibleStartDate.AddDays(AssignmentDayCount - 1);

        foreach (var item in assignmentItems)
        {
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.StartDate)) continue;

            if (!DateTime.TryParse(item.StartDate, null, DateTimeStyles.RoundtripKind, out var parsedStart))
                continue;

            DateTime startDate = parsedStart.ToLocalTime().Date;
            DateTime itemEndDate = startDate.AddDays(item.DurationDays - 1);

            if (itemEndDate < visibleStartDate || startDate > visibleEndDate)
                continue;

            int startOffset = Mathf.Max(0, (startDate - visibleStartDate).Days);
            int endOffset = Mathf.Min(AssignmentDayCount - 1, (itemEndDate - visibleStartDate).Days);
            int visibleDuration = (endOffset - startOffset) + 1;

            var row = timelineContents.Q<VisualElement>($"AssignmentTimelineRow_{GetRowIndexForAssignment(item)}");
            if (row == null) continue;

            var block = new VisualElement();
            block.AddToClassList("assignment-block");

            block.style.position = Position.Absolute;
            block.style.top = new Length(50, LengthUnit.Percent);
            block.style.left = new Length(startOffset * (100f / AssignmentDayCount), LengthUnit.Percent);
            block.style.width = new Length(visibleDuration * (100f / AssignmentDayCount), LengthUnit.Percent);
            block.style.height = 40;
            block.style.translate = new Translate(4, -20, 0);

            var titleLabel = new Label(item.Title);
            titleLabel.AddToClassList("assignment-block-title");

            var classLabel = new Label(item.ClassName);
            classLabel.AddToClassList("assignment-block-class");

            block.Add(titleLabel);
            block.Add(classLabel);

            block.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                string lesson = ResolveLessonNameByClassId(item.ClassId);
                OpenAssignmentDetailsModal(
                    item.Title,
                    item.ClassName,
                    lesson,
                    startDate,
                    item.DurationDays,
                    string.IsNullOrWhiteSpace(item.ExperimentName) ? "-" : item.ExperimentName
                );
            });

            row.Add(block);
        }
    }

    private string ResolveLessonNameByClassId(int classId)
    {
        if (lastItems == null)
            return "-";

        foreach (var c in lastItems)
        {
            if (c == null) continue;
            if (c.Id != classId) continue;
            return string.IsNullOrWhiteSpace(c.LessonName) ? "-" : c.LessonName;
        }

        return "-";
    }

    private int GetRowIndexForAssignment(AssignmentDto item)
    {
        if (item == null) return 0;
        return Mathf.Abs(item.ClassId.GetHashCode()) % AssignmentRowCount;
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + ((int)date.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    private string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpper(text[0], trCulture) + text.Substring(1);
    }

    private void OnAssignmentClassChanged(string className)
    {
        if (string.IsNullOrWhiteSpace(className) || lastItems == null)
            return;

        MyClassDto selectedClass = null;

        foreach (var item in lastItems)
        {
            if (item != null && item.Name == className)
            {
                selectedClass = item;
                break;
            }
        }

        if (selectedClass == null)
            return;

        var allowedLessons = GetAllowedLessonsByGrade(selectedClass.GradeLevel);
        if (assignmentLessonDropdown != null)
        {
            assignmentLessonDropdown.choices = allowedLessons;

            string preferred = string.IsNullOrWhiteSpace(selectedClass.LessonName)
                ? (allowedLessons.Count > 0 ? allowedLessons[0] : "")
                : selectedClass.LessonName;

            if (!allowedLessons.Contains(preferred))
                preferred = allowedLessons.Count > 0 ? allowedLessons[0] : "";

            assignmentLessonDropdown.SetValueWithoutNotify(preferred);
        }

        string selectedLesson = assignmentLessonDropdown != null
            ? (assignmentLessonDropdown.value ?? "")
            : (selectedClass.LessonName ?? "");

        if (string.IsNullOrWhiteSpace(selectedClass.GradeLevel))
        {
            Debug.LogWarning("[EXPERIMENTS] Sınıfta GradeLevel yok.");
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedLesson))
            return;

        StartCoroutine(FetchExperimentsByGradeAndLesson(selectedClass.GradeLevel, selectedLesson));
    }

    private void OnAssignmentLessonChanged(string lessonName)
    {
        if (string.IsNullOrWhiteSpace(lessonName) || assignmentClassDropdown == null || lastItems == null)
            return;

        string className = assignmentClassDropdown.value ?? "";
        if (string.IsNullOrWhiteSpace(className))
            return;

        MyClassDto selectedClass = null;
        foreach (var item in lastItems)
        {
            if (item != null && item.Name == className)
            {
                selectedClass = item;
                break;
            }
        }

        if (selectedClass == null || string.IsNullOrWhiteSpace(selectedClass.GradeLevel))
            return;

        StartCoroutine(FetchExperimentsByGradeAndLesson(selectedClass.GradeLevel, lessonName));
    }

    private List<string> GetAllowedLessonsByGrade(string gradeLevel)
    {
        bool isFiveToEight = IsGradeInRange(gradeLevel, 5, 8);
        if (isFiveToEight)
            return new List<string> { "Fen" };

        return new List<string> { "Fizik", "Kimya", "Biyoloji" };
    }

    private bool IsGradeInRange(string gradeText, int min, int max)
    {
        if (string.IsNullOrWhiteSpace(gradeText))
            return false;

        string trimmed = gradeText.Trim();
        if (!int.TryParse(trimmed, out int grade))
            return false;

        return grade >= min && grade <= max;
    }

    private IEnumerator FetchExperimentsByGradeAndLesson(string gradeLevel, string lessonName)
    {
        if (router == null) yield break;

        string url =
            $"{router.ApiBaseUrl}{experimentsByGradeLessonPath}" +
            $"?gradeLevel={UnityWebRequest.EscapeURL(gradeLevel)}" +
            $"&lessonName={UnityWebRequest.EscapeURL(lessonName)}";

        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[EXPERIMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        Debug.Log("[EXPERIMENTS] OK => " + raw);

        var wrapped = JsonUtility.FromJson<ExperimentListWrapper>("{\"items\":" + raw + "}");
        experimentItems = wrapped != null ? wrapped.items : null;

        RefreshUnitDropdown();
    }

    private void RefreshUnitDropdown()
    {
        if (assignmentUnitDropdown == null) return;

        unitToExperiments.Clear();

        var unitChoices = new List<string>();

        if (experimentItems != null)
        {
            foreach (var item in experimentItems)
            {
                if (item == null) continue;
                if (string.IsNullOrWhiteSpace(item.UnitName)) continue;

                if (!unitToExperiments.ContainsKey(item.UnitName))
                {
                    unitToExperiments[item.UnitName] = new List<ExperimentDto>();
                    unitChoices.Add(item.UnitName);
                }

                unitToExperiments[item.UnitName].Add(item);
            }
        }

        assignmentUnitDropdown.choices = unitChoices;
        assignmentUnitDropdown.index = -1;

        if (assignmentExperimentDropdown != null)
        {
            assignmentExperimentDropdown.choices = new List<string>();
            assignmentExperimentDropdown.index = -1;
        }
    }

    private void OnAssignmentUnitChanged(string unitName)
    {
        if (assignmentExperimentDropdown == null) return;

        var experimentChoices = new List<string>();

        if (!string.IsNullOrWhiteSpace(unitName) && unitToExperiments.TryGetValue(unitName, out var list))
        {
            foreach (var item in list)
            {
                if (item == null) continue;
                if (string.IsNullOrWhiteSpace(item.ExperimentName)) continue;

                experimentChoices.Add(item.ExperimentName);
            }
        }

        assignmentExperimentDropdown.choices = experimentChoices;
        assignmentExperimentDropdown.index = -1;
    }

    // ---------------- CALENDAR PAGE ----------------

    private void BindCalendarPage()
    {
        calendarPage = root.Q<VisualElement>("CalendarPage");

        calAddEventBtn = root.Q<Button>("CalAddEventBtn");
        calTodayBtnTop = root.Q<Button>("CalTodayBtnTop");
        calExportBtn = root.Q<Button>("CalExportBtn");
        calRefreshBtn = root.Q<Button>("CalRefreshBtn");

        calPrevBtn = root.Q<Button>("CalPrevBtn");
        calNextBtn = root.Q<Button>("CalNextBtn");
        calMiniPrevBtn = root.Q<Button>("CalMiniPrevBtn");
        calMiniNextBtn = root.Q<Button>("CalMiniNextBtn");

        calMonthViewBtn = root.Q<Button>("CalMonthViewBtn");
        calWeekViewBtn = root.Q<Button>("CalWeekViewBtn");
        calDayViewBtn = root.Q<Button>("CalDayViewBtn");
        calAgendaViewBtn = root.Q<Button>("CalAgendaViewBtn");

        calSearchInput = root.Q<TextField>("CalSearchInput");
        calFilterDropdown = root.Q<DropdownField>("CalFilterDropdown");

        calCurrentMonthLabel = root.Q<Label>("CalCurrentMonthLabel");
        calMiniMonthLabel = root.Q<Label>("CalMiniMonthLabel");

        calMiniGrid = root.Q<VisualElement>("CalMiniGrid");
        calUpcomingList = root.Q<VisualElement>("CalUpcomingList");
        calMonthGrid = root.Q<VisualElement>("CalMonthGrid");

        calMonthView = root.Q<VisualElement>("CalMonthView");
        calWeekView = root.Q<VisualElement>("CalWeekView");
        calDayView = root.Q<VisualElement>("CalDayView");
        calAgendaView = root.Q<VisualElement>("CalAgendaView");

        calWeekHeader = root.Q<VisualElement>("CalWeekHeader");
        calWeekBody = root.Q<VisualElement>("CalWeekBody");
        calDayHeader = root.Q<VisualElement>("CalDayHeader");
        calDayBody = root.Q<VisualElement>("CalDayBody");

        calAgendaList = root.Q<ScrollView>("CalAgendaList");
        calAgendaItems = root.Q<VisualElement>("CalAgendaItems");

        calEventModal = root.Q<VisualElement>("CalEventModal");
        calCloseModalBtn = root.Q<Button>("CalCloseModalBtn");
        calCancelEventBtn = root.Q<Button>("CalCancelEventBtn");
        calSaveEventBtn = root.Q<Button>("CalSaveEventBtn");
        calDeleteEventBtn = root.Q<Button>("CalDeleteEventBtn");

        calModalTitleLabel = root.Q<Label>("CalModalTitleLabel");
        calEventTitleInput = root.Q<TextField>("CalEventTitleInput");
        calEventTypeDropdown = root.Q<DropdownField>("CalEventTypeDropdown");
        calEventDateInput = root.Q<TextField>("CalEventDateInput");
        calEventTimeInput = root.Q<TextField>("CalEventTimeInput");
        calEventDescriptionInput = root.Q<TextField>("CalEventDescriptionInput");

        SetupCalendarDropdowns();
        SeedSampleCalendarEvents();

        if (calPrevBtn != null)
        {
            calPrevBtn.clicked -= OnCalendarPrevClicked;
            calPrevBtn.clicked += OnCalendarPrevClicked;
        }

        if (calNextBtn != null)
        {
            calNextBtn.clicked -= OnCalendarNextClicked;
            calNextBtn.clicked += OnCalendarNextClicked;
        }

        if (calMiniPrevBtn != null)
        {
            calMiniPrevBtn.clicked -= OnCalendarPrevClicked;
            calMiniPrevBtn.clicked += OnCalendarPrevClicked;
        }

        if (calMiniNextBtn != null)
        {
            calMiniNextBtn.clicked -= OnCalendarNextClicked;
            calMiniNextBtn.clicked += OnCalendarNextClicked;
        }

        if (calTodayBtnTop != null)
        {
            calTodayBtnTop.clicked -= OnCalendarTodayClicked;
            calTodayBtnTop.clicked += OnCalendarTodayClicked;
        }

        if (calMonthViewBtn != null)
        {
            calMonthViewBtn.clicked -= OnCalendarMonthViewClicked;
            calMonthViewBtn.clicked += OnCalendarMonthViewClicked;
        }

        if (calWeekViewBtn != null)
        {
            calWeekViewBtn.clicked -= OnCalendarWeekViewClicked;
            calWeekViewBtn.clicked += OnCalendarWeekViewClicked;
        }

        if (calDayViewBtn != null)
        {
            calDayViewBtn.clicked -= OnCalendarDayViewClicked;
            calDayViewBtn.clicked += OnCalendarDayViewClicked;
        }

        if (calAgendaViewBtn != null)
        {
            calAgendaViewBtn.clicked -= OnCalendarAgendaViewClicked;
            calAgendaViewBtn.clicked += OnCalendarAgendaViewClicked;
        }

        if (calAddEventBtn != null)
        {
            calAddEventBtn.clicked -= OpenCalendarModalForCreate;
            calAddEventBtn.clicked += OpenCalendarModalForCreate;
        }

        if (calCloseModalBtn != null)
        {
            calCloseModalBtn.clicked -= CloseCalendarModal;
            calCloseModalBtn.clicked += CloseCalendarModal;
        }

        if (calCancelEventBtn != null)
        {
            calCancelEventBtn.clicked -= CloseCalendarModal;
            calCancelEventBtn.clicked += CloseCalendarModal;
        }

        if (calSaveEventBtn != null)
        {
            calSaveEventBtn.clicked -= OnCalendarSaveClicked;
            calSaveEventBtn.clicked += OnCalendarSaveClicked;
        }

        if (calDeleteEventBtn != null)
        {
            calDeleteEventBtn.clicked -= OnCalendarDeleteClicked;
            calDeleteEventBtn.clicked += OnCalendarDeleteClicked;
        }

        if (calSearchInput != null)
        {
            calSearchInput.RegisterValueChangedCallback(_ => RenderCalendar());
        }

        if (calFilterDropdown != null)
        {
            calFilterDropdown.RegisterValueChangedCallback(_ => RenderCalendar());
        }

        if (calEventModal != null)
            calEventModal.style.display = DisplayStyle.None;

        SetCalendarView("month");
        RenderCalendar();
    }

    private void SetupCalendarDropdowns()
    {
        if (calFilterDropdown != null)
        {
            calFilterDropdown.choices = new List<string>
            {
                "Tüm Etkinlikler",
                "Ders",
                "Sınav",
                "Toplantı",
                "Son Tarih",
                "Simülasyon",
                "Hatırlatma"
            };
            calFilterDropdown.value = "Tüm Etkinlikler";
        }

        if (calEventTypeDropdown != null)
        {
            calEventTypeDropdown.choices = new List<string>
            {
                "Ders",
                "Sınav",
                "Toplantı",
                "Son Tarih",
                "Simülasyon",
                "Hatırlatma"
            };
            calEventTypeDropdown.value = "Ders";
        }
    }

    private void SeedSampleCalendarEvents()
    {
        if (sampleCalendarEvents.Count > 0) return;

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = "7C Fizik Lab",
            Category = "Ders",
            Date = new DateTime(2026, 3, 24),
            TimeText = "10:00",
            Description = "Fizik laboratuvar uygulaması"
        });

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = "Matematik Ödev Kontrolü",
            Category = "Hatırlatma",
            Date = new DateTime(2026, 3, 24),
            TimeText = "16:00",
            Description = "Ödev kontrolü"
        });

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = "5A Fen Quiz",
            Category = "Sınav",
            Date = new DateTime(2026, 3, 25),
            TimeText = "09:00",
            Description = "Kısa sınav"
        });

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = "7C Fizik Dersi",
            Category = "Ders",
            Date = new DateTime(2026, 3, 25),
            TimeText = "13:00",
            Description = "Haftalık fizik dersi"
        });

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = "Öğretmenler Kurulu",
            Category = "Toplantı",
            Date = new DateTime(2026, 3, 26),
            TimeText = "14:30",
            Description = "Kurul toplantısı"
        });

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = "6B Geometri Sınavı",
            Category = "Sınav",
            Date = new DateTime(2026, 3, 27),
            TimeText = "11:00",
            Description = "Geometri yazılısı"
        });
    }

    private void OnCalendarPrevClicked()
    {
        switch (currentCalendarView)
        {
            case "week":
                calendarCurrentDate = calendarCurrentDate.AddDays(-7);
                break;
            case "day":
                calendarCurrentDate = calendarCurrentDate.AddDays(-1);
                break;
            default:
                calendarCurrentDate = calendarCurrentDate.AddMonths(-1);
                break;
        }

        RenderCalendar();
    }

    private void OnCalendarNextClicked()
    {
        switch (currentCalendarView)
        {
            case "week":
                calendarCurrentDate = calendarCurrentDate.AddDays(7);
                break;
            case "day":
                calendarCurrentDate = calendarCurrentDate.AddDays(1);
                break;
            default:
                calendarCurrentDate = calendarCurrentDate.AddMonths(1);
                break;
        }

        RenderCalendar();
    }

    private void OnCalendarTodayClicked()
    {
        calendarCurrentDate = DateTime.Today;
        RenderCalendar();
    }

    private void OnCalendarMonthViewClicked() => SetCalendarView("month");
    private void OnCalendarWeekViewClicked() => SetCalendarView("week");
    private void OnCalendarDayViewClicked() => SetCalendarView("day");
    private void OnCalendarAgendaViewClicked() => SetCalendarView("agenda");

    private void SetCalendarView(string view)
    {
        currentCalendarView = view;

        SetDisplay(calMonthView, view == "month");
        SetDisplay(calWeekView, view == "week");
        SetDisplay(calDayView, view == "day");
        SetDisplay(calAgendaView, view == "agenda");

        SetCalendarViewButtonActive(calMonthViewBtn, view == "month");
        SetCalendarViewButtonActive(calWeekViewBtn, view == "week");
        SetCalendarViewButtonActive(calDayViewBtn, view == "day");
        SetCalendarViewButtonActive(calAgendaViewBtn, view == "agenda");

        RenderCalendar();
    }

    private void SetCalendarViewButtonActive(Button btn, bool active)
    {
        if (btn == null) return;

        if (active)
        {
            if (!btn.ClassListContains("active"))
                btn.AddToClassList("active");
        }
        else
        {
            btn.RemoveFromClassList("active");
        }
    }

    private void RenderCalendar()
    {
        UpdateCalendarMonthLabels();
        RenderMiniCalendar();
        RenderUpcomingEvents();

        switch (currentCalendarView)
        {
            case "week":
                RenderWeekView();
                break;
            case "day":
                RenderDayView();
                break;
            case "agenda":
                RenderAgendaView();
                break;
            default:
                RenderMonthView();
                break;
        }
    }

    private void UpdateCalendarMonthLabels()
    {
        string monthText = calendarCurrentDate.ToString("MMMM yyyy", trCulture);
        monthText = CapitalizeFirst(monthText);

        if (calCurrentMonthLabel != null)
            calCurrentMonthLabel.text = monthText;

        if (calMiniMonthLabel != null)
            calMiniMonthLabel.text = monthText;
    }

    private void RenderMiniCalendar()
    {
        if (calMiniGrid == null) return;

        calMiniGrid.Clear();

        string[] weekdays = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        foreach (var wd in weekdays)
        {
            var wdLabel = new Label(wd);
            wdLabel.AddToClassList("cal-mini-weekday");
            calMiniGrid.Add(wdLabel);
        }

        DateTime firstOfMonth = new DateTime(calendarCurrentDate.Year, calendarCurrentDate.Month, 1);
        int startOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        DateTime gridStart = firstOfMonth.AddDays(-startOffset);

        for (int i = 0; i < 42; i++)
        {
            DateTime date = gridStart.AddDays(i);

            var day = new Label(date.Day.ToString());
            day.AddToClassList("cal-mini-day");

            if (date.Month != calendarCurrentDate.Month)
                day.AddToClassList("other-month");

            if (date.Date == DateTime.Today)
                day.AddToClassList("today");

            calMiniGrid.Add(day);
        }
    }

    private void RenderUpcomingEvents()
    {
        if (calUpcomingList == null) return;

        calUpcomingList.Clear();

        var filtered = GetFilteredCalendarEvents();
        filtered.Sort((a, b) => a.Date.CompareTo(b.Date));

        int count = Mathf.Min(4, filtered.Count);

        for (int i = 0; i < count; i++)
            calUpcomingList.Add(BuildUpcomingEventItem(filtered[i]));
    }

    private VisualElement BuildUpcomingEventItem(CalendarEventData item)
    {
        var wrap = new VisualElement();
        wrap.AddToClassList("cal-upcoming-item");

        var title = new Label(item.Title);
        title.AddToClassList("cal-upcoming-title");

        var meta = new Label($"{item.TimeText} • {item.Date.ToString("dd MMM", trCulture)}");
        meta.AddToClassList("cal-upcoming-meta");

        wrap.Add(title);
        wrap.Add(meta);

        return wrap;
    }

    private void RenderMonthView()
    {
        if (calMonthGrid == null) return;

        calMonthGrid.Clear();

        DateTime firstOfMonth = new DateTime(calendarCurrentDate.Year, calendarCurrentDate.Month, 1);
        int startOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        DateTime gridStart = firstOfMonth.AddDays(-startOffset);

        var filteredEvents = GetFilteredCalendarEvents();

        for (int i = 0; i < 42; i++)
        {
            DateTime date = gridStart.AddDays(i);

            var cell = new VisualElement();
            cell.AddToClassList("cal-day-cell");

            if (date.Month != calendarCurrentDate.Month)
                cell.AddToClassList("other-month");

            var dayNumber = new Label(date.Day.ToString());
            dayNumber.AddToClassList("cal-day-number");
            cell.Add(dayNumber);

            var eventsWrap = new VisualElement();
            eventsWrap.AddToClassList("cal-day-events");

            int added = 0;
            foreach (var ev in filteredEvents)
            {
                if (ev.Date.Date != date.Date) continue;

                if (added >= 3) break;

                var pill = new Label(ev.Title);
                pill.AddToClassList("cal-event-pill");
                AddCalendarCategoryClass(pill, ev.Category);
                eventsWrap.Add(pill);
                added++;
            }

            cell.Add(eventsWrap);
            calMonthGrid.Add(cell);
        }
    }

    private void RenderWeekView()
    {
        if (calWeekHeader == null || calWeekBody == null) return;

        calWeekHeader.Clear();
        calWeekBody.Clear();

        DateTime weekStart = GetStartOfWeek(calendarCurrentDate);
        string[] weekdays = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

        for (int i = 0; i < 7; i++)
        {
            DateTime date = weekStart.AddDays(i);

            var label = new Label($"{weekdays[i]} {date:dd}");
            label.AddToClassList("cal-week-header-cell");
            calWeekHeader.Add(label);
        }

        var bodyLabel = new Label("Haftalık görünüm hazırlanıyor");
        bodyLabel.AddToClassList("cal-placeholder-label");
        calWeekBody.Add(bodyLabel);
    }

    private void RenderDayView()
    {
        if (calDayHeader == null || calDayBody == null) return;

        calDayHeader.Clear();
        calDayBody.Clear();

        var header = new Label(CapitalizeFirst(calendarCurrentDate.ToString("dd MMMM yyyy dddd", trCulture)));
        header.AddToClassList("cal-day-header-label");
        calDayHeader.Add(header);

        var events = GetFilteredCalendarEvents().FindAll(x => x.Date.Date == calendarCurrentDate.Date);

        if (events.Count == 0)
        {
            var empty = new Label("Bu gün için etkinlik yok.");
            empty.AddToClassList("cal-placeholder-label");
            calDayBody.Add(empty);
            return;
        }

        foreach (var ev in events)
        {
            calDayBody.Add(BuildAgendaItem(ev));
        }
    }

    private void RenderAgendaView()
    {
        if (calAgendaItems == null) return;

        calAgendaItems.Clear();

        var filtered = GetFilteredCalendarEvents();
        filtered.Sort((a, b) => a.Date.CompareTo(b.Date));

        foreach (var ev in filtered)
            calAgendaItems.Add(BuildAgendaItem(ev));
    }

    private VisualElement BuildAgendaItem(CalendarEventData item)
    {
        var row = new VisualElement();
        row.AddToClassList("cal-agenda-item");

        var left = new VisualElement();
        left.AddToClassList("cal-agenda-left");

        var title = new Label(item.Title);
        title.AddToClassList("cal-agenda-title");

        var subtitle = new Label($"{item.Date.ToString("dd MMMM yyyy", trCulture)} • {item.TimeText}");
        subtitle.AddToClassList("cal-agenda-subtitle");

        left.Add(title);
        left.Add(subtitle);

        var badge = new Label(item.Category);
        badge.AddToClassList("cal-agenda-badge");

        row.Add(left);
        row.Add(badge);

        return row;
    }

    private List<CalendarEventData> GetFilteredCalendarEvents()
    {
        var result = new List<CalendarEventData>();
        string search = calSearchInput != null ? (calSearchInput.value ?? "").Trim().ToLowerInvariant() : "";
        string filter = calFilterDropdown != null ? (calFilterDropdown.value ?? "Tüm Etkinlikler") : "Tüm Etkinlikler";

        foreach (var item in sampleCalendarEvents)
        {
            if (item == null) continue;

            if (filter != "Tüm Etkinlikler" && item.Category != filter)
                continue;

            if (!string.IsNullOrWhiteSpace(search))
            {
                bool matches =
                    (item.Title ?? "").ToLowerInvariant().Contains(search) ||
                    (item.Category ?? "").ToLowerInvariant().Contains(search) ||
                    (item.Description ?? "").ToLowerInvariant().Contains(search);

                if (!matches)
                    continue;
            }

            result.Add(item);
        }

        return result;
    }

    private void AddCalendarCategoryClass(VisualElement el, string category)
    {
        if (el == null) return;

        switch (category)
        {
            case "Sınav":
                el.AddToClassList("exam");
                break;
            case "Toplantı":
                el.AddToClassList("meeting");
                break;
            case "Son Tarih":
                el.AddToClassList("deadline");
                break;
        }
    }

    private void OpenCalendarModalForCreate()
    {
        if (calEventModal == null) return;

        if (calModalTitleLabel != null)
            calModalTitleLabel.text = "Etkinlik Ekle";

        if (calEventTitleInput != null) calEventTitleInput.value = "";
        if (calEventTypeDropdown != null) calEventTypeDropdown.value = "Ders";
        if (calEventDateInput != null) calEventDateInput.value = DateTime.Today.ToString("dd.MM.yyyy");
        if (calEventTimeInput != null) calEventTimeInput.value = "09:00";
        if (calEventDescriptionInput != null) calEventDescriptionInput.value = "";

        calEventModal.style.display = DisplayStyle.Flex;
    }

    private void CloseCalendarModal()
    {
        if (calEventModal == null) return;
        calEventModal.style.display = DisplayStyle.None;
    }

    private void OnCalendarSaveClicked()
    {
        string title = calEventTitleInput != null ? (calEventTitleInput.value ?? "").Trim() : "";
        string category = calEventTypeDropdown != null ? (calEventTypeDropdown.value ?? "Ders") : "Ders";
        string dateText = calEventDateInput != null ? (calEventDateInput.value ?? "").Trim() : "";
        string timeText = calEventTimeInput != null ? (calEventTimeInput.value ?? "").Trim() : "09:00";
        string desc = calEventDescriptionInput != null ? (calEventDescriptionInput.value ?? "").Trim() : "";

        if (string.IsNullOrWhiteSpace(title))
        {
            Debug.LogWarning("[CALENDAR] Başlık boş olamaz.");
            return;
        }

        if (!DateTime.TryParseExact(dateText, "dd.MM.yyyy", trCulture, DateTimeStyles.None, out var parsedDate))
        {
            Debug.LogWarning("[CALENDAR] Tarih formatı dd.MM.yyyy olmalı.");
            return;
        }

        sampleCalendarEvents.Add(new CalendarEventData
        {
            Title = title,
            Category = category,
            Date = parsedDate,
            TimeText = string.IsNullOrWhiteSpace(timeText) ? "09:00" : timeText,
            Description = desc
        });

        CloseCalendarModal();
        RenderCalendar();
    }

    private void OnCalendarDeleteClicked()
    {
        CloseCalendarModal();
    }

    [Serializable]
    private class CalendarEventData
    {
        public string Title;
        public string Category;
        public DateTime Date;
        public string TimeText;
        public string Description;
    }

    // ----------------------------------------------

    // ---------------- HELPERS ----------------

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

    // ---------------- DTOs ----------------

    [Serializable]
    public class MyClassDto
    {
        public int Id;
        public string Code;
        public string Name;
        public string LessonName;
        public bool IsActive;
        public string JoinedAt;
        public int StudentCount;
        public int AssignmentCount;
        public int SuccessRatePercent;
        public string GradeLevel;
    }

    [Serializable]
    private class ClassListWrapper
    {
        public MyClassDto[] items;
    }

    [Serializable]
    private class CreateClassRequest
    {
        public string Name;
        public string GradeLevel;
        public string LessonName;
    }

    [Serializable]
    private class UpdateClassStatusRequest
    {
        public bool IsActive;
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
    private class CreateAssignmentRequest
    {
        public string Title;
        public int ClassId;
        public string StartDate;
        public int DurationDays;
        public int ExperimentId;
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

    [Serializable]
    private class JoinRequestDto
    {
        public int UserId;
        public string Name;
        public string Surname;
        public string Email;
        public string RequestedAt;
        public string Status;
    }

    [Serializable]
    private class JoinRequestListWrapper
    {
        public JoinRequestDto[] items;
    }

    [Serializable]
    private class ClassStudentDto
    {
        public int UserId;
        public string Name;
        public string Surname;
        public string Email;
        public string JoinedAt;
    }

    [Serializable]
    private class StudentProfileHistoryItemDto
    {
        public string Title;
        public string Value;
        public string Date;
    }

    [Serializable]
    private class StudentProfileDto
    {
        public int StudentId;
        public string Name;
        public string Surname;
        public string Email;
        public string CreatedAt;
        public string LastLogin;
        public string JoinedAt;
        public int PerformancePercent;
        public int CompletedAssignments;
        public int TotalAssignments;
        public int CompletedExperiments;
        public string ParticipationLevel;
        public int CurrentStreakDays;
        public StudentProfileHistoryItemDto[] AssignmentHistory;
        public StudentProfileHistoryItemDto[] ExperimentHistory;
    }

    [Serializable]
    private class ClassStudentListWrapper
    {
        public ClassStudentDto[] items;
    }

    [Serializable]
    private class ClassActivityDto
    {
        public string ActivityId;
        public string Type;
        public string Title;
        public string Description;
        public string ActorName;
        public int ActorUserId;
        public string ActorRole;
        public string OccurredAt;
        public int LikesCount;
        public bool IsLikedByCurrentUser;
        public ActivityCommentDto[] Comments;
    }

    [Serializable]
    private class ClassActivityListWrapper
    {
        public ClassActivityDto[] items;
    }

    [Serializable]
    private class ActivityCommentDto
    {
        public int UserId;
        public string UserName;
        public string Text;
        public string CreatedAt;
    }

    [Serializable]
    private class ActivityCommentRequest
    {
        public string Text;
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
}