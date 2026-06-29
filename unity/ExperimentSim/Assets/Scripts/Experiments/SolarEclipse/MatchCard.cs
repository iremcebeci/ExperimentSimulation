using UnityEngine;
using UnityEngine.EventSystems;

public class MatchCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Cevap ID")]
    public string answerId;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;

    private Transform startParent;
    private Vector2 startAnchoredPosition;

    public MatchPlaceholder CurrentPlaceholder { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        startParent = transform.parent;
        startAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentPlaceholder != null)
        {
            CurrentPlaceholder.ClearPlaceholder(this);
            CurrentPlaceholder = null;
        }

        transform.SetParent(parentCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.85f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentCanvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (CurrentPlaceholder == null)
        {
            ReturnToStart();
        }
    }

    public void SetCurrentPlaceholder(MatchPlaceholder placeholder)
    {
        CurrentPlaceholder = placeholder;
    }

    public void SnapToPlaceholder(Transform placeholderTransform)
    {
        transform.SetParent(placeholderTransform, false);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    public void ReturnToStart()
    {
        transform.SetParent(startParent, false);
        rectTransform.anchoredPosition = startAnchoredPosition;
        rectTransform.localScale = Vector3.one;
        CurrentPlaceholder = null;
    }
}