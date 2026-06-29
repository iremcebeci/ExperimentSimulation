using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MatchPlaceholder : MonoBehaviour, IDropHandler
{
    [Header("Bu kutuya gelmesi gereken cevap ID")]
    public string expectedAnswerId;

    private MatchCard currentCard;

    public MatchCard CurrentCard => currentCard;

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

        MatchCard draggedCard = eventData.pointerDrag.GetComponent<MatchCard>();

        if (draggedCard == null)
            draggedCard = eventData.pointerDrag.GetComponentInParent<MatchCard>();

        if (draggedCard == null)
            return;

        if (currentCard != null && currentCard != draggedCard)
        {
            currentCard.ReturnToStart();
        }

        currentCard = draggedCard;
        draggedCard.SetCurrentPlaceholder(this);
        draggedCard.SnapToPlaceholder(transform);

        Debug.Log(gameObject.name + " alanına " + draggedCard.answerId + " bırakıldı.");
    }

    public void ClearPlaceholder(MatchCard card)
    {
        if (currentCard == card)
            currentCard = null;
    }

    public bool IsCorrect()
    {
        MatchCard cardInPlaceholder = currentCard;

        if (cardInPlaceholder == null)
            cardInPlaceholder = GetComponentInChildren<MatchCard>();

        if (cardInPlaceholder == null)
        {
            Debug.LogWarning(gameObject.name + " boş.");
            return false;
        }

        bool result = cardInPlaceholder.answerId == expectedAnswerId;

        Debug.Log(
            gameObject.name +
            " | Beklenen: " + expectedAnswerId +
            " | Gelen: " + cardInPlaceholder.answerId +
            " | Sonuç: " + result
        );

        return result;
    }
}