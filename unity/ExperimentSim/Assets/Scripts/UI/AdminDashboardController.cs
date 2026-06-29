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

    // -------------------------
    // Teacher Requests (TeacherRequestsPage)
    // -------------------------
    private ScrollView teacherRequestsList;
    private Label teacherRequestsStatusLabel;
    private Button teacherRequestsRefreshBtn;
    private List<TeacherRoleRequestRow> teacherRoleRequests = new();
    private List<TeacherRoleRequestRow> teacherRoleRequestNotificationItems = new();
    private bool teacherRequestsFetchFailed;

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
    [SerializeField] private string calendarCategoriesPath = "/api/Calendar/categories";
    [SerializeField] private string calendarEventsPath = "/api/Calendar/events";
    [SerializeField] private string teacherRoleRequestsPath = "/api/User/teacher-role-requests";

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
    private DashboardNotificationCenter notificationCenter;
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

    // -------------------------
    // Calendar (CalendarPage)
    // -------------------------
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
        root.Q<Button>("TeacherRequestsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToSubPage("TeacherRequestsPage", "RolePermission", "TeacherRequestsBtn");
            StartCoroutine(RefreshTeacherRequests());
        });

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

        root.Q<Button>("AccountBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToRootPage("ProfilePage", "ProfileBtn");
            StartCoroutine(LoadProfilePageData());
        });

        root.Q<Button>("SettingsBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            OpenSettingsModal();
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

        root.Q<Button>("CalendarBtn")?.RegisterCallback<ClickEvent>(_ =>
        {
            NavigateToRootPage("CalendarPage", "CalendarBtn");
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
        BindSettingsModal();
        BindTaskAssignPage();
        BindMissionsDataPage();
        BindCalendarPage();
        BindNotifications();

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

        BindTeacherRequestsPage();

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
        yield return StartCoroutine(FetchTeacherRoleRequestsForNotifications());
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
        yield return StartCoroutine(FetchTeacherRoleRequestsForNotifications());
        yield return StartCoroutine(FetchWeeklySessionHours());
        yield return StartCoroutine(FetchContentTasks());
        ApplyHomeDashboardMetrics();
    }

    private IEnumerator FetchTeacherRoleRequestsForNotifications()
    {
        if (router == null)
            yield break;

        string url = router.ApiBaseUrl + teacherRoleRequestsPath + "?status=All";
        using var req = new UnityWebRequest(url, "GET");
        req.downloadHandler = new DownloadHandlerBuffer();

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        if (!ok)
        {
            teacherRoleRequestNotificationItems = new List<TeacherRoleRequestRow>();
            yield break;
        }

        string json = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
        string trimmed = (json ?? string.Empty).TrimStart();

        if (trimmed.StartsWith("{"))
        {
            var wrapped = JsonUtility.FromJson<TeacherRoleRequestListResponse>(json);
            teacherRoleRequestNotificationItems = wrapped != null && wrapped.Data != null
                ? wrapped.Data.ToList()
                : new List<TeacherRoleRequestRow>();
        }
        else
        {
            teacherRoleRequestNotificationItems = JsonArrayHelper.FromJson<TeacherRoleRequestRow>(json);
        }
    }

    private void BindNotifications()
    {
        notificationCenter = new DashboardNotificationCenter(
            root,
            BuildNotificationItems,
            HandleNotificationSelected,
            () => $"admin-{profileMe?.id ?? 0}");
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

        foreach (var task in contentTaskItems ?? Array.Empty<ContentTaskItemDto>())
        {
            if (task == null)
                continue;

            if (IsTaskCompleted(task.status))
            {
                var completedAt = ParseDate(task.updatedAtUtc);
                if (completedAt == DateTime.MinValue)
                    completedAt = ParseDate(task.createdAtUtc);
                if (completedAt == DateTime.MinValue)
                    completedAt = now;

                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"admin-completed-task-{task.id}",
                    Title = "Tamamlanan Görev",
                    Message = $"{SafeText(task.assigneeName)} tarafından tamamlandı: {SafeText(task.title)}",
                    Timestamp = completedAt,
                    TargetPage = "MissionsDataPage",
                    TargetMenuButton = "MissionsDataBtn",
                    IsUnread = completedAt >= now.AddDays(-7)
                });
            }

            bool hasRevision = IsTaskInRevision(task.status)
                || !string.IsNullOrWhiteSpace(task.latestRevisionRequestedAt)
                || !string.IsNullOrWhiteSpace(task.latestRevisionNote);

            if (hasRevision)
            {
                var revisionAt = ParseDate(task.latestRevisionRequestedAt);
                if (revisionAt == DateTime.MinValue)
                    revisionAt = ParseDate(task.updatedAtUtc);
                if (revisionAt == DateTime.MinValue)
                    revisionAt = now;

                list.Add(new DashboardNotificationCenter.NotificationItem
                {
                    Id = $"admin-revision-task-{task.id}",
                    Title = "Görev Revizyonu",
                    Message = string.IsNullOrWhiteSpace(task.latestRevisionNote)
                        ? $"{SafeText(task.title)} için revizyon süreci aktif."
                        : task.latestRevisionNote,
                    Timestamp = revisionAt,
                    TargetPage = "MissionsDataPage",
                    TargetMenuButton = "MissionsDataBtn",
                    IsUnread = revisionAt >= now.AddDays(-7)
                });
            }
        }

        foreach (var activity in personalActivityItems ?? Array.Empty<ClassActivityDto>())
        {
            if (activity == null)
                continue;

            string combined = $"{activity.Type} {activity.Title} {activity.Description}";
            bool isTaskComment = combined.IndexOf("yorum", StringComparison.OrdinalIgnoreCase) >= 0
                || combined.IndexOf("comment", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isTaskComment)
                continue;

            var occurredAt = ParseDate(activity.OccurredAt);
            if (occurredAt == DateTime.MinValue)
                occurredAt = now;

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"admin-task-comment-{activity.ActivityId}",
                Title = "Görev Yorumu",
                Message = string.IsNullOrWhiteSpace(activity.Description)
                    ? "Bir görev yorumunda güncelleme var."
                    : activity.Description,
                Timestamp = occurredAt,
                TargetPage = "MissionsDataPage",
                TargetMenuButton = "MissionsDataBtn",
                IsUnread = occurredAt >= now.AddDays(-7)
            });
        }

        foreach (var request in teacherRoleRequestNotificationItems ?? new List<TeacherRoleRequestRow>())
        {
            if (request == null)
                continue;

            string status = request.Status ?? string.Empty;
            bool isPending = string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase);

            if (!isPending)
                continue;

            string eventRaw = GetTeacherRequestRequestedAt(request);

            DateTime requestTime = ParseDate(eventRaw);
            if (requestTime == DateTime.MinValue)
                requestTime = now;

            string userName = $"{SafeText(request.Name)} {SafeText(request.Surname)}".Trim();

            list.Add(new DashboardNotificationCenter.NotificationItem
            {
                Id = $"admin-teacher-request-{request.Id}-{status}",
                Title = "Öğretmen İsteği",
                Message = $"{userName} öğretmen rolü için başvurdu.",
                Timestamp = requestTime,
                TargetPage = "TeacherRequestsPage",
                TargetMenuButton = "TeacherRequestsBtn",
                IsUnread = requestTime >= now.AddDays(-14)
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

        if (string.Equals(item.TargetPage, "TeacherRequestsPage", StringComparison.OrdinalIgnoreCase))
        {
            NavigateToSubPage("TeacherRequestsPage", "RolePermission", "TeacherRequestsBtn");
            StartCoroutine(RefreshTeacherRequests());
            return;
        }

        if (string.Equals(item.TargetPage, "RolesPage", StringComparison.OrdinalIgnoreCase))
        {
            NavigateToSubPage("RolesPage", "RolePermission", "RolesBtn");
            StartCoroutine(FetchRolesAndUsers());
            return;
        }

        if (string.Equals(item.TargetPage, "MissionsDataPage", StringComparison.OrdinalIgnoreCase))
        {
            NavigateToSubPage("MissionsDataPage", "ContentManagement", "MissionsDataBtn");
            StartCoroutine(FetchContentTasks());
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.TargetPage) && !string.IsNullOrWhiteSpace(item.TargetMenuButton))
            NavigateToRootPage(item.TargetPage, item.TargetMenuButton);
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
            RefreshNotificationsBadge();
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ContentTaskItemListWrapper>("{\"items\":" + raw + "}");
        contentTaskItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ContentTaskItemDto>();

        RefreshCalendarClassDropdown();
        RefreshNotificationsBadge();
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
        RenderTeacherRequests();
        ApplyHomeDashboardMetrics();
        if (usersStatus != null)
            usersStatus.text = "Kullanıcı silindi.";
    }

    private void BindTeacherRequestsPage()
    {
        teacherRequestsList = root.Q<ScrollView>("TeacherRequestsList");
        teacherRequestsStatusLabel = root.Q<Label>("TeacherRequestsStatusLabel");
        teacherRequestsRefreshBtn = root.Q<Button>("TeacherRequestsRefreshBtn");

        if (teacherRequestsRefreshBtn != null)
            teacherRequestsRefreshBtn.clicked += () => StartCoroutine(RefreshTeacherRequests());
    }

    private IEnumerator RefreshTeacherRequests()
    {
        if (teacherRequestsStatusLabel != null)
            teacherRequestsStatusLabel.text = "Öğretmen başvuruları yükleniyor...";

        yield return StartCoroutine(FetchTeacherRoleRequests());

        if (teacherRequestsFetchFailed)
        {
            teacherRequestsList?.Clear();
            var failed = new Label("Başvurular alınamadı. Lütfen tekrar dene veya yetkini kontrol et.");
            failed.AddToClassList("teacher-request-empty");
            teacherRequestsList?.Add(failed);
            yield break;
        }

        RenderTeacherRequests();
    }

    private IEnumerator FetchTeacherRoleRequests()
    {
        if (router == null)
            yield break;

        teacherRequestsFetchFailed = false;

        string url = router.ApiBaseUrl + teacherRoleRequestsPath + "?status=Pending";
        using var req = new UnityWebRequest(url, "GET");
        req.downloadHandler = new DownloadHandlerBuffer();

        if (!string.IsNullOrEmpty(router.AccessToken))
            req.SetRequestHeader("Authorization", "Bearer " + router.AccessToken);

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300;
        if (!ok)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.LogError($"[TeacherRoleRequests] FAILED {(int)req.responseCode} => {err}");
            teacherRoleRequests = new List<TeacherRoleRequestRow>();
            teacherRequestsFetchFailed = true;
            if (teacherRequestsStatusLabel != null)
                teacherRequestsStatusLabel.text = $"Başvurular alınamadı ({req.responseCode})";
            yield break;
        }

        string json = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
        string trimmed = (json ?? string.Empty).TrimStart();

        if (trimmed.StartsWith("{"))
        {
            var wrapped = JsonUtility.FromJson<TeacherRoleRequestListResponse>(json);
            teacherRoleRequests = wrapped != null && wrapped.Data != null
                ? wrapped.Data.OrderByDescending(x => ParseDate(GetTeacherRequestRequestedAt(x))).ToList()
                : new List<TeacherRoleRequestRow>();
        }
        else
        {
            teacherRoleRequests = JsonArrayHelper.FromJson<TeacherRoleRequestRow>(json)
                .OrderByDescending(x => ParseDate(GetTeacherRequestRequestedAt(x)))
                .ToList();
        }
    }

    private void RenderTeacherRequests()
    {
        if (teacherRequestsList == null)
            return;

        teacherRequestsList.Clear();

        var pendingRequests = (teacherRoleRequests ?? new List<TeacherRoleRequestRow>())
            .Where(r => r != null && string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pendingRequests.Count == 0)
        {
            var empty = new Label("Bekleyen öğretmen olma isteği bulunmuyor.");
            empty.AddToClassList("teacher-request-empty");
            teacherRequestsList.Add(empty);

            if (teacherRequestsStatusLabel != null)
                teacherRequestsStatusLabel.text = "0 bekleyen istek";
            return;
        }

        foreach (var request in pendingRequests)
        {
            var row = new VisualElement();
            row.AddToClassList("teacher-request-row");

            string fullName = $"{SafeText(request.Name)} {SafeText(request.Surname)}";
            var title = new Label(fullName.Trim());
            title.AddToClassList("user-title");

            string requestedAt = FormatTeacherRequestDate(GetTeacherRequestRequestedAt(request));
            var meta = new Label($"{SafeText(request.Email)}  •  Talep: {requestedAt}");
            meta.AddToClassList("teacher-request-meta");

            var actions = new VisualElement();
            actions.AddToClassList("teacher-request-actions");

            var approveBtn = new Button(() => StartCoroutine(ApproveTeacherRequest(request)))
            {
                text = "Onayla"
            };
            approveBtn.AddToClassList("teacher-request-btn");
            approveBtn.AddToClassList("btn-approve");

            var rejectBtn = new Button(() => StartCoroutine(RejectTeacherRequest(request)))
            {
                text = "Reddet"
            };
            rejectBtn.AddToClassList("teacher-request-btn");
            rejectBtn.AddToClassList("btn-reject");

            actions.Add(approveBtn);
            actions.Add(rejectBtn);

            row.Add(title);
            row.Add(meta);
            row.Add(actions);
            teacherRequestsList.Add(row);
        }

        if (teacherRequestsStatusLabel != null)
            teacherRequestsStatusLabel.text = $"{pendingRequests.Count} bekleyen istek";
    }

    private IEnumerator ApproveTeacherRequest(TeacherRoleRequestRow request)
    {
        if (router == null || request == null || request.Id <= 0)
            yield break;

        if (teacherRequestsStatusLabel != null)
            teacherRequestsStatusLabel.text = $"#{request.Id} başvurusu onaylanıyor...";

        string url = router.ApiBaseUrl + $"{teacherRoleRequestsPath}/{request.Id}/approve";
        string json = JsonUtility.ToJson(new ReviewTeacherRoleRequestBody { Note = string.Empty });

        using var req = AuthedJson(url, "POST", json);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (teacherRequestsStatusLabel != null)
                teacherRequestsStatusLabel.text = $"Onay başarısız ({req.responseCode})";
            Debug.LogError($"[TeacherRequest] APPROVE FAILED {(int)req.responseCode} => {err}");
            yield break;
        }

        yield return StartCoroutine(FetchTeacherRoleRequestsForNotifications());

        yield return StartCoroutine(RefreshTeacherRequests());
        yield return StartCoroutine(FetchUsers());
        ApplyHomeDashboardMetrics();
        RefreshNotificationsBadge();

        if (teacherRequestsStatusLabel != null)
            teacherRequestsStatusLabel.text = $"#{request.Id} başvurusu onaylandı.";
    }

    private IEnumerator RejectTeacherRequest(TeacherRoleRequestRow request)
    {
        if (router == null || request == null || request.Id <= 0)
            yield break;

        if (teacherRequestsStatusLabel != null)
            teacherRequestsStatusLabel.text = $"#{request.Id} başvurusu reddediliyor...";

        string url = router.ApiBaseUrl + $"{teacherRoleRequestsPath}/{request.Id}/reject";
        string json = JsonUtility.ToJson(new ReviewTeacherRoleRequestBody { Note = string.Empty });

        using var req = AuthedJson(url, "POST", json);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (teacherRequestsStatusLabel != null)
                teacherRequestsStatusLabel.text = $"Red başarısız ({req.responseCode})";
            Debug.LogError($"[TeacherRequest] REJECT FAILED {(int)req.responseCode} => {err}");
            yield break;
        }

        yield return StartCoroutine(RefreshTeacherRequests());

        if (teacherRequestsStatusLabel != null)
            teacherRequestsStatusLabel.text = $"#{request.Id} başvurusu reddedildi.";
    }

    private string FormatTeacherRequestDate(string requestedAtRaw)
    {
        var dt = ParseDate(requestedAtRaw);
        if (dt == DateTime.MinValue)
            return "-";

        return dt.ToString("dd MMM yyyy HH:mm", new CultureInfo("tr-TR"));
    }

    private string GetTeacherRequestRequestedAt(TeacherRoleRequestRow request)
    {
        if (request == null)
            return string.Empty;

        return !string.IsNullOrWhiteSpace(request.RequestedAtUtc)
            ? request.RequestedAtUtc
            : request.RequestedAt;
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

        yield return StartCoroutine(FetchTeacherRoleRequestsForNotifications());

        if (rolesActionLabel != null) rolesActionLabel.text = "✅ Rol atandı!";
        StartCoroutine(FetchRolesAndUsers());
        RenderTeacherRequests();
        RefreshNotificationsBadge();
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

                yield return StartCoroutine(FetchTeacherRoleRequestsForNotifications());
                RefreshNotificationsBadge();
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

        if (contentTaskItems != null)
        {
            foreach (var item in contentTaskItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.experimentName))
                    continue;

                string cls = item.experimentName.Trim();
                if (!choices.Any(x => string.Equals(x, cls, StringComparison.OrdinalIgnoreCase)))
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
    private class TeacherRoleRequestRow
    {
        public int Id;
        public int UserId;
        public string Name;
        public string Surname;
        public string Email;
        public string Status;
        public string Note;
        public string DecisionNote;
        public string RequestedAtUtc;
        public string RequestedAt;
        public string ReviewedAtUtc;
        public int ReviewedByUserId;
    }

    [System.Serializable]
    private class TeacherRoleRequestListResponse
    {
        public TeacherRoleRequestRow[] Data;
    }

    [System.Serializable]
    private class ReviewTeacherRoleRequestBody
    {
        public string Note;
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
    private class SettingsApiMessageDto
    {
        public string message;
    }

    [System.Serializable]
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

public sealed class DashboardNotificationCenter
{
    [Serializable]
    private sealed class ReadIdsStoreDto
    {
        public string[] items;
    }

    private const string ReadIdsStorePrefix = "DashboardNotifications.ReadIds";
    private const int MaxStoredReadIds = 1000;

    public sealed class NotificationItem
    {
        public string Id;
        public string Title;
        public string Message;
        public DateTime Timestamp;
        public string TargetPage;
        public string TargetMenuButton;
        public bool IsUnread = true;
    }

    private readonly VisualElement root;
    private readonly Func<List<NotificationItem>> itemProvider;
    private readonly Action<NotificationItem> onItemSelected;
    private readonly Func<string> storageScopeProvider;
    private readonly CultureInfo trCulture = new("tr-TR");

    private readonly HashSet<string> readIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NotificationItem> items = new();
    private string activeStorageKey;
    private bool readIdsLoaded;

    private Button notificationsBtn;
    private Label badgeLabel;

    private VisualElement panelOverlay;
    private ScrollView panelList;
    private Label panelEmptyLabel;

    private VisualElement modalOverlay;
    private ScrollView modalList;
    private Label modalEmptyLabel;

    public DashboardNotificationCenter(
        VisualElement root,
        Func<List<NotificationItem>> itemProvider,
        Action<NotificationItem> onItemSelected,
        Func<string> storageScopeProvider = null)
    {
        this.root = root;
        this.itemProvider = itemProvider;
        this.onItemSelected = onItemSelected;
        this.storageScopeProvider = storageScopeProvider;
    }

    public void Bind(string notificationsButtonName = "NotificationsBtn")
    {
        if (root == null)
            return;

        notificationsBtn = root.Q<Button>(notificationsButtonName);
        if (notificationsBtn == null)
            return;

        BuildBadge();
        BuildPanelOverlay();
        BuildModalOverlay();

        notificationsBtn.clicked -= OnNotificationsClicked;
        notificationsBtn.clicked += OnNotificationsClicked;

        RefreshBadge();
    }

    public void RefreshBadge()
    {
        PullFromProvider();
        UpdateBadge();
    }

    private void OnNotificationsClicked()
    {
        PullFromProvider();
        MarkAllAsRead();
        UpdateBadge();
        RenderPanel();
        panelOverlay.style.display = DisplayStyle.Flex;
    }

    private void PullFromProvider()
    {
        EnsureReadIdsLoaded();
        items.Clear();

        if (itemProvider != null)
        {
            var provided = itemProvider.Invoke();
            if (provided != null)
                items.AddRange(provided.Where(i => i != null));
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N");

            if (readIds.Contains(item.Id))
                item.IsUnread = false;
        }

        items.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
    }

    private void MarkAllAsRead()
    {
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                continue;

            item.IsUnread = false;
            readIds.Add(item.Id);
        }

        SaveReadIds();
    }

    private void BuildBadge()
    {
        var badgeHost = new VisualElement();
        badgeHost.name = "NotificationsBadgeHost";
        badgeHost.pickingMode = PickingMode.Ignore;
        badgeHost.style.position = Position.Absolute;
        badgeHost.style.right = -2;
        badgeHost.style.top = -2;
        badgeHost.style.minWidth = 18;
        badgeHost.style.height = 18;
        badgeHost.style.paddingLeft = 4;
        badgeHost.style.paddingRight = 4;
        badgeHost.style.backgroundColor = new Color(0.90f, 0.20f, 0.20f, 1f);
        badgeHost.style.alignItems = Align.Center;
        badgeHost.style.justifyContent = Justify.Center;
        badgeHost.style.borderTopLeftRadius = 9;
        badgeHost.style.borderTopRightRadius = 9;
        badgeHost.style.borderBottomLeftRadius = 9;
        badgeHost.style.borderBottomRightRadius = 9;
        badgeHost.style.display = DisplayStyle.None;

        badgeLabel = new Label("0");
        badgeLabel.style.color = Color.white;
        badgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        badgeLabel.style.fontSize = 10;
        badgeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        badgeHost.Add(badgeLabel);
        notificationsBtn.Add(badgeHost);
    }

    private void BuildPanelOverlay()
    {
        panelOverlay = new VisualElement();
        panelOverlay.name = "NotificationsPanelOverlay";
        panelOverlay.style.position = Position.Absolute;
        panelOverlay.style.left = 0;
        panelOverlay.style.top = 0;
        panelOverlay.style.right = 0;
        panelOverlay.style.bottom = 0;
        panelOverlay.style.display = DisplayStyle.None;
        panelOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.02f);

        var panelCard = new VisualElement();
        panelCard.name = "NotificationsPanelCard";
        panelCard.style.position = Position.Absolute;
        panelCard.style.top = 110;
        panelCard.style.right = 16;
        panelCard.style.width = 410;
        panelCard.style.maxHeight = 480;
        panelCard.style.backgroundColor = Color.white;
        panelCard.style.borderTopLeftRadius = 12;
        panelCard.style.borderTopRightRadius = 12;
        panelCard.style.borderBottomLeftRadius = 12;
        panelCard.style.borderBottomRightRadius = 12;
        panelCard.style.borderLeftWidth = 1;
        panelCard.style.borderRightWidth = 1;
        panelCard.style.borderTopWidth = 1;
        panelCard.style.borderBottomWidth = 1;
        panelCard.style.borderLeftColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        panelCard.style.borderRightColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        panelCard.style.borderTopColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        panelCard.style.borderBottomColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        panelCard.style.paddingTop = 10;
        panelCard.style.paddingBottom = 10;
        panelCard.style.paddingLeft = 10;
        panelCard.style.paddingRight = 10;

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 8;

        var title = new Label("Bildirimler (Son 7 Gün)");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 14;
        title.style.color = new Color(0.13f, 0.17f, 0.21f, 1f);

        var showAllBtn = new Button(OpenAllModal) { text = "Tümünü Gör" };
        showAllBtn.style.height = 28;
        showAllBtn.style.paddingLeft = 10;
        showAllBtn.style.paddingRight = 10;
        showAllBtn.style.backgroundColor = Color.white;
        showAllBtn.style.borderTopLeftRadius = 12;
        showAllBtn.style.borderTopRightRadius = 12;
        showAllBtn.style.borderBottomLeftRadius = 12;
        showAllBtn.style.borderBottomRightRadius = 12;
        showAllBtn.style.borderLeftColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        showAllBtn.style.borderRightColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        showAllBtn.style.borderTopColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        showAllBtn.style.borderBottomColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        showAllBtn.style.fontSize = 14;

        header.Add(title);
        header.Add(showAllBtn);

        panelList = new ScrollView(ScrollViewMode.Vertical);
        panelList.style.maxHeight = 410;

        panelEmptyLabel = new Label("Son 7 günde bildirim yok.");
        panelEmptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        panelEmptyLabel.style.color = new Color(0.45f, 0.49f, 0.53f, 1f);
        panelEmptyLabel.style.paddingTop = 16;
        panelEmptyLabel.style.paddingBottom = 16;

        panelCard.Add(header);
        panelCard.Add(panelList);

        panelOverlay.Add(panelCard);
        panelOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == panelOverlay)
                panelOverlay.style.display = DisplayStyle.None;
        });

        root.Add(panelOverlay);
    }

    private void BuildModalOverlay()
    {
        modalOverlay = new VisualElement();
        modalOverlay.name = "NotificationsAllModalOverlay";
        modalOverlay.style.position = Position.Absolute;
        modalOverlay.style.left = 0;
        modalOverlay.style.top = 0;
        modalOverlay.style.right = 0;
        modalOverlay.style.bottom = 0;
        modalOverlay.style.display = DisplayStyle.None;
        modalOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.38f);
        modalOverlay.style.justifyContent = Justify.Center;
        modalOverlay.style.alignItems = Align.Center;

        var modalCard = new VisualElement();
        modalCard.style.width = 760;
        modalCard.style.maxWidth = 900;
        modalCard.style.height = 560;
        modalCard.style.maxHeight = 640;
        modalCard.style.backgroundColor = Color.white;
        modalCard.style.borderTopLeftRadius = 12;
        modalCard.style.borderTopRightRadius = 12;
        modalCard.style.borderBottomLeftRadius = 12;
        modalCard.style.borderBottomRightRadius = 12;
        modalCard.style.paddingTop = 12;
        modalCard.style.paddingBottom = 12;
        modalCard.style.paddingLeft = 12;
        modalCard.style.paddingRight = 12;

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.marginBottom = 8;

        var title = new Label("Tüm Bildirimler");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 16;

        var closeBtn = new Button(() => modalOverlay.style.display = DisplayStyle.None) { text = "Kapat" };
        closeBtn.style.height = 30;
        closeBtn.style.backgroundColor = Color.white;
        closeBtn.style.borderTopLeftRadius = 12;
        closeBtn.style.borderTopRightRadius = 12;
        closeBtn.style.borderBottomLeftRadius = 12;
        closeBtn.style.borderBottomRightRadius = 12;
        closeBtn.style.borderLeftColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        closeBtn.style.borderRightColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        closeBtn.style.borderTopColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        closeBtn.style.borderBottomColor = new Color(0.8667f, 0.8667f, 0.8667f, 1f);
        closeBtn.style.fontSize = 14;


        header.Add(title);
        header.Add(closeBtn);

        modalList = new ScrollView(ScrollViewMode.Vertical);
        modalList.style.flexGrow = 1;

        modalEmptyLabel = new Label("Bildirim bulunmuyor.");
        modalEmptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        modalEmptyLabel.style.color = new Color(0.45f, 0.49f, 0.53f, 1f);
        modalEmptyLabel.style.paddingTop = 16;
        modalEmptyLabel.style.paddingBottom = 16;

        modalCard.Add(header);
        modalCard.Add(modalList);

        modalOverlay.Add(modalCard);
        modalOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == modalOverlay)
                modalOverlay.style.display = DisplayStyle.None;
        });

        root.Add(modalOverlay);
    }

    private void RenderPanel()
    {
        panelList.Clear();

        DateTime cutoff = DateTime.Now.AddDays(-7);
        var recentItems = items
            .Where(i => i != null && i.Timestamp >= cutoff)
            .OrderByDescending(i => i.Timestamp)
            .ToList();

        if (recentItems.Count == 0)
        {
            panelList.Add(panelEmptyLabel);
            return;
        }

        foreach (var item in recentItems)
            panelList.Add(BuildNotificationRow(item));
    }

    private void OpenAllModal()
    {
        modalList.Clear();

        if (items.Count == 0)
        {
            modalList.Add(modalEmptyLabel);
        }
        else
        {
            foreach (var item in items.OrderByDescending(i => i.Timestamp))
                modalList.Add(BuildNotificationRow(item));
        }

        modalOverlay.style.display = DisplayStyle.Flex;
    }

    private VisualElement BuildNotificationRow(NotificationItem item)
    {
        var rowButton = new Button(() =>
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Id))
            {
                readIds.Add(item.Id);
                item.IsUnread = false;
                SaveReadIds();
            }

            panelOverlay.style.display = DisplayStyle.None;
            modalOverlay.style.display = DisplayStyle.None;
            UpdateBadge();
            onItemSelected?.Invoke(item);
        });

        rowButton.style.whiteSpace = WhiteSpace.Normal;
        rowButton.style.unityTextAlign = TextAnchor.UpperLeft;
        rowButton.style.justifyContent = Justify.FlexStart;
        rowButton.style.alignItems = Align.FlexStart;
        rowButton.style.flexDirection = FlexDirection.Column;
        rowButton.style.paddingTop = 10;
        rowButton.style.paddingBottom = 10;
        rowButton.style.paddingLeft = 10;
        rowButton.style.paddingRight = 10;
        rowButton.style.marginBottom = 6;
        rowButton.style.backgroundColor = new Color(0.97f, 0.98f, 1f, 1f);
        rowButton.style.borderLeftWidth = 1;
        rowButton.style.borderRightWidth = 1;
        rowButton.style.borderTopWidth = 1;
        rowButton.style.borderBottomWidth = 1;
        rowButton.style.borderLeftColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        rowButton.style.borderRightColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        rowButton.style.borderTopColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        rowButton.style.borderBottomColor = new Color(0.86f, 0.88f, 0.91f, 1f);
        rowButton.style.borderTopLeftRadius = 8;
        rowButton.style.borderTopRightRadius = 8;
        rowButton.style.borderBottomLeftRadius = 8;
        rowButton.style.borderBottomRightRadius = 8;
        rowButton.style.fontSize = 14;

        if (item.IsUnread && !string.IsNullOrWhiteSpace(item.Id) && !readIds.Contains(item.Id))
            rowButton.style.borderLeftColor = new Color(0.12f, 0.37f, 0.87f, 1f);

        var title = new Label(string.IsNullOrWhiteSpace(item.Title) ? "Bildirim" : item.Title);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.10f, 0.14f, 0.19f, 1f);

        var message = new Label(string.IsNullOrWhiteSpace(item.Message) ? "-" : item.Message);
        message.style.whiteSpace = WhiteSpace.Normal;
        message.style.color = new Color(0.27f, 0.31f, 0.35f, 1f);
        message.style.marginTop = 2;

        var date = new Label(item.Timestamp == DateTime.MinValue
            ? "Tarih belirtilmedi"
            : item.Timestamp.ToString("dd MMM yyyy HH:mm", trCulture));
        date.style.fontSize = 11;
        date.style.color = new Color(0.45f, 0.49f, 0.53f, 1f);
        date.style.marginTop = 6;

        rowButton.Add(title);
        rowButton.Add(message);
        rowButton.Add(date);

        return rowButton;
    }

    private void EnsureReadIdsLoaded()
    {
        string storageKey = ResolveStorageKey();
        if (readIdsLoaded && string.Equals(activeStorageKey, storageKey, StringComparison.Ordinal))
            return;

        activeStorageKey = storageKey;
        readIdsLoaded = true;
        readIds.Clear();

        if (!PlayerPrefs.HasKey(storageKey))
            return;

        string raw = PlayerPrefs.GetString(storageKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        try
        {
            var payload = JsonUtility.FromJson<ReadIdsStoreDto>(raw);
            if (payload?.items == null)
                return;

            foreach (var id in payload.items)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    readIds.Add(id);
            }
        }
        catch
        {
            readIds.Clear();
        }
    }

    private void SaveReadIds()
    {
        EnsureReadIdsLoaded();
        if (string.IsNullOrWhiteSpace(activeStorageKey))
            return;

        var data = new ReadIdsStoreDto
        {
            items = readIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Take(MaxStoredReadIds)
                .ToArray()
        };

        PlayerPrefs.SetString(activeStorageKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private string ResolveStorageKey()
    {
        string scope = storageScopeProvider != null ? storageScopeProvider.Invoke() : null;
        if (string.IsNullOrWhiteSpace(scope))
            scope = "global";

        return $"{ReadIdsStorePrefix}.{NormalizeStorageScope(scope)}";
    }

    private static string NormalizeStorageScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return "global";

        var sb = new StringBuilder(scope.Length);
        foreach (char ch in scope)
        {
            sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_');
        }

        return sb.ToString();
    }

    private void UpdateBadge()
    {
        if (badgeLabel == null)
            return;

        int unreadCount = items.Count(i =>
            i != null &&
            !string.IsNullOrWhiteSpace(i.Id) &&
            i.IsUnread &&
            !readIds.Contains(i.Id));

        badgeLabel.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
        badgeLabel.parent.style.display = unreadCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }
}