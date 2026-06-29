using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimulationMenuController : MonoBehaviour
{
    [Header("Scenes")]
    public string dashboardSceneName = "DashboardScene";
    public string solarSystemSceneName = "SolarSystemScene";
    public string eclipseSceneName = "SolarEclipseScene";

    [Header("Resolution")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    private Canvas canvas;

    private Sprite round12;
    private Sprite round18;
    private Sprite round24;
    private Sprite round32;

    private readonly Color32 pageBg = new Color32(243, 247, 253, 255);
    private readonly Color32 white = new Color32(255, 255, 255, 255);

    private readonly Color32 navy = new Color32(37, 55, 105, 255);
    private readonly Color32 navySoft = new Color32(232, 238, 255, 255);

    private readonly Color32 cyan = new Color32(32, 160, 205, 255);
    private readonly Color32 cyanSoft = new Color32(226, 248, 255, 255);

    private readonly Color32 purple = new Color32(110, 86, 200, 255);
    private readonly Color32 purpleSoft = new Color32(242, 237, 255, 255);

    private readonly Color32 darkText = new Color32(27, 32, 45, 255);
    private readonly Color32 mutedText = new Color32(105, 115, 135, 255);

    private readonly Color32 green = new Color32(53, 160, 95, 255);
    private readonly Color32 greenSoft = new Color32(230, 248, 238, 255);

    private void Start()
    {
        AssignmentSession.ClearAssignmentOnly();

        CreateSprites();
        CreateEventSystemIfNeeded();
        CreateCanvas();
        BuildMenu();
    }

    private void CreateSprites()
    {
        round12 = CreateRoundedSprite(12);
        round18 = CreateRoundedSprite(18);
        round24 = CreateRoundedSprite(24);
        round32 = CreateRoundedSprite(32);
    }

    private void CreateEventSystemIfNeeded()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<StandaloneInputModule>();
    }

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("SimulationMenuCanvas");

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void BuildMenu()
    {
        CreateFullBackground();

        CreateTopBar();
        CreateHero();
        CreateCardsTitle();
        CreateCards();
    }

    private void CreateFullBackground()
    {
        GameObject bg = CreateUIObject("Background", canvas.transform);
        Stretch(bg);

        Image image = bg.AddComponent<Image>();
        image.color = pageBg;
    }

    private void CreateTopBar()
    {
        GameObject topBar = CreatePanel(
            "TopBar",
            new Vector2(0, 385),
            new Vector2(1260, 88),
            white,
            round24,
            true
        );

        Button backButton = CreateButton(
            topBar.transform,
            "← Dashboard",
            new Vector2(-525, 0),
            new Vector2(150, 44),
            navySoft,
            navy,
            round12
        );

        backButton.onClick.AddListener(() =>
        {
            AssignmentSession.ClearAssignmentOnly();
            SceneManager.LoadScene(dashboardSceneName);
        });

        CreateText(
            topBar.transform,
            "Simülasyon Merkezi",
            new Vector2(-190, 14),
            new Vector2(600, 34),
            27,
            FontStyles.Bold,
            darkText,
            TextAlignmentOptions.Left
        );

        CreateText(
            topBar.transform,
            "Deney seç, gözlemle ve sonucu takip et.",
            new Vector2(-190, -18),
            new Vector2(600, 26),
            14,
            FontStyles.Normal,
            mutedText,
            TextAlignmentOptions.Left
        );

        CreatePill(
            topBar.transform,
            "Aktif Deneyler",
            new Vector2(505, 0),
            new Vector2(150, 36),
            purpleSoft,
            purple
        );
    }

    private void CreateHero()
    {
        GameObject hero = CreatePanel(
            "Hero",
            new Vector2(0, 225),
            new Vector2(1260, 175),
            navy,
            round32,
            true
        );

        CreateText(
            hero.transform,
            "Bugün hangi deneyi keşfedelim?",
            new Vector2(-255, 36),
            new Vector2(760, 48),
            31,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Left,
            true
        );

        CreateText(
            hero.transform,
            "Güneş sistemi, tutulmalar ve gökyüzü olaylarını etkileşimli simülasyonlarla incele.",
            new Vector2(-255, -15),
            new Vector2(760, 48),
            17,
            FontStyles.Normal,
            new Color32(225, 232, 255, 255),
            TextAlignmentOptions.Left,
            true
        );

        GameObject badge = CreatePanelChild(
            hero.transform,
            "HeroBadge",
            new Vector2(470, 0),
            new Vector2(235, 100),
            new Color32(255, 255, 255, 35),
            round24,
            false
        );

        CreateText(
            badge.transform,
            "Scienyx",
            new Vector2(0, 18),
            new Vector2(200, 34),
            27,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center
        );

        CreateText(
            badge.transform,
            "Deney Simülasyonları",
            new Vector2(0, -18),
            new Vector2(205, 26),
            14,
            FontStyles.Normal,
            new Color32(232, 238, 255, 255),
            TextAlignmentOptions.Center
        );
    }

    private void CreateCardsTitle()
    {
        CreateText(
            canvas.transform,
            "Deneyler",
            new Vector2(-545, 78),
            new Vector2(300, 40),
            24,
            FontStyles.Bold,
            darkText,
            TextAlignmentOptions.Left
        );

        CreateText(
            canvas.transform,
            "2 simülasyon",
            new Vector2(510, 78),
            new Vector2(220, 36),
            15,
            FontStyles.Bold,
            mutedText,
            TextAlignmentOptions.Right
        );
    }

    private void CreateCards()
    {
        CreateCard(
            new Vector2(-250, -120),
            "Güneş Sistemi Gözlem Deneyi",
            "Dünya ve Ay’ın hareketlerini gözlemle, 2026 tutulma tarihlerini cevapla.",
            "8. Sınıf Fen",
            "Gözlem",
            "GS",
            navy,
            navySoft,
            solarSystemSceneName
        );

        CreateCard(
            new Vector2(250, -120),
            "Güneş ve Ay Tutulması",
            "Güneş, Dünya ve Ay’ın konumlarını kullanarak tutulmaları incele.",
            "8. Sınıf Fen",
            "Tutulma",
            "TA",
            cyan,
            cyanSoft,
            eclipseSceneName
        );
    }

    private void CreateCard(
        Vector2 position,
        string title,
        string description,
        string grade,
        string tag,
        string iconText,
        Color32 accent,
        Color32 soft,
        string sceneName)
    {
        GameObject card = CreatePanel(
            title + " Card",
            position,
            new Vector2(450, 290),
            white,
            round24,
            true
        );

        Button cardButton = card.AddComponent<Button>();
        cardButton.transition = Selectable.Transition.ColorTint;
        cardButton.onClick.AddListener(() => OpenScene(sceneName));

        GameObject iconBox = CreatePanelChild(
            card.transform,
            "IconBox",
            new Vector2(-165, 92),
            new Vector2(58, 58),
            soft,
            round18,
            false
        );

        CreateText(
            iconBox.transform,
            iconText,
            Vector2.zero,
            new Vector2(58, 58),
            18,
            FontStyles.Bold,
            accent,
            TextAlignmentOptions.Center
        );

        CreatePill(
            card.transform,
            tag,
            new Vector2(-55, 92),
            new Vector2(130, 34),
            soft,
            accent
        );

        CreateText(
            card.transform,
            title,
            new Vector2(0, 36),
            new Vector2(380, 60),
            21,
            FontStyles.Bold,
            darkText,
            TextAlignmentOptions.Left,
            true
        );

        CreateText(
            card.transform,
            description,
            new Vector2(0, -34),
            new Vector2(380, 64),
            14,
            FontStyles.Normal,
            mutedText,
            TextAlignmentOptions.Left,
            true
        );

        CreatePill(
            card.transform,
            grade,
            new Vector2(-132, -96),
            new Vector2(150, 34),
            greenSoft,
            green
        );

        Button startButton = CreateButton(
            card.transform,
            "Başlat",
            new Vector2(125, -96),
            new Vector2(130, 42),
            accent,
            Color.white,
            round12
        );

        startButton.onClick.AddListener(() => OpenScene(sceneName));
    }

    private void OpenScene(string sceneName)
    {
        AssignmentSession.ClearAssignmentOnly();

        Debug.Log("[SimulationMenu] Açılan sahne: " + sceneName);

        SceneManager.LoadScene(sceneName);
    }

    private GameObject CreatePanel(
        string name,
        Vector2 position,
        Vector2 size,
        Color32 color,
        Sprite sprite,
        bool shadow)
    {
        GameObject obj = CreateUIObject(name, canvas.transform);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        if (shadow)
        {
            Shadow shadowComp = obj.AddComponent<Shadow>();
            shadowComp.effectColor = new Color32(35, 45, 75, 32);
            shadowComp.effectDistance = new Vector2(0, -4);
        }

        return obj;
    }

    private GameObject CreatePanelChild(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color32 color,
        Sprite sprite,
        bool shadow)
    {
        GameObject obj = CreateUIObject(name, parent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        if (shadow)
        {
            Shadow shadowComp = obj.AddComponent<Shadow>();
            shadowComp.effectColor = new Color32(35, 45, 75, 24);
            shadowComp.effectDistance = new Vector2(0, -3);
        }

        return obj;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        Color32 bgColor,
        Color textColor,
        Sprite sprite)
    {
        GameObject obj = CreatePanelChild(
            parent,
            label + " Button",
            position,
            size,
            bgColor,
            sprite,
            false
        );

        Button button = obj.AddComponent<Button>();

        CreateText(
            obj.transform,
            label,
            Vector2.zero,
            size,
            15,
            FontStyles.Bold,
            textColor,
            TextAlignmentOptions.Center
        );

        return button;
    }

    private TMP_Text CreatePill(
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        Color32 bgColor,
        Color32 textColor)
    {
        GameObject obj = CreatePanelChild(
            parent,
            label + " Pill",
            position,
            size,
            bgColor,
            round12,
            false
        );

        return CreateText(
            obj.transform,
            label,
            Vector2.zero,
            size,
            13,
            FontStyles.Bold,
            textColor,
            TextAlignmentOptions.Center
        );
    }

    private TMP_Text CreateText(
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        int fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment,
        bool wrap = false)
    {
        GameObject obj = CreateUIObject(value + " Text", parent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    private void CreateDecorBox(string name, Transform parent, Vector2 position, Vector2 size, Color32 color)
    {
        GameObject obj = CreatePanelChild(
            parent,
            name,
            position,
            size,
            color,
            round32,
            false
        );

        Image image = obj.GetComponent<Image>();
        image.raycastTarget = false;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;

        return obj;
    }

    private void Stretch(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite CreateRoundedSprite(int radius)
    {
        int size = 128;

        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(255, 255, 255, 0);
        Color32 solid = new Color32(255, 255, 255, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, size, size, radius);
                texture.SetPixel(x, y, inside ? solid : clear);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius)
        );
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        int left = radius;
        int right = width - radius - 1;
        int bottom = radius;
        int top = height - radius - 1;

        if (x >= left && x <= right)
            return true;

        if (y >= bottom && y <= top)
            return true;

        int cx = x < left ? left : right;
        int cy = y < bottom ? bottom : top;

        int dx = x - cx;
        int dy = y - cy;

        return dx * dx + dy * dy <= radius * radius;
    }
}