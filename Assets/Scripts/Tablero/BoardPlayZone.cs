// BoardPlayZone.cs
using UnityEngine;
using UnityEngine.EventSystems; // Necesario para IDropHandler y IPointerClickHandler
using System.Collections.Generic;
// using System.Linq; // Si lo usas en los métodos que no pegué

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class BoardPlayZone : MonoBehaviour, IPointerClickHandler, IDropHandler // IDropHandler debe estar aquí
{
    [Header("Configuración Carta Jugada")]
    public Transform playedCardContainer;
    [Range(0.1f, 2f)] public float scaleOnBoard = 0.4f;
    public float rotationOnBoard = 0f;

    [Header("Configuración de Slots en Tablero")]
    public int maxSlots = 5;
    [Tooltip("Espacio entre centros de cartas.")]
    public float cardSpacingOnBoard = 160f;
    public float verticalOffsetOnBoard = 0f;

    private List<GameObject> cardsOnBoard = new List<GameObject>();

    private void Awake()
    {
        UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
        if (img == null) {
            img = gameObject.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(1, 1, 1, 0); // Invisible pero raycasteable
        }
        if (!img.raycastTarget) img.raycastTarget = true; // Crucial para que OnDrop funcione

        if (playedCardContainer == null) playedCardContainer = this.transform;
    }

    // ... (Tus métodos CalculateBoardSlotPosition, ReorganizeBoardLayout, NotifyCardRemovedFromBoard, AddInstantiatedCardToBoardAndAnimate, CartasEnZona, HayCartasConFuerosActivas se mantienen igual)
    // Pega aquí tus métodos:
    // CalculateBoardSlotPosition
    // ReorganizeBoardLayout
    // NotifyCardRemovedFromBoard
    // AddInstantiatedCardToBoardAndAnimate
    // CartasEnZona
    // HayCartasConFuerosActivas
    // OnPointerClick (para jugar con click, si mantienes esa funcionalidad)

    // --- COMIENZO DE MÉTODOS PEGADOS (Asegúrate de tenerlos aquí) ---
    private Vector3 CalculateBoardSlotPosition(int cardIndex, int totalCards)
    {
        if (totalCards <= 0) return new Vector3(0, verticalOffsetOnBoard, 0);
        float spacing = Mathf.Max(10f, cardSpacingOnBoard);
        float totalWidth = (totalCards > 1) ? (totalCards - 1) * spacing : 0;
        float startX = -totalWidth / 2f;
        float cardX = startX + cardIndex * spacing;
        float cardY = verticalOffsetOnBoard;
        float cardZ = -cardIndex * 0.01f; // Para solapamiento visual si es necesario
        return new Vector3(cardX, cardY, cardZ);
    }

    public void ReorganizeBoardLayout()
    {
        cardsOnBoard.RemoveAll(item => item == null);
        int cardCount = cardsOnBoard.Count;
        Vector3 targetScale = Vector3.one * scaleOnBoard;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);

        for (int i = 0; i < cardCount; i++) {
            GameObject cartaGO = cardsOnBoard[i];
            if (cartaGO == null) continue;
            Vector3 targetLocalPosition = CalculateBoardSlotPosition(i, cardCount);
            RectTransform rt = cartaGO.GetComponent<RectTransform>();

            if (Application.isPlaying && rt != null) {
                if (LeanTween.isTweening(rt.gameObject)) LeanTween.cancel(rt.gameObject);
                if(Vector3.Distance(rt.localPosition, targetLocalPosition) > 0.1f) LeanTween.moveLocal(rt.gameObject, targetLocalPosition, 0.3f).setEaseOutExpo();
                else rt.localPosition = targetLocalPosition;
                if(Vector3.Distance(rt.localScale, targetScale) > 0.01f) LeanTween.scale(rt.gameObject, targetScale, 0.3f).setEaseOutExpo();
                else rt.localScale = targetScale;
                rt.localRotation = targetRotation;
            } else if (rt != null) {
                rt.localPosition = targetLocalPosition;
                rt.localScale = targetScale;
                rt.localRotation = targetRotation;
            }
        }
    }
    public void NotifyCardRemovedFromBoard(GameObject cardObject)
    {
        if (cardsOnBoard.Remove(cardObject)) ReorganizeBoardLayout();
    }
    public void AddInstantiatedCardToBoardAndAnimate(GameObject newBoardCardGO, ScriptableCard cardDataForInit)
{
    if (newBoardCardGO == null || playedCardContainer == null) {
        Debug.LogError("AddInstantiatedCardToBoardAndAnimate: newBoardCardGO o playedCardContainer es null.");
        if(newBoardCardGO != null) Destroy(newBoardCardGO);
        return;
    }

    newBoardCardGO.transform.SetParent(playedCardContainer, false);
    BoardCardController bcc = newBoardCardGO.GetComponent<BoardCardController>();

    if (bcc != null) {
        bcc.InitializeOnBoard(cardDataForInit, this); // Inicializa la carta para el tablero
    } else {
        Debug.LogError($"El cardPrefab '{newBoardCardGO.name}' instanciado no tiene BoardCardController!");
        Destroy(newBoardCardGO); // Si no tiene BCC, se destruye y no continuamos
        return;
    }

    // --- COMIENZO DE LAS LÍNEAS CORREGIDAS/AÑADIDAS ---
    // Desactivar CardDragHandler para que no se pueda arrastrar desde el tablero con esa lógica
    CardDragHandler dragHandlerOnBoardCard = newBoardCardGO.GetComponent<CardDragHandler>();
    if (dragHandlerOnBoardCard != null)
    {
        dragHandlerOnBoardCard.enabled = false; // La forma más simple de desactivarlo
        // Alternativamente, si nunca más se necesita: Destroy(dragHandlerOnBoardCard);
        Debug.Log($"[BoardPlayZone] CardDragHandler deshabilitado en la carta instanciada en tablero: {newBoardCardGO.name}");
    }

    // Desactivar el script 'Card.cs' si este maneja la lógica de click para cartas EN MANO
    // (como seleccionar para jugar o hacer zoom desde la mano, lo cual no aplica en tablero)
    Card cardHandLogicComponent = newBoardCardGO.GetComponent<Card>();
    if (cardHandLogicComponent != null)
    {
        cardHandLogicComponent.enabled = false; // Previene que OnPointerClick de Card.cs se active para cartas en tablero
        Debug.Log($"[BoardPlayZone] Componente 'Card' (lógica de mano) deshabilitado en la carta del tablero: {newBoardCardGO.name}");
    }
    // --- FIN DE LAS LÍNEAS CORREGIDAS/AÑADIDAS ---

    // Añadir a la lista lógica de cartas en el tablero
    if (!cardsOnBoard.Contains(newBoardCardGO)) {
        cardsOnBoard.Add(newBoardCardGO);
    } else {
        // Si la carta ya estaba (raro, pero posible), solo reorganizar y salir para evitar doble animación.
        ReorganizeBoardLayout();
        return;
    }

    // Calcular posición y animar
    int cardIndex = cardsOnBoard.Count - 1;
    int totalCards = cardsOnBoard.Count;
    Vector3 finalPos = CalculateBoardSlotPosition(cardIndex, totalCards); // Asumo que tienes este método
    RectTransform rect = newBoardCardGO.GetComponent<RectTransform>();

    if (rect != null && Application.isPlaying) {
        rect.localScale = Vector3.one * scaleOnBoard * 0.1f; // Escala inicial pequeña para animación
        rect.localPosition = finalPos + new Vector3(0, 150, 0); // Posición inicial arriba para animación
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);

        LeanTween.moveLocal(rect.gameObject, finalPos, 0.5f).setEaseOutExpo();
        LeanTween.scale(rect, Vector3.one * scaleOnBoard, 0.5f).setEaseOutBack().setOnComplete(ReorganizeBoardLayout);
    } else {
        // Si no está en playmode o no hay RectTransform, aplicar directamente
        if (rect != null) {
            rect.localPosition = finalPos;
            rect.localScale = Vector3.one * scaleOnBoard;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);
        }
        ReorganizeBoardLayout(); // Reorganizar igualmente
    }
}

    public List<GameObject> CartasEnZona() { cardsOnBoard.RemoveAll(item => item == null); return new List<GameObject>(cardsOnBoard); }
    
    public bool HayCartasConFuerosActivas() { 
        foreach (GameObject cGO in CartasEnZona()) { 
            if (cGO != null) { 
                BoardCardController b = cGO.GetComponent<BoardCardController>(); 
                if (b != null && b.cardData != null && b.cardData.TieneFueros && b.resistencia > 0 && !b.isDying) return true; 
            } 
        } 
        return false; 
    }


    // --- FIN DE MÉTODOS PEGADOS ---


    public void OnDrop(PointerEventData eventData)
{
    GameObject droppedCardGO = eventData.pointerDrag;
    if (droppedCardGO == null) {
        Debug.LogWarning("[BoardPlayZone.OnDrop] eventData.pointerDrag es null.");
        return;
    }

    Card cardComponent = droppedCardGO.GetComponent<Card>();
    CardDragHandler dragHandler = droppedCardGO.GetComponent<CardDragHandler>();

    // Es crucial tener el dragHandler para comunicar el éxito/fracaso del drop
    if (dragHandler == null) {
        Debug.LogError("[BoardPlayZone.OnDrop] CardDragHandler no encontrado en el objeto arrastrado: " + droppedCardGO.name);
        // Si no hay dragHandler, no podemos indicar que el drop falló para que vuelva a la mano.
        // Podrías destruir droppedCardGO aquí si es un estado irrecuperable, o simplemente retornar.
        return;
    }
    
    if (cardComponent == null || cardComponent.CardData == null) {
        Debug.LogError("[BoardPlayZone.OnDrop] Card component o CardData es null en: " + droppedCardGO.name);
        dragHandler.dropSuccessful = false; // Indicar fallo para que CardDragHandler retorne la carta
        return;
    }

    GameManager gm = GameManager.Instance;
    if (gm == null) {
        Debug.LogError("[BoardPlayZone.OnDrop] GameManager.Instance es null.");
        dragHandler.dropSuccessful = false;
        return;
    }

    // Verificar si es la zona del jugador actual
    if (gm.GetCurrentPlayerBoardZone() != this) {
        UIManager.Instance?.ShowStatusMessage("¡Solo puedes jugar en tu propia zona!");
        dragHandler.dropSuccessful = false;
        return;
    }

    ScriptableCard cardData = cardComponent.CardData;
    PlayerStats ownerStats = gm.GetCurrentPlayer();
    DeckManager deckManager = gm.GetCurrentPlayerDeckManager();

    if (ownerStats == null || deckManager == null) {
        Debug.LogError("[BoardPlayZone.OnDrop] OwnerStats o DeckManager es null.");
        dragHandler.dropSuccessful = false;
        return;
    }

    // --- INICIO DE LA NUEVA LÓGICA PARA DIFERENCIAR TIPOS DE CARTA ---
    // Asumo que tu ScriptableCard tiene un campo público string TipoCarta (ej. "Accion", "Político", "Evento")
    string tipoCartaNormalizado = cardData.TipoCarta?.Trim().ToLowerInvariant() ?? "desconocido";
    bool playedSuccessfully = false;

    Debug.Log($"[BoardPlayZone.OnDrop] Intentando jugar carta '{cardData.NombreCarta}' de tipo: '{tipoCartaNormalizado}'");

    if (tipoCartaNormalizado == "accion" || tipoCartaNormalizado == "acción" || tipoCartaNormalizado == "evento")
    {
        // Lógica para jugar una CARTA DE ACCIÓN/EVENTO
        if (ownerStats.PuedePagar(cardData))
        {
            ownerStats.Pagar(cardData);

            // Asumo que tienes un ActionEffectManager como Singleton
            // y que ExecuteEffect usa cardData.EffectType y los ParamXYZ para realizar la acción.
            if (ActionEffectManager.Instance != null)
            {
                Debug.Log($"[BoardPlayZone.OnDrop] Ejecutando efecto para '{cardData.NombreCarta}'..." +
                          $"EffectType: {cardData.effectType}, CartasARobar: {cardData.ParamCartasARobar}, ReduccionCosto: {cardData.ParamReduccionDeCosto}, Duracion: {cardData.ParamDuracionTurnos}");

                ActionEffectManager.Instance.ExecuteEffect(
                    cardData,
                    ownerStats,                       // Jugador que juega la carta
                    gm.GetOpponentPlayer(),           // Oponente
                    this,                             // Zona del jugador actual (puede ser null si el efecto no la necesita)
                    gm.GetOpponentPlayerBoardZone()   // Zona del oponente (puede ser null si el efecto no la necesita)
                );
            }
            else
            {
                Debug.LogError("[BoardPlayZone.OnDrop] ActionEffectManager.Instance es null. No se puede ejecutar el efecto de la carta.");
            }

            deckManager.AddToDiscard(cardData);      // Añadir el ScriptableCard al descarte lógico
            deckManager.RemoveCardFromHand(cardData); // Remover la carta (ScriptableCard y GameObject) de la mano
            playedSuccessfully = true;
            Debug.Log($"[BoardPlayZone.OnDrop] Carta de Acción/Evento '{cardData.NombreCarta}' jugada y enviada al descarte.");
        }
        else
        {
            UIManager.Instance?.ShowStatusMessage($"¡No tienes suficiente Poder Político para '{cardData.NombreCarta}'!");
            playedSuccessfully = false;
        }
    }
    else if (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico" || tipoCartaNormalizado == "apoyo" || tipoCartaNormalizado == "personaje" /*Añade otros tipos que van al tablero aquí*/)
    {
        // Lógica para jugar una CARTA QUE VA AL TABLERO (Políticos, Apoyos, etc.)
        // Primero, verifica si hay espacio (si aplica para este tipo de carta)
        bool requiresLimitedBoardSlot = (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico" || tipoCartaNormalizado == "personaje"); // Ajusta según tus tipos
        if (requiresLimitedBoardSlot && cardsOnBoard.Count >= maxSlots) { // Asumo que tienes List<GameObject> cardsOnBoard y int maxSlots
            UIManager.Instance?.ShowStatusMessage("¡No hay más espacio en el tablero para este tipo de carta!");
            playedSuccessfully = false;
        } else {
            // Si hay espacio (o no requiere slot limitado), intenta jugarla al tablero
            // DeckManager.TryPlayCardFromHandToBoard ya maneja el pago y la instanciación.
            playedSuccessfully = deckManager.TryPlayCardFromHandToBoard(droppedCardGO, this, ownerStats);
        }
    }
    else
    {
        Debug.LogWarning($"[BoardPlayZone.OnDrop] Tipo de carta desconocido o no manejado por drag-and-drop: '{cardData.TipoCarta}' para la carta '{cardData.NombreCarta}'");
        playedSuccessfully = false;
    }
    // --- FIN DE LA NUEVA LÓGICA ---

    dragHandler.dropSuccessful = playedSuccessfully;
    Debug.Log($"[BoardPlayZone.OnDrop] Procesamiento finalizado. Carta: {droppedCardGO?.name}, PlayedSuccessfully: {playedSuccessfully}, dragHandler.dropSuccessful: {dragHandler.dropSuccessful}");
}
    
    // TU MÉTODO OnPointerClick ORIGINAL
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        if (GameManager.Instance.selectedCardObject == null) return; // No hay carta seleccionada en mano para jugar con click

        // Verificar si esta es la zona del jugador actual
        if (this != GameManager.Instance.GetCurrentPlayerBoardZone()) {
            UIManager.Instance?.ShowStatusMessage("Solo puedes jugar cartas en tu propia zona.");
            return;
        }

        GameObject cardObjectInHand = GameManager.Instance.selectedCardObject;
        Card cardInHandComponent = cardObjectInHand.GetComponent<Card>();

        if (cardInHandComponent == null || cardInHandComponent.cardData == null) {
            Debug.LogError("Carta seleccionada en mano sin Card o CardData.");
            GameManager.Instance.selectedCardObject = null; 
            if(cardInHandComponent != null) cardInHandComponent.ToggleHighlight(false); // Asumo que tienes este método
            return;
        }

        ScriptableCard scToPlay = cardInHandComponent.cardData;
        PlayerStats currentPlayerStats = GameManager.Instance.GetCurrentPlayer();
        DeckManager currentDeckManager = GameManager.Instance.GetCurrentPlayerDeckManager();

        if (currentPlayerStats == null || currentDeckManager == null) {
            Debug.LogError("PlayerStats o DeckManager del jugador actual son nulos.");
            return;
        }

        string tipoCartaNormalizado = scToPlay.TipoCarta?.Trim().ToLowerInvariant() ?? "desconocido";

        // Lógica para jugar carta por TIPO (Político, Acción, etc.)
        if (tipoCartaNormalizado == "accion" || tipoCartaNormalizado == "acción" || tipoCartaNormalizado == "evento")
        {
            if (!currentPlayerStats.PuedePagar(scToPlay)) 
            {
                UIManager.Instance?.ShowStatusMessage("¡No tienes suficiente Poder Político!");
                return;
            }
            currentPlayerStats.Pagar(scToPlay); 

            if (ActionEffectManager.Instance != null) {
                PlayerStats opponentPlayer = GameManager.Instance.GetOpponentPlayer();
                BoardPlayZone opponentBoardZone = GameManager.Instance.GetOpponentPlayerBoardZone();
                ActionEffectManager.Instance.ExecuteEffect(scToPlay, currentPlayerStats, opponentPlayer, this, opponentBoardZone);
            } else {
                Debug.LogError("ActionEffectManager no encontrado.");
            }
            currentDeckManager.AddToDiscard(scToPlay); // Añade ScriptableCard a la cripta
            currentDeckManager.RemoveCardFromHand(scToPlay); // Remueve ScriptableCard de la mano y su GO
            GameManager.Instance.selectedCardObject = null; // Deselecciona la carta
            if(cardInHandComponent != null) cardInHandComponent.ToggleHighlight(false);
        }
        else if (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico" || tipoCartaNormalizado == "apoyo")
        {
            if (!currentPlayerStats.PuedePagar(scToPlay)) 
            {
                UIManager.Instance?.ShowStatusMessage("¡No tienes suficiente Poder Político!");
                return;
            }
            
            // Solo políticos (o los tipos que definas) ocupan slots limitados
            bool requiresLimitedBoardSlot = (tipoCartaNormalizado == "político" || tipoCartaNormalizado == "politico");
            if (requiresLimitedBoardSlot && cardsOnBoard.Count >= maxSlots) {
                UIManager.Instance?.ShowStatusMessage("¡No hay más espacio en el tablero para Políticos!");
                return;
            }

            currentPlayerStats.Pagar(scToPlay); 
            currentDeckManager.RemoveCardFromHand(scToPlay); // Remueve ScriptableCard de la mano y su GO

            if (currentDeckManager.cardPrefab != null) {
                GameObject newBoardCardGO = Instantiate(currentDeckManager.cardPrefab); // Usa el prefab del DeckManager
                // Aquí AddInstantiatedCardToBoardAndAnimate se encarga de ponerla en el tablero
                AddInstantiatedCardToBoardAndAnimate(newBoardCardGO, scToPlay);
            } else {
                Debug.LogError("DeckManager no tiene cardPrefab asignado para instanciar en tablero.");
            }
            GameManager.Instance.selectedCardObject = null; // Deselecciona la carta
            if(cardInHandComponent != null) cardInHandComponent.ToggleHighlight(false);
        }
        else {
            UIManager.Instance?.ShowStatusMessage($"Tipo de carta '{scToPlay.TipoCarta}' desconocido o no manejado para jugar con click.");
            GameManager.Instance.selectedCardObject = null; 
            if(cardInHandComponent != null) cardInHandComponent.ToggleHighlight(false);
        }
    }
}