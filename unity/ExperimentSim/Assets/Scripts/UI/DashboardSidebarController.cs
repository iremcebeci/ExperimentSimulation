using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DashboardSidebarController : MonoBehaviour
{
    private const string PrefKeySidebarClosed = "sidebarClosed";

    private AppRouter router;
    private VisualElement root;
    private VisualElement sidebar;

    private Button toggleButton;
    private Button logoutButton;
    private VisualElement toggleIcon;

    private readonly List<DropdownGroup> dropdowns = new();

    private VisualElement modalOverlay;
    private VisualElement modalCard;
    private Button btnToLogin;
    private Button btnCloseApp;
    private Button btnCancel;

    private bool isBound;

    public void Bind(AppRouter router, VisualElement dashboardRoot)
    {
        Unbind();

        this.router = router;
        this.root = dashboardRoot;

        if (root == null)
        {
            Debug.LogError("[DashboardSidebarController] Bind root null geldi.");
            return;
        }

        sidebar = root.Q<VisualElement>("Sidebar");
        if (sidebar == null)
        {
            Debug.LogWarning("[DashboardSidebarController] Sidebar(name='Sidebar') bulunamadı. Bu dashboard sidebar içermiyor olabilir.");
            return;
        }

        toggleButton = root.Q<Button>("ToggleButton");
        toggleIcon = root.Q<VisualElement>("ToggleIcon");

        if (toggleButton == null)
        {
            Debug.LogError("[DashboardSidebarController] ToggleButton (name='ToggleButton') bulunamadı.");
            return;
        }

        bool isClosed = PlayerPrefs.GetInt(PrefKeySidebarClosed, 0) == 1;
        ApplySidebarClosed(isClosed, save: false);

        toggleButton.clicked -= ToggleSidebar;
        toggleButton.clicked += ToggleSidebar;

        logoutButton = root.Q<Button>("LogoutBtn");
        if (logoutButton != null)
        {
            logoutButton.clicked -= OnLogoutClicked;
            logoutButton.clicked += OnLogoutClicked;
        }
        else
        {
            Debug.LogWarning("[DashboardSidebarController] LogoutBtn bulunamadı.");
        }

        RegisterDropdown("UserManagementBtn", "UserManagementSubMenu", "UserManagementChevron");
        RegisterDropdown("RolePermissionBtn", "RolePermissionSubMenu", "RolePermissionChevron");

        BuildLogoutModalIfNeeded();

        isBound = true;
        Debug.Log("[DashboardSidebarController] Bind OK");
    }

    public void Unbind()
    {
        if (!isBound)
        {
            if (modalOverlay != null && root != null && root.Contains(modalOverlay))
                modalOverlay.RemoveFromHierarchy();
        }

        if (toggleButton != null)
            toggleButton.clicked -= ToggleSidebar;

        if (logoutButton != null)
            logoutButton.clicked -= OnLogoutClicked;

        foreach (var d in dropdowns)
        {
            if (d.Button != null && d.ClickHandler != null)
                d.Button.clicked -= d.ClickHandler;
        }
        dropdowns.Clear();

        if (modalOverlay != null)
            modalOverlay.UnregisterCallback<PointerDownEvent>(OnModalOverlayPointerDown);

        if (root != null)
            root.UnregisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);

        if (modalOverlay != null && root != null && root.Contains(modalOverlay))
            modalOverlay.RemoveFromHierarchy();

        router = null;
        root = null;
        sidebar = null;
        toggleButton = null;
        logoutButton = null;
        toggleIcon = null;

        isBound = false;
    }

    private void ToggleSidebar()
    {
        if (sidebar == null) return;

        bool willClose = !sidebar.ClassListContains("close");
        ApplySidebarClosed(willClose, save: true);
        CloseAllSubMenus();
    }

    private void ApplySidebarClosed(bool closed, bool save)
    {
        if (sidebar == null || toggleButton == null) return;

        sidebar.EnableInClassList("close", closed);
        toggleButton.EnableInClassList("rotate", closed);

        if (toggleIcon != null)
            toggleIcon.EnableInClassList("rotate", closed);

        if (save)
        {
            PlayerPrefs.SetInt(PrefKeySidebarClosed, closed ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void RegisterDropdown(string buttonName, string subMenuName, string chevronName)
    {
        if (root == null) return;

        var btn = root.Q<Button>(buttonName);
        var sub = root.Q<VisualElement>(subMenuName);
        var chev = root.Q<VisualElement>(chevronName);

        if (btn == null || sub == null || chev == null)
        {
            return;
        }

        var group = new DropdownGroup(btn, sub, chev);

        void Handler() => ToggleSubMenu(group);

        group.ClickHandler = Handler;

        btn.clicked -= Handler;
        btn.clicked += Handler;

        dropdowns.Add(group);
    }

    private void ToggleSubMenu(DropdownGroup group)
    {
        if (group?.SubMenu == null || group.Chevron == null) return;

        bool willOpen = !group.SubMenu.ClassListContains("show");

        if (willOpen)
            CloseAllSubMenus();

        group.SubMenu.EnableInClassList("show", willOpen);
        group.Chevron.EnableInClassList("rotate", willOpen);

        if (willOpen && sidebar != null && sidebar.ClassListContains("close"))
            ApplySidebarClosed(false, save: true);
    }

    private void CloseAllSubMenus()
    {
        foreach (var d in dropdowns)
        {
            d.SubMenu?.EnableInClassList("show", false);
            d.Chevron?.EnableInClassList("rotate", false);
        }
    }

    private void OnLogoutClicked()
    {
        ShowLogoutModal();
    }



    private void BuildLogoutModalIfNeeded()
    {
        if (modalOverlay != null) return;

        modalOverlay = new VisualElement { name = "LogoutModalOverlay" };
        modalOverlay.pickingMode = PickingMode.Position;
        modalOverlay.style.position = Position.Absolute;
        modalOverlay.style.left = 0;
        modalOverlay.style.top = 0;
        modalOverlay.style.right = 0;
        modalOverlay.style.bottom = 0;
        modalOverlay.style.justifyContent = Justify.Center;
        modalOverlay.style.alignItems = Align.Center;
        modalOverlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.55f));

        modalCard = new VisualElement { name = "LogoutModalCard" };
        modalCard.pickingMode = PickingMode.Position;
        modalCard.style.width = 516;
        modalCard.style.paddingLeft = 18;
        modalCard.style.paddingRight = 18;
        modalCard.style.paddingTop = 16;
        modalCard.style.paddingBottom = 16;
        modalCard.style.backgroundColor = Color.white;

        modalCard.style.borderTopLeftRadius = 16;
        modalCard.style.borderTopRightRadius = 16;
        modalCard.style.borderBottomLeftRadius = 16;
        modalCard.style.borderBottomRightRadius = 16;

        var title = new Label("Çıkış seçenekleri");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 18;
        title.style.marginBottom = 8;

        var desc = new Label("Ne yapmak istiyorsun?");
        desc.style.opacity = 0.8f;
        desc.style.marginBottom = 16;

        var actions = new VisualElement();
        actions.style.flexDirection = FlexDirection.Row;
        actions.style.justifyContent = Justify.FlexEnd;
        actions.style.alignItems = Align.Center;
        actions.style.marginTop = 6;
        actions.style.paddingTop = 4;

        btnCancel = new Button(HideLogoutModal) { text = "Vazgeç" };
        btnToLogin = new Button(() => { HideLogoutModal(); ConfirmLogout(); }) { text = "Giriş sayfasına dön" };
        btnCloseApp = new Button(() => { HideLogoutModal(); CloseApplication(); }) { text = "Uygulamayı kapat" };

        StyleButton(btnCancel, "secondary");
        StyleButton(btnToLogin, "primary");
        StyleButton(btnCloseApp, "danger");

        btnCancel.style.marginRight = 8;
        btnToLogin.style.marginRight = 8;

        actions.Add(btnCancel);
        actions.Add(btnToLogin);
        actions.Add(btnCloseApp);

        modalCard.Add(title);
        modalCard.Add(desc);
        modalCard.Add(actions);

        modalOverlay.Add(modalCard);

        modalOverlay.RegisterCallback<PointerDownEvent>(OnModalOverlayPointerDown);
    }

    private void StyleButton(Button b, string type)
    {
        b.style.height = 36;
        b.style.minWidth = 150;
        b.style.fontSize = 13;

        b.style.paddingLeft = 14;
        b.style.paddingRight = 14;

        b.style.borderTopLeftRadius = 12;
        b.style.borderTopRightRadius = 12;
        b.style.borderBottomLeftRadius = 12;
        b.style.borderBottomRightRadius = 12;

        if (type == "primary")
        {
            b.style.backgroundColor = new StyleColor(new Color32(36, 53, 103, 255));
            b.style.color = Color.white;
        }
        else if (type == "secondary")
        {
            b.style.backgroundColor = new StyleColor(new Color32(235, 238, 248, 255));
            b.style.color = new StyleColor(new Color32(36, 53, 103, 255));
        }
        else if (type == "danger")
        {
            b.style.backgroundColor = new StyleColor(new Color32(200, 45, 45, 255));
            b.style.color = Color.white;
        }
    }

    private void OnModalOverlayPointerDown(PointerDownEvent evt)
    {
        if (evt.target == modalOverlay)
            HideLogoutModal();
    }

    private void OnRootKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape && IsModalOpen())
            HideLogoutModal();
    }

    private bool IsModalOpen()
    {
        return modalOverlay != null && root != null && root.Contains(modalOverlay);
    }

    private void ShowLogoutModal()
    {
        if (root == null) return;
        BuildLogoutModalIfNeeded();

        root.UnregisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
        root.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);

        if (!IsModalOpen())
        {
            root.Add(modalOverlay);
            modalOverlay.Focus();
        }
    }

    private void HideLogoutModal()
    {
        if (IsModalOpen())
            modalOverlay.RemoveFromHierarchy();
    }

    private void ConfirmLogout()
    {
        Debug.Log("[DashboardSidebarController] Logout: clearing session and showing login.");

        ClearUserSession();

        if (router != null)
            router.ShowLogin();
        else
            Debug.LogError("[DashboardSidebarController] Router null! (Bind çağrılmamış olabilir)");
    }

    private void CloseApplication()
    {
        Debug.Log("[DashboardSidebarController] Closing application (no logout).");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ClearUserSession()
    {
        PlayerPrefs.DeleteKey(PrefKeySidebarClosed);

        PlayerPrefs.DeleteKey("auth_user");

        PlayerPrefs.Save();
    }

    private class DropdownGroup
    {
        public Button Button;
        public VisualElement SubMenu;
        public VisualElement Chevron;
        public Action ClickHandler;

        public DropdownGroup(Button button, VisualElement subMenu, VisualElement chevron)
        {
            Button = button;
            SubMenu = subMenu;
            Chevron = chevron;
        }
    }
}