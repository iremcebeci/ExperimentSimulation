using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CelestialInfoCard : MonoBehaviour
{
    public enum InfoType
    {
        None,
        Sun,
        Earth,
        Moon
    }

    [Header("Info Card")]
    public GameObject cardObject;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Image infoImage;

    [Header("Textures")]
    public Texture2D sunTexture;
    public Texture2D earthTexture;
    public Texture2D moonTexture;

    [Header("Info Button")]
    public Button infoButton;

    [Header("UI Panel Manager")]
    public UIPanelManager uiPanelManager;

    private InfoType currentInfoType = InfoType.None;
    private bool isCardOpen = false;

    void Start()
    {
        if (infoButton != null)
        {
            infoButton.onClick.RemoveListener(ToggleCurrentInfo);
            infoButton.onClick.AddListener(ToggleCurrentInfo);
        }

        HideAll();
    }

    public void OpenSunInfo()
    {
        OpenInfo(InfoType.Sun);
    }

    public void OpenEarthInfo()
    {
        OpenInfo(InfoType.Earth);
    }

    public void OpenMoonInfo()
    {
        OpenInfo(InfoType.Moon);
    }

    public void OpenInfo(InfoType infoType)
    {
        currentInfoType = infoType;

        SetContent(infoType);

        if (cardObject != null)
            cardObject.SetActive(true);

        if (infoButton != null)
            infoButton.gameObject.SetActive(true);

        isCardOpen = true;

        if (uiPanelManager != null)
            uiPanelManager.UpdateAllButtonVisuals();
    }

    public void ToggleCurrentInfo()
    {
        if (currentInfoType == InfoType.None)
            return;

        isCardOpen = !isCardOpen;

        if (cardObject != null)
            cardObject.SetActive(isCardOpen);

        if (uiPanelManager != null)
            uiPanelManager.UpdateAllButtonVisuals();
    }

    public void HideAll()
    {
        currentInfoType = InfoType.None;
        isCardOpen = false;

        if (cardObject != null)
            cardObject.SetActive(false);

        if (infoButton != null)
            infoButton.gameObject.SetActive(false);

        if (uiPanelManager != null)
            uiPanelManager.UpdateAllButtonVisuals();
    }

    private void SetContent(InfoType infoType)
    {
        switch (infoType)
        {
            case InfoType.Sun:
                SetText(
                    "Güneş",
                    "Güneş, Güneş Sistemi’nin merkezinde yer alan büyük bir yıldızdır. \n\n" +
                    "Güneş’in güçlü çekim kuvveti sayesinde Dünya, Ay ve diğer gezegenler belirli yörüngelerde hareket eder. \n\n" +
                    "Güneş’in ışığı, Dünya’nın bir tarafını aydınlatırken diğer tarafının karanlıkta kalmasına neden olur. Bu durum, gece ve gündüz oluşumunu gözlemlememizi sağlar. \n\n" +
                    "Güneş kendi ekseni etrafında da döner. Ancak katı bir yüzeye sahip olmadığı için farklı bölgeleri farklı hızlarda dönebilir. Bu özellik, Güneş’in gaz yapılı bir yıldız olduğunu gösterir."
                );

                SetImage(sunTexture);
                break;

            case InfoType.Earth:
                SetText(
                    "Dünya",
                    "Dünya, Güneş Sistemi’nde üzerinde yaşam olduğu bilinen tek gezegendir. \n\n" +
                    "Dünya kendi ekseni etrafında döner. Bu dönüş sırasında Güneş’e bakan taraf aydınlık olurken diğer taraf karanlıkta kalır ve gece-gündüz oluşur. \n\n" +
                    "Dünya aynı zamanda Güneş’in etrafında belirli bir yörüngede dolanır. Bu hareket yaklaşık 365 gün sürer ve bir yılın oluşmasını sağlar. \n\n" +
                    "Dünya’nın eksen eğikliği nedeniyle Güneş ışınları yıl boyunca farklı açılarla gelir. Bu durum sıcaklık değişimlerine ve mevsimlerin oluşmasına neden olur."
                );

                SetImage(earthTexture);
                break;

            case InfoType.Moon:
                SetText(
                    "Ay",
                    "Ay, Dünya’nın doğal uydusudur ve Dünya’nın etrafında belirli bir yörüngede dolanır. \n\n" +
                    "Ay’ın kendi ışığı yoktur. Güneş’ten gelen ışığı yansıttığı için geceleri parlak görünür. \n\n" +
                    "Ay, Dünya etrafında dolanırken aynı zamanda Dünya ile birlikte Güneş’in etrafındaki harekete de katılır. \n\n" +
                    "Ay’ın Dünya’dan görünen şekli zamanla değişir. Bunun nedeni Ay’ın, Dünya ve Güneş’e göre konumunun değişmesidir. Bu değişimler hilal, yarım ay ve dolunay gibi Ay evrelerini oluşturur."
                );

                SetImage(moonTexture);
                break;

            default:
                HideAll();
                break;
        }
    }

    private void SetText(string title, string body)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;
    }

    private void SetImage(Texture2D texture)
    {
        if (infoImage == null)
            return;

        if (texture == null)
        {
            infoImage.gameObject.SetActive(false);
            return;
        }

        Sprite generatedSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        infoImage.sprite = generatedSprite;
        infoImage.preserveAspect = true;
        infoImage.gameObject.SetActive(true);
    }
}