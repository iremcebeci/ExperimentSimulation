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
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string contentTaskPath = "/api/ContentTask";
    [SerializeField] private string todoPath = "/api/Todo";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";

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

    // Cached API data
    private AssignmentDto[] assignmentItems = Array.Empty<AssignmentDto>();
    private ClassActivityDto[] personalActivityItems = Array.Empty<ClassActivityDto>();
    private ProfileMeDto profileMe;
    private Coroutine sessionHeartbeatRoutine;

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
        BindMyMissionsPage();
        BindTodoPage();

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

        root.Q<Button>("StartSimulationBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            SetMenuActive("StartSimulationBtn");
            ShowPage("StartSimulationPage");
        });
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
        var names = new[] { "HomeBtn", "ProgressBtn", "ActivityBtn", "ProfileBtn", "StartSimulationBtn", "ExperimentsBtn", "MyMissionsBtn", "ToDoBtn" };

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

        // Profile istatistiklerini güncel görev durumu ile göstermek için görev verisini tazele.
        yield return StartCoroutine(FetchMyMissionTasks());

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
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ClassActivityListWrapper>("{\"items\":" + raw + "}");
        personalActivityItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ClassActivityDto>();

        RenderPersonalActivityFeed();
        ApplyHomeDashboardMetrics();
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
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<TodoItemListWrapper>("{\"items\":" + raw + "}");
        todoItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<TodoItemDto>();
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
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentTaskItemListWrapper>("{\"items\":" + raw + "}");
        myMissionItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentTaskItemDto>();
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