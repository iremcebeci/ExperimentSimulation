using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class ContentCreatorDashboardController : MonoBehaviour
{
    private AppRouter router;
    private VisualElement root;
    private VisualElement mainContent;

    [Header("Controllers")]
    [SerializeField] private DashboardSidebarController sidebarController;
    [SerializeField] private DashboardsHeaderController headerController;

    [Header("API Paths")]
    [SerializeField] private string myAssignmentsPath = "/api/Assignment/my";
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string userPath = "/api/User";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string contentTaskPath = "/api/ContentTask";
    [SerializeField] private string todoPath = "/api/Todo";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";
    [SerializeField] private string calendarCategoriesPath = "/api/Calendar/categories";
    [SerializeField] private string calendarEventsPath = "/api/Calendar/events";

    // Home
    private Label welcomeUsernameLabel;
    private VisualElement homePage;
    private Label homeActiveTaskValueLabel;
    private Label homeCompletedTaskValueLabel;
    private Label homeRevisionWaitingValueLabel;
    private Label homeUpcomingDeadlineValueLabel;
    private ScrollView homeSummaryScroll;
    private Label homeChartPeakInfoLabel;
    private readonly List<VisualElement> homeChartBars = new();
    private readonly List<Label> homeChartValueLabels = new();
    private readonly float[] homeWeeklyHours = new float[7];

    // Activity
    private VisualElement personalActivityPage;
    private VisualElement personalActivityFeed;
    private Button actTabAllBtn;
    private Button actTabExperimentBtn;
    private Button actTabMissionBtn;
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
    private Label profileCreatorIdLabel;
    private Label profileMailLabel;
    private Label profileJoinDateLabel;
    private Label profileLastLoginLabel;
    private VisualElement profileStatsGrid;
    private Button profileHomeBtn;
    private Button profileMissionsBtn;
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

    // Cached API data
    private AssignmentDto[] assignmentItems = Array.Empty<AssignmentDto>();
    private ClassActivityDto[] personalActivityItems = Array.Empty<ClassActivityDto>();
    private ProfileMeDto profileMe;
    private DashboardNotificationCenter notificationCenter;
    private readonly List<RoleChangeNotificationDto> roleChangeNotificationItems = new();
    private Coroutine sessionHeartbeatRoutine;
    private readonly CultureInfo trCulture = new("tr-TR");

    // MyMissions
    private string activeMissionTab = "Tümü";
    private ContentTaskItemDto[] myMissionItems = Array.Empty<ContentTaskItemDto>();
    private ContentTaskItemDto selectedMyMission;
    private int selectedMyMissionTaskId;

    private ScrollView missionRowsScroll;
    private TextField missionSearchInput;
    private DropdownField missionPriorityFilter;
    private DropdownField missionExperimentFilter;
    private Label myActiveTaskCountLabel;
    private Label myTodayDeadlineCountLabel;
    private Label myOverdueCountLabel;
    private Label myCompletedCountLabel;

    private Label missionDetailKickerLabel;
    private Label missionDetailTitleLabel;
    private Label missionDetailStatusBadge;
    private Label missionDetailPriorityBadge;
    private Label missionDetailRevisionPriorityBadge;
    private Label missionDetailOwnerBadge;
    private Label senderAdminLabel;
    private Label missionStartDateLabel;
    private Label missionDetailDeadlineLabel;
    private Label missionTypeLabel;
    private Label missionStatusInfoLabel;
    private Label missionDetailDescLabel;
    private VisualElement missionExpectedOutputsList;
    private VisualElement missionRevisionNoteBlock;
    private Label missionRevisionNoteLabel;
    private ScrollView missionTimelineScroll;
    private VisualElement missionCommentsList;
    private TextField missionCommentInput;
    private Label missionCommentStatusLabel;
    private Button addMissionCommentBtn;
    private Button backToMissionsBtn;
    private Button sendMissionBtn;

    // ToDo
    private string activeTodoTab = "all";
    private bool isApplyingTodoDateInput;
    private bool isTodoDatePlaceholderActive;
    private TodoItemDto[] todoItems = Array.Empty<TodoItemDto>();
    private int selectedTodoId;

    private Label todoTodayCountLabel;
    private Label todoCompletedCountLabel;
    private Label todoPendingCountLabel;
    private Label todoOverdueCountLabel;
    private Label todoProgressTextLabel;
    private ScrollView todoListScroll;
    private VisualElement todoDetailPanel;
    private Label todoDetailTitleLabel;
    private Label todoDetailPriorityLabel;
    private Label todoDetailDateLabel;
    private TextField todoTitleInput;
    private DropdownField todoPriorityInput;
    private TextField todoDateInput;
    private TextField todoDetailDescField;
    private TextField todoDetailNotesField;
    private VisualElement todoSubtaskList;
    private TextField todoSubtaskInput;
    private Button addTodoBtn;
    private Button saveTodoBtn;
    private Button markDoneBtn;
    private Button addSubtaskBtn;

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

    public void Bind(AppRouter router, VisualElement independentView)
    {
        this.router = router;
        root = independentView;

        if (root == null)
        {
            Debug.LogError("[ContentCreatorDashboardController] root null.");
            return;
        }

        mainContent = root.Q<VisualElement>("MainContent");
        if (mainContent == null)
        {
            Debug.LogError("[ContentCreatorDashboardController] MainContent not found (name=\"MainContent\").");
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

        BindMenuButtons();
        BindHomePage();
        BindPersonalActivityPage();
        BindProfilePage();
        BindSettingsModal();
        BindMyMissionsPage();
        BindTodoPage();
        BindCalendarPage();
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

    private void BindMenuButtons()
    {
        root.Q<Button>("HomeBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("HomeBtn");
            ShowPage("HomePage");
            StartCoroutine(RefreshHomeData());
        });

        root.Q<Button>("ProgressBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ProgressBtn");
            ShowPage("ProgressPage");
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

        root.Q<Button>("ExperimentsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ExperimentsBtn");
            ShowPage("ExperimentsPage");
        });

        root.Q<Button>("MyMissionsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("MyMissionsBtn");
            ShowPage("MyMissionsPage");
            StartCoroutine(LoadMyMissionsPageData());
        });

        root.Q<Button>("ToDoBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("ToDoBtn");
            ShowPage("ToDoPage");
            StartCoroutine(LoadTodoPageData());
        });

        root.Q<Button>("CalendarBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("CalendarBtn");
            ShowPage("CalendarPage");
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
            () => $"creator-{profileMe?.id ?? 0}");
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

        foreach (var task in myMissionItems ?? Array.Empty<ContentTaskItemDto>())
        {
            if (task == null)
                continue;

            var createdAt = ParseDate(task.createdAtUtc);
            if (createdAt != DateTime.MinValue)
            {
                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"creator-assigned-task-{task.id}",
                    Title = "Atanan Görev",
                    Message = $"{SafeText(task.title)} görevi atandı.",
                    Timestamp = createdAt,
                    TargetPage = "MyMissionsPage",
                    TargetMenuButton = "MyMissionsBtn",
                    IsUnread = createdAt >= now.AddDays(-7)
                });
            }

            if (!IsTaskCompleted(task.status))
            {
                var due = ParseTaskDateOnly(task.deadline);
                bool dueIsValid = due != DateTime.MinValue.Date;
                bool isUpcoming = dueIsValid && due >= now.Date && due <= now.Date.AddDays(3);

                if (isUpcoming)
                {
                    list.Add(new DashboardNotificationCenter.NotificationItem
                    {
                        Id = $"creator-upcoming-task-{task.id}",
                        Title = "Yaklaşan Teslim",
                        Message = $"{SafeText(task.title)} için teslim tarihi: {due.ToString("dd MMM yyyy", trCulture)}",
                        Timestamp = due.AddHours(9),
                        TargetPage = "MyMissionsPage",
                        TargetMenuButton = "MyMissionsBtn",
                        IsUnread = true
                    });
                }
            }

            bool hasRevision = IsInRevisionStatus(task.status) || !string.IsNullOrWhiteSpace(task.latestRevisionRequestedAt);
            if (hasRevision)
            {
                var revisionAt = ParseDate(task.latestRevisionRequestedAt);
                if (revisionAt == DateTime.MinValue)
                    revisionAt = ParseDate(task.updatedAtUtc);
                if (revisionAt == DateTime.MinValue)
                    revisionAt = now;

                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"creator-revision-{task.id}",
                    Title = "Gelen Revize",
                    Message = string.IsNullOrWhiteSpace(task.latestRevisionNote)
                        ? $"{SafeText(task.title)} görevi için revize talebi var."
                        : task.latestRevisionNote,
                    Timestamp = revisionAt,
                    TargetPage = "MyMissionsPage",
                    TargetMenuButton = "MyMissionsBtn",
                    IsUnread = revisionAt >= now.AddDays(-7)
                });
            }
        }

        foreach (var todo in todoItems ?? Array.Empty<TodoItemDto>())
        {
            if (todo == null || todo.isCompleted)
                continue;

            var due = ParseTaskDateOnly(todo.dueDate);
            if (due == DateTime.MinValue.Date)
                continue;

            bool isUpcoming = due >= now.Date && due <= now.Date.AddDays(2);
            if (!isUpcoming)
                continue;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"creator-upcoming-todo-{todo.id}",
                Title = "Yaklaşan ToDo",
                Message = $"{SafeText(todo.title)} için son tarih: {due.ToString("dd MMM yyyy", trCulture)}",
                Timestamp = due.AddHours(9),
                TargetPage = "ToDoPage",
                TargetMenuButton = "ToDoBtn",
                IsUnread = true
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
            .Take(300)
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

        if (string.Equals(item.TargetPage, "MyMissionsPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(LoadMyMissionsPageData());
        else if (string.Equals(item.TargetPage, "ToDoPage", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(LoadTodoPageData());
    }

    private void ShowPage(string pageName)
    {
        foreach (var child in mainContent.Children())
            child.RemoveFromClassList("active");

        var page = mainContent.Q<VisualElement>(pageName);
        if (page == null)
        {
            Debug.LogError($"[ContentCreatorDashboardController] Page not found: {pageName}");
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
        var names = new[] { "HomeBtn", "ProgressBtn", "ActivityBtn", "ProfileBtn", "StartSimulationBtn", "ExperimentsBtn", "MyMissionsBtn", "ToDoBtn", "CalendarBtn" };

        foreach (var n in names)
            root.Q<Button>(n)?.RemoveFromClassList("active");

        root.Q<Button>(activeButtonName)?.AddToClassList("active");
    }

    private IEnumerator InitialLoad()
    {
        yield return StartCoroutine(FetchMyAssignments());
        yield return StartCoroutine(FetchMyMissionTasks());
        yield return StartCoroutine(FetchPersonalActivity());
        yield return StartCoroutine(LoadProfilePageData());
        yield return StartCoroutine(FetchWeeklySessionHours());

        ApplyHomeDashboardMetrics();
        RenderPersonalActivityFeed();
    }

    private IEnumerator RefreshHomeData()
    {
        yield return StartCoroutine(FetchMyAssignments());
        yield return StartCoroutine(FetchMyMissionTasks());
        yield return StartCoroutine(FetchPersonalActivity());
        yield return StartCoroutine(FetchWeeklySessionHours());
        ApplyHomeDashboardMetrics();
    }

    private void BindHomePage()
    {
        homePage = root.Q<VisualElement>("StudentHomePage");
        if (homePage == null)
            return;

        homeActiveTaskValueLabel = homePage.Q<Label>("TcTotalClassValueLabel");
        homeCompletedTaskValueLabel = homePage.Q<Label>("TcTotalStudentValueLabel");
        homeRevisionWaitingValueLabel = homePage.Q<Label>("TcActiveAssignmentValueLabel");
        homeUpcomingDeadlineValueLabel = homePage.Q<Label>("TcCompletedAssignmentValueLabel");
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
        var tasks = (myMissionItems ?? Array.Empty<ContentTaskItemDto>()).Where(t => t != null).ToArray();
        int activeTasks = tasks.Count(t => !IsTaskCompleted(t.status));
        int completedTasks = tasks.Count(t => IsTaskCompleted(t.status));
        int revisionWaiting = tasks.Count(t => IsInRevisionStatus(t.status));

        int upcomingDeadlines = tasks.Count(t =>
        {
            if (IsTaskCompleted(t.status))
                return false;

            var due = ParseTaskDateOnly(t.deadline);
            if (due == DateTime.MinValue.Date)
                return false;

            int dayDiff = (due - DateTime.Today).Days;
            return dayDiff >= 0 && dayDiff <= 3;
        });

        if (homeActiveTaskValueLabel != null) homeActiveTaskValueLabel.text = activeTasks.ToString();
        if (homeCompletedTaskValueLabel != null) homeCompletedTaskValueLabel.text = completedTasks.ToString();
        if (homeRevisionWaitingValueLabel != null) homeRevisionWaitingValueLabel.text = revisionWaiting.ToString();
        if (homeUpcomingDeadlineValueLabel != null) homeUpcomingDeadlineValueLabel.text = upcomingDeadlines.ToString();

        var nearestDue = tasks
            .Select(t => new { item = t, due = ParseTaskDateOnly(t.deadline) })
            .Where(x => x.due != DateTime.MinValue.Date && !IsTaskCompleted(x.item.status))
            .OrderBy(x => x.due)
            .FirstOrDefault();

        if (nearestDue == null)
            SetHomeSummaryItem(0, "-", "Yakın teslim bulunmuyor");
        else
            SetHomeSummaryItem(0, SafeText(nearestDue.item.title), nearestDue.due.ToString("dd MMM yyyy", new CultureInfo("tr-TR")));

        var latestCompleted = tasks
            .Where(t => IsTaskCompleted(t.status))
            .Select(t => new
            {
                item = t,
                completedAt = ParseDate(t.updatedAtUtc) != DateTime.MinValue ? ParseDate(t.updatedAtUtc) : ParseTaskDateOnly(t.deadline)
            })
            .Where(x => x.completedAt != DateTime.MinValue)
            .OrderByDescending(x => x.completedAt)
            .FirstOrDefault();

        if (latestCompleted == null)
            SetHomeSummaryItem(1, "-", "Henüz tamamlanan iş yok");
        else
            SetHomeSummaryItem(1, SafeText(latestCompleted.item.title), latestCompleted.completedAt.ToString("dd MMM HH:mm", new CultureInfo("tr-TR")));

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

    private void BindPersonalActivityPage()
    {
        personalActivityPage = root.Q<VisualElement>("ActivityPage");
        if (personalActivityPage == null)
            return;

        personalActivityFeed = personalActivityPage.Q<VisualElement>("ActFeed");
        actTabAllBtn = personalActivityPage.Q<Button>("ActTabAllBtn");
        actTabExperimentBtn = personalActivityPage.Q<Button>("ActTabExperimentBtn");
        actTabMissionBtn = personalActivityPage.Q<Button>("ActTabMissionsBtn") ?? personalActivityPage.Q<Button>("ActTabAssignmentBtn");
        actTabProgressBtn = personalActivityPage.Q<Button>("ActTabProgressBtn");
        actTabParticipationBtn = personalActivityPage.Q<Button>("ActTabParticipationBtn");
        personalActivitySearchInput = personalActivityPage.Q<TextField>("AssignmentSearchInput");
        actDateFilterDropdown = personalActivityPage.Q<DropdownField>("ActDateFilterDropdown");

        BindActivityTabButton(actTabAllBtn, "all");
        BindActivityTabButton(actTabExperimentBtn, "experiment");
        BindActivityTabButton(actTabMissionBtn, "assignment");
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
        var tabs = new[] { actTabAllBtn, actTabExperimentBtn, actTabMissionBtn, actTabProgressBtn, actTabParticipationBtn };
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
        profileCreatorIdLabel = profilePage.Q<Label>("TeacherCreatorIdLabel");
        profileMailLabel = profilePage.Q<Label>("TeacherMailLabel");
        profileJoinDateLabel = profilePage.Q<Label>("TeacherJoinDateLabel");
        profileLastLoginLabel = profilePage.Q<Label>("TeacherLastLoginLabel");
        profileStatsGrid = profilePage.Q<VisualElement>(className: "teacher-stats-grid");

        var quickActions = profilePage.Q<VisualElement>(className: "teacher-quick-actions");
        if (quickActions == null)
            return;

        var buttons = quickActions.Query<Button>().ToList();
        if (buttons.Count > 0) profileHomeBtn = buttons[0];
        if (buttons.Count > 1) profileMissionsBtn = buttons[1];
        if (buttons.Count > 2) profileLogoutBtn = buttons[2];

        if (profileHomeBtn != null)
            profileHomeBtn.clicked += () =>
            {
                SetMenuActive("HomeBtn");
                ShowPage("HomePage");
                StartCoroutine(RefreshHomeData());
            };

        if (profileMissionsBtn != null)
            profileMissionsBtn.clicked += () =>
            {
                SetMenuActive("MyMissionsBtn");
                ShowPage("MyMissionsPage");
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

        string url = router.ApiBaseUrl + myProfilePath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CONTENT CREATOR PROFILE] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        profileMe = JsonUtility.FromJson<ProfileMeDto>(raw);
        TrackRoleChangeNotification(profileMe);

        // Profile istatistiklerini güncel görev durumu ile göstermek için görev verisini tazele.
        yield return StartCoroutine(FetchMyMissionTasks());

        ApplyProfileIdentity();
        BuildProfileStatsCards();
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
            Id = $"creator-role-change-{router.CurrentUserId}-{DateTime.UtcNow.Ticks}",
            Message = $"Rolünüz {previousRole} rolünden {newRole} rolüne güncellendi.",
            Timestamp = DateTime.Now
        });

        PlayerPrefs.SetString(snapshotKey, newRole);
    }

    private string GetRoleSnapshotKey()
    {
        int userId = router != null ? router.CurrentUserId : 0;
        return $"creator-role-snapshot-{userId}";
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
            profileRoleLabel.text = profileMe != null && !string.IsNullOrWhiteSpace(profileMe.roleName) ? profileMe.roleName : "İçerik Üreticisi";
        if (profileStatusLabel != null)
            profileStatusLabel.text = profileMe != null && profileMe.isActive ? "Aktif" : "Pasif";
        if (profileCreatorIdLabel != null)
            profileCreatorIdLabel.text = profileMe != null ? $"CR{profileMe.id:D4}" : "-";
        if (profileMailLabel != null)
            profileMailLabel.text = profileMe != null ? SafeText(profileMe.email) : "-";
        if (profileJoinDateLabel != null)
            profileJoinDateLabel.text = FormatDateTr(profileMe != null ? profileMe.createdAt : null);
        if (profileLastLoginLabel != null)
            profileLastLoginLabel.text = FormatDateTr(profileMe != null ? profileMe.lastLogin : null);
    }

    private void BuildProfileStatsCards()
    {
        if (profileStatsGrid == null)
            return;

        profileStatsGrid.Clear();

        var missions = (myMissionItems ?? Array.Empty<ContentTaskItemDto>()).Where(m => m != null).ToArray();
        int completed = missions.Count(m => IsTaskCompleted(m.status));
        int active = missions.Count(m => !IsTaskCompleted(m.status));
        int total = missions.Length;
        int ratio = total > 0 ? Mathf.RoundToInt((completed / (float)total) * 100f) : 0;
        int streak = profileMe != null ? Mathf.Max(profileMe.currentActiveStreakDays, 0) : 0;
        int activeDays = profileMe != null ? Mathf.Max(profileMe.totalActiveDays, 0) : 0;
        float activeHours = profileMe != null ? Mathf.Max(profileMe.totalActiveHours, 0f) : 0f;

        profileStatsGrid.Add(BuildProfileStatCard(completed.ToString(), "Tamamlanan İş", total > 0 ? $"Toplam: {total}" : "Henüz veri yok"));
        profileStatsGrid.Add(BuildProfileStatCard(active.ToString(), "Aktif İş", active > 0 ? "Devam eden içerikler" : "Aktif iş yok"));
        profileStatsGrid.Add(BuildProfileStatCard($"%{ratio}", "Tamamlama Oranı", ratio >= 70 ? "İyi gidiyor" : "Geliştirilebilir", true));
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

    private IEnumerator FetchMyAssignments()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + myAssignmentsPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[CONTENT CREATOR ASSIGNMENTS] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
            assignmentItems = Array.Empty<AssignmentDto>();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<AssignmentListWrapper>("{\"items\":" + raw + "}");
        assignmentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<AssignmentDto>();

        RefreshCalendarClassDropdown();

        ApplyHomeDashboardMetrics();
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
            Debug.LogError($"[CONTENT CREATOR ACTIVITY] FAILED {(int)req.responseCode} => {req.downloadHandler?.text}");
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
        if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase)) return "İş";
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

    private UnityWebRequest AuthedJson(string url, string method, string json)
    {
        var req = new UnityWebRequest(url, method);
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json ?? "{}"));
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

    private void BindTodoPage()
    {
        todoTodayCountLabel = root.Q<Label>("TodayCountLabel");
        todoCompletedCountLabel = root.Q<Label>("CompletedCountLabel");
        todoPendingCountLabel = root.Q<Label>("PendingCountLabel");
        todoOverdueCountLabel = root.Q<Label>("OverdueCountLabel");
        todoProgressTextLabel = root.Q<Label>("TodoProgressTextLabel");
        todoListScroll = root.Q<ScrollView>("TodoListScroll");
        todoDetailPanel = root.Q<VisualElement>("TodoDetailPanel");
        todoDetailTitleLabel = root.Q<Label>("TodoDetailTitleLabel");
        todoDetailPriorityLabel = root.Q<Label>("TodoDetailPriorityLabel");
        todoDetailDateLabel = root.Q<Label>("TodoDetailDateLabel");
        todoTitleInput = root.Q<TextField>("TodoTitleInput");
        todoPriorityInput = root.Q<DropdownField>("TodoPriorityInput");
        todoDateInput = root.Q<TextField>("TodoDateInput");
        todoDetailDescField = root.Q<TextField>("TodoDetailDescField");
        todoDetailNotesField = root.Q<TextField>("TodoDetailNotesField");
        todoSubtaskList = root.Q<VisualElement>("TodoSubtaskList");
        todoSubtaskInput = root.Q<TextField>("TodoSubtaskInput");
        addTodoBtn = root.Q<Button>("AddTodoBtn");
        saveTodoBtn = root.Q<Button>("SaveTodoBtn");
        markDoneBtn = root.Q<Button>("MarkDoneBtn");
        addSubtaskBtn = root.Q<Button>("AddSubtaskBtn");

        root.Q<Button>("TodoTabTodayBtn")?.RegisterCallback<ClickEvent>(_ => SetTodoTab("today", "TodoTabTodayBtn"));
        root.Q<Button>("TodoTabTomorrowBtn")?.RegisterCallback<ClickEvent>(_ => SetTodoTab("tomorrow", "TodoTabTomorrowBtn"));
        root.Q<Button>("TodoTabWeekBtn")?.RegisterCallback<ClickEvent>(_ => SetTodoTab("week", "TodoTabWeekBtn"));
        root.Q<Button>("TodoTabAllBtn")?.RegisterCallback<ClickEvent>(_ => SetTodoTab("all", "TodoTabAllBtn"));
        root.Q<Button>("TodoTabPastBtn")?.RegisterCallback<ClickEvent>(_ => SetTodoTab("past", "TodoTabPastBtn"));
        root.Q<Button>("CloseTodoPanelBtn")?.RegisterCallback<ClickEvent>(_ => SetTodoDetailVisible(false));

        if (todoDateInput != null)
        {
            todoDateInput.RegisterValueChangedCallback(OnTodoDateInputChanged);
            todoDateInput.RegisterCallback<FocusInEvent>(_ =>
            {
                if (!isTodoDatePlaceholderActive)
                    return;

                isApplyingTodoDateInput = true;
                todoDateInput.SetValueWithoutNotify(string.Empty);
                isApplyingTodoDateInput = false;
                isTodoDatePlaceholderActive = false;
            });
            todoDateInput.RegisterCallback<FocusOutEvent>(_ => EnsureTodoDatePlaceholder());
            EnsureTodoDatePlaceholder();
        }

        if (addTodoBtn != null)
            addTodoBtn.clicked += () => StartCoroutine(AddTodoFromQuickForm());

        if (saveTodoBtn != null)
            saveTodoBtn.clicked += () => StartCoroutine(SaveSelectedTodo());

        if (markDoneBtn != null)
            markDoneBtn.clicked += () => StartCoroutine(ToggleSelectedTodoDone());

        if (addSubtaskBtn != null)
            addSubtaskBtn.clicked += () => StartCoroutine(AddSelectedTodoSubtask());

        SetTodoDetailVisible(false);
    }

    private void SetTodoTab(string tab, string activeButtonName)
    {
        activeTodoTab = tab;
        string[] names = { "TodoTabAllBtn", "TodoTabTodayBtn", "TodoTabTomorrowBtn", "TodoTabWeekBtn", "TodoTabPastBtn" };

        foreach (var n in names)
            root.Q<Button>(n)?.RemoveFromClassList("active");

        root.Q<Button>(activeButtonName)?.AddToClassList("active");
        RenderTodoList();
    }

    private IEnumerator LoadTodoPageData(int preferredTodoId = 0)
    {
        yield return StartCoroutine(FetchMyTodos());

        ApplyTodoKpis();
        RenderTodoList();

        int targetId = preferredTodoId > 0 ? preferredTodoId : selectedTodoId;
        var selected = (todoItems ?? Array.Empty<TodoItemDto>()).FirstOrDefault(t => t != null && t.id == targetId);

        if (selected == null)
        {
            selectedTodoId = 0;
            SetTodoDetailVisible(false);
            yield break;
        }

        SelectTodo(selected.id);
    }

    private IEnumerator FetchMyTodos()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + todoPath + "/my";
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            todoItems = Array.Empty<TodoItemDto>();
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<TodoItemListWrapper>("{\"items\":" + raw + "}");
        todoItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<TodoItemDto>();
        RefreshNotificationsBadge();
    }

    private void ApplyTodoKpis()
    {
        var all = (todoItems ?? Array.Empty<TodoItemDto>()).Where(t => t != null).ToList();
        var today = DateTime.Today;

        int todayCount = all.Count(t => ParseTaskDateOnly(t.dueDate) == today && !t.isCompleted);
        int completed = all.Count(t => t.isCompleted);
        int pending = all.Count(t => !t.isCompleted);
        int overdue = all.Count(t => !t.isCompleted && ParseTaskDateOnly(t.dueDate) < today);
        int total = all.Count;
        int progress = total > 0 ? Mathf.RoundToInt((completed / (float)total) * 100f) : 0;

        if (todoTodayCountLabel != null) todoTodayCountLabel.text = todayCount.ToString();
        if (todoCompletedCountLabel != null) todoCompletedCountLabel.text = completed.ToString();
        if (todoPendingCountLabel != null) todoPendingCountLabel.text = pending.ToString();
        if (todoOverdueCountLabel != null) todoOverdueCountLabel.text = overdue.ToString();
        if (todoProgressTextLabel != null) todoProgressTextLabel.text = $"%{progress} tamamlandı";
    }

    private void RenderTodoList()
    {
        if (todoListScroll == null)
            return;

        todoListScroll.Clear();

        var filtered = (todoItems ?? Array.Empty<TodoItemDto>())
            .Where(t => t != null)
            .Where(MatchesTodoTab)
            .OrderBy(t => t.isCompleted)
            .ThenBy(t => ParseTaskDateOnly(t.dueDate))
            .ToList();

        if (filtered.Count == 0)
        {
            todoListScroll.Add(new Label("Bu filtrede ToDo kaydı yok."));
            return;
        }

        foreach (var item in filtered)
            todoListScroll.Add(BuildTodoRow(item));
    }

    private VisualElement BuildTodoRow(TodoItemDto item)
    {
        var row = new VisualElement();
        row.AddToClassList("todo-item");

        if (item.isCompleted)
            row.AddToClassList("is-completed");

        bool isOverdue = !item.isCompleted && ParseTaskDateOnly(item.dueDate) < DateTime.Today;
        if (isOverdue)
            row.AddToClassList("is-overdue");

        var main = new VisualElement();
        main.AddToClassList("todo-item-main");

        var check = new Toggle();
        check.AddToClassList("todo-check");
        check.value = item.isCompleted;
        int todoId = item.id;
        check.RegisterValueChangedCallback(evt => StartCoroutine(SetTodoCompleted(todoId, evt.newValue)));

        var copy = new VisualElement();
        copy.AddToClassList("todo-item-copy");
        copy.Add(new Label(SafeText(item.title)));
        copy.Add(new Label(SafeText(item.description)));

        main.Add(check);
        main.Add(copy);

        var meta = new VisualElement();
        meta.AddToClassList("todo-item-meta");

        var priority = new Label(SafeText(item.priority));
        priority.AddToClassList("todo-priority");
        priority.AddToClassList(GetPriorityClass(item.priority));

        var date = new Label(FormatTaskDateLong(item.dueDate));
        date.AddToClassList("todo-date");
        if (isOverdue)
            date.AddToClassList("overdue");

        var editBtn = new Button(() => SelectTodo(todoId)) { text = "Düzenle" };
        editBtn.AddToClassList("todo-mini-btn");
        editBtn.AddToClassList("edit");

        var deleteBtn = new Button(() => StartCoroutine(DeleteTodo(todoId))) { text = "Sil" };
        deleteBtn.AddToClassList("todo-mini-btn");
        deleteBtn.AddToClassList("delete");

        meta.Add(priority);
        meta.Add(date);
        meta.Add(editBtn);
        meta.Add(deleteBtn);

        row.Add(main);
        row.Add(meta);
        row.RegisterCallback<ClickEvent>(_ => SelectTodo(todoId));
        return row;
    }

    private bool MatchesTodoTab(TodoItemDto item)
    {
        var due = ParseTaskDateOnly(item.dueDate);
        var today = DateTime.Today;

        return activeTodoTab switch
        {
            "all" => true,
            "today" => due == today,
            "tomorrow" => due == today.AddDays(1),
            "week" => due >= today && due <= today.AddDays(6),
            "past" => due < today && !item.isCompleted,
            _ => true
        };
    }

    private void OnTodoDateInputChanged(ChangeEvent<string> evt)
    {
        if (todoDateInput == null || isApplyingTodoDateInput)
            return;

        if (isTodoDatePlaceholderActive)
            isTodoDatePlaceholderActive = false;

        string current = evt.newValue ?? string.Empty;
        string formatted = FormatTodoDateInput(current);
        if (formatted == current)
            return;

        isApplyingTodoDateInput = true;
        todoDateInput.SetValueWithoutNotify(formatted);
        // Cursor'u daima sona alarak nokta karakterinden sonra atlama/silme problemini engeller.
        todoDateInput.cursorIndex = formatted.Length;
        todoDateInput.selectIndex = formatted.Length;
        isApplyingTodoDateInput = false;
    }

    private static string FormatTodoDateInput(string raw)
    {
        string digits = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > 8)
            digits = digits.Substring(0, 8);

        if (digits.Length <= 2)
            return digits;

        if (digits.Length <= 4)
            return digits.Substring(0, 2) + "." + digits.Substring(2);

        return digits.Substring(0, 2) + "." + digits.Substring(2, 2) + "." + digits.Substring(4);
    }

    private void EnsureTodoDatePlaceholder()
    {
        if (todoDateInput == null)
            return;

        string val = todoDateInput.value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(val) && !string.Equals(val, "Tarih", StringComparison.OrdinalIgnoreCase))
        {
            isTodoDatePlaceholderActive = false;
            return;
        }

        isApplyingTodoDateInput = true;
        todoDateInput.SetValueWithoutNotify("Tarih");
        isApplyingTodoDateInput = false;
        isTodoDatePlaceholderActive = true;
    }

    private string GetTodoDateInputIsoOrEmpty()
    {
        string raw = (todoDateInput?.value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "Tarih", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (!DateTime.TryParseExact(raw, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return string.Empty;

        return parsed.ToString("yyyy-MM-dd");
    }

    private void SelectTodo(int todoId)
    {
        var todo = (todoItems ?? Array.Empty<TodoItemDto>()).FirstOrDefault(t => t != null && t.id == todoId);
        if (todo == null)
        {
            selectedTodoId = 0;
            SetTodoDetailVisible(false);
            return;
        }

        selectedTodoId = todo.id;
        SetTodoDetailVisible(true);

        if (todoDetailTitleLabel != null) todoDetailTitleLabel.text = SafeText(todo.title);
        if (todoDetailPriorityLabel != null)
        {
            todoDetailPriorityLabel.text = SafeText(todo.priority);
            todoDetailPriorityLabel.RemoveFromClassList("high");
            todoDetailPriorityLabel.RemoveFromClassList("medium");
            todoDetailPriorityLabel.RemoveFromClassList("low");
            todoDetailPriorityLabel.AddToClassList(GetPriorityClass(todo.priority));
        }

        if (todoDetailDateLabel != null) todoDetailDateLabel.text = FormatTaskDateLong(todo.dueDate);
        if (todoDetailDescField != null) todoDetailDescField.value = todo.description ?? string.Empty;
        if (todoDetailNotesField != null) todoDetailNotesField.value = todo.notes ?? string.Empty;

        if (markDoneBtn != null)
            markDoneBtn.text = todo.isCompleted ? "Bekleyen Olarak İşaretle" : "Tamamlandı İşaretle";

        RenderTodoSubtasks(todo);
        RenderTodoList();
    }

    private void RenderTodoSubtasks(TodoItemDto todo)
    {
        if (todoSubtaskList == null)
            return;

        todoSubtaskList.Clear();
        var subtasks = (todo?.subtasks ?? Array.Empty<TodoSubtaskDto>()).Where(s => s != null).ToArray();

        if (subtasks.Length == 0)
        {
            todoSubtaskList.Add(new Label("Alt görev yok."));
            return;
        }

        foreach (var sub in subtasks)
        {
            var item = new VisualElement();
            item.AddToClassList("todo-subtask-item");
            if (sub.isCompleted)
                item.AddToClassList("done");

            var toggle = new Toggle { value = sub.isCompleted };
            int subId = sub.id;
            toggle.RegisterValueChangedCallback(evt => StartCoroutine(UpdateSubtask(subId, sub.title, evt.newValue)));

            var label = new Label(SafeText(sub.title));
            var delete = new Button(() => StartCoroutine(DeleteSubtask(subId))) { text = "Sil" };
            delete.AddToClassList("todo-mini-btn");
            delete.AddToClassList("delete");

            item.Add(toggle);
            item.Add(label);
            item.Add(delete);
            todoSubtaskList.Add(item);
        }
    }

    private void SetTodoDetailVisible(bool visible)
    {
        if (todoDetailPanel == null)
            return;

        todoDetailPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private IEnumerator AddTodoFromQuickForm()
    {
        if (router == null)
            yield break;

        string title = (todoTitleInput?.value ?? string.Empty).Trim();
        string priority = (todoPriorityInput?.value ?? "Orta").Trim();
        string dueDate = GetTodoDateInputIsoOrEmpty();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(dueDate))
            yield break;

        var payload = new UpsertTodoItemRequest
        {
            title = title,
            priority = priority,
            dueDate = dueDate,
            description = string.Empty,
            notes = string.Empty,
            isCompleted = false
        };

        string url = router.ApiBaseUrl + todoPath;
        using var req = AuthedJson(url, "POST", JsonUtility.ToJson(payload));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        int createdId = 0;
        var created = JsonUtility.FromJson<TodoItemDto>(req.downloadHandler != null ? req.downloadHandler.text : "{}");
        if (created != null)
            createdId = created.id;

        if (todoTitleInput != null) todoTitleInput.value = string.Empty;
        if (todoDateInput != null)
        {
            todoDateInput.value = string.Empty;
            EnsureTodoDatePlaceholder();
        }

        yield return StartCoroutine(LoadTodoPageData(createdId));
    }

    private IEnumerator SaveSelectedTodo()
    {
        if (selectedTodoId <= 0 || router == null)
            yield break;

        var current = (todoItems ?? Array.Empty<TodoItemDto>()).FirstOrDefault(t => t != null && t.id == selectedTodoId);
        if (current == null)
            yield break;

        var payload = new UpsertTodoItemRequest
        {
            title = current.title,
            priority = current.priority,
            dueDate = current.dueDate,
            description = (todoDetailDescField?.value ?? string.Empty).Trim(),
            notes = (todoDetailNotesField?.value ?? string.Empty).Trim(),
            isCompleted = current.isCompleted
        };

        string url = router.ApiBaseUrl + todoPath + "/" + selectedTodoId;
        using var req = AuthedJson(url, "PUT", JsonUtility.ToJson(payload));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        yield return StartCoroutine(LoadTodoPageData(selectedTodoId));
    }

    private IEnumerator ToggleSelectedTodoDone()
    {
        if (selectedTodoId <= 0)
            yield break;

        var current = (todoItems ?? Array.Empty<TodoItemDto>()).FirstOrDefault(t => t != null && t.id == selectedTodoId);
        if (current == null)
            yield break;

        yield return StartCoroutine(SetTodoCompleted(selectedTodoId, !current.isCompleted));
    }

    private IEnumerator SetTodoCompleted(int todoId, bool isCompleted)
    {
        if (todoId <= 0 || router == null)
            yield break;

        var current = (todoItems ?? Array.Empty<TodoItemDto>()).FirstOrDefault(t => t != null && t.id == todoId);
        if (current == null)
            yield break;

        var payload = new UpsertTodoItemRequest
        {
            title = current.title,
            priority = current.priority,
            dueDate = current.dueDate,
            description = current.description,
            notes = current.notes,
            isCompleted = isCompleted
        };

        string url = router.ApiBaseUrl + todoPath + "/" + todoId;
        using var req = AuthedJson(url, "PUT", JsonUtility.ToJson(payload));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        yield return StartCoroutine(LoadTodoPageData(todoId));
    }

    private IEnumerator DeleteTodo(int todoId)
    {
        if (todoId <= 0 || router == null)
            yield break;

        string url = router.ApiBaseUrl + todoPath + "/" + todoId;
        using var req = AuthedJson(url, "DELETE", "{}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        int preferred = selectedTodoId == todoId ? 0 : selectedTodoId;
        yield return StartCoroutine(LoadTodoPageData(preferred));
    }

    private IEnumerator AddSelectedTodoSubtask()
    {
        if (selectedTodoId <= 0 || router == null)
            yield break;

        string title = (todoSubtaskInput?.value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
            yield break;

        var payload = new TodoSubtaskCreateRequest { title = title };
        string url = router.ApiBaseUrl + todoPath + "/" + selectedTodoId + "/subtasks";
        using var req = AuthedJson(url, "POST", JsonUtility.ToJson(payload));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        if (todoSubtaskInput != null)
            todoSubtaskInput.value = string.Empty;

        yield return StartCoroutine(LoadTodoPageData(selectedTodoId));
    }

    private IEnumerator UpdateSubtask(int subtaskId, string title, bool isCompleted)
    {
        if (selectedTodoId <= 0 || subtaskId <= 0 || router == null)
            yield break;

        var payload = new TodoSubtaskUpdateRequest { title = title, isCompleted = isCompleted };
        string url = router.ApiBaseUrl + todoPath + "/" + selectedTodoId + "/subtasks/" + subtaskId;
        using var req = AuthedJson(url, "PUT", JsonUtility.ToJson(payload));
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        yield return StartCoroutine(LoadTodoPageData(selectedTodoId));
    }

    private IEnumerator DeleteSubtask(int subtaskId)
    {
        if (selectedTodoId <= 0 || subtaskId <= 0 || router == null)
            yield break;

        string url = router.ApiBaseUrl + todoPath + "/" + selectedTodoId + "/subtasks/" + subtaskId;
        using var req = AuthedJson(url, "DELETE", "{}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        yield return StartCoroutine(LoadTodoPageData(selectedTodoId));
    }

    private void BindMyMissionsPage()
    {
        missionRowsScroll = root.Q<ScrollView>("MissionRowsScroll");
        missionSearchInput = root.Q<TextField>("MissionSearchInput");
        missionPriorityFilter = root.Q<DropdownField>("MissionPriorityFilter");
        missionExperimentFilter = root.Q<DropdownField>("MissionExperimentFilter");
        myActiveTaskCountLabel = root.Q<Label>("MyActiveTaskCountLabel");
        myTodayDeadlineCountLabel = root.Q<Label>("MyTodayDeadlineCountLabel");
        myOverdueCountLabel = root.Q<Label>("MyOverdueCountLabel");
        myCompletedCountLabel = root.Q<Label>("MyCompletedCountLabel");

        missionDetailKickerLabel = root.Q<Label>("MissionDetailKickerLabel");
        missionDetailTitleLabel = root.Q<Label>("MissionDetailTitleLabel");
        missionDetailStatusBadge = root.Q<Label>("MissionDetailStatusBadge");
        missionDetailPriorityBadge = root.Q<Label>("MissionDetailPriorityBadge");
        missionDetailRevisionPriorityBadge = root.Q<Label>("MissionDetailRevisionPriorityBadge");
        missionDetailOwnerBadge = root.Q<Label>("MissionDetailOwnerBadge");
        senderAdminLabel = root.Q<Label>("SenderAdminLabel");
        missionDetailDescLabel = root.Q<Label>("MissionDetailDescLabel");
        missionExpectedOutputsList = root.Q<VisualElement>("MissionExpectedOutputsList");
        missionRevisionNoteBlock = root.Q<VisualElement>("MissionRevisionNoteBlock");
        missionRevisionNoteLabel = root.Q<Label>("MissionRevisionNoteLabel");
        missionCommentsList = root.Q<VisualElement>("MissionCommentsList");
        missionCommentInput = root.Q<TextField>("MissionCommentInput");
        missionCommentStatusLabel = root.Q<Label>("MissionCommentStatusLabel");
        missionStartDateLabel = root.Q<Label>("MissionStartDateLabel");
        missionDetailDeadlineLabel = root.Q<Label>("MissionDeadlineDateLabel");
        missionTypeLabel = root.Q<Label>("MissionTypeLabel");
        missionStatusInfoLabel = root.Q<Label>("MissionStatusInfoLabel");
        missionTimelineScroll = root.Q<ScrollView>("MissionTimelineScroll");
        addMissionCommentBtn = root.Q<Button>("AddMissionCommentBtn");
        backToMissionsBtn = root.Q<Button>("BackToMissionsBtn");
        sendMissionBtn = root.Q<Button>("SendMissionBtn");

        root.Q<Button>("MissionTabAllBtn")?.RegisterCallback<ClickEvent>(_ => SetMissionTab("Tümü", "MissionTabAllBtn"));
        root.Q<Button>("MissionTabInProgressBtn")?.RegisterCallback<ClickEvent>(_ => SetMissionTab("Devam Edenler", "MissionTabInProgressBtn"));
        root.Q<Button>("MissionTabReviewBtn")?.RegisterCallback<ClickEvent>(_ => SetMissionTab("İncelemede", "MissionTabReviewBtn"));
        root.Q<Button>("MissionTabRevisionBtn")?.RegisterCallback<ClickEvent>(_ => SetMissionTab("Revizyonda", "MissionTabRevisionBtn"));
        root.Q<Button>("MissionTabDoneBtn")?.RegisterCallback<ClickEvent>(_ => SetMissionTab("Tamamlananlar", "MissionTabDoneBtn"));
        root.Q<Button>("MissionTabLateBtn")?.RegisterCallback<ClickEvent>(_ => SetMissionTab("Gecikenler", "MissionTabLateBtn"));

        missionSearchInput?.RegisterValueChangedCallback(_ => RenderMyMissionRows());
        missionPriorityFilter?.RegisterValueChangedCallback(_ => RenderMyMissionRows());
        missionExperimentFilter?.RegisterValueChangedCallback(_ => RenderMyMissionRows());

        if (backToMissionsBtn != null)
            backToMissionsBtn.clicked += () => ShowPage("MyMissionsPage");

        if (addMissionCommentBtn != null)
            addMissionCommentBtn.clicked += () => StartCoroutine(AddMissionComment());

        if (sendMissionBtn != null)
            sendMissionBtn.clicked += () => StartCoroutine(SubmitMissionForReview());
    }

    private void SetMissionTab(string tab, string activeButtonName)
    {
        activeMissionTab = tab;

        string[] names =
        {
            "MissionTabAllBtn",
            "MissionTabInProgressBtn",
            "MissionTabReviewBtn",
            "MissionTabRevisionBtn",
            "MissionTabDoneBtn",
            "MissionTabLateBtn"
        };

        foreach (var n in names)
            root.Q<Button>(n)?.RemoveFromClassList("active");

        root.Q<Button>(activeButtonName)?.AddToClassList("active");
        RenderMyMissionRows();
    }

    private IEnumerator LoadMyMissionsPageData()
    {
        yield return StartCoroutine(FetchMyMissionTasks());
        ApplyMyMissionKpis();
        BuildMissionExperimentFilterChoices();
        RenderMyMissionRows();
    }

    private IEnumerator FetchMyMissionTasks()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath + "/my";
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            myMissionItems = Array.Empty<ContentTaskItemDto>();
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentTaskItemListWrapper>("{\"items\":" + raw + "}");
        myMissionItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentTaskItemDto>();

        RefreshCalendarClassDropdown();
        RefreshNotificationsBadge();
    }

    private void BuildMissionExperimentFilterChoices()
    {
        if (missionExperimentFilter == null)
            return;

        var experiments = (myMissionItems ?? Array.Empty<ContentTaskItemDto>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.experimentName))
            .Select(x => x.experimentName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var choices = new List<string> { "Deney: Tümü" };
        choices.AddRange(experiments);

        missionExperimentFilter.choices = choices;
        if (string.IsNullOrWhiteSpace(missionExperimentFilter.value) || !choices.Contains(missionExperimentFilter.value))
            missionExperimentFilter.value = choices[0];
    }

    private void ApplyMyMissionKpis()
    {
        var all = (myMissionItems ?? Array.Empty<ContentTaskItemDto>()).Where(t => t != null).ToList();
        var today = DateTime.Today;

        int active = all.Count(t => !IsTaskCompleted(t.status));
        int todayDeadline = all.Count(t => ParseTaskDateOnly(t.deadline).Date == today);
        int overdue = all.Count(t => ParseTaskDateOnly(t.deadline).Date < today && !IsTaskCompleted(t.status));
        int completed = all.Count(t => IsTaskCompleted(t.status));

        if (myActiveTaskCountLabel != null) myActiveTaskCountLabel.text = active.ToString();
        if (myTodayDeadlineCountLabel != null) myTodayDeadlineCountLabel.text = todayDeadline.ToString();
        if (myOverdueCountLabel != null) myOverdueCountLabel.text = overdue.ToString();
        if (myCompletedCountLabel != null) myCompletedCountLabel.text = completed.ToString();
    }

    private void RenderMyMissionRows()
    {
        string search = missionSearchInput?.value?.Trim().ToLowerInvariant() ?? "";
        string priority = missionPriorityFilter?.value == "Öncelik: Tümü" ? "" : missionPriorityFilter?.value ?? "";
        string exp = missionExperimentFilter?.value == "Deney: Tümü" ? "" : missionExperimentFilter?.value ?? "";

        var filtered = (myMissionItems ?? Array.Empty<ContentTaskItemDto>())
            .Where(rowData => rowData != null)
            .Where(rowData => MatchesMissionTab(rowData))
            .Where(rowData => string.IsNullOrEmpty(search) || (rowData.title ?? "").ToLowerInvariant().Contains(search))
            .Where(rowData => string.IsNullOrEmpty(priority) || string.Equals(rowData.priority ?? "", priority, StringComparison.OrdinalIgnoreCase))
            .Where(rowData => string.IsNullOrEmpty(exp) || string.Equals(rowData.experimentName ?? "", exp, StringComparison.OrdinalIgnoreCase))
            .OrderBy(rowData => ParseTaskDateOnly(rowData.deadline))
            .ToList();

        if (missionRowsScroll == null)
            return;

        missionRowsScroll.Clear();

        if (filtered.Count == 0)
        {
            missionRowsScroll.Add(new Label("Görev bulunamadı."));
            return;
        }

        foreach (var task in filtered)
            missionRowsScroll.Add(BuildMissionRow(task));
    }

    private VisualElement BuildMissionRow(ContentTaskItemDto task)
    {
        var row = new VisualElement();
        row.AddToClassList("my-row");
        if (selectedMyMissionTaskId == task.id)
            row.AddToClassList("is-selected");

        var titleCell = new VisualElement();
        titleCell.AddToClassList("my-cell");
        titleCell.AddToClassList("my-title-cell");
        titleCell.Add(new Label(SafeText(task.title)) { name = "MissionTitle" });
        titleCell.Q<Label>("MissionTitle")?.AddToClassList("my-title-main");
        var assignerText = $"Atayan: {ResolveAssignerName(task)}";
        var sub = new Label(assignerText);
        sub.AddToClassList("my-title-sub");
        titleCell.Add(sub);

        row.Add(titleCell);
        row.Add(BuildRowCell(SafeText(task.experimentName)));
        row.Add(BuildRowCell(SafeText(task.taskType)));

        var priority = new Label(SafeText(task.priority));
        priority.AddToClassList("my-cell");
        priority.AddToClassList("my-priority");
        priority.AddToClassList(GetPriorityClass(task.priority));
        row.Add(priority);

        var status = new Label(SafeText(task.status));
        bool isOverdue = IsTaskOverdue(task);
        string statusText = isOverdue ? "Geçmiş" : SafeText(task.status);
        status.text = statusText;
        status.AddToClassList("my-cell");
        status.AddToClassList("my-status");
        status.AddToClassList(isOverdue ? "red" : GetStatusClass(task.status));
        row.Add(status);

        var deadline = new Label(FormatTaskDateLong(task.deadline));
        deadline.AddToClassList("my-cell");
        deadline.AddToClassList("my-deadline");
        if (isOverdue)
            deadline.AddToClassList("danger");
        row.Add(deadline);

        int taskId = task.id;
        row.RegisterCallback<ClickEvent>(_ => StartCoroutine(OpenMissionDetailPage(taskId)));
        return row;
    }

    private Label BuildRowCell(string text)
    {
        var label = new Label(text);
        label.AddToClassList("my-cell");
        return label;
    }

    private bool MatchesMissionTab(ContentTaskItemDto row)
    {
        if (activeMissionTab == "Tümü") return true;
        if (activeMissionTab == "Devam Edenler") return IsInProgressStatus(row.status);
        if (activeMissionTab == "İncelemede") return IsInReviewStatus(row.status);
        if (activeMissionTab == "Revizyonda") return IsInRevisionStatus(row.status);
        if (activeMissionTab == "Tamamlananlar") return IsTaskCompleted(row.status);
        if (activeMissionTab == "Gecikenler") return IsTaskOverdue(row);
        return true;
    }

    private IEnumerator OpenMissionDetailPage(int taskId)
    {
        if (taskId <= 0)
            yield break;

        yield return StartCoroutine(FetchMissionDetail(taskId));
        if (selectedMyMission == null)
            yield break;

        ShowPage("MissionDetailPage");
        yield return StartCoroutine(FetchMissionComments(taskId));
    }

    private IEnumerator FetchMissionDetail(int taskId)
    {
        selectedMyMission = null;
        selectedMyMissionTaskId = 0;

        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath + "/" + taskId;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        selectedMyMission = JsonUtility.FromJson<ContentTaskItemDto>(raw);
        if (selectedMyMission == null)
            yield break;

        selectedMyMissionTaskId = selectedMyMission.id;
        PopulateMissionDetail(selectedMyMission);
        RenderMyMissionRows();
    }

    private void PopulateMissionDetail(ContentTaskItemDto data)
    {
        if (data == null)
            return;

        if (missionDetailKickerLabel != null) missionDetailKickerLabel.text = $"Görev #{data.id}";
        if (missionDetailTitleLabel != null) missionDetailTitleLabel.text = SafeText(data.title);
        bool isOverdue = IsTaskOverdue(data);
        if (missionDetailStatusBadge != null) missionDetailStatusBadge.text = isOverdue ? "Geçmiş" : SafeText(data.status);
        if (missionDetailPriorityBadge != null) missionDetailPriorityBadge.text = $"{SafeText(data.priority)} Öncelik";
        if (missionDetailRevisionPriorityBadge != null)
        {
            bool hasRevisionPriority = !string.IsNullOrWhiteSpace(data.latestRevisionPriority);
            missionDetailRevisionPriorityBadge.text = hasRevisionPriority
                ? $"Revizyon Önceliği: {SafeText(data.latestRevisionPriority)}"
                : "Revizyon Önceliği: -";
            missionDetailRevisionPriorityBadge.style.display = hasRevisionPriority ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (missionDetailOwnerBadge != null) missionDetailOwnerBadge.text = SafeText(data.assigneeName);
        if (senderAdminLabel != null) senderAdminLabel.text = ResolveAssignerName(data);
        if (missionDetailDescLabel != null) missionDetailDescLabel.text = SafeText(data.description);

        if (missionRevisionNoteBlock != null && missionRevisionNoteLabel != null)
        {
            bool hasRevisionNote = !string.IsNullOrWhiteSpace(data.latestRevisionNote);
            missionRevisionNoteBlock.style.display = hasRevisionNote ? DisplayStyle.Flex : DisplayStyle.None;
            missionRevisionNoteLabel.text = hasRevisionNote ? SafeText(data.latestRevisionNote) : "-";
        }
        if (missionStartDateLabel != null) missionStartDateLabel.text = FormatTaskDateLong(data.startDate);
        if (missionDetailDeadlineLabel != null) missionDetailDeadlineLabel.text = FormatTaskDateLong(data.deadline);
        if (missionTypeLabel != null) missionTypeLabel.text = SafeText(data.taskType);
        if (missionStatusInfoLabel != null) missionStatusInfoLabel.text = isOverdue ? "Geçmiş" : SafeText(data.status);

        RenderExpectedOutputs(data);
        RenderMissionTimeline(data);

        if (sendMissionBtn != null)
            sendMissionBtn.SetEnabled(CanSubmitTask(data.status));

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "";
    }

    private void RenderExpectedOutputs(ContentTaskItemDto data)
    {
        if (missionExpectedOutputsList == null)
            return;

        missionExpectedOutputsList.Clear();
        var parts = (data != null ? (data.expectedOutput ?? "") : "")
            .Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (parts.Count == 0)
            parts.Add("Beklenen çıktı bilgisi girilmedi.");

        foreach (var part in parts)
        {
            var label = new Label($"• {part}");
            label.AddToClassList("detail-list-item");
            missionExpectedOutputsList.Add(label);
        }
    }

    private void RenderMissionTimeline(ContentTaskItemDto item)
    {
        if (missionTimelineScroll == null)
            return;

        missionTimelineScroll.Clear();
        missionTimelineScroll.Add(BuildTimelineItem($"{FormatDateTimeTr(item.createdAtUtc)} Görev atandı."));

        if (!string.IsNullOrWhiteSpace(item.latestRevisionRequestedAt))
        {
            missionTimelineScroll.Add(BuildTimelineItem($"{FormatDateTimeTr(item.latestRevisionRequestedAt)} Revizyon talebi geldi.", true));
            if (!string.IsNullOrWhiteSpace(item.latestRevisionType))
                missionTimelineScroll.Add(BuildTimelineItem($"Revizyon Türü: {SafeText(item.latestRevisionType)}", true));
            if (!string.IsNullOrWhiteSpace(item.latestRevisionDeadline))
                missionTimelineScroll.Add(BuildTimelineItem($"Yeni Teslim Tarihi: {FormatTaskDateLong(item.latestRevisionDeadline)}", true));
        }

        if (!string.IsNullOrWhiteSpace(item.updatedAtUtc) && !string.Equals(item.updatedAtUtc, item.createdAtUtc, StringComparison.OrdinalIgnoreCase))
            missionTimelineScroll.Add(BuildTimelineItem($"{FormatDateTimeTr(item.updatedAtUtc)} Durum '{SafeText(item.status)}'."));

        missionTimelineScroll.Add(BuildTimelineItem($"Son teslim: {FormatTaskDateLong(item.deadline)}"));
    }

    private Label BuildTimelineItem(string text, bool isRevision = false)
    {
        var label = new Label(SafeText(text));
        label.AddToClassList("timeline-item");
        if (isRevision)
            label.AddToClassList("revision-highlight");
        return label;
    }

    private IEnumerator FetchMissionComments(int taskId)
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath + "/" + taskId + "/comments";
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            RenderMissionComments(Array.Empty<ContentTaskCommentDto>());
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Yorumlar alınamadı ({req.responseCode}).";
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentTaskCommentListWrapper>("{\"items\":" + raw + "}");
        var items = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentTaskCommentDto>();
        RenderMissionComments(items);
    }

    private void RenderMissionComments(ContentTaskCommentDto[] comments)
    {
        if (missionCommentsList == null)
            return;

        missionCommentsList.Clear();
        var items = comments ?? Array.Empty<ContentTaskCommentDto>();
        if (items.Length == 0)
        {
            var empty = new Label("Henüz yorum yok.");
            empty.AddToClassList("comment-text");
            missionCommentsList.Add(empty);
            return;
        }

        foreach (var c in items)
        {
            var commentItem = new VisualElement();
            commentItem.AddToClassList("comment-item");

            var author = new Label($"{SafeText(c.userName)} • {FormatDateTimeTr(c.createdAt)}");
            author.AddToClassList("comment-author");

            var text = new Label(SafeText(c.text));
            text.AddToClassList("comment-text");

            bool isAfterRevision = IsCommentAfterLatestRevision(c.createdAt);
            if (isAfterRevision)
            {
                commentItem.AddToClassList("revision-highlight");
                author.AddToClassList("revision-highlight");
                text.AddToClassList("revision-highlight");
            }

            commentItem.Add(author);
            commentItem.Add(text);
            missionCommentsList.Add(commentItem);
        }
    }

    private bool IsCommentAfterLatestRevision(string createdAt)
    {
        if (selectedMyMission == null || string.IsNullOrWhiteSpace(selectedMyMission.latestRevisionRequestedAt))
            return false;

        DateTime revisionAt = ParseDate(selectedMyMission.latestRevisionRequestedAt);
        DateTime commentAt = ParseDate(createdAt);
        if (revisionAt == DateTime.MinValue || commentAt == DateTime.MinValue)
            return false;

        return commentAt >= revisionAt;
    }

    private IEnumerator AddMissionComment()
    {
        if (router == null || selectedMyMissionTaskId <= 0)
            yield break;

        string text = missionCommentInput != null ? (missionCommentInput.value ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(text))
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Yorum metni zorunlu.";
            yield break;
        }

        string url = router.ApiBaseUrl + contentTaskPath + "/" + selectedMyMissionTaskId + "/comments";
        string json = JsonUtility.ToJson(new CreateContentTaskCommentRequest { text = text });
        using var req = AuthedJson(url, "POST", json);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Yorum eklenemedi ({req.responseCode}).";
            yield break;
        }

        if (missionCommentInput != null)
            missionCommentInput.value = "";

        yield return StartCoroutine(FetchMissionComments(selectedMyMissionTaskId));
        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Yorum eklendi.";
    }

    private IEnumerator SubmitMissionForReview()
    {
        if (router == null || selectedMyMissionTaskId <= 0 || selectedMyMission == null)
            yield break;

        if (!CanSubmitTask(selectedMyMission.status))
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Bu durumdan incelemeye gönderilemez.";
            yield break;
        }

        string url = router.ApiBaseUrl + contentTaskPath + "/" + selectedMyMissionTaskId + "/submit";
        using var req = AuthedJson(url, "POST", "{}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string resp = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Gönderim başarısız ({req.responseCode}). {resp}";
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        var persisted = JsonUtility.FromJson<ContentTaskItemDto>(raw);
        if (persisted == null || !IsInReviewStatus(persisted.status))
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Görev incelemeye alındı doğrulanamadı. Lütfen tekrar deneyin.";
            yield break;
        }

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Görev incelemeye gönderildi ve veritabanına kaydedildi.";

        yield return StartCoroutine(LoadMyMissionsPageData());
        yield return StartCoroutine(OpenMissionDetailPage(selectedMyMissionTaskId));
    }

    private bool CanSubmitTask(string status)
    {
        string normalized = NormalizeStatus(status);
        return normalized == "atandi" || normalized == "atandı" || normalized == "revizyonda";
    }

    private bool IsTaskCompleted(string status)
    {
        string normalized = NormalizeStatus(status);
        return normalized == "tamamlandi" || normalized == "tamamlandı" || normalized == "completed";
    }

    private bool IsInReviewStatus(string status)
    {
        string normalized = NormalizeStatus(status);
        return normalized.Contains("inceleme") || normalized.Contains("review");
    }

    private bool IsInRevisionStatus(string status)
    {
        string normalized = NormalizeStatus(status);
        return normalized.Contains("revizyon");
    }

    private bool IsInProgressStatus(string status)
    {
        if (IsTaskCompleted(status) || IsInReviewStatus(status) || IsInRevisionStatus(status))
            return false;

        string normalized = NormalizeStatus(status);
        return normalized == "atandi" || normalized == "atandı" || normalized.Contains("devam");
    }

    private bool IsTaskOverdue(ContentTaskItemDto task)
    {
        if (task == null || IsTaskCompleted(task.status))
            return false;

        var deadline = ParseTaskDateOnly(task.deadline);
        return deadline != DateTime.MinValue.Date && deadline < DateTime.Today;
    }

    private string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;

        string input = status.Trim().ToLowerInvariant().Replace('ı', 'i');
        string decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (char c in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private string GetPriorityClass(string priority)
    {
        string normalized = (priority ?? "").Trim().ToLowerInvariant();
        if (normalized.Contains("yüksek") || normalized.Contains("high")) return "high";
        if (normalized.Contains("orta") || normalized.Contains("medium")) return "medium";
        return "low";
    }

    private string GetStatusClass(string status)
    {
        if (IsInRevisionStatus(status)) return "purple";
        if (IsInReviewStatus(status)) return "orange";
        if (IsTaskCompleted(status)) return "green";
        if (IsInProgressStatus(status)) return "blue";
        return "red";
    }

    private string BuildAssignerText(int createdByUserId)
    {
        return createdByUserId > 0 ? $"Yönetici #{createdByUserId}" : "Yönetici";
    }

    private string ResolveAssignerName(ContentTaskItemDto item)
    {
        if (item == null)
            return "Yönetici";

        if (!string.IsNullOrWhiteSpace(item.createdByName))
            return item.createdByName.Trim();

        return BuildAssignerText(item.createdByUserId);
    }

    private DateTime ParseTaskDateOnly(string raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact.Date;

        return ParseDate(raw).Date;
    }

    private string FormatTaskDateLong(string raw)
    {
        var dt = ParseTaskDateOnly(raw);
        if (dt == DateTime.MinValue.Date)
            return "-";

        return dt.ToString("dd MMMM yyyy", new CultureInfo("tr-TR"));
    }

    private string FormatDateTimeTr(string raw)
    {
        var dt = ParseDate(raw);
        if (dt == DateTime.MinValue)
            return "-";

        return dt.ToString("dd MMM HH:mm", new CultureInfo("tr-TR"));
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

        if (assignmentItems != null)
        {
            foreach (var item in assignmentItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ExperimentName))
                    continue;

                string name = item.ExperimentName.Trim();
                if (!choices.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                    choices.Add(name);
            }
        }

        if (myMissionItems != null)
        {
            foreach (var item in myMissionItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.experimentName))
                    continue;

                string name = item.experimentName.Trim();
                if (!choices.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                    choices.Add(name);
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
        calToolbarMonthLabel.text = monthDate.ToString("MMMM yyyy", new CultureInfo("tr-TR"));
    }

    private void RenderCalendarMini()
    {
        if (calMiniGrid == null)
            return;

        calMiniGrid.Clear();

        var monthDate = new DateTime(calCurrentYear, calCurrentMonth + 1, 1);
        if (calMiniMonthLabel != null)
            calMiniMonthLabel.text = monthDate.ToString("MMMM yyyy", new CultureInfo("tr-TR"));

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
        calDayHeaderLabel.text = day.ToString("dddd, dd MMMM yyyy", new CultureInfo("tr-TR"));

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

            var lbl = new Label(date == default ? group.Key : date.ToString("dddd, dd MMMM yyyy", new CultureInfo("tr-TR")));
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

    [Serializable]
    private class AssignmentDto
    {
        public int Id;
        public string Title;
        public bool IsActive;
        public string StartDate;
        public int DurationDays;
        public string ExperimentName;
    }

    [Serializable]
    private class AssignmentListWrapper
    {
        public AssignmentDto[] items;
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
    private class ContentTaskItemDto
    {
        public int id;
        public string title;
        public string taskType;
        public string experimentName;
        public string estimatedDuration;
        public string startDate;
        public string deadline;
        public int assigneeUserId;
        public string assigneeName;
        public string priority;
        public string status;
        public int progressPercent;
        public string description;
        public string expectedOutput;
        public int createdByUserId;
        public string createdByName;
        public string createdAtUtc;
        public string updatedAtUtc;
        public string latestRevisionType;
        public string latestRevisionPriority;
        public string latestRevisionDeadline;
        public string latestRevisionNote;
        public string latestRevisionRequestedAt;
    }

    [Serializable]
    private class ContentTaskItemListWrapper
    {
        public ContentTaskItemDto[] items;
    }

    [Serializable]
    private class ContentTaskCommentDto
    {
        public int userId;
        public string userName;
        public string text;
        public string createdAt;
    }

    [Serializable]
    private class ContentTaskCommentListWrapper
    {
        public ContentTaskCommentDto[] items;
    }

    [Serializable]
    private class CreateContentTaskCommentRequest
    {
        public string text;
    }

    [Serializable]
    private class TodoItemDto
    {
        public int id;
        public string title;
        public string priority;
        public string dueDate;
        public string description;
        public string notes;
        public bool isCompleted;
        public string createdAtUtc;
        public string updatedAtUtc;
        public TodoSubtaskDto[] subtasks;
    }

    [Serializable]
    private class TodoItemListWrapper
    {
        public TodoItemDto[] items;
    }

    [Serializable]
    private class TodoSubtaskDto
    {
        public int id;
        public string title;
        public bool isCompleted;
    }

    [Serializable]
    private class UpsertTodoItemRequest
    {
        public string title;
        public string priority;
        public string dueDate;
        public string description;
        public string notes;
        public bool isCompleted;
    }

    [Serializable]
    private class TodoSubtaskCreateRequest
    {
        public string title;
    }

    [Serializable]
    private class TodoSubtaskUpdateRequest
    {
        public string title;
        public bool isCompleted;
    }
}