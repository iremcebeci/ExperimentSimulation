using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableBody : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Gök Cismi Türü")]
    public CelestialBodyType bodyType;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;

    private Transform startParent;
    private Vector2 startAnchoredPosition;

    public BodyDropSlot CurrentSlot { get; private set; }

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
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearSlot(this);
            CurrentSlot = null;
        }

        transform.SetParent(parentCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.85f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (CurrentSlot == null)
        {
            ReturnToStart();
        }
    }

    public void SetCurrentSlot(BodyDropSlot slot)
    {
        CurrentSlot = slot;
    }

    public void SnapToSlot(Transform slotTransform)
    {
        transform.SetParent(slotTransform, false);

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
        CurrentSlot = null;
    }
}