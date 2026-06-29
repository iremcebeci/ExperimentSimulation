using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class IndependentUserDashboardController : MonoBehaviour
{
    private AppRouter router;
    private VisualElement root;
    private VisualElement mainContent;

    [Header("Controllers")]
    [SerializeField] private DashboardSidebarController sidebarController;
    [SerializeField] private DashboardsHeaderController headerController;

    [Header("API Paths")]
    [SerializeField] private string myAssignmentsPath = "/api/Assignment/my";
    [SerializeField] private string experimentsPath = "/api/Experiment";
    [SerializeField] private string userPath = "/api/User";
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string teacherRoleRequestPath = "/api/User/teacher-role-request";
    [SerializeField] private string classJoinPath = "/api/Class/join";
    [SerializeField] private string classMyPath = "/api/Class/my";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";
    [SerializeField] private string calendarCategoriesPath = "/api/Calendar/categories";
    [SerializeField] private string calendarEventsPath = "/api/Calendar/events";

    // Home
    private Label welcomeUsernameLabel;
    private VisualElement homePage;
    private Label homeActiveDayValueLabel;
    private Label homeStreakValueLabel;
    private Label homeCompletedExperimentsValueLabel;
    private ScrollView homeSummaryScroll;
    private Label homeChartPeakInfoLabel;
    private readonly List<VisualElement> homeChartBars = new();
    private readonly List<Label> homeChartValueLabels = new();
    private readonly float[] homeWeeklyHours = new float[7];

    // Progress
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
    private readonly List<string> progressLessonTabs = new();

    // Activity
    private VisualElement personalActivityPage;
    private VisualElement personalActivityFeed;
    private Button actTabAllBtn;
    private Button actTabExperimentBtn;
    private Button actTabAssignmentBtn;
    private Button actTabProgressBtn;
    private Button actTabParticipationBtn;
    private TextField personalActivitySearchInput;
    private DropdownField actDateFilterDropdown;
    private string personalActivityFilterMode = "all";
    private string personalActivitySearchQuery = "";

    // Profile
    private VisualElement profilePage;
    private Label profileAvatarLabel;
    private Label profileNameLabel;
    private Label profileRoleLabel;
    private Label profileStatusLabel;
    private Label profileMailLabel;
    private Label profileJoinDateLabel;
    private Label profileLastLoginLabel;
    private VisualElement profileStatsGrid;
    private Button profileHomeBtn;
    private Button profileProgressBtn;
    private Button profileLogoutBtn;

    // Settings modal
    private VisualElement settingsModal;
    private Button settingsModalCloseBtn;
    private Button settingsCancelBtn;
    private Button settingsSaveProfileBtn;
    private TextField settingsNameInput;
    private TextField settingsSurnameInput;
    private TextField settingsEmailInput;
    private TextField settingsPhoneInput;
    private VisualElement settingsRoleActionsSection;
    private Button settingsBecomeTeacherBtn;
    private Label settingsTeacherStateLabel;
    private TextField settingsClassCodeInput;
    private Button settingsBecomeStudentBtn;
    private VisualElement settingsPendingClassList;
    private Label settingsStatusLabel;
    private bool settingsTeacherRequestPending;
    private string settingsTeacherRequestStatus = "None";
    private string settingsTeacherDecisionNote = "";
    private UserUpdatePayloadDto settingsUserSnapshot;

    // Cached data
    private AssignmentDto[] assignmentItems = Array.Empty<AssignmentDto>();
    private ExperimentDto[] experimentItems = Array.Empty<ExperimentDto>();
    private ClassActivityDto[] personalActivityItems = Array.Empty<ClassActivityDto>();
    private ProfileMeDto profileMe;
    private DashboardNotificationCenter notificationCenter;
    private readonly List<RoleChangeNotificationDto> roleChangeNotificationItems = new();
    private Coroutine sessionHeartbeatRoutine;

    // Calendar
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
    private static readonly CultureInfo trCulture = new CultureInfo("tr-TR");
    private const int IndependentRoleId = 3;

    public void Bind(AppRouter router, VisualElement independentView)
    {
        this.router = router;
        root = independentView;

        if (root == null)
        {
            Debug.LogError("[IndependentUserDashboardController] root null.");
            return;
        }

        mainContent = root.Q<VisualElement>("MainContent");
        if (mainContent == null)
        {
            Debug.LogError("[IndependentUserDashboardController] MainContent not found (name=\"MainContent\").");
            return;
        }

        // Home
        welcomeUsernameLabel = root.Q<Label>("WelcomeUsernameLabel");
        var welcomeMessageLabel = root.Q<Label>("WelcomeMessageLabel");

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

        BindHomePage();
        BindProgressPage();
        BindPersonalActivityPage();
        BindProfilePage();
        BindSettingsModal();
        BindCalendarPage();
        BindMenuButtons();
        BindNotifications();

        ShowPage("HomePage");
        SetMenuActive("HomeBtn");

        if (sessionHeartbeatRoutine != null)
            StopCoroutine(sessionHeartbeatRoutine);
        sessionHeartbeatRoutine = StartCoroutine(SessionHeartbeatLoop());

        StartCoroutine(InitialLoad());
    }

    private void HandleHeaderUserLoaded()
    {
        if (welcomeUsernameLabel != null)
            welcomeUsernameLabel.text = $"Merhaba, {router.CurrentName} {router.CurrentSurname}!";
    }

    private IEnumerator InitialLoad()
    {
        yield return StartCoroutine(FetchAllExperiments());
        yield return StartCoroutine(FetchMyAssignments());
        yield return StartCoroutine(FetchPersonalActivity());
        yield return StartCoroutine(LoadProfilePageData());
        yield return StartCoroutine(FetchWeeklySessionHours());

        ApplyHomeDashboardMetrics();
        RebuildProgressTabs();
        RenderProgressColumns();
        RenderPersonalActivityFeed();
    }

    private IEnumerator RefreshHomeSessionChart()
    {
        yield return StartCoroutine(FetchWeeklySessionHours());
        ApplyHomeDashboardMetrics();
    }

    private void BindHomePage()
    {
        homePage = root.Q<VisualElement>("StudentHomePage");
        if (homePage == null)
            return;

        homeActiveDayValueLabel = homePage.Q<Label>("TcTotalClassValueLabel");
        homeStreakValueLabel = homePage.Q<Label>("TcActiveAssignmentValueLabel");
        homeCompletedExperimentsValueLabel = homePage.Q<Label>("TcTotalStudentValueLabel");
        homeSummaryScroll = homePage.Q<ScrollView>("TcSummaryScroll");
        homeChartPeakInfoLabel = homePage.Q<Label>("TcChartPeakInfoLabel");

        homeChartBars.Clear();
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarMon"));
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarTue"));
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarWed"));
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarThu"));
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarFri"));
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarSat"));
        homeChartBars.Add(homePage.Q<VisualElement>("TcBarSun"));

        homeChartValueLabels.Clear();
        homeChartValueLabels.AddRange(homePage.Query<Label>(className: "cg-bar-value").ToList());
    }

    private void ApplyHomeDashboardMetrics()
    {
        int totalActiveDays = profileMe != null ? Mathf.Max(profileMe.totalActiveDays, 0) : 0;
        int streakDays = profileMe != null ? Mathf.Max(profileMe.currentActiveStreakDays, 0) : 0;
        int completedExperiments = assignmentItems.Count(a => a != null && !a.IsActive);

        if (homeActiveDayValueLabel != null) homeActiveDayValueLabel.text = totalActiveDays.ToString();
        if (homeStreakValueLabel != null) homeStreakValueLabel.text = streakDays.ToString();
        if (homeCompletedExperimentsValueLabel != null) homeCompletedExperimentsValueLabel.text = completedExperiments.ToString();

        var lessonCounts = assignmentItems
            .Where(a => a != null)
            .GroupBy(a => SafeText(a.ClassName))
            .Select(g => new { lesson = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .FirstOrDefault();

        if (lessonCounts == null)
            SetHomeSummaryItem(0, "-", "Henüz deney aktivitesi yok");
        else
            SetHomeSummaryItem(0, lessonCounts.lesson, $"Toplam {lessonCounts.count} deney ataması");

        var latestCompleted = assignmentItems
            .Where(a => a != null && !a.IsActive)
            .Select(a => new { item = a, due = GetAssignmentDueAt(a) })
            .Where(x => x.due.HasValue)
            .OrderByDescending(x => x.due.Value)
            .FirstOrDefault();

        if (latestCompleted == null)
            SetHomeSummaryItem(1, "-", "Henüz tamamlanan deney yok");
        else
            SetHomeSummaryItem(1, SafeText(latestCompleted.item.ExperimentName), latestCompleted.due.Value.ToString("dd MMM HH:mm"));

        int todayActivities = CountActivitiesForDate(DateTime.Today);
        int yesterdayActivities = CountActivitiesForDate(DateTime.Today.AddDays(-1));
        SetHomeSummaryItem(2, todayActivities.ToString(), $"Dün: {yesterdayActivities}");

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

    private int CountActivitiesForDate(DateTime date)
    {
        if (personalActivityItems == null)
            return 0;

        int count = 0;
        foreach (var item in personalActivityItems)
        {
            if (item == null) continue;
            var dt = ParseDate(item.OccurredAt);
            if (dt.Date == date.Date)
                count++;
        }

        return count;
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
                homeChartBars[i].style.height = 14f + (ratio * 96f);
            }
        }

        if (homeChartPeakInfoLabel != null)
            homeChartPeakInfoLabel.text = peak <= 0f ? "Tepe: -" : $"Tepe: {peak:0.0} saat ({dayNames[Mathf.Clamp(peakIndex, 0, dayNames.Length - 1)]})";
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

        var counts = progressPageContent.Query<Label>(className: "progress-column-count").ToList();
        if (counts.Count >= 3)
        {
            progressNotStartedCountLabel = counts[0];
            progressInProgressCountLabel = counts[1];
            progressCompletedCountLabel = counts[2];
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

    private void RebuildProgressTabs()
    {
        if (progressTabsBar == null)
            return;

        progressTabsBar.Clear();
        progressLessonTabs.Clear();

        progressLessonTabs.Add("Fen");
        progressLessonTabs.Add("Fizik");
        progressLessonTabs.Add("Kimya");
        progressLessonTabs.Add("Biyoloji");
        progressLessonTabs.Add("Matematik");

        if (string.IsNullOrWhiteSpace(progressSelectedLesson) || !progressLessonTabs.Contains(progressSelectedLesson))
            progressSelectedLesson = progressLessonTabs[0];

        foreach (var lesson in progressLessonTabs)
        {
            var btn = new Button(() =>
            {
                progressSelectedLesson = lesson;
                SetProgressTabActive();
                RenderProgressColumns();
            });
            btn.AddToClassList("progress-tab");
            if (lesson == progressSelectedLesson)
                btn.AddToClassList("active");

            var icon = new Label("🔬");
            icon.AddToClassList("progress-tab-icon");
            var text = new Label(lesson);
            text.AddToClassList("progress-tab-text");
            btn.Add(icon);
            btn.Add(text);
            progressTabsBar.Add(btn);
        }
    }

    private void SetProgressTabActive()
    {
        if (progressTabsBar == null)
            return;

        var buttons = progressTabsBar.Query<Button>().ToList();
        foreach (var btn in buttons)
            btn.RemoveFromClassList("active");

        foreach (var btn in buttons)
        {
            var label = btn.Q<Label>(className: "progress-tab-text");
            if (label != null && label.text == progressSelectedLesson)
            {
                btn.AddToClassList("active");
                break;
            }
        }
    }

    private void RenderProgressColumns()
    {
        var ns = progressNotStartedScroll != null ? progressNotStartedScroll.contentContainer : null;
        var ip = progressInProgressScroll != null ? progressInProgressScroll.contentContainer : null;
        var cp = progressCompletedScroll != null ? progressCompletedScroll.contentContainer : null;

        ns?.Clear();
        ip?.Clear();
        cp?.Clear();

        if (experimentItems == null || experimentItems.Length == 0)
        {
            ns?.Add(BuildProgressEmptyCard("Henüz aktif deney bulunamadı."));
            ip?.Add(BuildProgressEmptyCard("Henüz aktif deney bulunamadı."));
            cp?.Add(BuildProgressEmptyCard("Henüz aktif deney bulunamadı."));
            SetProgressCounts(0, 0, 0);
            UpdateProgressSummary(0, 0, 0);
            return;
        }

        string q = (progressSearchField != null ? progressSearchField.value : "") ?? "";
        q = q.Trim().ToLowerInvariant();

        string status = progressStatusDropdown != null ? (progressStatusDropdown.value ?? "Tüm Durumlar") : "Tüm Durumlar";
        string difficulty = progressDifficultyDropdown != null ? (progressDifficultyDropdown.value ?? "Tüm Zorluklar") : "Tüm Zorluklar";

        int nsCount = 0, ipCount = 0, cpCount = 0;

        foreach (var exp in experimentItems)
        {
            if (exp == null) continue;

            string lesson = ResolveExperimentSubject(exp);
            if (!string.IsNullOrWhiteSpace(progressSelectedLesson) && lesson != progressSelectedLesson)
                continue;

            string title = SafeText(exp.ExperimentName);
            if (!string.IsNullOrWhiteSpace(q) && !title.ToLowerInvariant().Contains(q) && !lesson.ToLowerInvariant().Contains(q))
                continue;

            var relatedAssignments = GetAssignmentsForExperiment(exp);
            string itemStatus = GetExperimentProgressStatus(relatedAssignments);
            if (status != "Tüm Durumlar" && itemStatus != status)
                continue;

            string itemDifficulty = GetExperimentDifficulty(relatedAssignments);
            if (difficulty != "Tüm Zorluklar" && itemDifficulty != difficulty)
                continue;

            int inProgressPercent = GetExperimentInProgressPercent(relatedAssignments);
            var card = BuildProgressCard(title, lesson, itemDifficulty, itemStatus, inProgressPercent);

            if (itemStatus == "Başlanmadı")
            {
                ns?.Add(card);
                nsCount++;
            }
            else if (itemStatus == "Devam Ediyor")
            {
                ip?.Add(card);
                ipCount++;
            }
            else
            {
                cp?.Add(card);
                cpCount++;
            }
        }

        if (nsCount == 0) ns?.Add(BuildProgressEmptyCard("Başlanmadı deney yok."));
        if (ipCount == 0) ip?.Add(BuildProgressEmptyCard("Devam eden deney yok."));
        if (cpCount == 0) cp?.Add(BuildProgressEmptyCard("Tamamlanan deney yok."));

        SetProgressCounts(nsCount, ipCount, cpCount);
        UpdateProgressSummary(nsCount, ipCount, cpCount);
    }

    private void SetProgressCounts(int ns, int ip, int cp)
    {
        if (progressNotStartedCountLabel != null) progressNotStartedCountLabel.text = ns.ToString();
        if (progressInProgressCountLabel != null) progressInProgressCountLabel.text = ip.ToString();
        if (progressCompletedCountLabel != null) progressCompletedCountLabel.text = cp.ToString();
    }

    private void UpdateProgressSummary(int ns, int ip, int cp)
    {
        int total = ns + ip + cp;
        int overallPercent = total <= 0 ? 0 : Mathf.RoundToInt((cp / (float)total) * 100f);

        if (overallPercentLabel != null)
            overallPercentLabel.text = $"%{overallPercent}";
        SetFillWidth(overallFillBar, overallPercent);

        string lesson = string.IsNullOrWhiteSpace(progressSelectedLesson) ? "Fen" : progressSelectedLesson;
        int lessonTotal = ns + ip + cp;
        int lessonPercent = lessonTotal <= 0 ? 0 : Mathf.RoundToInt((cp / (float)lessonTotal) * 100f);

        if (subjectProgressLabel != null)
            subjectProgressLabel.text = $"{lesson} İlerlemesi";
        if (subjectPercentLabel != null)
            subjectPercentLabel.text = $"%{lessonPercent}";
        if (subjectSubLabel != null)
            subjectSubLabel.text = $"{cp} tamamlandı - {ip} devam ediyor";
        SetFillWidth(subjectFillBar, lessonPercent);
    }

    private void SetFillWidth(VisualElement fill, int pct)
    {
        if (fill == null)
            return;

        int clamped = Mathf.Clamp(pct, 0, 100);
        fill.style.width = new Length(clamped, LengthUnit.Percent);
    }

    private VisualElement BuildProgressCard(string title, string lesson, string difficulty, string status, int inProgressPercent)
    {
        var card = new VisualElement();
        card.AddToClassList("exp-card");

        var top = new VisualElement();
        top.AddToClassList("exp-card-top");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("exp-card-title");
        top.Add(titleLabel);

        var badge = new Label(difficulty);
        badge.AddToClassList("exp-card-badge");
        badge.AddToClassList(difficulty switch
        {
            "Kolay" => "easy",
            "Orta" => "medium",
            _ => "hard"
        });
        top.Add(badge);
        card.Add(top);

        var desc = new Label($"Branş: {lesson}");
        desc.AddToClassList("exp-card-desc");
        card.Add(desc);

        if (status == "Devam Ediyor")
        {
            int progress = Mathf.Clamp(inProgressPercent, 1, 99);
            var progressWrap = new VisualElement();
            progressWrap.AddToClassList("exp-card-progress");

            var row = new VisualElement();
            row.AddToClassList("exp-card-progress-row");
            var rowLabel = new Label("İlerleme");
            rowLabel.AddToClassList("exp-card-progress-label");
            var rowVal = new Label($"%{progress}");
            rowVal.AddToClassList("exp-card-progress-val");
            row.Add(rowLabel);
            row.Add(rowVal);

            var track = new VisualElement();
            track.AddToClassList("exp-card-track");
            var fill = new VisualElement();
            fill.AddToClassList("exp-card-fill");
            fill.style.width = new Length(progress, LengthUnit.Percent);
            track.Add(fill);

            progressWrap.Add(row);
            progressWrap.Add(track);
            card.Add(progressWrap);
        }
        else if (status == "Tamamlandı")
        {
            var score = new Label("Skor: %100");
            score.AddToClassList("exp-card-score");
            card.Add(score);
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

    private string GetAssignmentProgressStatus(AssignmentDto a)
    {
        if (a == null)
            return "Başlanmadı";

        if (!TryParseDate(a.StartDate, out var startDate))
            return a.IsActive ? "Devam Ediyor" : "Tamamlandı";

        int duration = Mathf.Max(a.DurationDays, 1);
        DateTime endExclusive = startDate.Date.AddDays(duration);

        if (!a.IsActive || DateTime.Today >= endExclusive)
            return "Tamamlandı";

        if (DateTime.Today < startDate.Date)
            return "Başlanmadı";

        return "Devam Ediyor";
    }

    private int GetInProgressPercent(AssignmentDto a)
    {
        if (a == null)
            return 0;

        if (!TryParseDate(a.StartDate, out var startDate))
            return a.IsActive ? 50 : 100;

        int duration = Mathf.Max(a.DurationDays, 1);
        int elapsed = (DateTime.Today - startDate.Date).Days + 1;
        float ratio = Mathf.Clamp01(elapsed / (float)duration);
        return Mathf.RoundToInt(ratio * 100f);
    }

    private string GetAssignmentDifficulty(AssignmentDto a)
    {
        int days = a != null ? Mathf.Max(a.DurationDays, 1) : 1;
        if (days <= 2) return "Kolay";
        if (days <= 5) return "Orta";
        return "Zor";
    }

    private string ResolveAssignmentSubject(AssignmentDto a)
    {
        string source = $"{a?.ClassName} {a?.Title} {a?.ExperimentName}".ToLowerInvariant();

        if (source.Contains("fizik")) return "Fizik";
        if (source.Contains("kimya")) return "Kimya";
        if (source.Contains("biyoloji")) return "Biyoloji";
        if (source.Contains("matematik") || source.Contains("geometri") || source.Contains("cebir")) return "Matematik";
        if (source.Contains("fen")) return "Fen";

        return "Fen";
    }

    private AssignmentDto[] GetAssignmentsForExperiment(ExperimentDto exp)
    {
        if (assignmentItems == null || assignmentItems.Length == 0)
            return Array.Empty<AssignmentDto>();

        return assignmentItems
            .Where(a => a != null &&
                        (a.ExperimentId == exp.Id ||
                         string.Equals(SafeText(a.ExperimentName), SafeText(exp.ExperimentName), StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private string GetExperimentProgressStatus(AssignmentDto[] related)
    {
        if (related == null || related.Length == 0)
            return "Başlanmadı";

        if (related.Any(a => a != null && !a.IsActive))
            return "Tamamlandı";

        if (related.Any(a => a != null && a.IsActive))
            return "Devam Ediyor";

        return "Başlanmadı";
    }

    private string GetExperimentDifficulty(AssignmentDto[] related)
    {
        var first = related != null ? related.FirstOrDefault(a => a != null) : null;
        return GetAssignmentDifficulty(first);
    }

    private int GetExperimentInProgressPercent(AssignmentDto[] related)
    {
        if (related == null || related.Length == 0)
            return 0;

        var active = related.FirstOrDefault(a => a != null && a.IsActive);
        if (active == null)
            return related.Any(a => a != null && !a.IsActive) ? 100 : 0;

        return GetInProgressPercent(active);
    }

    private string ResolveExperimentSubject(ExperimentDto exp)
    {
        string source = $"{exp?.LessonName} {exp?.UnitName} {exp?.ExperimentName}".ToLowerInvariant();

        if (source.Contains("fizik")) return "Fizik";
        if (source.Contains("kimya")) return "Kimya";
        if (source.Contains("biyoloji")) return "Biyoloji";
        if (source.Contains("matematik") || source.Contains("geometri") || source.Contains("cebir")) return "Matematik";
        if (source.Contains("fen")) return "Fen";

        return "Fen";
    }

    private int ToMondayIndex(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            DayOfWeek.Sunday => 6,
            _ => 0
        };
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

        BindActivityTabButton(actTabAllBtn, "all");
        BindActivityTabButton(actTabExperimentBtn, "experiment");
        BindActivityTabButton(actTabAssignmentBtn, "assignment");
        BindActivityTabButton(actTabProgressBtn, "account");
        BindActivityTabButton(actTabParticipationBtn, "participation");

        if (personalActivitySearchInput != null)
            personalActivitySearchInput.RegisterValueChangedCallback(evt =>
            {
                personalActivitySearchQuery = evt.newValue ?? "";
                RenderPersonalActivityFeed();
            });

        if (actDateFilterDropdown != null)
            actDateFilterDropdown.RegisterValueChangedCallback(_ => RenderPersonalActivityFeed());
    }

    private void BindActivityTabButton(Button button, string mode)
    {
        if (button == null)
            return;

        button.clicked += () =>
        {
            personalActivityFilterMode = mode;
            SetActivityTabActive(button);
            RenderPersonalActivityFeed();
        };
    }

    private void SetActivityTabActive(Button active)
    {
        var tabs = new[] { actTabAllBtn, actTabExperimentBtn, actTabAssignmentBtn, actTabProgressBtn, actTabParticipationBtn };
        foreach (var tab in tabs)
            tab?.RemoveFromClassList("active");

        active?.AddToClassList("active");
    }

    private void RenderPersonalActivityFeed()
    {
        if (personalActivityFeed == null)
            return;

        personalActivityFeed.Clear();

        var filtered = (personalActivityItems ?? Array.Empty<ClassActivityDto>())
            .Where(i => i != null)
            .Where(MatchesPersonalActivityFilter)
            .Where(MatchesPersonalActivitySearch)
            .Where(i => MatchesPersonalActivityDateFilter(ParseDate(i.OccurredAt)))
            .OrderByDescending(i => ParseDate(i.OccurredAt))
            .ToList();

        if (filtered.Count == 0)
        {
            personalActivityFeed.Add(new Label("Bu filtrede aktivite bulunamadı."));
            return;
        }

        string currentDateHeader = null;
        foreach (var item in filtered)
        {
            var dt = ParseDate(item.OccurredAt);
            string dateHeader = dt.ToString("dd MMMM yyyy", new CultureInfo("tr-TR"));
            if (dateHeader != currentDateHeader)
            {
                currentDateHeader = dateHeader;
                var divider = new Label($"📌 {dateHeader}");
                divider.AddToClassList("act-date-divider");
                personalActivityFeed.Add(divider);
            }

            personalActivityFeed.Add(BuildActivityItemCard(item, dt));
        }
    }

    private VisualElement BuildActivityItemCard(ClassActivityDto item, DateTime dt)
    {
        var card = new VisualElement();
        card.AddToClassList("act-item");

        var avatar = new Label(BuildInitials(item.ActorName));
        avatar.AddToClassList("act-avatar");

        var body = new VisualElement();
        body.AddToClassList("act-item-body");

        var top = new VisualElement();
        top.AddToClassList("act-item-top");

        var badge = new Label(GetPersonalBadgeText(item.Type));
        badge.AddToClassList("act-type-badge");
        AddPersonalBadgeVariant(badge, item.Type);

        var time = new Label(dt == DateTime.MinValue ? "-" : dt.ToString("HH:mm"));
        time.AddToClassList("act-item-time");

        top.Add(badge);
        top.Add(time);

        var title = new Label(SafeText(item.Title));
        title.AddToClassList("act-item-title");

        var desc = new Label(SafeText(item.Description));
        desc.AddToClassList("act-item-desc");

        body.Add(top);
        body.Add(title);
        body.Add(desc);

        card.Add(avatar);
        card.Add(body);
        return card;
    }

    private bool MatchesPersonalActivityFilter(ClassActivityDto item)
    {
        if (item == null)
            return false;

        string type = item.Type ?? "";
        return personalActivityFilterMode switch
        {
            "experiment" => string.Equals(type, "ClassCreated", StringComparison.OrdinalIgnoreCase),
            "assignment" => string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase),
            "account" => string.Equals(type, "AccountCreated", StringComparison.OrdinalIgnoreCase),
            "progress" => string.Equals(type, "Achievement", StringComparison.OrdinalIgnoreCase),
            "participation" => string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private bool MatchesPersonalActivitySearch(ClassActivityDto item)
    {
        string q = (personalActivitySearchQuery ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(q))
            return true;

        string title = (item.Title ?? "").ToLowerInvariant();
        string desc = (item.Description ?? "").ToLowerInvariant();
        string actor = (item.ActorName ?? "").ToLowerInvariant();
        return title.Contains(q) || desc.Contains(q) || actor.Contains(q);
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

        var quickActions = profilePage.Q<VisualElement>(className: "teacher-quick-actions");
        if (quickActions == null)
            return;

        var buttons = quickActions.Query<Button>().ToList();
        if (buttons.Count > 0) profileHomeBtn = buttons[0];
        if (buttons.Count > 1) profileProgressBtn = buttons[1];
        if (buttons.Count > 2) profileLogoutBtn = buttons[2];

        if (profileHomeBtn != null)
            profileHomeBtn.clicked += () =>
            {
                SetMenuActive("HomeBtn");
                ShowPage("HomePage");
                StartCoroutine(RefreshHomeSessionChart());
            };

        if (profileProgressBtn != null)
            profileProgressBtn.clicked += () =>
            {
                SetMenuActive("ProgressBtn");
                ShowPage("ProgressPage");
                StartCoroutine(LoadProgressPageData(forceRefresh: true));
            };

        if (profileLogoutBtn != null)
            profileLogoutBtn.clicked += () => StartCoroutine(EndSessionAndLogout());
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

        settingsRoleActionsSection = root.Q<VisualElement>("SettingsRoleActionsSection");
        settingsBecomeTeacherBtn = root.Q<Button>("SettingsBecomeTeacherBtn");
        settingsTeacherStateLabel = root.Q<Label>("SettingsTeacherStateLabel");
        settingsClassCodeInput = root.Q<TextField>("SettingsClassCodeInput");
        settingsBecomeStudentBtn = root.Q<Button>("SettingsBecomeStudentBtn");
        settingsPendingClassList = root.Q<VisualElement>("SettingsPendingClassList");
        settingsStatusLabel = root.Q<Label>("SettingsStatusLabel");

        settingsTeacherRequestPending = false;
        settingsTeacherRequestStatus = "None";
        settingsTeacherDecisionNote = "";
        UpdateTeacherRequestState();
        ShowSettingsStatus(string.Empty);

        if (settingsModalCloseBtn != null)
            settingsModalCloseBtn.clicked += CloseSettingsModal;
        if (settingsCancelBtn != null)
            settingsCancelBtn.clicked += CloseSettingsModal;
        if (settingsSaveProfileBtn != null)
            settingsSaveProfileBtn.clicked += () => StartCoroutine(SaveSettingsProfile());
        if (settingsBecomeTeacherBtn != null)
            settingsBecomeTeacherBtn.clicked += () => StartCoroutine(SubmitTeacherRequest());
        if (settingsBecomeStudentBtn != null)
            settingsBecomeStudentBtn.clicked += () => StartCoroutine(SubmitStudentJoinByCode());

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
        yield return StartCoroutine(RefreshTeacherRequestStateFromApi());

        FillSettingsFieldsFromProfile();
        UpdateTeacherRequestState();
        settingsModal.RemoveFromClassList("hidden");
        settingsModal.AddToClassList("open");

        yield return StartCoroutine(RefreshPendingClassRequests());

        if (TryRedirectByCurrentRole())
            yield break;
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
        settingsUserSnapshot = JsonUtility.FromJson<UserUpdatePayloadDto>(detailRaw);
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

    private IEnumerator SubmitTeacherRequest()
    {
        if (settingsTeacherRequestPending)
            yield break;

        if (router == null)
            yield break;

        settingsBecomeTeacherBtn?.SetEnabled(false);
        ShowSettingsStatus("Öğretmen başvurun gönderiliyor...");

        string url = router.ApiBaseUrl + teacherRoleRequestPath;
        string payload = JsonUtility.ToJson(new CreateTeacherRoleRequestDto
        {
            Note = "Bağımsız kullanıcı öğretmen rolü başvurusu"
        });

        using var req = AuthedJson(url, "POST", payload);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ShowSettingsStatus(ReadApiMessage(req.downloadHandler?.text, "Öğretmen başvurusu gönderilemedi."), isError: true);
            UpdateTeacherRequestState();
            yield break;
        }

        yield return StartCoroutine(RefreshTeacherRequestStateFromApi());

        UpdateTeacherRequestState();
        ShowSettingsStatus(ReadApiMessage(req.downloadHandler?.text, "Öğretmen başvurun gönderildi. Admin onayı bekleniyor."), isSuccess: true);
    }

    private IEnumerator RefreshTeacherRequestStateFromApi()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + teacherRoleRequestPath + "/me";
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            settingsTeacherRequestPending = false;
            settingsTeacherRequestStatus = "None";
            settingsTeacherDecisionNote = string.Empty;
            UpdateTeacherRequestState();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        var dto = JsonUtility.FromJson<TeacherRoleRequestStateDto>(raw);

        settingsTeacherRequestStatus = dto != null && !string.IsNullOrWhiteSpace(dto.Status)
            ? dto.Status
            : "None";

        settingsTeacherDecisionNote = dto != null ? (dto.DecisionNote ?? string.Empty) : string.Empty;
        settingsTeacherRequestPending = string.Equals(settingsTeacherRequestStatus, "Pending", StringComparison.OrdinalIgnoreCase);
        UpdateTeacherRequestState();
    }

    private void UpdateTeacherRequestState()
    {
        if (settingsBecomeTeacherBtn == null || settingsTeacherStateLabel == null)
            return;

        bool isIndependent = router != null && router.CurrentRoleId == IndependentRoleId;
        if (!isIndependent)
        {
            settingsBecomeTeacherBtn.SetEnabled(false);
            settingsTeacherStateLabel.text = "Bu işlem yalnızca bağımsız kullanıcı hesabında kullanılabilir.";
            return;
        }

        if (string.Equals(settingsTeacherRequestStatus, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            settingsBecomeTeacherBtn.text = "Başvuru Bekleniyor";
            settingsBecomeTeacherBtn.SetEnabled(false);
            settingsTeacherStateLabel.text = "Öğretmen başvurun admin onayında bekliyor.";
            return;
        }

        if (string.Equals(settingsTeacherRequestStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            settingsBecomeTeacherBtn.text = "Tekrar Başvur";
            settingsBecomeTeacherBtn.SetEnabled(true);
            settingsTeacherStateLabel.text = string.IsNullOrWhiteSpace(settingsTeacherDecisionNote)
                ? "Öğretmen başvurun reddedildi. Tekrar başvurabilirsin."
                : $"Öğretmen başvurun reddedildi: {settingsTeacherDecisionNote}";
            return;
        }

        settingsBecomeTeacherBtn.text = "Öğretmen Ol";
        settingsBecomeTeacherBtn.SetEnabled(true);
        settingsTeacherStateLabel.text = "Öğretmen rolüne geçiş için başvuru gönderebilirsin.";
    }

    private IEnumerator SubmitStudentJoinByCode()
    {
        if (router == null)
            yield break;

        string code = (settingsClassCodeInput?.value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            ShowSettingsStatus("Öğrenci başvurusu için sınıf kodu gir.", isError: true);
            yield break;
        }

        settingsBecomeStudentBtn?.SetEnabled(false);
        ShowSettingsStatus("Sınıfa katılma isteğin gönderiliyor...");

        var requestBody = new JoinClassByCodeRequest { ClassCode = code };
        string json = JsonUtility.ToJson(requestBody);
        string url = router.ApiBaseUrl + classJoinPath;

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ShowSettingsStatus(ReadApiMessage(req.downloadHandler?.text, "Sınıf başvurusu gönderilemedi."), isError: true);
            settingsBecomeStudentBtn?.SetEnabled(true);
            yield break;
        }

        ShowSettingsStatus(ReadApiMessage(req.downloadHandler?.text, "Katılma isteğin gönderildi."), isSuccess: true);
        if (settingsClassCodeInput != null)
            settingsClassCodeInput.value = string.Empty;

        yield return StartCoroutine(RefreshPendingClassRequests());
        yield return StartCoroutine(LoadProfilePageData());
        settingsBecomeStudentBtn?.SetEnabled(true);

        if (TryRedirectByCurrentRole())
            yield break;
    }

    private IEnumerator RefreshPendingClassRequests()
    {
        if (router == null || settingsPendingClassList == null)
            yield break;

        settingsPendingClassList.Clear();

        string url = router.ApiBaseUrl + classMyPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            AddPendingClassItem("Bekleyen sınıf bilgileri alınamadı.");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<MyClassListWrapper>("{\"items\":" + raw + "}");
        var pendingClasses = wrapped != null && wrapped.items != null
            ? wrapped.items.Where(c => c != null && IsPendingStatus(c.status)).ToList()
            : new List<MyClassSummaryDto>();

        if (pendingClasses.Count == 0)
        {
            AddPendingClassItem("Bekleyen sınıf başvurun yok.");
            yield break;
        }

        foreach (var item in pendingClasses)
            AddPendingClassItem($"{SafeText(item.name)} - Bekleniyor");
    }

    private void AddPendingClassItem(string text)
    {
        if (settingsPendingClassList == null)
            return;

        var label = new Label(string.IsNullOrWhiteSpace(text) ? "-" : text);
        label.AddToClassList("settings-pending-item");
        settingsPendingClassList.Add(label);
    }

    private bool IsPendingStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Beklemede", StringComparison.OrdinalIgnoreCase);
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

    private string ReadApiMessage(string raw, string fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        try
        {
            var dto = JsonUtility.FromJson<ApiMessageDto>(raw);
            if (dto != null && !string.IsNullOrWhiteSpace(dto.message))
                return dto.message;
        }
        catch { }

        return fallback;
    }

    private bool TryRedirectByCurrentRole()
    {
        if (router == null || profileMe == null)
            return false;

        int roleId = ResolveRoleId(profileMe.roleName);
        if (roleId <= 0 || roleId == IndependentRoleId)
            return false;

        router.SetSession(
            router.CurrentUserId,
            router.AccessToken,
            string.IsNullOrWhiteSpace(profileMe.name) ? router.CurrentName : profileMe.name,
            string.IsNullOrWhiteSpace(profileMe.surname) ? router.CurrentSurname : profileMe.surname,
            roleId,
            profileMe.roleName);

        CloseSettingsModal();
        router.ShowDashboardByRole(profileMe.roleName, roleId);
        return true;
    }

    private int ResolveRoleId(string roleName)
    {
        string role = (roleName ?? string.Empty).Trim().ToLowerInvariant();
        if (role.Contains("student") || role.Contains("öğrenci") || role.Contains("ogrenci")) return 1;
        if (role.Contains("teacher") || role.Contains("öğretmen") || role.Contains("ogretmen")) return 2;
        if (role.Contains("independent") || role.Contains("bağımsız") || role.Contains("bagimsiz")) return 3;
        if (role.Contains("contentcreator") || role.Contains("içerik") || role.Contains("icerik")) return 4;
        if (role.Contains("admin") || role.Contains("yönetici") || role.Contains("yonetici")) return 5;
        return 0;
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

        var roleChange = new RoleChangeNotificationDto
        {
            Id = $"independent-role-change-{router.CurrentUserId}-{DateTime.UtcNow.Ticks}",
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

        string message = PlayerPrefs.GetString(GetRoleNotificationMessageKey(), string.Empty);
        if (string.IsNullOrWhiteSpace(message))
            return false;

        string id = PlayerPrefs.GetString(GetRoleNotificationIdKey(), string.Empty);
        string rawTimestamp = PlayerPrefs.GetString(GetRoleNotificationTimestampKey(), string.Empty);

        if (!DateTime.TryParse(rawTimestamp, null, DateTimeStyles.RoundtripKind, out var parsed))
            parsed = DateTime.Now;

        roleChange = new RoleChangeNotificationDto
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? $"independent-role-change-{router.CurrentUserId}-{parsed.ToUniversalTime().Ticks}"
                : id,
            Message = message,
            Timestamp = parsed
        };

        return true;
    }

    private string GetRoleNotificationIdKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-notification-id-{userId}";
    }

    private string GetRoleNotificationMessageKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-notification-message-{userId}";
    }

    private string GetRoleNotificationTimestampKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-notification-time-{userId}";
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

        profileMe = me;
        TrackRoleChangeNotification(me);
    }

    private string GetRoleSnapshotKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"independent-role-snapshot-{userId}";
    }

    private IEnumerator LoadProfilePageData()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + myProfilePath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[INDEPENDENT PROFILE] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        profileMe = JsonUtility.FromJson<ProfileMeDto>(raw);
        TrackRoleChangeNotification(profileMe);
        ApplyProfileIdentity();
        BuildProfileStatsCards();
    }

    private void ApplyProfileIdentity()
    {
        string fullName = profileMe != null ? $"{profileMe.name} {profileMe.surname}".Trim() : $"{router.CurrentName} {router.CurrentSurname}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = "-";

        if (profileAvatarLabel != null)
            profileAvatarLabel.text = BuildInitials(fullName);
        if (profileNameLabel != null)
            profileNameLabel.text = fullName;
        if (profileRoleLabel != null)
            profileRoleLabel.text = profileMe != null && !string.IsNullOrWhiteSpace(profileMe.roleName) ? profileMe.roleName : "Bağımsız Kullanıcı";
        if (profileStatusLabel != null)
            profileStatusLabel.text = profileMe != null && profileMe.isActive ? "Aktif" : "Pasif";
        if (profileMailLabel != null)
            profileMailLabel.text = profileMe != null ? SafeText(profileMe.email) : "-";
        if (profileJoinDateLabel != null)
            profileJoinDateLabel.text = FormatDateTr(profileMe != null ? profileMe.createdAt : null);
        if (profileLastLoginLabel != null)
            profileLastLoginLabel.text = FormatDateTr(profileMe != null ? profileMe.lastLogin : null);

        if (settingsRoleActionsSection != null)
        {
            int roleId = ResolveRoleId(profileMe != null ? profileMe.roleName : null);
            bool showRoleActions = roleId <= 0 || roleId == IndependentRoleId;
            settingsRoleActionsSection.style.display = showRoleActions ? DisplayStyle.Flex : DisplayStyle.None;
        }

        UpdateTeacherRequestState();
    }

    private void BuildProfileStatsCards()
    {
        if (profileStatsGrid == null)
            return;

        profileStatsGrid.Clear();

        int completed = assignmentItems.Count(a => a != null && !a.IsActive);
        int total = assignmentItems.Count(a => a != null);
        int ratio = total > 0 ? Mathf.RoundToInt((completed / (float)total) * 100f) : 0;
        int streak = profileMe != null ? Mathf.Max(profileMe.currentActiveStreakDays, 0) : 0;
        int activeDays = profileMe != null ? Mathf.Max(profileMe.totalActiveDays, 0) : 0;
        float activeHours = profileMe != null ? Mathf.Max(profileMe.totalActiveHours, 0f) : 0f;

        profileStatsGrid.Add(BuildProfileStatCard(completed.ToString(), "Tamamlanan Deney", total > 0 ? $"Toplam: {total}" : "Henüz veri yok"));
        profileStatsGrid.Add(BuildProfileStatCard($"%{ratio}", "Ortalama Başarı", ratio >= 70 ? "İyi gidiyor" : "Geliştirilebilir", true));
        profileStatsGrid.Add(BuildProfileStatCard(streak.ToString(), "Aktif Gün Serisi", "Üst üste giriş yapılan gün"));
        profileStatsGrid.Add(BuildProfileStatCard(activeDays.ToString(), "Aktif Toplam Gün", $"Toplam süre: {activeHours:0.0} saat"));
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

    private void BindMenuButtons()
    {
        root.Q<Button>("HomeBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("HomeBtn");
            ShowPage("HomePage");
            StartCoroutine(RefreshHomeSessionChart());
        });

        root.Q<Button>("ProgressBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ProgressBtn");
            ShowPage("ProgressPage");
            StartCoroutine(LoadProgressPageData(forceRefresh: true));
        });

        root.Q<Button>("CalendarBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            if (calendarPage == null)
            {
                Debug.LogWarning("[IndependentUserDashboardController] CalendarPage is not defined in IndependentUserDashboard.uxml.");
                return;
            }

            SetMenuActive("CalendarBtn");
            ShowPage("CalendarPage");
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
            () => $"independent-{profileMe?.id ?? 0}");
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

            var dueAt = GetAssignmentDueAt(assignment);
            if (!dueAt.HasValue)
                continue;

            bool isUpcoming = dueAt.Value >= now && dueAt.Value <= now.AddDays(3);
            if (isUpcoming)
            {
                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"independent-upcoming-assignment-{assignment.Id}",
                    Title = "Yaklaşan Teslim",
                    Message = $"{SafeText(assignment.Title)} için son tarih: {dueAt.Value.ToString("dd MMM yyyy HH:mm", trCulture)}",
                    Timestamp = dueAt.Value,
                    TargetPage = "ProgressPage",
                    TargetMenuButton = "ProgressBtn",
                    IsUnread = true
                });
            }
        }

        foreach (var experiment in experimentItems ?? Array.Empty<ExperimentDto>())
        {
            if (experiment == null)
                continue;

            var createdAt = ParseDate(experiment.CreatedAt);
            if (createdAt == DateTime.MinValue)
                continue;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"independent-new-experiment-{experiment.Id}",
                Title = "Yeni Deney",
                Message = $"{SafeText(experiment.ExperimentName)} deneyi eklendi.",
                Timestamp = createdAt,
                TargetPage = "ProgressPage",
                TargetMenuButton = "ProgressBtn",
                IsUnread = createdAt >= now.AddDays(-7)
            });
        }

        foreach (var activity in personalActivityItems ?? Array.Empty<ClassActivityDto>())
        {
            if (activity == null)
                continue;

            bool isJoinApproved = string.Equals(activity.Type, "JoinApproved", StringComparison.OrdinalIgnoreCase);
            bool isProgress = (activity.Type ?? string.Empty).IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0
                || (activity.Type ?? string.Empty).IndexOf("Progress", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isJoinApproved && !isProgress)
                continue;

            var occurredAt = ParseDate(activity.OccurredAt);
            if (occurredAt == DateTime.MinValue)
                occurredAt = now;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"independent-activity-{activity.ActivityId}",
                Title = isJoinApproved ? "Onaylanan Katılım" : "İlerleme Güncellemesi",
                Message = string.IsNullOrWhiteSpace(activity.Description)
                    ? SafeText(activity.Title)
                    : activity.Description,
                Timestamp = occurredAt,
                TargetPage = isJoinApproved ? "ActivityPage" : "ProgressPage",
                TargetMenuButton = isJoinApproved ? "ActivityBtn" : "ProgressBtn",
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

        if (!string.IsNullOrWhiteSpace(item.TargetMenuButton))
            SetMenuActive(item.TargetMenuButton);

        if (!string.IsNullOrWhiteSpace(item.TargetPage))
            ShowPage(item.TargetPage);

        if (string.Equals(item.TargetPage, "ProgressPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(LoadProgressPageData(forceRefresh: true));
        else if (string.Equals(item.TargetPage, "ActivityPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(FetchPersonalActivity());
    }
    private void ShowPage(string pageName)
    {
        foreach (var child in mainContent.Children())
            child.RemoveFromClassList("active");

        var page = mainContent.Q<VisualElement>(pageName);
        if (page == null)
        {
            Debug.LogError($"[IndependentUserDashboardController] Page not found: {pageName}");
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
        var names = new[] { "HomeBtn", "ProgressBtn", "CalendarBtn", "ActivityBtn", "ProfileBtn", "StartSimulationBtn" };

        foreach (var n in names)
            root.Q<Button>(n)?.RemoveFromClassList("active");

        root.Q<Button>(activeButtonName)?.AddToClassList("active");
    }

    private IEnumerator LoadProgressPageData(bool forceRefresh)
    {
        if (forceRefresh || experimentItems == null || experimentItems.Length == 0)
            yield return StartCoroutine(FetchAllExperiments());

        if (forceRefresh || assignmentItems == null || assignmentItems.Length == 0)
            yield return StartCoroutine(FetchMyAssignments());

        RebuildProgressTabs();
        RenderProgressColumns();
    }

    private IEnumerator FetchMyAssignments()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + myAssignmentsPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[INDEPENDENT ASSIGNMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            assignmentItems = Array.Empty<AssignmentDto>();
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<AssignmentListWrapper>("{\"items\":" + raw + "}");
        assignmentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<AssignmentDto>();
        RefreshCalendarClassDropdown();

        ApplyHomeDashboardMetrics();
        RefreshNotificationsBadge();
    }

    private IEnumerator FetchAllExperiments()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + experimentsPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[INDEPENDENT EXPERIMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            experimentItems = Array.Empty<ExperimentDto>();
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ExperimentListWrapper>("{\"items\":" + raw + "}");
        experimentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ExperimentDto>();
        RefreshNotificationsBadge();
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

    private IEnumerator FetchPersonalActivity()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + personalActivityPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[INDEPENDENT ACTIVITY] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            personalActivityItems = Array.Empty<ClassActivityDto>();
            RenderPersonalActivityFeed();
            ApplyHomeDashboardMetrics();
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassActivityListWrapper>("{\"items\":" + raw + "}");
        personalActivityItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ClassActivityDto>();

        RenderPersonalActivityFeed();
        ApplyHomeDashboardMetrics();
        RefreshNotificationsBadge();
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
                yield return StartCoroutine(FetchPersonalActivity());
                RefreshNotificationsBadge();
            }

            yield return new WaitForSeconds(45f);
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

    // ---------------- CALENDAR ----------------

    #region Calendar

    [System.Serializable]
    private class CalendarEventItem
    {
        public int Id;
        public string Title;
        public string Type;
        public string Date;
        public string Start;
        public string End;
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
        calendarBtn = root.Q<Button>("CalendarBtn");
        calendarPage = root.Q<VisualElement>("CalendarPage");
        if (calendarPage == null)
        {
            calendarBtn?.SetEnabled(false);
            return;
        }

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
        if (assignmentItems != null)
        {
            foreach (var item in assignmentItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ClassName))
                    continue;

                string cls = item.ClassName.Trim();
                if (!choices.Contains(cls))
                    choices.Add(cls);
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
                string hay = $"{ev.Title} {ev.RelatedClass} {ev.Desc}".ToLowerInvariant();
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
            var cls = new Label(string.IsNullOrWhiteSpace(ev.RelatedClass) ? "Kişisel" : ev.RelatedClass);
            cls.AddToClassList("cal-day-event-loc");

            block.Add(title);
            block.Add(time);
            block.Add(cls);

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

                var meta = new Label($"{ev.Start} - {ev.End}" + (string.IsNullOrWhiteSpace(ev.RelatedClass) ? "" : $" · {ev.RelatedClass}"));
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
            calCategoryNameInput.value = string.Empty;

        calSelectedPresetColor = string.Empty;
        RefreshCalendarPresetSelectionUI();

        if (calCategoryColorInput != null)
            calCategoryColorInput.value = string.Empty;

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
        if (calDetailLocationLabel != null) calDetailLocationLabel.text = string.IsNullOrWhiteSpace(ev.RelatedClass) ? "Kişisel" : ev.RelatedClass;
        if (calDetailDescLabel != null) calDetailDescLabel.text = string.IsNullOrWhiteSpace(ev.Desc) ? "-" : ev.Desc;
    }

    private void CloseCalendarDetailModal()
    {
        calDetailModal?.AddToClassList("hidden");
        calCurrentDetailEvent = null;
    }

    private void SaveCalendarEvent()
    {
        string title = calAddTitleInput?.value?.Trim() ?? string.Empty;
        string type = calAddTypeDropdown?.value ?? string.Empty;
        string date = calAddDateInput?.value?.Trim() ?? string.Empty;
        string start = calAddStartInput?.value?.Trim() ?? "09:00";
        string end = calAddEndInput?.value?.Trim() ?? "10:00";
        string relatedClass = calAddClassDropdown?.value ?? string.Empty;
        string desc = calAddDescInput?.value?.Trim() ?? string.Empty;

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
        string label = calCategoryNameInput?.value?.Trim() ?? string.Empty;
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
        string manual = calCategoryColorInput?.value?.Trim() ?? string.Empty;

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

    private DateTime? GetAssignmentDueAt(AssignmentDto assignment)
    {
        if (!TryParseDate(assignment?.StartDate, out var startDate))
            return null;

        int duration = Mathf.Max(assignment.DurationDays, 1);
        return startDate.Date.AddDays(duration).AddSeconds(-1);
    }

    private string SafeText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private string BuildInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, 1).ToUpper();

        return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
    }

    private string FormatDateTr(string raw)
    {
        if (!TryParseDate(raw, out var dt))
            return "-";

        return dt.ToString("dd MMMM yyyy", new CultureInfo("tr-TR"));
    }

    private DateTime ParseDate(string raw)
    {
        return TryParseDate(raw, out var dt) ? dt : DateTime.MinValue;
    }

    private bool TryParseDate(string raw, out DateTime dt)
    {
        dt = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var iso))
        {
            dt = iso.ToLocalTime();
            return true;
        }

        if (DateTime.TryParse(raw, out var parsed))
        {
            dt = parsed;
            return true;
        }

        return false;
    }

    private string GetPersonalBadgeText(string type)
    {
        if (string.Equals(type, "AccountCreated", StringComparison.OrdinalIgnoreCase)) return "Hesap";
        if (string.Equals(type, "ClassCreated", StringComparison.OrdinalIgnoreCase)) return "Sınıf";
        if (string.Equals(type, "ExperimentCompleted", StringComparison.OrdinalIgnoreCase)) return "Tamamlandı";
        if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase)) return "Ödev";
        if (string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase)) return "Katılım";
        if (string.Equals(type, "Achievement", StringComparison.OrdinalIgnoreCase)) return "Başarı";
        return "Aktivite";
    }

    private void AddPersonalBadgeVariant(Label badge, string type)
    {
        if (badge == null)
            return;

        if (string.Equals(type, "AccountCreated", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("achievement");
        else if (string.Equals(type, "ExperimentCompleted", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("completed");
        else if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("submitted");
        else if (string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("joined");
        else
            badge.AddToClassList("achievement");
    }

    private UnityWebRequest AuthedGet(string url)
    {
        var req = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(router?.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);
        return req;
    }

    private UnityWebRequest AuthedJson(string url, string method, string json)
    {
        var req = new UnityWebRequest(url, method);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json ?? string.Empty));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

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

    private void OnDisable()
    {
        if (sessionHeartbeatRoutine != null)
        {
            StopCoroutine(sessionHeartbeatRoutine);
            sessionHeartbeatRoutine = null;
        }
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
    private class ClassActivityDto
    {
        public string ActivityId;
        public string Type;
        public string Title;
        public string Description;
        public string ActorName;
        public string ActorRole;
        public string OccurredAt;
    }

    [Serializable]
    private class ClassActivityListWrapper
    {
        public ClassActivityDto[] items;
    }

    [Serializable]
    private class ApiMessageDto
    {
        public string message;
    }

    [Serializable]
    private class UserUpdatePayloadDto
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
    private class JoinClassByCodeRequest
    {
        public string ClassCode;
    }

    [Serializable]
    private class CreateTeacherRoleRequestDto
    {
        public string Note;
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
    private class MyClassSummaryDto
    {
        public int id;
        public string name;
        public string status;
    }

    [Serializable]
    private class MyClassListWrapper
    {
        public MyClassSummaryDto[] items;
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
    private class RoleChangeNotificationDto
    {
        public string Id;
        public string Message;
        public DateTime Timestamp;
    }

    [Serializable]
    private class WeeklySessionDayDto
    {
        public int dayIndex;
        public string dayLabel;
        public float hours;
    }
}