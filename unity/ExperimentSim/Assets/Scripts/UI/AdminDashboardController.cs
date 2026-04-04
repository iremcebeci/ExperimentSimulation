using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

public class AdminDashboardController : MonoBehaviour
{
    // =========================
    // 1) REFERENCES / FIELDS
    // =========================

    // Router (API base url vs)
    private AppRouter router;

    // Root + sayfa container
    private VisualElement root;
    private VisualElement mainContent;

    // -------------------------
    // Users List (ListUsersPage)
    // -------------------------
    private ScrollView usersList;
    private Label usersStatus;
    private Button refreshUsersBtn;
    private TextField userSearchTf;

    // -------------------------
    // Add User (AddUserPage)
    // -------------------------
    private TextField addNameTf, addSurnameTf, addEmailTf, addPasswordTf;
    private DropdownField addRoleDd;
    private Toggle addIsActiveTg;
    private Button addSaveBtn, addClearBtn;
    private Label addStatusLabel;

    // Users cache
    private List<UserRow> cachedUsers = new();

    // -------------------------
    // Roles (RolesPage)
    // -------------------------
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

    // Roles & users-lite cache
    private List<RoleRow> cachedRoles = new();
    private List<UserLite> cachedUsersLite = new();

    [SerializeField] private DashboardsHeaderController headerController;

    // -------------------------
    // API Paths
    // -------------------------
    [SerializeField] private string userPath = "/api/User";
    [SerializeField] private string rolesPath = "/api/Role";
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string contentTaskPath = "/api/ContentTask";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";

    // -------------------------
    // Home (HomePage)
    // -------------------------
    private VisualElement homePage;
    private Label homeTotalUsersValueLabel;
    private Label homeActiveUsersValueLabel;
    private Label homeRoleCountValueLabel;
    private Label homeNewUsersValueLabel;
    private ScrollView homeSummaryScroll;
    private Label homeChartPeakInfoLabel;
    private readonly List<VisualElement> homeChartBars = new();
    private readonly List<Label> homeChartValueLabels = new();
    private readonly float[] homeWeeklyHours = new float[7];

    // -------------------------
    // Activity (ActivityPage)
    // -------------------------
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
    private ClassActivityDto[] personalActivityItems = Array.Empty<ClassActivityDto>();

    // -------------------------
    // Profile (ProfilePage)
    // -------------------------
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
    private Button profileListUsersBtn;
    private Button profileLogoutBtn;
    private ProfileMeDto profileMe;

    // -------------------------
    // Content Management (CreatorsPage)
    // -------------------------
    private VisualElement creatorsPage;
    private Label taskActiveCountLabel;
    private Label taskTodayDeadlineCountLabel;
    private Label taskOverdueCountLabel;
    private Label taskReviewCountLabel;
    private TextField taskTitleInput;
    private TextField taskStartDateInput;
    private DropdownField taskTypeDropdown;
    private TextField taskDeadlineInput;
    private TextField taskExperimentInput;
    private TextField taskEstimatedDurationInput;
    private DropdownField taskAssigneeDropdown;
    private DropdownField taskPriorityDropdown;
    private TextField taskDescriptionInput;
    private TextField taskExpectedOutputInput;
    private Button assignTaskBtn;
    private Label assignTaskStatusLabel;
    private Label previewTaskTitleLabel;
    private Label previewTaskAssigneeLabel;
    private Label previewStatusBadge;
    private Label previewPriorityBadge;
    private Label previewDeadlineLabel;
    private Label previewDurationLabel;
    private ContentCreatorLiteDto[] taskAssigneeItems = Array.Empty<ContentCreatorLiteDto>();

    // -------------------------
    // Content Management (MissionsDataPage)
    // -------------------------
    private VisualElement missionsDataPage;
    private Label missionActiveCountLabel;
    private Label missionTodayCountLabel;
    private Label missionLateCountLabel;
    private Label missionReviewCountLabel;
    private Label missionActivePanelCountLabel;
    private Label missionTodayPanelCountLabel;
    private Label missionLatePanelCountLabel;
    private Label missionReviewPanelCountLabel;
    private ScrollView activeMissionList;
    private ScrollView todayMissionList;
    private ScrollView lateMissionList;
    private ScrollView reviewMissionList;
    private VisualElement missionDetailPage;
    private Label missionDetailKickerLabel;
    private Label missionDetailTitleLabel;
    private Label missionDetailStatusBadge;
    private Label missionDetailPriorityBadge;
    private Label missionDetailRevisionPriorityBadge;
    private Label missionDetailOwnerBadge;
    private Label missionAssignedCreatorLabel;
    private Label missionDetailDescLabel;
    private VisualElement missionExpectedOutputsList;
    private VisualElement missionRevisionNoteBlock;
    private Label missionRevisionNoteLabel;
    private VisualElement missionDetailFilesList;
    private Label missionStartDateLabel;
    private Label missionDeadlineDateLabel;
    private Label missionTypeLabel;
    private Label missionStatusInfoLabel;
    private Label missionTagsLabel;
    private Label missionProgressTextLabel;
    private VisualElement missionProgressFill;
    private ScrollView missionTimelineScroll;
    private VisualElement missionCommentsList;
    private TextField missionCommentInput;
    private Button addMissionCommentBtn;
    private Label missionCommentStatusLabel;
    private VisualElement revisionModal;
    private Button openRevisionModalBtn;
    private Button approveMissionBtn;
    private Button closeRevisionModalBtn;
    private Button cancelRevisionBtn;
    private Button submitRevisionBtn;
    private DropdownField revisionTypeDropdown;
    private DropdownField revisionPriorityDropdown;
    private TextField revisionDeadlineInput;
    private TextField revisionNoteInput;
    private Button backToMissionsBtn;
    private ContentTaskItemDto[] contentTaskItems = Array.Empty<ContentTaskItemDto>();
    private ContentTaskItemDto selectedMissionTask;
    private int selectedMissionTaskId;

    private Coroutine sessionHeartbeatRoutine;

    // =========================
    // 2) BIND / UI WIRING
    // =========================

    // Admin ekranı açılırken UI elemanlarını bulur, menü butonlarını bağlar
    public void Bind(AppRouter router, VisualElement dashboardView)
    {
        this.router = router;
        root = dashboardView;

        headerController?.Bind(router, root);

        // Ana sayfa container
        mainContent = root.Q<VisualElement>("MainContent");
        if (mainContent == null)
        {
            Debug.LogError("MainContent bulunamadı. AdminDashboard.uxml içine name=\"MainContent\" ekle.");
            return;
        }

        var welcomeUsernameLabel = root.Q<UnityEngine.UIElements.Label>("WelcomeUsernameLabel");
        var welcomeMessageLabel = root.Q<UnityEngine.UIElements.Label>("WelcomeMessageLabel");

        if (welcomeUsernameLabel != null)
            welcomeUsernameLabel.text = $"Merhaba, {router.CurrentName} {router.CurrentSurname}!";

        if (welcomeMessageLabel != null)
            welcomeMessageLabel.text = WelcomeText.BuildRoleMessage(router.CurrentRoleName);

        // -------------------------
        // Menü / Sayfa geçişleri
        // -------------------------
        root.Q<Button>("HomeBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToRootPage("HomePage", "HomeBtn");
            StartCoroutine(RefreshHomeData());
        });
        root.Q<Button>("AddUserBtn")?.RegisterCallback<ClickEvent>(_ => NavigateToSubPage("AddUserPage", "UserManagement", "AddUserBtn"));

