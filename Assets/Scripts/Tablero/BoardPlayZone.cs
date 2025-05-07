using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(GraphicRaycaster))]
public class BoardPlayZone : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuración Carta Jugada")]
    public Transform playedCardContainer;
    [Range(0.1f, 10f)]
    public float scaleOnBoard = 0.32f;
    public float rotationOnBoard = 0f;

    [Header("Configuración de Slots en Tablero")]
    public int maxSlots = 7;
    public float cardSpacingOnBoard = 150f;
    public float verticalOffsetOnBoard = 0f;

    [Header("Referencias")]
    public DeckManager deckManager;

    private List<GameObject> cardsOnBoard = new List<GameObject>();

    private void Awake()
    {
        Image img = GetComponent<Image>();
        if (img != null) { if (!img.raycastTarget) img.raycastTarget = true; } else { Debug.LogError($"BoardPlayZone necesita Image."); }
        if (playedCardContainer == null) { playedCardContainer = transform; }
        if (GetComponent<GraphicRaycaster>() == null) { Debug.LogError($"BoardPlayZone necesita Graphic Raycaster."); }
    }

    Vector3 CalculateBoardSlotPosition(int cardIndex, int totalCards)
    {
        if (totalCards <= 0) return Vector3.zero;
        float totalWidth = (totalCards > 1) ? (totalCards - 1) * cardSpacingOnBoard : 0;
        float startX = -totalWidth / 2f;
        float cardX = startX + cardIndex * cardSpacingOnBoard;
        float cardY = verticalOffsetOnBoard;
        float cardZ = -cardIndex * 0.01f;
        return new Vector3(cardX, cardY, cardZ);
    }

    public void ReorganizeBoardLayout()
    {
        int cardCount = cardsOnBoard.Count;
        Debug.Log($"[BoardPlayZone] Reorganizando {cardCount} cartas en tablero.");

        Vector3 targetScale = Vector3.one * scaleOnBoard;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);

        for (int i = 0; i < cardCount; i++)
        {
            if (cardsOnBoard[i] == null) continue;
            Vector3 targetLocalPosition = CalculateBoardSlotPosition(i, cardCount);

            cardsOnBoard[i].GetComponent<RectTransform>().localPosition = targetLocalPosition;
            cardsOnBoard[i].transform.localScale = targetScale;
            cardsOnBoard[i].transform.localRotation = targetRotation;
        }
    }

    public void NotifyCardRemovedFromBoard(GameObject cardObject)
    {
        if (cardsOnBoard.Remove(cardObject))
        {
            Debug.Log($"[BoardPlayZone] Carta {cardObject.name} quitada. Reorganizando.");
            ReorganizeBoardLayout();
        }
    }

    // --- ANIMACION DE ENTRADA DE CARTA AL TABLERO ---
    public void AgregarCartaAlTablero(GameObject carta)
    {
        carta.transform.SetParent(playedCardContainer, false);

        // POSICIÓN FINAL
        int cardIndex = cardsOnBoard.Count;
        int totalCards = cardIndex + 1;
        Vector3 finalPos = CalculateBoardSlotPosition(cardIndex, totalCards);

        // ANIMACIÓN: Aparece arriba y en chiquito
        RectTransform rect = carta.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;
        rect.localPosition = finalPos + new Vector3(0, 200, 0); // 200 píxeles arriba

        // Agrega la carta a la lista ANTES de animar
        cardsOnBoard.Add(carta);

        // ANIMACIÓN: mover y escalar
        LeanTween.moveLocal(rect.gameObject, finalPos, 0.5f).setEaseOutExpo();
        LeanTween.scale(rect, Vector3.one * scaleOnBoard, 0.5f).setEaseOutBack();

        // Rotación (si la usás)
        carta.transform.localRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);
    }

    public List<GameObject> CartasEnZona()
    {
        return cardsOnBoard;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Debug.Log("BoardPlayZone: Clic Izquierdo Detectado!");

        if (GameManager.Instance == null || GameManager.Instance.selectedCardObject == null) return;
        GameObject selectedCardGO = GameManager.Instance.selectedCardObject;
        Card cardComponent = selectedCardGO.GetComponent<Card>();
        if (cardComponent == null || cardComponent.cardData == null) return;
        ScriptableCard cardDataToPlay = cardComponent.cardData;

        if (cardsOnBoard.Count >= maxSlots) { Debug.LogWarning("BoardPlayZone: No hay slots."); return; }

        PlayerStats currentPlayerStats = GameManager.Instance.jugador1;
        if (currentPlayerStats == null || !currentPlayerStats.PuedePagar(cardDataToPlay.CostoPoderPolitico)) { return; }

        Debug.Log("BoardPlayZone: Jugando carta...");
        currentPlayerStats.Pagar(cardDataToPlay.CostoPoderPolitico);

        if (deckManager != null && deckManager.cardPrefab != null)
        {
            GameObject playedCard = Instantiate(deckManager.cardPrefab, playedCardContainer);
            playedCard.name = cardDataToPlay.NombreCarta + " (Jugada)";

            Card playedCardComponent = playedCard.GetComponent<Card>();
            if (playedCardComponent != null) { playedCardComponent.SetCardData(cardDataToPlay); }

            playedCard.transform.localRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);

            if (playedCard.GetComponent<BoardCardController>() == null)
                playedCard.AddComponent<BoardCardController>().cardData = cardDataToPlay;

            // --- USAMOS ANIMACION DE ENTRADA ---
            AgregarCartaAlTablero(playedCard);

            Debug.Log($"Carta {cardDataToPlay.NombreCarta} añadida al tablero. Total: {cardsOnBoard.Count}");
        }
        else { Debug.LogError("BoardPlayZone: No se puede instanciar!"); }

        if (deckManager != null) { deckManager.RemoveCardFromHand(selectedCardGO); }
        GameManager.Instance.selectedCardObject = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        ReorganizeBoardLayout();
    }
#endif
}