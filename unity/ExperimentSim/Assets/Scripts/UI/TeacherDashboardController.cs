using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
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
    [SerializeField] private string userPath = "/api/User";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string teacherRoleRequestPath = "/api/User/teacher-role-request/me";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";
    [SerializeField] private string calendarCategoriesPath = "/api/Calendar/categories";
    [SerializeField] private string calendarEventsPath = "/api/Calendar/events";
    [SerializeField] private string assignmentCompletedStudentsPathTemplate = "/api/AssignmentResult/assignment/{assignmentId}/completed-students";
    [SerializeField] private string assignmentResultAnswersPathTemplate = "/api/AssignmentResult/{resultId}/answers";

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
    private TeacherJoinRequestNotificationItem[] notificationJoinRequestItems = Array.Empty<TeacherJoinRequestNotificationItem>();
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
    // ------------------------------------------

    // ---------------- CALENDAR ----------------
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

    // add event modal
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

    // category modal
    private VisualElement calCategoryModal;
    private Button calCategoryModalCloseBtn;
    private Button calCategoryCancelBtn;
    private Button calSaveCategoryBtn;
    private TextField calCategoryNameInput;
    private TextField calCategoryColorInput;
    private Button calTextColorWhiteBtn;
    private Button calTextColorBlackBtn;

    // detail modal
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

    // ---------------- ASSIGNMENT RESULTS PAGE ----------------
    private VisualElement assignmentResultsPage;
    private Button assignmentResultsBackBtn;
    private Label assignmentResultsTitleLabel;
    private Label assignmentResultsSummaryLabel;
    private Label assignmentResultsSelectedStudentLabel;
    private VisualElement assignmentResultsStudentsContainer;
    private VisualElement assignmentResultsAnswersContainer;

    private AssignmentDto currentResultsAssignment;
    // ------------------------------------------
    // ------------- GO SIMULATION --------------
    private Button goSimulationBtn;
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

        goSimulationBtn = root.Q<Button>("GoSimulationBtn");
        if (goSimulationBtn != null)
        {
            goSimulationBtn.clicked -= OnGoSimulationClicked;
            goSimulationBtn.clicked += OnGoSimulationClicked;
        }


        BindFilters();
        BindAddClassModal();
        BindAddAssignmentPage();
        BindHomePage();
        BindFilters();
        BindAddClassModal();
        BindAddAssignmentPage();
        BindPersonalActivityPage();
        BindProfilePage();
        BindSettingsModal();
        BindMenuButtons();
        BindCalendarPage();
        BindNotifications();

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
        yield return StartCoroutine(RefreshRoleChangeStateForNotifications());

        ApplyTeacherHomeDashboardMetrics();
        RefreshNotificationsBadge();
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

        root.Q<Button>("AccountBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("ProfileBtn");
            ShowPage("ProfilePage");
            StartCoroutine(LoadProfilePageData());
        });

        root.Q<Button>("SettingsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            OpenSettingsModal();
        });

        root.Q<Button>("CalendarBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            CloseClassDetailsOverlays();
            SetMenuActive("CalendarBtn");
            ShowPage("CalendarPage");
            RenderCalendarAll();
        });
    }

    private void BindNotifications()
    {
        notificationCenter = new DashboardNotificationCenter(
            root,
            BuildNotificationItems,
            HandleNotificationSelected,
            () => $"teacher-{router?.CurrentUserId ?? 0}");
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

            var dueAt = GetTeacherAssignmentDueAt(assignment);
            if (!dueAt.HasValue)
                continue;

            string status = GetTeacherAssignmentStatus(assignment);
            bool isUpcoming = dueAt.Value >= now && dueAt.Value <= now.AddDays(3);

            if (isUpcoming && !string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"teacher-upcoming-assignment-{assignment.Id}",
                    Title = "Yaklaşan Teslim",
                    Message = $"{SafeText(assignment.Title)} için son tarih: {dueAt.Value.ToString("dd MMM yyyy HH:mm", trCulture)}",
                    Timestamp = dueAt.Value,
                    TargetPage = "ClassesPage",
                    TargetMenuButton = "ClassBtn",
                    IsUnread = true
                });
            }
        }

        foreach (var req in notificationJoinRequestItems ?? Array.Empty<TeacherJoinRequestNotificationItem>())
        {
            if (req == null)
                continue;

            bool isPending = string.IsNullOrWhiteSpace(req.Status)
                || string.Equals(req.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                || string.Equals(req.Status, "Beklemede", StringComparison.OrdinalIgnoreCase);

            if (!isPending)
                continue;

            var requestedAt = TryParseDashboardDate(req.RequestedAt, out var parsed) ? parsed : now;
            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"teacher-join-request-{req.ClassId}-{req.UserId}",
                Title = "Sınıfa Katılma İsteği",
                Message = $"{SafeText(req.Name)} {SafeText(req.Surname)} {SafeText(req.ClassName)} sınıfına katılmak istiyor.",
                Timestamp = requestedAt,
                TargetPage = "ClassesPage",
                TargetMenuButton = "ClassBtn",
                IsUnread = requestedAt >= now.AddDays(-7)
            });
        }

        foreach (var activity in personalActivityItems ?? Array.Empty<ClassActivityDto>())
        {
            if (activity == null)
                continue;

            bool isSubmission = string.Equals(activity.Type, "AssignmentSubmitted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(activity.Type, "HomeworkSubmitted", StringComparison.OrdinalIgnoreCase)
                || (activity.Type ?? string.Empty).IndexOf("Submitted", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isSubmission)
                continue;

            var occurredAt = ParseActivityDate(activity.OccurredAt);
            if (occurredAt == DateTime.MinValue)
                occurredAt = now;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"teacher-submitted-{activity.ActivityId}",
                Title = "Ödev Teslimi",
                Message = string.IsNullOrWhiteSpace(activity.Description)
                    ? $"{SafeText(activity.ActorName)} bir ödev teslim etti."
                    : activity.Description,
                Timestamp = occurredAt,
                TargetPage = "ActivityPage",
                TargetMenuButton = "ActivityBtn",
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

        if (TryLoadPersistedRoleChangeNotification(out var persistedRoleChange)
            && !list.Any(x => string.Equals(x.Id, persistedRoleChange.Id, StringComparison.Ordinal)))
        {
            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = persistedRoleChange.Id,
                Title = "Rol Güncellemesi",
                Message = persistedRoleChange.Message,
                Timestamp = persistedRoleChange.Timestamp,
                TargetPage = "ProfilePage",
                TargetMenuButton = "ProfileBtn",
                IsUnread = persistedRoleChange.Timestamp >= now.AddDays(-7)
            });
        }

        return list
            .Where(x => x.Timestamp != DateTime.MinValue)
            .OrderByDescending(x => x.Timestamp)
            .Take(250)
            .ToList();
    }

    private void HandleNotificationSelected(DashboardNotificationCenter.NotificationItem item)
    {
        if (item == null)
            return;

        CloseClassDetailsOverlays();

        if (!string.IsNullOrWhiteSpace(item.TargetMenuButton))
            SetMenuActive(item.TargetMenuButton);

        if (!string.IsNullOrWhiteSpace(item.TargetPage))
            ShowPage(item.TargetPage);

        if (string.Equals(item.TargetPage, "ActivityPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(FetchPersonalActivity());
        else if (string.Equals(item.TargetPage, "ClassesPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(FetchMyClasses());
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
                Debug.LogError($"[TEACHER PROFILE] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
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
            previousRole = LoadLegacySnapshotRole();
            if (!string.IsNullOrWhiteSpace(previousRole))
                PlayerPrefs.SetString(snapshotKey, previousRole);
        }

        if (string.IsNullOrWhiteSpace(previousRole))
        {
            PlayerPrefs.SetString(snapshotKey, newRole);
            return;
        }

        if (string.Equals(previousRole, newRole, StringComparison.OrdinalIgnoreCase))
            return;

        var roleChange = new RoleChangeNotificationDto
        {
            Id = $"teacher-role-change-{router.CurrentUserId}-{DateTime.UtcNow.Ticks}",
            Message = BuildRoleChangeMessage(previousRole, newRole),
            Timestamp = DateTime.Now
        };

        roleChangeNotificationItems.Add(roleChange);
        PersistRoleChangeNotification(roleChange);

        PlayerPrefs.SetString(snapshotKey, newRole);
    }

    private string BuildRoleChangeMessage(string previousRole, string newRole)
    {
        string normalized = (newRole ?? string.Empty).ToLowerInvariant();
        if (normalized.Contains("teacher") || normalized.Contains("öğretmen") || normalized.Contains("ogretmen"))
            return "Öğretmenlik başvurunuz onaylandı. Rolünüz öğretmen olarak güncellendi.";

        if (normalized.Contains("student") || normalized.Contains("öğrenci") || normalized.Contains("ogrenci"))
            return "Öğrencilik başvurunuz onaylandı. Rolünüz öğrenci olarak güncellendi.";

        return $"Rolünüz {previousRole} rolünden {newRole} rolüne güncellendi.";
    }

    private void PersistRoleChangeNotification(RoleChangeNotificationDto roleChange)
    {
        if (roleChange == null)
            return;

        PlayerPrefs.SetString(GetRoleNotificationIdKey(), roleChange.Id ?? string.Empty);
        PlayerPrefs.SetString(GetRoleNotificationMessageKey(), roleChange.Message ?? string.Empty);
        PlayerPrefs.SetString(GetRoleNotificationTimestampKey(), roleChange.Timestamp.ToString("O"));
        PlayerPrefs.Save();
    }

    private bool TryLoadPersistedRoleChangeNotification(out RoleChangeNotificationDto roleChange)
    {
        roleChange = null;
        if (router == null)
            return false;

        string id = PlayerPrefs.GetString(GetRoleNotificationIdKey(), string.Empty);
        string message = PlayerPrefs.GetString(GetRoleNotificationMessageKey(), string.Empty);
        string rawTimestamp = PlayerPrefs.GetString(GetRoleNotificationTimestampKey(), string.Empty);

        if (string.IsNullOrWhiteSpace(message))
        {
            id = PlayerPrefs.GetString(GetLegacyRoleNotificationIdKey(), string.Empty);
            message = PlayerPrefs.GetString(GetLegacyRoleNotificationMessageKey(), string.Empty);
            rawTimestamp = PlayerPrefs.GetString(GetLegacyRoleNotificationTimestampKey(), string.Empty);
        }

        if (string.IsNullOrWhiteSpace(message))
            return false;

        if (!DateTime.TryParse(rawTimestamp, null, DateTimeStyles.RoundtripKind, out var parsed))
            parsed = DateTime.Now;

        roleChange = new RoleChangeNotificationDto
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? $"teacher-role-change-{router.CurrentUserId}-{parsed.ToUniversalTime().Ticks}"
                : id,
            Message = message,
            Timestamp = parsed
        };

        return true;
    }

    private string LoadLegacySnapshotRole()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        string[] keys =
        {
            $"independent-role-snapshot-{userId}",
            $"student-role-snapshot-{userId}",
            $"creator-role-snapshot-{userId}",
            $"teacher-role-snapshot-{userId}"
        };

        foreach (var key in keys)
        {
            string value = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private string GetRoleNotificationIdKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"role-notification-id-{userId}";
    }

    private string GetRoleNotificationMessageKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"role-notification-message-{userId}";
    }

    private string GetRoleNotificationTimestampKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"role-notification-time-{userId}";
    }

    private string GetLegacyRoleNotificationIdKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-notification-id-{userId}";
    }

    private string GetLegacyRoleNotificationMessageKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-notification-message-{userId}";
    }

    private string GetLegacyRoleNotificationTimestampKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-notification-time-{userId}";
    }

    private string GetRoleSnapshotKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"role-snapshot-{userId}";
    }

    private IEnumerator RefreshRoleChangeStateForNotifications()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + myProfilePath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        var me = JsonUtility.FromJson<ProfileMeDto>(raw);
        if (me == null)
            yield break;

        TrackRoleChangeNotification(me);

        if (string.IsNullOrWhiteSpace(me.roleName)
            || !me.roleName.ToLowerInvariant().Contains("teacher") && !me.roleName.ToLowerInvariant().Contains("öğretmen") && !me.roleName.ToLowerInvariant().Contains("ogretmen"))
            yield break;

        using var teacherReq = AuthedGet(router.ApiBaseUrl + teacherRoleRequestPath);
        yield return teacherReq.SendWebRequest();

        if (teacherReq.result != UnityWebRequest.Result.Success)
            yield break;

        string teacherRaw = teacherReq.downloadHandler != null ? teacherReq.downloadHandler.text : "{}";
        var teacherState = JsonUtility.FromJson<TeacherRoleRequestStateDto>(teacherRaw);
        if (teacherState == null || !string.Equals(teacherState.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            yield break;

        if (TryLoadPersistedRoleChangeNotification(out _))
            yield break;

        var approvalNotification = new RoleChangeNotificationDto
        {
            Id = $"teacher-approved-{router.CurrentUserId}-{DateTime.UtcNow.Ticks}",
            Message = "Öğretmenlik başvurunuz onaylandı. Rolünüz öğretmen olarak güncellendi.",
            Timestamp = DateTime.Now
        };

        roleChangeNotificationItems.Add(approvalNotification);
        PersistRoleChangeNotification(approvalNotification);
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

                yield return StartCoroutine(RefreshRoleChangeStateForNotifications());

                if (lastItems != null)
                {
                    yield return StartCoroutine(FetchPendingJoinRequestsForNotifications());
                    RefreshNotificationsBadge();
                }
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
        if (mainContent == null)
            return;

        var pages = mainContent.Query<VisualElement>(className: "page").ToList();

        foreach (var pageItem in pages)
        {
            pageItem.RemoveFromClassList("active");
            pageItem.style.display = DisplayStyle.None;
        }

        var targetPage = mainContent.Q<VisualElement>(pageName);

        if (targetPage == null)
        {
            Debug.LogError($"[TeacherDashboardController] Page not found: {pageName}");
            return;
        }

        targetPage.style.display = DisplayStyle.Flex;
        targetPage.AddToClassList("active");

        Debug.Log($"[TeacherDashboardController] Opened page: {pageName}");
    }

    private void SetMenuActive(string activeButtonName)
    {
        var names = new[] { "HomeBtn", "ClassBtn", "AddAssignmentBtn", "CalendarBtn", "StartSimulationBtn", "EmailBtn", "ActivityBtn", "ProfileBtn" };

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
        RefreshCalendarClassDropdown();

        yield return StartCoroutine(FetchPendingJoinRequestsForNotifications());

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
        RefreshNotificationsBadge();

        RefreshAssignmentLessonDropdown();
        RefreshAssignmentClassDropdown();
        ApplyFiltersAndRender();
    }

    private IEnumerator FetchPendingJoinRequestsForNotifications()
    {
        if (router == null)
            yield break;

        var classes = (lastItems ?? Array.Empty<MyClassDto>()).Where(c => c != null).ToArray();
        var pendingItems = new List<TeacherJoinRequestNotificationItem>();

        foreach (var cls in classes)
        {
            if (cls == null || cls.Id <= 0)
                continue;

            string url = BuildJoinRequestsUrl(cls.Id);
            using var req = AuthedGet(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                continue;

            string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
            var wrapped = JsonUtility.FromJson<JoinRequestListWrapper>("{\"items\":" + raw + "}");
            var items = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<JoinRequestDto>();

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                bool isPending = string.IsNullOrWhiteSpace(item.Status)
                    || string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.Status, "Beklemede", StringComparison.OrdinalIgnoreCase);

                if (!isPending)
                    continue;

                pendingItems.Add(new TeacherJoinRequestNotificationItem
                {
                    ClassId = cls.Id,
                    ClassName = cls.Name,
                    UserId = item.UserId,
                    Name = item.Name,
                    Surname = item.Surname,
                    Email = item.Email,
                    RequestedAt = item.RequestedAt,
                    Status = item.Status
                });
            }
        }

        notificationJoinRequestItems = pendingItems
            .OrderByDescending(x => ParseActivityDate(x.RequestedAt))
            .ToArray();
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
            assignmentItems = Array.Empty<AssignmentDto>();
            RefreshNotificationsBadge();
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
        RefreshNotificationsBadge();
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

        assignmentResultsPage = root.Q<VisualElement>("AssignmentResultsPage");
        assignmentResultsBackBtn = root.Q<Button>("AssignmentResultsBackBtn");
        assignmentResultsTitleLabel = root.Q<Label>("AssignmentResultsTitleLabel");
        assignmentResultsSummaryLabel = root.Q<Label>("AssignmentResultsSummaryLabel");
        assignmentResultsSelectedStudentLabel = root.Q<Label>("AssignmentResultsSelectedStudentLabel");

        var assignmentResultsStudentsScroll = root.Q<ScrollView>("AssignmentResultsStudentsScroll");
        assignmentResultsStudentsContainer = assignmentResultsStudentsScroll != null
            ? assignmentResultsStudentsScroll.contentContainer
            : root.Q<VisualElement>("AssignmentResultsStudentsContainer");

        var assignmentResultsAnswersScroll = root.Q<ScrollView>("AssignmentResultsAnswersScroll");
        assignmentResultsAnswersContainer = assignmentResultsAnswersScroll != null
            ? assignmentResultsAnswersScroll.contentContainer
            : root.Q<VisualElement>("AssignmentResultsAnswersContainer");

        if (assignmentResultsBackBtn != null)
        {
            assignmentResultsBackBtn.clicked -= CloseAssignmentResultsPage;
            assignmentResultsBackBtn.clicked += CloseAssignmentResultsPage;
        }

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
            content.Add(BuildStudentHistoryItem("Kayıt bulunamadı", "", "-"));
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
            content.Add(BuildStudentHistoryItem("Kayıt bulunamadı", "", "-"));
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

        var titleLabel = new Label(string.IsNullOrWhiteSpace(title) ? "-" : title);
        titleLabel.AddToClassList("history-item-title");

        var valueLabel = new Label(string.IsNullOrWhiteSpace(score) ? "-" : score);
        valueLabel.AddToClassList("history-item-score");

        top.Add(titleLabel);
        top.Add(valueLabel);

        var bottom = new Label(string.IsNullOrWhiteSpace(date) ? "-" : date);
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

        int completedStudentTotal = 0;
        int expectedStudentTotal = 0;

        foreach (var assignment in classAssignments)
        {
            if (assignment == null)
                continue;

            int completed = Mathf.Max(assignment.CompletedStudentCount, 0);

            int totalForAssignment = Mathf.Max(assignment.ClassStudentCount, 0);

            if (totalForAssignment <= 0)
            {
                totalForAssignment = Mathf.Max(
                    Mathf.Max(currentSelectedClass != null ? currentSelectedClass.StudentCount : 0, 0),
                    completed + Mathf.Max(assignment.IncompleteStudentCount, 0)
                );
            }

            completedStudentTotal += completed;
            expectedStudentTotal += totalForAssignment;
        }

        int completionPercent = expectedStudentTotal > 0
            ? Mathf.RoundToInt((completedStudentTotal / (float)expectedStudentTotal) * 100f)
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

        int successRate = student != null ? Mathf.Clamp(student.SuccessRatePercent, 0, 100) : 0;

        var successCell = new Label($"%{successRate}");

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

                // Burada artık tamamlanma durumunu öğrenci sonuçlarına göre filtreliyoruz.
                "completed" => a.CompletedStudentCount > 0,
                "incomplete" => a.IncompleteStudentCount > 0,

                _ => true
            };

            if (!include)
                continue;

            if (!string.IsNullOrWhiteSpace(q))
            {
                string title = (a.Title ?? "").ToLowerInvariant();
                string cls = (a.ClassName ?? "").ToLowerInvariant();
                string experiment = (a.ExperimentName ?? "").ToLowerInvariant();

                if (!title.Contains(q) && !cls.Contains(q) && !experiment.Contains(q))
                    continue;
            }

            int completed = Mathf.Max(0, a.CompletedStudentCount);
            int incomplete = Mathf.Max(0, a.IncompleteStudentCount);
            int percent = Mathf.Clamp(a.CompletionPercent, 0, 100);

            assignmentsCardsRow.Add(BuildAssignmentCard(
    a,
    string.IsNullOrWhiteSpace(a.Title) ? "-" : a.Title,
    string.IsNullOrWhiteSpace(a.ExperimentName) ? "-" : a.ExperimentName,
    "Başlangıç Seviyesi",
    GetRemainingDaysText(a),
    incomplete.ToString(),
    completed.ToString(),
    percent
));

            rendered++;
        }

        if (rendered == 0)
            assignmentsCardsRow.Add(new Label("Filtreye uygun ödev bulunamadı."));
    }

    private VisualElement BuildAssignmentCard(AssignmentDto assignment, string title, string unit, string difficulty, string dayCount, string incomplete, string complete, int percent)
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
        progressFill.style.width = new Length(Mathf.Clamp(percent, 0, 100), LengthUnit.Percent);

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

        detailBtn.clicked += () =>
        {
            OpenAssignmentResultsPage(assignment);
        };

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
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<JoinRequestListWrapper>("{\"items\":" + raw + "}");
        currentRequestItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<JoinRequestDto>();

        BuildRequestList();
        RefreshNotificationsBadge();
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
        StartCoroutine(FetchPendingJoinRequestsForNotifications());
        RefreshNotificationsBadge();
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
        StartCoroutine(FetchPendingJoinRequestsForNotifications());
        RefreshNotificationsBadge();
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
        if (timelineContents == null)
            return;

        if (assignmentItems == null || assignmentItems.Length == 0)
            return;

        DateTime visibleEndDate = visibleStartDate.AddDays(AssignmentDayCount - 1);

        var visibleAssignments = new List<TimelineAssignmentBlockData>();

        foreach (var item in assignmentItems)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.StartDate))
                continue;

            if (!DateTime.TryParse(item.StartDate, null, DateTimeStyles.RoundtripKind, out var parsedStart))
                continue;

            DateTime startDate = parsedStart.ToLocalTime().Date;
            int durationDays = Mathf.Max(item.DurationDays, 1);
            DateTime itemEndDate = startDate.AddDays(durationDays - 1);

            if (itemEndDate < visibleStartDate || startDate > visibleEndDate)
                continue;

            int startOffset = Mathf.Max(0, (startDate - visibleStartDate).Days);
            int endOffset = Mathf.Min(AssignmentDayCount - 1, (itemEndDate - visibleStartDate).Days);
            int visibleDuration = (endOffset - startOffset) + 1;

            visibleAssignments.Add(new TimelineAssignmentBlockData
            {
                Item = item,
                StartDate = startDate,
                EndDate = itemEndDate,
                StartOffset = startOffset,
                EndOffset = endOffset,
                VisibleDuration = visibleDuration
            });
        }

        visibleAssignments = visibleAssignments
            .OrderBy(x => x.StartOffset)
            .ThenByDescending(x => x.VisibleDuration)
            .ThenBy(x => x.Item != null ? x.Item.Title : "")
            .ToList();

        int[] rowEndOffsets = new int[AssignmentRowCount];

        for (int i = 0; i < rowEndOffsets.Length; i++)
            rowEndOffsets[i] = -1;

        foreach (var data in visibleAssignments)
        {
            int rowIndex = FindAvailableAssignmentTimelineRow(data.StartOffset, data.EndOffset, rowEndOffsets);

            if (rowIndex < 0)
                rowIndex = AssignmentRowCount - 1;

            rowEndOffsets[rowIndex] = Mathf.Max(rowEndOffsets[rowIndex], data.EndOffset);

            var row = timelineContents.Q<VisualElement>($"AssignmentTimelineRow_{rowIndex}");

            if (row == null)
                continue;

            var block = new VisualElement();
            block.AddToClassList("assignment-block");

            block.style.position = Position.Absolute;
            block.style.top = new Length(50, LengthUnit.Percent);
            block.style.left = new Length(data.StartOffset * (100f / AssignmentDayCount), LengthUnit.Percent);
            block.style.width = new Length(data.VisibleDuration * (100f / AssignmentDayCount), LengthUnit.Percent);
            block.style.height = 40;
            block.style.translate = new Translate(4, -20, 0);

            var titleLabel = new Label(data.Item.Title);
            titleLabel.AddToClassList("assignment-block-title");

            var classLabel = new Label(data.Item.ClassName);
            classLabel.AddToClassList("assignment-block-class");

            block.Add(titleLabel);
            block.Add(classLabel);

            block.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();

                string lesson = ResolveLessonNameByClassId(data.Item.ClassId);

                OpenAssignmentDetailsModal(
                    data.Item.Title,
                    data.Item.ClassName,
                    lesson,
                    data.StartDate,
                    data.Item.DurationDays,
                    string.IsNullOrWhiteSpace(data.Item.ExperimentName) ? "-" : data.Item.ExperimentName
                );
            });

            row.Add(block);
        }
    }

    private int FindAvailableAssignmentTimelineRow(int startOffset, int endOffset, int[] rowEndOffsets)
    {
        if (rowEndOffsets == null || rowEndOffsets.Length == 0)
            return -1;

        for (int i = 0; i < rowEndOffsets.Length; i++)
        {
            if (startOffset > rowEndOffsets[i])
                return i;
        }

        return -1;
    }

    private class TimelineAssignmentBlockData
    {
        public AssignmentDto Item;
        public DateTime StartDate;
        public DateTime EndDate;
        public int StartOffset;
        public int EndOffset;
        public int VisibleDuration;
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


    private void OpenAssignmentResultsPage(AssignmentDto assignment)
    {
        if (assignment == null)
            return;

        currentResultsAssignment = assignment;

        ShowPage("AssignmentResultsPage");
        SetMenuActive("ClassBtn");

        if (assignmentResultsTitleLabel != null)
            assignmentResultsTitleLabel.text = $"{SafeText(assignment.Title)} - Sonuçlar";

        if (assignmentResultsSummaryLabel != null)
        {
            assignmentResultsSummaryLabel.text =
                $"Tamamlayan: {Mathf.Max(assignment.CompletedStudentCount, 0)} | " +
                $"Tamamlamayan: {Mathf.Max(assignment.IncompleteStudentCount, 0)} | " +
                $"Tamamlanma: %{Mathf.Clamp(assignment.CompletionPercent, 0, 100)}";
        }

        if (assignmentResultsSelectedStudentLabel != null)
            assignmentResultsSelectedStudentLabel.text = "Cevapları görmek için bir öğrenci seç.";

        assignmentResultsStudentsContainer?.Clear();
        assignmentResultsAnswersContainer?.Clear();

        StartCoroutine(FetchCompletedStudentsForAssignment(assignment.Id));
    }

    private void CloseAssignmentResultsPage()
    {
        currentResultsAssignment = null;

        ShowPage("ClassDetailsPage");
        SetMenuActive("ClassBtn");
        SetClassDetailsTab("assignments");
    }

    private IEnumerator FetchCompletedStudentsForAssignment(int assignmentId)
    {
        if (router == null)
            yield break;

        string path = assignmentCompletedStudentsPathTemplate
            .Replace("{assignmentId}", assignmentId.ToString());

        string url = router.ApiBaseUrl + path;

        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ASSIGNMENT RESULTS STUDENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");

            assignmentResultsStudentsContainer?.Clear();
            assignmentResultsStudentsContainer?.Add(new Label("Tamamlayan öğrenciler getirilemedi."));
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<CompletedStudentResultListWrapper>("{\"items\":" + raw + "}");

        var items = wrapped != null && wrapped.items != null
            ? wrapped.items
            : Array.Empty<CompletedStudentResultDto>();

        BuildCompletedStudentResults(items);
    }

    private void BuildCompletedStudentResults(CompletedStudentResultDto[] items)
    {
        if (assignmentResultsStudentsContainer == null)
            return;

        assignmentResultsStudentsContainer.Clear();

        if (items == null || items.Length == 0)
        {
            assignmentResultsStudentsContainer.Add(new Label("Bu ödevi henüz tamamlayan öğrenci yok."));
            return;
        }

        foreach (var item in items)
        {
            if (item == null)
                continue;

            assignmentResultsStudentsContainer.Add(BuildCompletedStudentResultRow(item));
        }
    }

    private VisualElement BuildCompletedStudentResultRow(CompletedStudentResultDto item)
    {
        string fullName = $"{item.StudentName} {item.StudentSurname}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = "-";

        var row = new VisualElement();
        row.AddToClassList("assignment-result-student-row");

        var left = new VisualElement();
        left.AddToClassList("assignment-result-student-left");

        var avatar = new Label(BuildInitials(item.StudentName, item.StudentSurname));
        avatar.AddToClassList("student-avatar");

        var nameWrap = new VisualElement();
        nameWrap.AddToClassList("assignment-result-student-info");

        var nameLabel = new Label(fullName);
        nameLabel.AddToClassList("assignment-result-student-name");

        var dateLabel = new Label(FormatDate(item.CompletedAt));
        dateLabel.AddToClassList("assignment-result-student-date");

        nameWrap.Add(nameLabel);
        nameWrap.Add(dateLabel);

        left.Add(avatar);
        left.Add(nameWrap);

        var score = new Label($"%{Mathf.Clamp(item.Score, 0, 100)}");
        score.AddToClassList("assignment-result-score");

        row.Add(left);
        row.Add(score);

        row.RegisterCallback<ClickEvent>(_ =>
        {
            if (assignmentResultsSelectedStudentLabel != null)
                assignmentResultsSelectedStudentLabel.text = $"{fullName} - Cevaplar";

            StartCoroutine(FetchStudentAnswersForResult(item.ResultId));
        });

        return row;
    }

    private IEnumerator FetchStudentAnswersForResult(int resultId)
    {
        if (router == null)
            yield break;

        string path = assignmentResultAnswersPathTemplate
            .Replace("{resultId}", resultId.ToString());

        string url = router.ApiBaseUrl + path;

        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ASSIGNMENT RESULT ANSWERS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");

            assignmentResultsAnswersContainer?.Clear();
            assignmentResultsAnswersContainer?.Add(new Label("Öğrenci cevapları getirilemedi."));
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<StudentAnswerListWrapper>("{\"items\":" + raw + "}");

        var answers = wrapped != null && wrapped.items != null
            ? wrapped.items
            : Array.Empty<StudentAnswerDto>();

        BuildStudentAnswerCards(answers);
    }

    private void BuildStudentAnswerCards(StudentAnswerDto[] answers)
    {
        if (assignmentResultsAnswersContainer == null)
            return;

        assignmentResultsAnswersContainer.Clear();

        if (answers == null || answers.Length == 0)
        {
            assignmentResultsAnswersContainer.Add(new Label("Bu öğrenci için kayıtlı cevap bulunamadı."));
            return;
        }

        for (int i = 0; i < answers.Length; i++)
        {
            assignmentResultsAnswersContainer.Add(BuildStudentAnswerCard(i + 1, answers[i]));
        }
    }

    private VisualElement BuildStudentAnswerCard(int order, StudentAnswerDto answer)
    {
        var card = new VisualElement();
        card.AddToClassList("student-answer-card");

        var top = new VisualElement();
        top.AddToClassList("student-answer-top");

        var questionTitle = new Label($"Soru {order}");
        questionTitle.AddToClassList("student-answer-question-no");

        var status = new Label(answer.IsCorrect ? "Doğru" : "Yanlış");
        status.AddToClassList(answer.IsCorrect ? "answer-status-correct" : "answer-status-wrong");

        top.Add(questionTitle);
        top.Add(status);

        var question = new Label(SafeText(answer.QuestionText));
        question.AddToClassList("student-answer-question");

        var studentAnswer = new Label("Öğrenci Cevabı: " + SafeText(answer.StudentAnswer));
        studentAnswer.AddToClassList("student-answer-text");

        var correctAnswer = new Label("Doğru Cevap: " + SafeText(answer.CorrectAnswer));
        correctAnswer.AddToClassList("student-answer-correct");

        card.Add(top);
        card.Add(question);
        card.Add(studentAnswer);
        card.Add(correctAnswer);

        return card;
    }

    // ----------------------------------------------

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

    private void SeedCalendarData()
    {
        // Intentionally left blank so categories and events start empty.
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

        if (!string.IsNullOrWhiteSpace(manual) && ColorUtility.TryParseHtmlString(manual, out _))
            return manual;

        if (!string.IsNullOrWhiteSpace(calSelectedPresetColor) && ColorUtility.TryParseHtmlString(calSelectedPresetColor, out _))
            return calSelectedPresetColor;

        return CalendarDefaultCategoryColors[0];
    }

    private string ResolveCalendarCategoryTextColor(string backgroundColor)
    {
        if (!string.IsNullOrWhiteSpace(calSelectedTextColor) && ColorUtility.TryParseHtmlString(calSelectedTextColor, out _))
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
        if (cat != null && !string.IsNullOrWhiteSpace(cat.TextColor) && ColorUtility.TryParseHtmlString(cat.TextColor, out _))
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
        if (!ColorUtility.TryParseHtmlString(backgroundHex, out var bg))
            return "#ffffff";

        float luminance = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
        return luminance > 0.62f ? "#111111" : "#ffffff";
    }

    private string MakeCalendarTypeKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "kategori";

        string key = input.Trim().ToLowerInvariant();
        key = key.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        key = new string(key.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray());
        key = string.Join("-", key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(key) ? "kategori" : key;
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
        if (ColorUtility.TryParseHtmlString(hex, out var c))
            return c;

        return new Color32(127, 140, 141, 255);
    }

    #endregion

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

    private void OnGoSimulationClicked()
    {
        SceneManager.LoadScene("SolarSystemScene", LoadSceneMode.Single);
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
        public int CompletedStudentCount;
        public int IncompleteStudentCount;
        public int ClassStudentCount;
        public int CompletionPercent;
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

    private class TeacherJoinRequestNotificationItem
    {
        public int ClassId;
        public string ClassName;
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

        public int SuccessRatePercent;
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
    private class TeacherRoleRequestStateDto
    {
        public bool HasRequest;
        public int Id;
        public string Status;
        public string Note;
        public string DecisionNote;
        public string RequestedAtUtc;
        public string ReviewedAtUtc;
        public int ReviewedByUserId;
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
    private class CompletedStudentResultDto
    {
        public int ResultId;
        public int StudentId;
        public string StudentName;
        public string StudentSurname;
        public int CorrectCount;
        public int WrongCount;
        public int TotalQuestionCount;
        public int Score;
        public string CompletedAt;
    }

    [Serializable]
    private class CompletedStudentResultListWrapper
    {
        public CompletedStudentResultDto[] items;
    }

    [Serializable]
    private class StudentAnswerDto
    {
        public string QuestionText;
        public string StudentAnswer;
        public string CorrectAnswer;
        public bool IsCorrect;
    }

    [Serializable]
    private class StudentAnswerListWrapper
    {
        public StudentAnswerDto[] items;
    }
}