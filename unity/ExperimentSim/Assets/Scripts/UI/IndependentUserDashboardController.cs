using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    [SerializeField] private string myProfilePath = "/api/User/me";
    [SerializeField] private string personalActivityPath = "/api/Class/activity/personal";
    [SerializeField] private string sessionHeartbeatPath = "/api/User/session/heartbeat";
    [SerializeField] private string sessionEndPath = "/api/User/session/end";
    [SerializeField] private string sessionWeeklyHoursPath = "/api/User/session/weekly-hours";

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

    // Cached data
    private AssignmentDto[] assignmentItems = Array.Empty<AssignmentDto>();
    private ExperimentDto[] experimentItems = Array.Empty<ExperimentDto>();
    private ClassActivityDto[] personalActivityItems = Array.Empty<ClassActivityDto>();
    private ProfileMeDto profileMe;
    private Coroutine sessionHeartbeatRoutine;

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
        BindMenuButtons();

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
        var names = new[] { "HomeBtn", "ProgressBtn", "ActivityBtn", "ProfileBtn", "StartSimulationBtn" };

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
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<AssignmentListWrapper>("{\"items\":" + raw + "}");
        assignmentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<AssignmentDto>();

        ApplyHomeDashboardMetrics();
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
            yield break;
        }

        string raw = req.downloadHandler != null ? req.downloadHandler.text : "[]";
        var wrapped = JsonUtility.FromJson<ExperimentListWrapper>("{\"items\":" + raw + "}");
        experimentItems = wrapped != null && wrapped.items != null ? wrapped.items : Array.Empty<ExperimentDto>();
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