using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BodyDropSlot : MonoBehaviour, IDropHandler
{
    [Header("Doğru Cevap")]
    public CelestialBodyType expectedBodyType;

    private DraggableBody currentBody;

    public DraggableBody CurrentBody => currentBody;

    private void Awake()
    {
        Image img = GetComponent<Image>();

        if (img == null)
            img = gameObject.AddComponent<Image>();

        img.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        DraggableBody draggedBody = eventData.pointerDrag.GetComponent<DraggableBody>();

        if (draggedBody == null)
            draggedBody = eventData.pointerDrag.GetComponentInParent<DraggableBody>();

        if (draggedBody == null)
            return;

        if (currentBody != null && currentBody != draggedBody)
        {
            currentBody.ReturnToStart();
        }

        currentBody = draggedBody;

        draggedBody.SetCurrentSlot(this);
        draggedBody.SnapToSlot(transform);

        Debug.Log(gameObject.name + " slotuna " + draggedBody.bodyType + " bırakıldı.");
    }

    public void ClearSlot(DraggableBody body)
    {
        if (currentBody == body)
            currentBody = null;
    }

    public bool IsCorrect()
    {
        DraggableBody bodyInSlot = currentBody;

        // Güvenlik: currentBody boşsa slotun child objesinden tekrar bul.
        if (bodyInSlot == null)
            bodyInSlot = GetComponentInChildren<DraggableBody>();

        if (bodyInSlot == null)
        {
            Debug.LogWarning(gameObject.name + " boş görünüyor.");
            return false;
        }

        bool result = bodyInSlot.bodyType == expectedBodyType;

        Debug.Log(
            gameObject.name +
            " | Beklenen: " + expectedBodyType +
            " | Gelen: " + bodyInSlot.bodyType +
            " | Sonuç: " + result
        );

        return result;
    }
}