        root.Q<Button>("ListUsersBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToSubPage("ListUsersPage", "UserManagement", "ListUsersBtn");
            StartCoroutine(FetchUsers());
        });

        root.Q<Button>("RolesBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToSubPage("RolesPage", "RolePermission", "RolesBtn");
            StartCoroutine(FetchRolesAndUsers());
        });

        root.Q<Button>("PermissionsBtn")?.RegisterCallback<ClickEvent>(_ => NavigateToSubPage("PermissionsPage", "RolePermission", "PermissionsBtn"));

        root.Q<Button>("ActivityBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToRootPage("ActivityPage", "ActivityBtn");
            StartCoroutine(FetchPersonalActivity());
        });

        root.Q<Button>("ProfileBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToRootPage("ProfilePage", "ProfileBtn");
            StartCoroutine(LoadProfilePageData());
        });

        root.Q<Button>("CreatorsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToSubPage("CreatorsPage", "ContentManagement", "CreatorsBtn");
            StartCoroutine(LoadTaskAssignPageData());
        });
        root.Q<Button>("MissionsDataBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToSubPage("MissionsDataPage", "ContentManagement", "MissionsDataBtn");
            StartCoroutine(LoadMissionsDataPageData());
        });

        root.Q<Button>("DatabaseOpsBtn")?.RegisterCallback<ClickEvent>(_ => NavigateToRootPage("DatabaseOpsPage", "DatabaseOpsBtn"));
        root.Q<Button>("SystemBtn")?.RegisterCallback<ClickEvent>(_ => NavigateToRootPage("SystemPage", "SystemBtn"));
        root.Q<Button>("StartSimulationBtn")?.RegisterCallback<ClickEvent>(_ => NavigateToRootPage("StartSimulationPage", "StartSimulationBtn"));

        // -------------------------
        // Users List UI refs
        // -------------------------
        usersList = root.Q<ScrollView>("UsersList");
        usersStatus = root.Q<Label>("UsersStatusLabel");
        refreshUsersBtn = root.Q<Button>("RefreshUsersBtn");
        userSearchTf = root.Q<TextField>("UserSearchTf");

        // -------------------------
        // Add User UI refs
        // -------------------------
        addNameTf = root.Q<TextField>("AddUser_NameTf");
        addSurnameTf = root.Q<TextField>("AddUser_SurnameTf");
        addEmailTf = root.Q<TextField>("AddUser_EmailTf");
        addPasswordTf = root.Q<TextField>("AddUser_PasswordTf");

        addRoleDd = root.Q<DropdownField>("AddUser_RoleDd");
        addIsActiveTg = root.Q<Toggle>("AddUser_IsActiveTg");

        addSaveBtn = root.Q<Button>("AddUser_SaveBtn");
        addClearBtn = root.Q<Button>("AddUser_ClearBtn");

        addStatusLabel = root.Q<Label>("AddUser_StatusLabel");

        // Add User rol dropdown seçenekleri
        if (addRoleDd != null)
        {
            addRoleDd.choices = new List<string> {
                "Öğrenci","Öğretmen","Bağımsız Kullanıcı","İçerik Üreticisi","Yönetici"
            };
            addRoleDd.value = "Öğrenci";
        }

        // Add User buton aksiyonları
        if (addSaveBtn != null)
            addSaveBtn.clicked += () => StartCoroutine(AddUser());

        if (addClearBtn != null)
            addClearBtn.clicked += ClearAddUserForm;

        // Users list refresh + arama
        if (refreshUsersBtn != null)
            refreshUsersBtn.clicked += () => StartCoroutine(FetchUsers());

        if (userSearchTf != null)
            userSearchTf.RegisterValueChangedCallback(_ => RenderUsersFiltered());

        // -------------------------
        // Home + Activity + Profile refs
        // -------------------------
        BindHomePage();
        BindPersonalActivityPage();
        BindProfilePage();
        BindTaskAssignPage();
        BindMissionsDataPage();

        // -------------------------
        // Roles UI refs
        // -------------------------
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

        // Roles buton aksiyonları
        if (rolesRefreshBtn != null)
            rolesRefreshBtn.clicked += () => StartCoroutine(FetchRolesAndUsers());

        if (rolesAddBtn != null)
            rolesAddBtn.clicked += () => StartCoroutine(AddRole());

        if (rolesAssignBtn != null)
            rolesAssignBtn.clicked += () => StartCoroutine(AssignRoleToUser());

        // Varsayılan sayfa
        NavigateToRootPage("HomePage", "HomeBtn");

        if (sessionHeartbeatRoutine != null)
            StopCoroutine(sessionHeartbeatRoutine);
        sessionHeartbeatRoutine = StartCoroutine(SessionHeartbeatLoop());

        StartCoroutine(InitialLoad());
    }

    private IEnumerator InitialLoad()
    {
        yield return StartCoroutine(FetchUsers());
        yield return StartCoroutine(FetchRoles());
        yield return StartCoroutine(FetchWeeklySessionHours());
        yield return StartCoroutine(FetchPersonalActivity());
        yield return StartCoroutine(LoadProfilePageData());
        yield return StartCoroutine(LoadTaskAssignPageData());
        yield return StartCoroutine(FetchContentTasks());
        RenderMissionsDataLists();
        ApplyHomeDashboardMetrics();
    }

    private IEnumerator RefreshHomeData()
    {
        yield return StartCoroutine(FetchUsers());
        yield return StartCoroutine(FetchRoles());
        yield return StartCoroutine(FetchWeeklySessionHours());
        yield return StartCoroutine(FetchContentTasks());
        ApplyHomeDashboardMetrics();
    }

    // =========================
    // 3) PAGE SWITCHING
    // =========================

    // MainContent içindeki sayfalardan sadece birini active yapar
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

    private void NavigateToRootPage(string pageName, string menuButtonName)
    {
        ShowPage(pageName);
        CloseAllSubMenus();
        SetSidebarActiveState(menuButtonName, null);
    }

    private void NavigateToSubPage(string pageName, string groupPrefix, string subMenuButtonName)
    {
        ShowPage(pageName);
        CloseAllSubMenus();
        SetSubMenuExpanded(groupPrefix, true);
        SetSidebarActiveState(groupPrefix + "Btn", subMenuButtonName);
    }

    private void CloseAllSubMenus()
    {
        SetSubMenuExpanded("UserManagement", false);
        SetSubMenuExpanded("RolePermission", false);
        SetSubMenuExpanded("ContentManagement", false);
    }

    private void SetSubMenuExpanded(string groupPrefix, bool expanded)
    {
        var subMenu = root.Q<VisualElement>(groupPrefix + "SubMenu");
        var chevron = root.Q<VisualElement>(groupPrefix + "Chevron");

        if (subMenu != null)
        {
            if (expanded) subMenu.AddToClassList("show");
            else subMenu.RemoveFromClassList("show");
        }

        if (chevron != null)
        {
            if (expanded) chevron.AddToClassList("rotate");
            else chevron.RemoveFromClassList("rotate");
        }
    }

    private void SetSidebarActiveState(string activeMenuButtonName, string activeSubMenuButtonName)
    {
        var menuButtons = root.Query<Button>(className: "menu-item").ToList();
        foreach (var btn in menuButtons)
            btn.RemoveFromClassList("active");

        var subMenuButtons = root.Query<Button>(className: "sub-menu-item").ToList();
        foreach (var btn in subMenuButtons)
            btn.RemoveFromClassList("active");

        root.Q<Button>(activeMenuButtonName)?.AddToClassList("active");
        if (!string.IsNullOrWhiteSpace(activeSubMenuButtonName))
            root.Q<Button>(activeSubMenuButtonName)?.AddToClassList("active");
    }

    private void BindHomePage()
    {
        homePage = root.Q<VisualElement>("StudentHomePage");
        if (homePage == null)
            return;

        homeTotalUsersValueLabel = homePage.Q<Label>("TcTotalClassValueLabel");
        homeActiveUsersValueLabel = homePage.Q<Label>("TcActiveAssignmentValueLabel");
        homeRoleCountValueLabel = homePage.Q<Label>("TcTotalStudentValueLabel");
        homeNewUsersValueLabel = homePage.Q<Label>("TcCompletedAssignmentValueLabel");
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
        var users = (cachedUsers ?? new List<UserRow>()).Where(u => u != null).ToList();
        int totalUsers = users.Count;
        int activeUsers = users.Count(u => u.IsActive);
        int totalRoles = (cachedRoles ?? new List<RoleRow>()).Count;
        int newUsersThisWeek = users.Count(u =>
        {
            if (!TryParseDate(u.CreatedAt, out var dt)) return false;
            return dt.Date >= DateTime.Today.AddDays(-6);
        });

        if (homeTotalUsersValueLabel != null) homeTotalUsersValueLabel.text = totalUsers.ToString();
        if (homeActiveUsersValueLabel != null) homeActiveUsersValueLabel.text = activeUsers.ToString();
        if (homeRoleCountValueLabel != null) homeRoleCountValueLabel.text = totalRoles.ToString();
        if (homeNewUsersValueLabel != null) homeNewUsersValueLabel.text = newUsersThisWeek.ToString();

        var tasks = (contentTaskItems ?? Array.Empty<ContentTaskItemDto>())
            .Where(t => t != null)
            .ToList();

        var latestAssignedTask = tasks
            .Select(t => new { item = t, created = ParseDate(t.createdAtUtc) })
            .OrderByDescending(x => x.created)
            .FirstOrDefault();

        if (latestAssignedTask == null)
            SetHomeSummaryItem(0, "-", "Atanan iş bulunmuyor");
        else
            SetHomeSummaryItem(
                0,
                SafeText(latestAssignedTask.item.title),
                $"Atanan: {SafeText(latestAssignedTask.item.assigneeName)} • {FormatTaskDateLong(latestAssignedTask.item.createdAtUtc)}");

        var latestCompletedTask = tasks
            .Where(t => IsTaskCompleted(t.status))
            .Select(t => new { item = t, updated = ParseDate(t.updatedAtUtc) })
            .OrderByDescending(x => x.updated)
            .FirstOrDefault();

        if (latestCompletedTask == null)
            SetHomeSummaryItem(1, "-", "Tamamlanan iş bulunmuyor");
        else
            SetHomeSummaryItem(
                1,
                SafeText(latestCompletedTask.item.title),
                $"Atanan: {SafeText(latestCompletedTask.item.assigneeName)} • {FormatTaskDateLong(latestCompletedTask.item.updatedAtUtc)}");

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

    private IEnumerator FetchPersonalActivity()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + personalActivityPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
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
            "assignment" => string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "TaskAssigned", StringComparison.OrdinalIgnoreCase),
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

        profileHomeBtn = profilePage.Q<Button>("AdminGoHomeBtn");
        profileListUsersBtn = profilePage.Q<Button>("AdminGoUsersBtn");
        profileLogoutBtn = profilePage.Q<Button>("AdminLogoutBtn") ?? profilePage.Q<Button>("TeacherLogoutBtn");

        if (profileHomeBtn != null)
            profileHomeBtn.clicked += () =>
            {
                NavigateToRootPage("HomePage", "HomeBtn");
                StartCoroutine(RefreshHomeData());
            };

        if (profileListUsersBtn != null)
            profileListUsersBtn.clicked += () =>
            {
                NavigateToSubPage("ListUsersPage", "UserManagement", "ListUsersBtn");
                StartCoroutine(FetchUsers());
            };

        if (profileLogoutBtn != null)
            profileLogoutBtn.clicked += () => StartCoroutine(EndSessionAndLogout());
    }

    private void BindTaskAssignPage()
    {
        creatorsPage = root.Q<VisualElement>("CreatorsPage");
        if (creatorsPage == null)
            return;

        taskActiveCountLabel = creatorsPage.Q<Label>("TaskActiveCountLabel");
        taskTodayDeadlineCountLabel = creatorsPage.Q<Label>("TaskTodayDeadlineCountLabel");
        taskOverdueCountLabel = creatorsPage.Q<Label>("TaskOverdueCountLabel");
        taskReviewCountLabel = creatorsPage.Q<Label>("TaskReviewCountLabel");

        taskTitleInput = creatorsPage.Q<TextField>("TaskTitleInput");
        taskStartDateInput = creatorsPage.Q<TextField>("TaskStartDateInput");
        taskTypeDropdown = creatorsPage.Q<DropdownField>("TaskTypeDropdown");
        taskDeadlineInput = creatorsPage.Q<TextField>("TaskDeadlineInput");
        taskExperimentInput = creatorsPage.Q<TextField>("TaskExperimentInput");
        taskEstimatedDurationInput = creatorsPage.Q<TextField>("TaskEstimatedDurationInput");
        taskAssigneeDropdown = creatorsPage.Q<DropdownField>("TaskAssigneeDropdown");
        taskPriorityDropdown = creatorsPage.Q<DropdownField>("TaskPriorityDropdown");
        taskDescriptionInput = creatorsPage.Q<TextField>("TaskDescriptionInput");
        taskExpectedOutputInput = creatorsPage.Q<TextField>("TaskExpectedOutputInput");
        assignTaskBtn = creatorsPage.Q<Button>("AssignTaskBtn");
        assignTaskStatusLabel = creatorsPage.Q<Label>("AssignTaskStatusLabel");

        previewTaskTitleLabel = creatorsPage.Q<Label>("PreviewTaskTitleLabel");
        previewTaskAssigneeLabel = creatorsPage.Q<Label>("PreviewTaskAssigneeLabel");
        previewStatusBadge = creatorsPage.Q<Label>("PreviewStatusBadge");
        previewPriorityBadge = creatorsPage.Q<Label>("PreviewPriorityBadge");
        previewDeadlineLabel = creatorsPage.Q<Label>("PreviewDeadlineLabel");
        previewDurationLabel = creatorsPage.Q<Label>("PreviewDurationLabel");

        if (taskTypeDropdown != null)
        {
            taskTypeDropdown.choices = new List<string>
            {
                "Deney Optimizasyonu",
                "Hata Düzeltme",
                "Test / Kontrol",
                "Rapor Hazırlama",
                "Yeni Deney Ekleme"
            };
            taskTypeDropdown.value = taskTypeDropdown.choices[0];
        }

        if (taskPriorityDropdown != null)
        {
            taskPriorityDropdown.choices = new List<string> { "Düşük", "Orta", "Yüksek", "Kritik" };
            taskPriorityDropdown.value = "Orta";
        }

        if (taskStartDateInput != null && string.IsNullOrWhiteSpace(taskStartDateInput.value))
            taskStartDateInput.value = DateTime.Today.ToString("yyyy-MM-dd");
        if (taskDeadlineInput != null && string.IsNullOrWhiteSpace(taskDeadlineInput.value))
            taskDeadlineInput.value = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

        if (assignTaskBtn != null)
            assignTaskBtn.clicked += () => StartCoroutine(AssignContentTask());

        RegisterTaskPreviewCallbacks();
        UpdateTaskPreview();
    }

    private void RegisterTaskPreviewCallbacks()
    {
        taskTitleInput?.RegisterValueChangedCallback(_ => UpdateTaskPreview());
        taskAssigneeDropdown?.RegisterValueChangedCallback(_ => UpdateTaskPreview());
        taskPriorityDropdown?.RegisterValueChangedCallback(_ => UpdateTaskPreview());
        taskDeadlineInput?.RegisterValueChangedCallback(_ => UpdateTaskPreview());
        taskEstimatedDurationInput?.RegisterValueChangedCallback(_ => UpdateTaskPreview());
    }

    private void UpdateTaskPreview()
    {
        if (previewTaskTitleLabel != null)
            previewTaskTitleLabel.text = SafeText(taskTitleInput != null ? taskTitleInput.value : "-");

        if (previewTaskAssigneeLabel != null)
        {
            string assignee = taskAssigneeDropdown != null ? taskAssigneeDropdown.value : "-";
            previewTaskAssigneeLabel.text = $"Atanan Kişi: {SafeText(assignee)}";
        }

        if (previewStatusBadge != null)
            previewStatusBadge.text = "Atandı";

        if (previewPriorityBadge != null)
            previewPriorityBadge.text = $"{SafeText(taskPriorityDropdown != null ? taskPriorityDropdown.value : "Orta")} Öncelik";

        if (previewDeadlineLabel != null)
            previewDeadlineLabel.text = SafeText(taskDeadlineInput != null ? taskDeadlineInput.value : "-");

        if (previewDurationLabel != null)
        {
            string duration = SafeText(taskEstimatedDurationInput != null ? taskEstimatedDurationInput.value : "-");
            previewDurationLabel.text = duration == "-" ? "-" : duration;
        }
    }

    private void BindMissionsDataPage()
    {
        missionsDataPage = root.Q<VisualElement>("MissionsDataPage");
        missionDetailPage = root.Q<VisualElement>("MissionDetailPage");

        if (missionsDataPage == null)
            return;

        missionActiveCountLabel = missionsDataPage.Q<Label>("MissionActiveCountLabel");
        missionTodayCountLabel = missionsDataPage.Q<Label>("MissionTodayCountLabel");
        missionLateCountLabel = missionsDataPage.Q<Label>("MissionLateCountLabel");
        missionReviewCountLabel = missionsDataPage.Q<Label>("MissionReviewCountLabel");

        activeMissionList = missionsDataPage.Q<ScrollView>("ActiveMissionList");
        todayMissionList = missionsDataPage.Q<ScrollView>("TodayMissionList");
        lateMissionList = missionsDataPage.Q<ScrollView>("LateMissionList");
        reviewMissionList = missionsDataPage.Q<ScrollView>("ReviewMissionList");

        var panelCounts = missionsDataPage.Query<Label>(className: "mission-panel-count").ToList();
        if (panelCounts.Count >= 4)
        {
            missionActivePanelCountLabel = panelCounts[0];
            missionTodayPanelCountLabel = panelCounts[1];
            missionLatePanelCountLabel = panelCounts[2];
            missionReviewPanelCountLabel = panelCounts[3];
        }

        missionDetailKickerLabel = root.Q<Label>("MissionDetailKickerLabel");
        missionDetailTitleLabel = root.Q<Label>("MissionDetailTitleLabel");
        missionDetailStatusBadge = root.Q<Label>("MissionDetailStatusBadge");
        missionDetailPriorityBadge = root.Q<Label>("MissionDetailPriorityBadge");
        missionDetailRevisionPriorityBadge = root.Q<Label>("MissionDetailRevisionPriorityBadge");
        missionDetailOwnerBadge = root.Q<Label>("MissionDetailOwnerBadge");
        missionAssignedCreatorLabel = root.Q<Label>("MissionAssignedCreatorLabel");
        missionDetailDescLabel = root.Q<Label>("MissionDetailDescLabel");
        missionExpectedOutputsList = root.Q<VisualElement>("MissionExpectedOutputsList");
        missionRevisionNoteBlock = root.Q<VisualElement>("MissionRevisionNoteBlock");
        missionRevisionNoteLabel = root.Q<Label>("MissionRevisionNoteLabel");
        missionDetailFilesList = root.Q<VisualElement>("MissionDetailFilesList");
        missionStartDateLabel = root.Q<Label>("MissionStartDateLabel");
        missionDeadlineDateLabel = root.Q<Label>("MissionDeadlineDateLabel");
        missionTypeLabel = root.Q<Label>("MissionTypeLabel");
        missionStatusInfoLabel = root.Q<Label>("MissionStatusInfoLabel");
        missionTagsLabel = root.Q<Label>("MissionTagsLabel");
        missionProgressTextLabel = root.Q<Label>("MissionProgressTextLabel");
        missionProgressFill = root.Q<VisualElement>("MissionProgressFill");
        missionTimelineScroll = root.Q<ScrollView>("MissionTimelineScroll");
        missionCommentsList = root.Q<VisualElement>("MissionCommentsList");
        missionCommentInput = root.Q<TextField>("MissionCommentInput");
        addMissionCommentBtn = root.Q<Button>("AddMissionCommentBtn");
        missionCommentStatusLabel = root.Q<Label>("MissionCommentStatusLabel");
        revisionModal = root.Q<VisualElement>("RevisionModal");
        openRevisionModalBtn = root.Q<Button>("OpenRevisionModalBtn");
        approveMissionBtn = root.Q<Button>("ApproveMissionBtn");
        closeRevisionModalBtn = root.Q<Button>("CloseRevisionModalBtn");
        cancelRevisionBtn = root.Q<Button>("CancelRevisionBtn");
        submitRevisionBtn = root.Q<Button>("SubmitRevisionBtn");
        revisionTypeDropdown = root.Q<DropdownField>("RevisionTypeDropdown");
        revisionPriorityDropdown = root.Q<DropdownField>("RevisionPriorityDropdown");
        revisionDeadlineInput = root.Q<TextField>("RevisionDeadlineInput");
        revisionNoteInput = root.Q<TextField>("RevisionNoteInput");
        backToMissionsBtn = root.Q<Button>("BackToMissionsBtn");

        if (backToMissionsBtn != null)
        {
            backToMissionsBtn.clicked += () =>
            {
                NavigateToSubPage("MissionsDataPage", "ContentManagement", "MissionsDataBtn");
            };
        }

        if (addMissionCommentBtn != null)
            addMissionCommentBtn.clicked += () => StartCoroutine(AddMissionComment());

        if (openRevisionModalBtn != null)
            openRevisionModalBtn.clicked += OpenRevisionModal;

        if (approveMissionBtn != null)
            approveMissionBtn.clicked += () => StartCoroutine(ApproveSelectedMissionTask());

        if (closeRevisionModalBtn != null)
            closeRevisionModalBtn.clicked += CloseRevisionModal;

        if (cancelRevisionBtn != null)
            cancelRevisionBtn.clicked += CloseRevisionModal;

        if (submitRevisionBtn != null)
            submitRevisionBtn.clicked += () => StartCoroutine(SubmitRevisionRequest());

        if (revisionModal != null)
        {
            revisionModal.AddToClassList("hidden");
            revisionModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == revisionModal)
                    CloseRevisionModal();
            });
        }

        InitializeRevisionModalFields();
    }

    private void InitializeRevisionModalFields()
    {
        if (revisionTypeDropdown != null)
        {
            revisionTypeDropdown.choices = new List<string> { "İçerik", "Teknik", "Biçim", "Veri", "Diğer" };
            revisionTypeDropdown.value = revisionTypeDropdown.choices[0];
        }

        if (revisionPriorityDropdown != null)
        {
            revisionPriorityDropdown.choices = new List<string> { "Düşük", "Orta", "Yüksek", "Kritik" };
            revisionPriorityDropdown.value = "Orta";
        }

        if (revisionDeadlineInput != null)
            revisionDeadlineInput.value = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");

        if (revisionNoteInput != null)
            revisionNoteInput.value = "";
    }

    private IEnumerator LoadMissionsDataPageData()
    {
        yield return StartCoroutine(FetchContentTaskSummary());
        yield return StartCoroutine(FetchContentTasks());
        RenderMissionsDataLists();
    }

    private IEnumerator LoadTaskAssignPageData()
    {
        yield return StartCoroutine(FetchContentTaskCreators());
        yield return StartCoroutine(FetchContentTaskSummary());
        UpdateTaskPreview();
    }

    private IEnumerator FetchContentTaskCreators()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath + "/creators";
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            taskAssigneeItems = Array.Empty<ContentCreatorLiteDto>();
            if (assignTaskStatusLabel != null)
                assignTaskStatusLabel.text = $"İçerik üreticileri alınamadı ({req.responseCode}).";
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentCreatorLiteListWrapper>("{\"items\":" + raw + "}");
        taskAssigneeItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentCreatorLiteDto>();

        if (taskAssigneeDropdown != null)
        {
            var choices = new List<string>();
            foreach (var c in taskAssigneeItems)
                choices.Add($"{SafeText(c.fullName)}");

            if (choices.Count == 0)
                choices.Add("-");

            taskAssigneeDropdown.choices = choices;
            taskAssigneeDropdown.value = choices[0];
        }

        if (assignTaskStatusLabel != null)
            assignTaskStatusLabel.text = "";
    }

    private IEnumerator FetchContentTaskSummary()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath + "/summary";
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        var summary = JsonUtility.FromJson<ContentTaskSummaryDto>(raw);
        if (summary == null)
            yield break;

        if (taskActiveCountLabel != null) taskActiveCountLabel.text = Mathf.Max(0, summary.activeCount).ToString();
        if (taskTodayDeadlineCountLabel != null) taskTodayDeadlineCountLabel.text = Mathf.Max(0, summary.todayDeadlineCount).ToString();
        if (taskOverdueCountLabel != null) taskOverdueCountLabel.text = Mathf.Max(0, summary.overdueCount).ToString();
        if (taskReviewCountLabel != null) taskReviewCountLabel.text = Mathf.Max(0, summary.reviewCount).ToString();

        if (missionActiveCountLabel != null) missionActiveCountLabel.text = Mathf.Max(0, summary.activeCount).ToString();
        if (missionTodayCountLabel != null) missionTodayCountLabel.text = Mathf.Max(0, summary.todayDeadlineCount).ToString();
        if (missionLateCountLabel != null) missionLateCountLabel.text = Mathf.Max(0, summary.overdueCount).ToString();
        if (missionReviewCountLabel != null) missionReviewCountLabel.text = Mathf.Max(0, summary.reviewCount).ToString();

        if (missionActivePanelCountLabel != null) missionActivePanelCountLabel.text = Mathf.Max(0, summary.activeCount).ToString();
        if (missionTodayPanelCountLabel != null) missionTodayPanelCountLabel.text = Mathf.Max(0, summary.todayDeadlineCount).ToString();
        if (missionLatePanelCountLabel != null) missionLatePanelCountLabel.text = Mathf.Max(0, summary.overdueCount).ToString();
        if (missionReviewPanelCountLabel != null) missionReviewPanelCountLabel.text = Mathf.Max(0, summary.reviewCount).ToString();
    }

    private IEnumerator FetchContentTasks()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            contentTaskItems = Array.Empty<ContentTaskItemDto>();
            RenderMissionsDataLists();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentTaskItemListWrapper>("{\"items\":" + raw + "}");
        contentTaskItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentTaskItemDto>();
    }

    private void RenderMissionsDataLists()
    {
        var all = (contentTaskItems ?? Array.Empty<ContentTaskItemDto>())
            .Where(t => t != null)
            .ToList();

        var today = DateTime.Today;
        var todayItems = all.Where(t => ParseTaskDateOnly(t.deadline).Date == today).ToList();
        var lateItems = all.Where(t => ParseTaskDateOnly(t.deadline).Date < today && !IsTaskCompleted(t.status)).ToList();
        var reviewItems = all.Where(t => IsTaskReviewStatus(t.status) || IsLikelyReviewCandidate(t)).ToList();

        RenderMissionList(activeMissionList, all, null, BuildMissionMetaText);
        RenderMissionList(todayMissionList, todayItems, null, BuildMissionMetaText);
        RenderMissionList(lateMissionList, lateItems, "late", BuildMissionMetaText);
        RenderMissionList(reviewMissionList, reviewItems, "review", BuildReviewMissionMetaText);

        if (missionActivePanelCountLabel != null) missionActivePanelCountLabel.text = all.Count.ToString();
        if (missionTodayPanelCountLabel != null) missionTodayPanelCountLabel.text = todayItems.Count.ToString();
        if (missionLatePanelCountLabel != null) missionLatePanelCountLabel.text = lateItems.Count.ToString();
        if (missionReviewPanelCountLabel != null) missionReviewPanelCountLabel.text = reviewItems.Count.ToString();
    }

    private void RenderMissionList(
        ScrollView list,
        List<ContentTaskItemDto> items,
        string missionItemExtraClass,
        Func<ContentTaskItemDto, string> metaFactory)
    {
        if (list == null)
            return;

        list.Clear();

        if (items == null || items.Count == 0)
        {
            var empty = new Label("Kayıt bulunamadı.");
            empty.AddToClassList("mission-item-text");
            list.Add(empty);
            return;
        }

        foreach (var item in items)
        {
            var button = new Button();
            button.AddToClassList("mission-link");
            int missionId = item.id;
            button.clicked += () => StartCoroutine(OpenMissionDetailPage(missionId));

            var missionItem = new VisualElement();
            missionItem.AddToClassList("mission-item");
            if (!string.IsNullOrWhiteSpace(missionItemExtraClass))
                missionItem.AddToClassList(missionItemExtraClass);

            if (string.IsNullOrWhiteSpace(missionItemExtraClass))
            {
                if (IsTaskInRevision(item.status))
                    missionItem.AddToClassList("revision");
                else if (IsTaskPastDue(item) && !IsTaskCompleted(item.status))
                    missionItem.AddToClassList("past");
            }

            if (string.Equals(missionItemExtraClass, "review", StringComparison.OrdinalIgnoreCase) && IsTaskInRevision(item.status))
                missionItem.AddToClassList("revision");

            var title = new Label(SafeText(item.title));
            title.AddToClassList("mission-item-title");

            var meta = new Label(metaFactory != null ? metaFactory(item) : BuildMissionMetaText(item));
            meta.AddToClassList("mission-item-text");

            missionItem.Add(title);
            missionItem.Add(meta);
            button.Add(missionItem);
            list.Add(button);
        }
    }

    private IEnumerator OpenMissionDetailPage(int taskId)
    {
        if (taskId <= 0)
            yield break;

        yield return StartCoroutine(FetchContentTaskDetail(taskId));

        if (selectedMissionTask == null)
            yield break;

        ShowPage("MissionDetailPage");
        SetSubMenuExpanded("ContentManagement", true);
        SetSidebarActiveState("ContentManagementBtn", "MissionsDataBtn");

        yield return StartCoroutine(FetchMissionComments(taskId));
    }

    private IEnumerator FetchContentTaskDetail(int taskId)
    {
        selectedMissionTask = null;
        selectedMissionTaskId = 0;

        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + contentTaskPath + "/" + taskId;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            selectedMissionTask = (contentTaskItems ?? Array.Empty<ContentTaskItemDto>())
                .FirstOrDefault(t => t != null && t.id == taskId);

            if (selectedMissionTask == null)
            {
                if (missionCommentStatusLabel != null)
                    missionCommentStatusLabel.text = $"Görev detayı alınamadı ({req.responseCode}).";
                yield break;
            }
        }
        else
        {
            string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
            selectedMissionTask = JsonUtility.FromJson<ContentTaskItemDto>(raw);
            if (selectedMissionTask == null)
                yield break;
        }

        selectedMissionTaskId = selectedMissionTask.id;
        PopulateMissionDetail(selectedMissionTask);
    }

    private void PopulateMissionDetail(ContentTaskItemDto item)
    {
        if (item == null)
            return;

        if (missionDetailKickerLabel != null) missionDetailKickerLabel.text = $"Görev #{item.id}";
        if (missionDetailTitleLabel != null) missionDetailTitleLabel.text = SafeText(item.title);
        if (missionDetailStatusBadge != null) missionDetailStatusBadge.text = SafeText(item.status);
        if (missionDetailPriorityBadge != null) missionDetailPriorityBadge.text = $"{SafeText(item.priority)} Öncelik";
        if (missionDetailRevisionPriorityBadge != null)
        {
            bool hasRevisionPriority = !string.IsNullOrWhiteSpace(item.latestRevisionPriority);
            missionDetailRevisionPriorityBadge.text = hasRevisionPriority
                ? $"Revizyon Önceliği: {SafeText(item.latestRevisionPriority)}"
                : "Revizyon Önceliği: -";
            missionDetailRevisionPriorityBadge.style.display = hasRevisionPriority ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (missionDetailOwnerBadge != null) missionDetailOwnerBadge.text = SafeText(item.assigneeName);
        if (missionAssignedCreatorLabel != null) missionAssignedCreatorLabel.text = SafeText(item.assigneeName);
        if (missionDetailDescLabel != null) missionDetailDescLabel.text = SafeText(item.description);

        if (missionRevisionNoteBlock != null && missionRevisionNoteLabel != null)
        {
            bool hasRevisionNote = !string.IsNullOrWhiteSpace(item.latestRevisionNote);
            missionRevisionNoteBlock.style.display = hasRevisionNote ? DisplayStyle.Flex : DisplayStyle.None;
            missionRevisionNoteLabel.text = hasRevisionNote ? SafeText(item.latestRevisionNote) : "-";
        }

        if (missionStartDateLabel != null) missionStartDateLabel.text = FormatTaskDateLong(item.startDate);
        if (missionDeadlineDateLabel != null) missionDeadlineDateLabel.text = FormatTaskDateLong(item.deadline);
        if (missionTypeLabel != null) missionTypeLabel.text = SafeText(item.taskType);
        if (missionStatusInfoLabel != null) missionStatusInfoLabel.text = SafeText(item.status);
        if (missionTagsLabel != null) missionTagsLabel.text = BuildTaskTags(item);

        int progress = Mathf.Clamp(item.progressPercent, 0, 100);
        if (missionProgressTextLabel != null) missionProgressTextLabel.text = $"%{progress}";
        if (missionProgressFill != null) missionProgressFill.style.width = new Length(progress, LengthUnit.Percent);

        RenderExpectedOutputs(item);
        RenderTaskFiles(item);
        RenderMissionTimeline(item);

        ApplyRevisionDetailHighlight(item);

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "";

        if (openRevisionModalBtn != null)
            openRevisionModalBtn.SetEnabled(true);

        if (approveMissionBtn != null)
            approveMissionBtn.SetEnabled(true);
    }

    private bool CanRequestRevision(ContentTaskItemDto item)
    {
        if (item == null)
            return false;

        string normalized = NormalizeStatusForMatch(item.status);
        bool inReview = normalized.Contains("incele") || normalized.Contains("review");
        bool pastDue = IsTaskPastDue(item) && !IsTaskCompleted(item.status);
        return inReview || pastDue;
    }

    private void RenderExpectedOutputs(ContentTaskItemDto item)
    {
        if (missionExpectedOutputsList == null)
            return;

        missionExpectedOutputsList.Clear();
        var parts = (item != null ? (item.expectedOutput ?? "") : "")
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

    private void RenderTaskFiles(ContentTaskItemDto item)
    {
        if (missionDetailFilesList == null)
            return;

        missionDetailFilesList.Clear();
        var fallback = new Label("Ek dosya bulunmuyor.");
        fallback.AddToClassList("detail-list-item");
        missionDetailFilesList.Add(fallback);
    }

    private void RenderMissionTimeline(ContentTaskItemDto item)
    {
        if (missionTimelineScroll == null)
            return;

        missionTimelineScroll.Clear();

        missionTimelineScroll.Add(BuildTimelineItem($"{FormatDateTimeTr(item.createdAtUtc)} Görev atandı."));

        if (!string.IsNullOrWhiteSpace(item.updatedAtUtc) && !string.Equals(item.updatedAtUtc, item.createdAtUtc, StringComparison.OrdinalIgnoreCase))
            missionTimelineScroll.Add(BuildTimelineItem($"{FormatDateTimeTr(item.updatedAtUtc)} Durum '{SafeText(item.status)}' olarak güncellendi."));

        if (!string.IsNullOrWhiteSpace(item.latestRevisionRequestedAt))
        {
            missionTimelineScroll.Add(BuildTimelineItem($"{FormatDateTimeTr(item.latestRevisionRequestedAt)} Revizyon talebi oluşturuldu.", true));
            missionTimelineScroll.Add(BuildTimelineItem($"Revizyon Türü: {SafeText(item.latestRevisionType)}", true));
            if (!string.IsNullOrWhiteSpace(item.latestRevisionDeadline))
                missionTimelineScroll.Add(BuildTimelineItem($"Yeni Teslim Tarihi: {FormatTaskDateLong(item.latestRevisionDeadline)}", true));
        }

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
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Yorumlar alınamadı ({req.responseCode}).";
            RenderMissionComments(Array.Empty<ContentTaskCommentDto>());
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentTaskCommentListWrapper>("{\"items\":" + raw + "}");
        var items = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentTaskCommentDto>();
        RenderMissionComments(items);

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "";
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

    private void ApplyRevisionDetailHighlight(ContentTaskItemDto item)
    {
        bool hasRevisionContext = item != null && !string.IsNullOrWhiteSpace(item.latestRevisionRequestedAt);

        ToggleClass(missionDetailStatusBadge, "revision-highlight", IsTaskInRevision(item != null ? item.status : ""));
        ToggleClass(missionDeadlineDateLabel, "revision-highlight", hasRevisionContext && !string.IsNullOrWhiteSpace(item.latestRevisionDeadline));
    }

    private bool IsCommentAfterLatestRevision(string createdAt)
    {
        if (selectedMissionTask == null || string.IsNullOrWhiteSpace(selectedMissionTask.latestRevisionRequestedAt))
            return false;

        DateTime revisionAt = ParseDate(selectedMissionTask.latestRevisionRequestedAt);
        DateTime commentAt = ParseDate(createdAt);
        if (revisionAt == DateTime.MinValue || commentAt == DateTime.MinValue)
            return false;

        return commentAt >= revisionAt;
    }

    private bool IsTaskInRevision(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        string normalized = NormalizeStatusForMatch(status);
        return normalized.Contains("revizyon");
    }

    private void ToggleClass(VisualElement el, string className, bool add)
    {
        if (el == null || string.IsNullOrWhiteSpace(className))
            return;

        if (add)
            el.AddToClassList(className);
        else
            el.RemoveFromClassList(className);
    }

    private IEnumerator AddMissionComment()
    {
        if (router == null || selectedMissionTaskId <= 0)
            yield break;

        string text = missionCommentInput != null ? (missionCommentInput.value ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(text))
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Yorum metni zorunlu.";
            yield break;
        }

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Yorum ekleniyor...";

        string url = router.ApiBaseUrl + contentTaskPath + "/" + selectedMissionTaskId + "/comments";
        string json = JsonUtility.ToJson(new CreateContentTaskCommentRequest { text = text });
        using var req = AuthedJson(url, "POST", json);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string resp = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Yorum eklenemedi ({req.responseCode}) {resp}";
            yield break;
        }

        if (missionCommentInput != null)
            missionCommentInput.value = "";

        yield return StartCoroutine(FetchMissionComments(selectedMissionTaskId));

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Yorum eklendi.";
    }

    private void OpenRevisionModal()
    {
        if (selectedMissionTaskId <= 0 || selectedMissionTask == null)
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Önce bir görev detayı açın.";
            return;
        }

        InitializeRevisionModalFields();
        if (revisionModal != null)
            revisionModal.RemoveFromClassList("hidden");
    }

    private void CloseRevisionModal()
    {
        if (revisionModal != null)
            revisionModal.AddToClassList("hidden");
    }

    private IEnumerator SubmitRevisionRequest()
    {
        if (router == null || selectedMissionTaskId <= 0)
            yield break;

        string note = revisionNoteInput != null ? (revisionNoteInput.value ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(note))
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Revizyon notu zorunlu.";
            yield break;
        }

        string deadlineText = revisionDeadlineInput != null ? (revisionDeadlineInput.value ?? "").Trim() : "";
        if (!string.IsNullOrWhiteSpace(deadlineText) && ParseTaskDateOnly(deadlineText) == DateTime.MinValue.Date)
        {
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = "Tarih formatı geçersiz. yyyy-MM-dd kullanın.";
            yield break;
        }

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Revizyon talebi gönderiliyor...";

        var payload = new TransitionTaskStatusRequest
        {
            revisionType = string.IsNullOrWhiteSpace(revisionTypeDropdown != null ? revisionTypeDropdown.value : null)
                ? "İçerik"
                : revisionTypeDropdown.value.Trim(),
            priority = string.IsNullOrWhiteSpace(revisionPriorityDropdown != null ? revisionPriorityDropdown.value : null)
                ? "Orta"
                : revisionPriorityDropdown.value.Trim(),
            deadline = deadlineText,
            note = note
        };

        string url = router.ApiBaseUrl + contentTaskPath + "/" + selectedMissionTaskId + "/request-revision";
        string json = JsonUtility.ToJson(payload);
        using var req = AuthedJson(url, "POST", json);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string resp = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Revizyon talebi gönderilemedi ({req.responseCode}) {resp}";
            yield break;
        }

        CloseRevisionModal();
        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Revizyon talebi kaydedildi.";

        yield return StartCoroutine(FetchContentTaskDetail(selectedMissionTaskId));
        yield return StartCoroutine(FetchMissionComments(selectedMissionTaskId));
        yield return StartCoroutine(FetchContentTasks());
        RenderMissionsDataLists();
    }

    private IEnumerator ApproveSelectedMissionTask()
    {
        if (router == null || selectedMissionTaskId <= 0)
            yield break;

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Görev onaylanıyor...";

        string url = router.ApiBaseUrl + contentTaskPath + "/" + selectedMissionTaskId + "/approve";
        using var req = AuthedJson(url, "POST", "{}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string resp = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (missionCommentStatusLabel != null)
                missionCommentStatusLabel.text = $"Onay başarısız ({req.responseCode}) {resp}";
            yield break;
        }

        if (missionCommentStatusLabel != null)
            missionCommentStatusLabel.text = "Görev onaylandı.";

        yield return StartCoroutine(FetchContentTaskDetail(selectedMissionTaskId));
        yield return StartCoroutine(FetchMissionComments(selectedMissionTaskId));
        yield return StartCoroutine(FetchContentTasks());
        RenderMissionsDataLists();
    }

    private string BuildTaskTags(ContentTaskItemDto item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.experimentName)) parts.Add(item.experimentName.Trim());
        if (!string.IsNullOrWhiteSpace(item.taskType)) parts.Add(item.taskType.Trim());
        if (!string.IsNullOrWhiteSpace(item.priority)) parts.Add(item.priority.Trim());
        return parts.Count > 0 ? string.Join(", ", parts) : "-";
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

    private string BuildMissionMetaText(ContentTaskItemDto item)
    {
        string assignee = SafeText(item.assigneeName);
        string deadline = FormatTaskDate(item.deadline);
        string status = SafeText(item.status);
        return $"Atanan: {assignee} • Teslim: {deadline} • Durum: {status}";
    }

    private string BuildReviewMissionMetaText(ContentTaskItemDto item)
    {
        string priority = SafeText(item.priority);
        string status = SafeText(item.status);
        string assignee = SafeText(item.assigneeName);
        return $"{status} • Öncelik: {priority} • Atanan: {assignee}";
    }

    private bool IsTaskCompleted(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        string normalized = NormalizeStatusForMatch(status);
        return normalized == "tamamlandı" || normalized == "tamamlandi" || normalized == "completed";
    }

    private bool IsTaskReviewStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        string normalized = NormalizeStatusForMatch(status);
        return normalized.Contains("incele")
            || normalized.Contains("review")
            || normalized.Contains("onay")
            || normalized.Contains("revizyon");
    }

    private bool IsLikelyReviewCandidate(ContentTaskItemDto item)
    {
        if (item == null)
            return false;

        if (IsTaskCompleted(item.status))
            return false;

        // Fallback: some environments keep non-standard status text, but submit flow raises progress.
        return item.progressPercent >= 90;
    }

    private bool IsTaskPastDue(ContentTaskItemDto item)
    {
        if (item == null)
            return false;

        var dt = ParseTaskDateOnly(item.deadline);
        if (dt == DateTime.MinValue.Date)
            return false;

        return dt.Date < DateTime.Today;
    }

    private string NormalizeStatusForMatch(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string input = raw.Trim().ToLowerInvariant().Replace('ı', 'i');
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

    private DateTime ParseTaskDateOnly(string raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact.Date;

        return ParseDate(raw).Date;
    }

    private string FormatTaskDate(string raw)
    {
        var dt = ParseTaskDateOnly(raw);
        if (dt == DateTime.MinValue.Date)
            return "-";

        return dt.ToString("dd MMM", new CultureInfo("tr-TR"));
    }

    private IEnumerator AssignContentTask()
    {
        if (router == null)
            yield break;

        string title = taskTitleInput != null ? (taskTitleInput.value ?? "").Trim() : "";
        string type = taskTypeDropdown != null ? (taskTypeDropdown.value ?? "").Trim() : "";
        string experiment = taskExperimentInput != null ? (taskExperimentInput.value ?? "").Trim() : "";
        string duration = taskEstimatedDurationInput != null ? (taskEstimatedDurationInput.value ?? "").Trim() : "";
        string startDate = taskStartDateInput != null ? (taskStartDateInput.value ?? "").Trim() : "";
        string deadline = taskDeadlineInput != null ? (taskDeadlineInput.value ?? "").Trim() : "";
        string priority = taskPriorityDropdown != null ? (taskPriorityDropdown.value ?? "").Trim() : "Orta";
        string description = taskDescriptionInput != null ? (taskDescriptionInput.value ?? "").Trim() : "";
        string expectedOutput = taskExpectedOutputInput != null ? (taskExpectedOutputInput.value ?? "").Trim() : "";
        string assigneeName = taskAssigneeDropdown != null ? (taskAssigneeDropdown.value ?? "").Trim() : "";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(assigneeName))
        {
            if (assignTaskStatusLabel != null)
                assignTaskStatusLabel.text = "Görev başlığı ve içerik üreticisi zorunludur.";
            yield break;
        }

        int assigneeUserId = 0;
        foreach (var c in taskAssigneeItems)
        {
            if (c != null && string.Equals(SafeText(c.fullName), assigneeName, StringComparison.OrdinalIgnoreCase))
            {
                assigneeUserId = c.userId;
                break;
            }
        }

        if (assigneeUserId <= 0)
        {
            if (assignTaskStatusLabel != null)
                assignTaskStatusLabel.text = "Seçilen içerik üreticisi geçerli değil.";
            yield break;
        }

        var payload = new CreateContentTaskRequest
        {
            title = title,
            taskType = type,
            experimentName = experiment,
            estimatedDuration = duration,
            startDate = startDate,
            deadline = deadline,
            assigneeUserId = assigneeUserId,
            priority = priority,
            description = description,
            expectedOutput = expectedOutput
        };

        string url = router.ApiBaseUrl + contentTaskPath;
        string json = JsonUtility.ToJson(payload);

        if (assignTaskStatusLabel != null)
            assignTaskStatusLabel.text = "Görev atanıyor...";

        using var req = AuthedJson(url, "POST", json);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string resp = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (assignTaskStatusLabel != null)
                assignTaskStatusLabel.text = $"Görev atanamadı ({req.responseCode}) {resp}";
            yield break;
        }

        if (assignTaskStatusLabel != null)
            assignTaskStatusLabel.text = "Görev başarıyla atandı.";

        if (taskTitleInput != null) taskTitleInput.value = "";
        if (taskExperimentInput != null) taskExperimentInput.value = "";
        if (taskEstimatedDurationInput != null) taskEstimatedDurationInput.value = "";
        if (taskDescriptionInput != null) taskDescriptionInput.value = "";
        if (taskExpectedOutputInput != null) taskExpectedOutputInput.value = "";

        yield return StartCoroutine(FetchContentTaskSummary());
        yield return StartCoroutine(FetchContentTasks());
        RenderMissionsDataLists();
        UpdateTaskPreview();
    }

    private IEnumerator LoadProfilePageData()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + myProfilePath;
        using var req = AuthedGet(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            yield break;

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "{}";
        profileMe = JsonUtility.FromJson<ProfileMeDto>(raw);
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
            profileRoleLabel.text = profileMe != null && !string.IsNullOrWhiteSpace(profileMe.roleName) ? profileMe.roleName : "Yönetici";
        if (profileStatusLabel != null)
            profileStatusLabel.text = profileMe != null && profileMe.isActive ? "Aktif" : "Pasif";
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

        int totalUsers = (cachedUsers ?? new List<UserRow>()).Count;
        int activeUsers = (cachedUsers ?? new List<UserRow>()).Count(u => u != null && u.IsActive);
        int passiveUsers = Mathf.Max(totalUsers - activeUsers, 0);
        int roleCount = (cachedRoles ?? new List<RoleRow>()).Count;
        int streak = profileMe != null ? Mathf.Max(profileMe.currentActiveStreakDays, 0) : 0;
        float activeHours = profileMe != null ? Mathf.Max(profileMe.totalActiveHours, 0f) : 0f;

        profileStatsGrid.Add(BuildProfileStatCard(totalUsers.ToString(), "Toplam Kullanıcı", roleCount > 0 ? $"Rol: {roleCount}" : "-"));
        profileStatsGrid.Add(BuildProfileStatCard(activeUsers.ToString(), "Aktif Kullanıcı", $"Pasif: {passiveUsers}"));
        profileStatsGrid.Add(BuildProfileStatCard(roleCount.ToString(), "Toplam Rol", "Sistem rolleri", true));
        profileStatsGrid.Add(BuildProfileStatCard(streak.ToString(), "Aktif Gün Serisi", "Üst üste giriş"));
        profileStatsGrid.Add(BuildProfileStatCard($"{activeHours:0.0}", "Toplam Aktif Saat", "Bu hesap için"));
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

    // =========================
    // 4) USERS LIST (FETCH + RENDER)
    // =========================

    // Kullanıcı listesini API'den çeker
    private IEnumerator FetchUsers()
    {
        if (router == null)
        {
            if (usersStatus != null) usersStatus.text = "Router yok (ApiBaseUrl).";
            yield break;
        }

        if (string.IsNullOrEmpty(router.AccessToken))
        {
            if (usersStatus != null) usersStatus.text = "Oturum yok (JWT token). Lütfen yeniden giriş yap.";
            yield break;
        }

        string url = router.ApiBaseUrl + userPath;
        Debug.Log("[Users] GET => " + url);

        if (usersStatus != null) usersStatus.text = "Yükleniyor...";

        using var req = AuthedGet(url);
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
        ApplyHomeDashboardMetrics();

        if (usersStatus != null) usersStatus.text = $"Toplam: {cachedUsers.Count}";
    }

    // Arama kutusuna göre kullanıcı listesini filtreleyip render eder
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

    // Tek kullanıcı satırını UI olarak üretir
    private VisualElement BuildUserItem(UserRow u)
    {
        var row = new VisualElement();
        row.AddToClassList("user-row");

        var title = new Label($"{u.Name} {u.Surname}");
        title.AddToClassList("user-title");

        var sub = new Label($"{u.Email}  •  {u.RoleName}  •  {(u.IsActive ? "Active" : "Passive")}");
        sub.AddToClassList("user-sub");

        var actions = new VisualElement();
        actions.AddToClassList("user-row-actions");

        var deleteBtn = new Button(() => StartCoroutine(DeleteUser(u.Id))) { text = "Sil" };
        deleteBtn.AddToClassList("user-delete-btn");
        actions.Add(deleteBtn);

        row.Add(title);
        row.Add(sub);
        row.Add(actions);

        return row;
    }

    private IEnumerator DeleteUser(int userId)
    {
        if (router == null || userId <= 0)
            yield break;

        string url = router.ApiBaseUrl + $"{userPath}?id={userId}";
        var req = new UnityWebRequest(url, "DELETE");
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        if (usersStatus != null)
            usersStatus.text = "Kullanıcı siliniyor...";

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        if (!ok)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError($"[DeleteUser] FAILED {(int)req.responseCode} => {err}");
            if (usersStatus != null)
                usersStatus.text = $"Silinemedi ({req.responseCode}) {err}";
            yield break;
        }

        cachedUsers.RemoveAll(x => x != null && x.Id == userId);
        RenderUsersFiltered();
        ApplyHomeDashboardMetrics();
        if (usersStatus != null)
            usersStatus.text = "Kullanıcı silindi.";
    }

    // =========================
    // 5) ADD USER (FORM + POST)
    // =========================

    // AddUser formunu temizler
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

    // Yeni kullanıcı ekleme (POST /api/User)
    private IEnumerator AddUser()
    {
        if (router == null)
        {
            if (addStatusLabel != null) addStatusLabel.text = "Router yok (ApiBaseUrl).";
            yield break;
        }

        if (string.IsNullOrEmpty(router.AccessToken))
        {
            if (addStatusLabel != null) addStatusLabel.text = "Oturum yok (JWT token). Lütfen yeniden giriş yap.";
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

        using var uwr = AuthedJson(url, "POST", json);

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

    // Dropdown'daki görünen rol adını roleId'ye çevirir
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

    // (Kullanılmıyor) string'i sha256'a çeviren helper
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

    // =========================
    // 6) ROLES (FETCH + RENDER + ADD + ASSIGN)
    // =========================

    // Roles sayfası için roller + kullanıcılar(lite) birlikte çekilir, dropdownlar doldurulur
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

    // Rolleri API'den çeker
    private IEnumerator FetchRoles()
    {
        string url = router.ApiBaseUrl + rolesPath;
        using var req = AuthedGet(url);
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
        ApplyHomeDashboardMetrics();
    }

    // Kullanıcıları dropdown için lite şekilde hazırlar
    private IEnumerator FetchUsersLite()
    {
        string url = router.ApiBaseUrl + userPath;
        using var req = AuthedGet(url);
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

    // Roller listesini UI'ye basar
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

    // Role dropdown'unu doldurur
    private void PopulateRoleDropdown()
    {
        if (rolesRoleDd == null) return;

        var choices = new List<string>();
        foreach (var r in cachedRoles)
            choices.Add($"{r.Id} - {r.Name}");

        rolesRoleDd.choices = choices;
        rolesRoleDd.value = choices.Count > 0 ? choices[0] : "";
    }

    // User dropdown'unu doldurur
    private void PopulateUserDropdown()
    {
        if (rolesUserDd == null) return;

        var choices = new List<string>();
        foreach (var u in cachedUsersLite)
            choices.Add($"{u.Id} - {u.Display}");

        rolesUserDd.choices = choices;
        rolesUserDd.value = choices.Count > 0 ? choices[0] : "";
    }

    // Yeni rol ekler (POST /api/Role)
    private IEnumerator AddRole()
    {
        if (router == null) yield break;

        if (string.IsNullOrEmpty(router.AccessToken))
        {
            if (rolesActionLabel != null) rolesActionLabel.text = "Oturum yok (JWT token). Lütfen yeniden giriş yap.";
            yield break;
        }

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

        using var req = AuthedJson(url, "POST", json);

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

    // Kullanıcıya rol atar (PUT /api/User/{id}/role)
    private IEnumerator AssignRoleToUser()
    {
        if (router == null) yield break;
        if (rolesUserDd == null || rolesRoleDd == null) yield break;

        if (string.IsNullOrEmpty(router.AccessToken))
        {
            if (rolesActionLabel != null) rolesActionLabel.text = "Oturum yok (JWT token). Lütfen yeniden giriş yap.";
            yield break;
        }

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

        using var req = AuthedJson(url, "PUT", json);

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

    // Dropdown text'inden baştaki id'yi parse eder (örn: "12 - Ali Veli" -> 12)
    private int ParseLeadingInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int dash = s.IndexOf('-');
        string part = dash > 0 ? s.Substring(0, dash).Trim() : s.Trim();
        int.TryParse(part, out int v);
        return v;
    }

    private IEnumerator SessionHeartbeatLoop()
    {
        while (true)
        {
            if (router != null && !string.IsNullOrEmpty(router.AccessToken))
            {
                using var req = AuthedJson(router.ApiBaseUrl + sessionHeartbeatPath, "POST", "{}");
                yield return req.SendWebRequest();
            }

            yield return new WaitForSeconds(45f);
        }
    }

    private IEnumerator EndSessionAndLogout()
    {
        if (router != null && !string.IsNullOrEmpty(router.AccessToken))
        {
            using var req = AuthedJson(router.ApiBaseUrl + sessionEndPath, "POST", "{}");
            yield return req.SendWebRequest();
        }

        router?.ClearSession();
        router?.ShowLogin();
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
        if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase)) return "Atama";
        if (string.Equals(type, "TaskAssigned", StringComparison.OrdinalIgnoreCase)) return "Atama";
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
        else if (string.Equals(type, "AssignmentCreated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "TaskAssigned", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("submitted");
        else if (string.Equals(type, "JoinApproved", StringComparison.OrdinalIgnoreCase))
            badge.AddToClassList("joined");
        else
            badge.AddToClassList("achievement");
    }

    private void OnDisable()
    {
        if (sessionHeartbeatRoutine != null)
        {
            StopCoroutine(sessionHeartbeatRoutine);
            sessionHeartbeatRoutine = null;
        }
    }

    // =========================
    // 7) DTOs / REQUEST MODELS
    // =========================

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
    private class AssignRoleBody
    {
        public int RoleId;
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

    [System.Serializable]
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

    [System.Serializable]
    private class ClassActivityListWrapper
    {
        public ClassActivityDto[] items;
    }

    [System.Serializable]
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

    [System.Serializable]
    private class WeeklySessionHoursDto
    {
        public WeeklySessionDayDto[] items;
    }

    [System.Serializable]
    private class WeeklySessionDayDto
    {
        public int dayIndex;
        public string dayLabel;
        public float hours;
    }

    [System.Serializable]
    private class ContentCreatorLiteDto
    {
        public int userId;
        public string fullName;
        public string email;
    }

    [System.Serializable]
    private class ContentCreatorLiteListWrapper
    {
        public ContentCreatorLiteDto[] items;
    }

    [System.Serializable]
    private class ContentTaskSummaryDto
    {
        public int activeCount;
        public int todayDeadlineCount;
        public int overdueCount;
        public int reviewCount;
    }

    [System.Serializable]
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
        public string createdAtUtc;
        public string updatedAtUtc;
        public string latestRevisionType;
        public string latestRevisionPriority;
        public string latestRevisionDeadline;
        public string latestRevisionNote;
        public string latestRevisionRequestedAt;
    }

    [System.Serializable]
    private class ContentTaskItemListWrapper
    {
        public ContentTaskItemDto[] items;
    }

    [System.Serializable]
    private class ContentTaskCommentDto
    {
        public int userId;
        public string userName;
        public string text;
        public string createdAt;
    }

    [System.Serializable]
    private class ContentTaskCommentListWrapper
    {
        public ContentTaskCommentDto[] items;
    }

    [System.Serializable]
    private class CreateContentTaskCommentRequest
    {
        public string text;
    }

    [System.Serializable]
    private class TransitionTaskStatusRequest
    {
        public string revisionType;
        public string priority;
        public string deadline;
        public string note;
    }

    [System.Serializable]
    private class CreateContentTaskRequest
    {
        public string title;
        public string taskType;
        public string experimentName;
        public string estimatedDuration;
        public string startDate;
        public string deadline;
        public int assigneeUserId;
        public string priority;
        public string description;
        public string expectedOutput;
    }

    // =========================
    // 8) JSON ARRAY HELPER
    // =========================

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

    // =========================
    // 9) NETWORK HELPERS (JWT)
    // =========================

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
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json ?? ""));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(router?.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);
        return req;
    }
}