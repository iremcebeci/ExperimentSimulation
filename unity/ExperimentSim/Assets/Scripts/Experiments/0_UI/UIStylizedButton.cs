using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIStylizedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("State")]
    public bool isActive = false;
    public bool useActiveState = true;

    [Header("Background Colors")]
    public Color normalBackground = new Color(0.08f, 0.10f, 0.16f, 0.92f);
    public Color hoverBackground = new Color(0.12f, 0.16f, 0.24f, 0.95f);
    public Color pressedBackground = new Color(0.05f, 0.07f, 0.12f, 0.98f);
    public Color activeBackground = new Color(0.10f, 0.14f, 0.22f, 0.98f);

    [Header("Text Colors")]
    public Color normalText = new Color(0.92f, 0.96f, 1f, 1f);
    public Color hoverText = Color.white;
    public Color pressedText = Color.white;
    public Color activeText = Color.white;

    [Header("Border")]
    public bool useBorder = true;
    public Color normalBorder = new Color(0.25f, 0.32f, 0.45f, 0.75f);
    public Color activeBorder = new Color(0.22f, 0.74f, 0.97f, 1f);
    public float borderSize = 2f;

    [Header("Text Settings")]
    public bool overrideFontSize = false;
    public float fontSize = 22f;

    private Image backgroundImage;
    private TMP_Text text;
    private Outline outline;
    private Button button;

    private bool isHovering;
    private bool isPressing;

    void Awake()
    {
        Setup();
        Refresh();
    }

    void OnEnable()
    {
        Setup();
        Refresh();
    }

    void OnValidate()
    {
        Setup();
        Refresh();
    }

    private void Setup()
    {
        backgroundImage = GetComponent<Image>();
        text = GetComponentInChildren<TMP_Text>();
        button = GetComponent<Button>();

        if (button != null)
        {
            button.transition = Selectable.Transition.None;
        }

        if (useBorder)
        {
            outline = GetComponent<Outline>();

            if (outline == null)
                outline = gameObject.AddComponent<Outline>();

            outline.useGraphicAlpha = false;
        }
        else
        {
            outline = GetComponent<Outline>();

            if (outline != null)
                outline.enabled = false;
        }
    }

    public void SetActiveState(bool value)
    {
        isActive = value;
        Refresh();
    }

    public void Activate()
    {
        SetActiveState(true);
    }

    public void Deactivate()
    {
        SetActiveState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        isPressing = false;
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        Refresh();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        Refresh();
    }

    private void Refresh()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        if (backgroundImage != null)
        {
            backgroundImage.color = GetBackgroundColor();
        }

        if (text != null)
        {
            text.color = GetTextColor();

            if (overrideFontSize)
            {
                text.fontSize = fontSize;
            }
        }

        if (useBorder)
        {
            if (outline == null)
                outline = GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled = true;
                outline.effectColor = GetBorderColor();
                outline.effectDistance = new Vector2(borderSize, -borderSize);
            }
        }
    }

    private Color GetBackgroundColor()
    {
        if (isPressing)
            return pressedBackground;

        if (useActiveState && isActive)
            return activeBackground;

        if (isHovering)
            return hoverBackground;

        return normalBackground;
    }

    private Color GetTextColor()
    {
        if (isPressing)
            return pressedText;

        if (useActiveState && isActive)
            return activeText;

        if (isHovering)
            return hoverText;

        return normalText;
    }

    private Color GetBorderColor()
    {
        if (useActiveState && isActive)
            return activeBorder;

        return normalBorder;
    }
}