// DeckManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// using TMPro; // Si lo usas
// using static LeanTween; // Si lo usas

public class DeckManager : MonoBehaviour
{
    [Header("Mazo Asignado")]
    public CardCollection mazoAsignado;

    [Header("Prefab de Carta y Referencias Visuales")]
    public GameObject cardPrefab;
    public Transform handContainer;
    public int cartasEnManoInicial = 5;

    [Header("Configuración Visual de la Mano")]
    public float handCardScale = 0.5f;
    public float handCardSpacing = 150f;

    [Header("Configuración Visual de la Cripta (Discard Pile)")]
    public Transform discardPileVisualArea;
    public float discardPileCardScale = 0.4f;
    [Range(0f, 45f)] public float discardPileMaxRotation = 10f;

    [Header("Configuración del Jugador")]
    public bool esJugadorHumano = true;

    [Header("Referencias Asociadas")]
    public PlayerStats associatedPlayerStats;

    [Header("Estado en Juego")]
    public List<ScriptableCard> mazo = new List<ScriptableCard>();
    public List<ScriptableCard> playerHand = new List<ScriptableCard>();
    public List<GameObject> handCardObjects = new List<GameObject>();
    public List<ScriptableCard> discardPile = new List<ScriptableCard>();
    private GameObject lastDiscardedCardVisual = null;


    [Header("Debug")]
    public bool DEBUG_ShowOpponentHandCosts = false;

    // ... (Tus métodos Awake, CrearMazos, BarajarMazo, SetupPlayerDeckAndDealInitialHand, DealInitialHand se mantienen igual)
    // Pega aquí tus métodos:
    // Awake
    // CrearMazos
    // BarajarMazo
    // SetupPlayerDeckAndDealInitialHand
    // DealInitialHand
    // ArrangeHandVisuals
    // RobarCartaVisual
    // AddToDiscard
    // UpdateDiscardVisual
    // GetDiscardPileCount
    // EncontrarCartaJugable
    // GetHandCardObject
    // ActualizarCostosVisualesEnMano

    // --- COMIENZO DE MÉTODOS PEGADOS (Asegúrate de tenerlos aquí) ---
    void Awake()
    {
        if (cardPrefab == null) Debug.LogError($"DeckManager ({gameObject.name}): ¡Card Prefab no está asignado!");
        if (handContainer == null) Debug.LogError($"DeckManager ({gameObject.name}): ¡Hand Container no está asignado!");
        if (associatedPlayerStats == null) Debug.LogError($"DeckManager ({gameObject.name}): ¡Associated Player Stats no está asignado!");
        
        CrearMazos(); 
        // StartCoroutine(SetupPlayerDeckAndDealInitialHand()); // GameManager suele iniciar el robo inicial
    }

    public void CrearMazos()
    {
        mazo.Clear();
        playerHand.Clear();
        discardPile.Clear();
        foreach (GameObject go in handCardObjects) { if (go != null) Destroy(go); }
        handCardObjects.Clear();
        if (lastDiscardedCardVisual != null) { Destroy(lastDiscardedCardVisual); lastDiscardedCardVisual = null; }

        if (mazoAsignado != null && mazoAsignado.Cartas != null && mazoAsignado.Cartas.Count > 0) {
            mazo.AddRange(mazoAsignado.Cartas);
        } else {
            Debug.LogError($"❌ DeckManager ({gameObject.name}): No hay mazoAsignado o está vacío.");
        }
        if (UIManager.Instance != null) UIManager.Instance.UpdateDiscardCountUI(this, 0); // Asumo que tienes UIManager
    }

    public void BarajarMazo()
    {
        for (int i = 0; i < mazo.Count; i++) {
            ScriptableCard t = mazo[i]; 
            int r = Random.Range(i, mazo.Count); 
            mazo[i] = mazo[r]; 
            mazo[r] = t;
        }
    }

    public IEnumerator SetupPlayerDeckAndDealInitialHand() // Lo llamas desde GameManager normalmente
    {
        if (mazo == null || mazo.Count == 0) { 
            Debug.LogWarning($"[DeckManager - {this.name}] Intento de SetupPlayerDeckAndDealInitialHand con mazo vacío o nulo.");
            yield break; 
        }
        BarajarMazo(); 
        yield return StartCoroutine(DealInitialHand(cartasEnManoInicial)); 
    }

    public IEnumerator DealInitialHand(int cantidad)
{
    Debug.Log($"[DeckManager - {gameObject.name}] Iniciando DealInitialHand para {cantidad} cartas.");
    for (int i = 0; i < cantidad; i++)
    {
        if (mazo.Count == 0 && discardPile.Count > 0) {
             // Aquí podrías tener lógica para barajar la cripta en el mazo si te quedas sin cartas
             // ShuffleDiscardIntoDeck(); // Necesitarías implementar este método
             Debug.Log($"[DeckManager - {this.name}] Mazo vacío durante reparto inicial, intentando barajar cripta (si implementado).");
        }
        if (mazo.Count == 0) {
            Debug.LogWarning($"[DeckManager - {gameObject.name}] Mazo vacío, no se pueden robar más cartas para mano inicial. Robadas: {i}");
            break;
        }
        yield return StartCoroutine(RobarCartaVisual()); // RobarCartaVisual ya actualiza contadores y visuales
    }
    Debug.Log($"[DeckManager - {gameObject.name}] Finalizado DealInitialHand. Cartas en mano visual: {handCardObjects.Count}");
    // ArrangeHandVisuals(true); // RobarCartaVisual ya debería llamar a ArrangeHandVisuals
    // ActualizarCostosVisualesEnMano(); // RobarCartaVisual ya debería llamar a esto
}
    
    public void ArrangeHandVisuals(bool animate)
    {
        handCardObjects.RemoveAll(item => item == null); // Limpiar nulos primero
        int cardCount = handCardObjects.Count;
        if (cardCount == 0 && handContainer != null && handContainer.childCount > 0) {
            // Corrección por si handCardObjects se desincroniza de los hijos reales
            Debug.LogWarning($"[DeckManager - {this.name}] ArrangeHandVisuals: handCardObjects estaba vacío pero handContainer tenía hijos. Reconstruyendo...");
            handCardObjects.Clear();
            foreach (Transform child in handContainer) {
                if (child.gameObject.GetComponent<Card>() != null) { // Asegurarse que es una carta
                     handCardObjects.Add(child.gameObject);
                }
            }
            cardCount = handCardObjects.Count;
        }
        if (cardCount == 0) return;


        float spacing = handCardSpacing;
        // Si tienes una carta muy ancha, el spacing podría necesitar ser dinámico o basado en el RectTransform.width
        // float cardWidth = handCardObjects[0].GetComponent<RectTransform>().rect.width * handCardScale;
        // spacing = cardWidth + somePadding;

        float totalWidth = (cardCount > 1) ? (cardCount - 1) * spacing : 0;
        Vector3 targetLocalScale = Vector3.one * handCardScale;

        for (int i = 0; i < cardCount; i++) {
            GameObject cGO = handCardObjects[i];
            if (cGO == null) continue;
            
            // Asegurarse que la carta esté como hija del handContainer
            if(cGO.transform.parent != handContainer) {
                cGO.transform.SetParent(handContainer, false); // false para mantener posición local relativa si es necesario inicialmente
            }

            Vector3 tPos = new Vector3(-totalWidth / 2f + i * spacing, 0, -i * 0.02f); // El Z es para solapamiento
            RectTransform rt = cGO.GetComponent<RectTransform>();

            if (rt != null && Application.isPlaying && animate) { // Verifica si LeanTween está inicializado // Verifica si LeanTween está inicializado 
                if (LeanTween.isTweening(rt.gameObject)) LeanTween.cancel(rt.gameObject);
                
                // Solo animar si hay una diferencia significativa para evitar jiggles
                if(Vector3.Distance(rt.localPosition, tPos) > 0.1f || rt.localPosition.y != tPos.y) {
                    LeanTween.moveLocal(rt.gameObject, tPos, 0.3f).setEaseOutExpo();
                } else { rt.localPosition = tPos; } // Snap si está muy cerca

                if(Vector3.Distance(rt.localScale, targetLocalScale) > 0.01f) {
                    LeanTween.scale(rt.gameObject, targetLocalScale, 0.3f).setEaseOutExpo();
                } else { rt.localScale = targetLocalScale; } // Snap

                rt.localRotation = Quaternion.identity; // Las cartas en mano usualmente no rotan
            } else if (rt != null) { 
                rt.localPosition = tPos;
                rt.localScale = targetLocalScale;
                rt.localRotation = Quaternion.identity;
            }
        }
    }

    public IEnumerator RobarCartaVisual()
    {
        if (mazo.Count == 0) { 
            // Lógica opcional: si el mazo está vacío, intentar barajar la cripta en el mazo.
            // if (discardPile.Count > 0) { ShuffleDiscardIntoDeck(); }
            // else { Debug.Log($"[DeckManager - {gameObject.name}] Mazo y cripta vacíos. No se puede robar."); yield break; }
            if (mazo.Count == 0) { // Comprobar de nuevo después de posible barajado de cripta
                 Debug.Log($"[DeckManager - {gameObject.name}] Mazo vacío. No se puede robar.");
                 // Aquí podrías invocar un evento de "fatiga" o pérdida si el juego lo maneja así.
                 GameManager.Instance?.EndGameByDeckOut(associatedPlayerStats); // Notificar a GameManager
                 yield break;
            }
        }

        ScriptableCard cartaRobada = mazo[0];
        mazo.RemoveAt(0);
        playerHand.Add(cartaRobada); // Lógica de mano
        
        // Debug.Log($"[DeckManager - {gameObject.name}] Robó '{cartaRobada.NombreCarta}'. Mazo: {mazo.Count}. Mano lógica: {playerHand.Count}.");

        if (cardPrefab == null || handContainer == null) { 
            Debug.LogError($"[DeckManager - {gameObject.name}] CardPrefab o HandContainer es null. No se puede instanciar visualmente la carta robada.");
            yield break; 
        }
        GameObject nuevaCartaGO = Instantiate(cardPrefab, handContainer);
        nuevaCartaGO.name = cartaRobada.NombreCarta + (esJugadorHumano ? " (Mano Jugador)" : " (Mano IA - Reverso)");

        Card cardComponent = nuevaCartaGO.GetComponent<Card>();
        if (cardComponent != null) {
            cardComponent.SetCardData(cartaRobada); // Esto debería llamar a CardUI.Setup() dentro
            cardComponent.ShowAsCardBack(!esJugadorHumano); // Mostrar reverso si es IA
            cardComponent.SetInteractable(esJugadorHumano); // Solo el jugador humano interactúa con sus cartas
            var hover = nuevaCartaGO.GetComponent<CardHoverEffect>(); // Asumo que tienes este script
            if(hover != null) hover.enabled = esJugadorHumano;
        } else {
             Debug.LogError($"[DeckManager - {gameObject.name}] El CardPrefab no tiene el componente 'Card'.");
        }
        handCardObjects.Add(nuevaCartaGO); // Añadir a la lista de GameObjects en mano

        ArrangeHandVisuals(true); // Organizar visualmente la mano con animación
        ActualizarCostosVisualesEnMano(); // Actualizar el costo visual (si hay modificadores)
        
       // if (UIManager.Instance != null) UIManager.Instance.UpdateDeckCountUI(this, mazo.Count); // Actualizar UI del mazo
        yield return null; // Esperar un frame para que las animaciones comiencen bien
    }

    public void AddToDiscard(ScriptableCard cardData)
    {
        if (cardData != null) {
            discardPile.Add(cardData);
            UpdateDiscardVisual(cardData); // Actualizar el visual de la cripta
            if (UIManager.Instance != null) UIManager.Instance.UpdateDiscardCountUI(this, discardPile.Count);
        }
    }

    private void UpdateDiscardVisual(ScriptableCard lastCard)
    {
        if (discardPileVisualArea == null || cardPrefab == null || lastCard == null) return;
        
        // Destruir la carta visual anterior de la cripta si existe
        if (lastDiscardedCardVisual != null) Destroy(lastDiscardedCardVisual);
        
        lastDiscardedCardVisual = Instantiate(cardPrefab, discardPileVisualArea);
        lastDiscardedCardVisual.name = lastCard.NombreCarta + " (Visual Cripta)";
        
        Card cardComponent = lastDiscardedCardVisual.GetComponent<Card>();
        if (cardComponent != null) {
            cardComponent.SetCardData(lastCard);
            cardComponent.ShowAsCardBack(false); // Siempre mostrar cara en la cripta (o como prefieras)
            cardComponent.isInteractable = false; // No interactuable en cripta
            cardComponent.SetDiscardedLook(true); // Método para apariencia de "descartada"
            
            var hover = lastDiscardedCardVisual.GetComponent<CardHoverEffect>();
            if(hover != null) hover.enabled = false; // Deshabilitar hover en cripta
            
            // Remover otros componentes que no aplican en cripta, como BoardCardController o CardDragHandler si los tuviera
            BoardCardController bcc = lastDiscardedCardVisual.GetComponent<BoardCardController>();
            if (bcc != null) Destroy(bcc); 
            CardDragHandler cdh = lastDiscardedCardVisual.GetComponent<CardDragHandler>();
            if (cdh != null) Destroy(cdh);
        }
        
        lastDiscardedCardVisual.transform.localScale = Vector3.one * discardPileCardScale;
        lastDiscardedCardVisual.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-discardPileMaxRotation, discardPileMaxRotation));
        lastDiscardedCardVisual.transform.localPosition = Vector3.zero; // Centrado en el discardPileVisualArea
    }

    public int GetDiscardPileCount() { return discardPile.Count; }

    public ScriptableCard EncontrarCartaJugable(int poderDisponibleObsoleto_NO_USAR, BoardPlayZone zonaTableroIA)
    {
        if (associatedPlayerStats == null) return null;
        List<ScriptableCard> manoActual = new List<ScriptableCard>(playerHand); // Iterar sobre una copia
        manoActual.Sort((a, b) => b.CostoPoderPolitico.CompareTo(a.CostoPoderPolitico)); // Opcional: Priorizar cartas más caras o más baratas

        foreach (var carta in manoActual) {
            if (carta == null) continue;
            if (associatedPlayerStats.PuedePagar(carta)) { 
                bool puedeColocar = true;
                string tipoNorm = carta.TipoCarta?.Trim().ToLowerInvariant() ?? "";
                if (tipoNorm == "político" || tipoNorm == "politico") { 
                    if (zonaTableroIA.CartasEnZona().Count >= zonaTableroIA.maxSlots) {
                        puedeColocar = false;
                    }
                }
                if (puedeColocar) return carta;
            }
        }
        return null;
    }
    
    public GameObject GetHandCardObject(ScriptableCard cardData)
    {
        if (cardData == null) return null;
        foreach (GameObject cardGO in handCardObjects) {
            if (cardGO != null) {
                Card cardComponent = cardGO.GetComponent<Card>();
                // Compara por IdUnico si lo tienes, o por referencia de ScriptableObject si son únicos.
                if (cardComponent != null && cardComponent.CardData != null && cardComponent.CardData == cardData) { // O cardComponent.CardData.IdUnico == cardData.IdUnico
                    return cardGO;
                }
            }
        }
        Debug.LogWarning($"[DeckManager - {this.name}] No se encontró el GameObject para ScriptableCard '{cardData.NombreCarta}' en la mano.");
        return null;
    }
    
    public void ActualizarCostosVisualesEnMano()
    {
        if (associatedPlayerStats == null) return;
        if (!esJugadorHumano && !GameManager.Instance.DEBUG_ShowOpponentHandCosts) return; // Ocultar costos de IA a menos que sea debug
        
        foreach(GameObject cardGO in handCardObjects)
        {
            if (cardGO == null) continue;
            Card cardComponent = cardGO.GetComponent<Card>();
            CardUI cardUIComponent = cardGO.GetComponent<CardUI>(); // Asumo que tienes CardUI
            
            if (cardComponent != null && cardComponent.CardData != null && cardUIComponent != null)
            {
                ScriptableCard sc = cardComponent.CardData;
                int costoReal = associatedPlayerStats.GetCostoRealCarta(sc);
                cardUIComponent.ActualizarCostoVisual(costoReal);
            }
        }
    }


    // --- FIN DE MÉTODOS PEGADOS ---

    public void ReturnCardToHand(GameObject cardObject)
    {
        if (cardObject == null || handContainer == null) return;

        // Asegurarse que la carta no esté ya lógicamente en la mano si solo fue un drag fallido
        // y no se removió de playerHand
        Card cardComponent = cardObject.GetComponent<Card>();
        if (cardComponent != null && cardComponent.CardData != null) {
            if (!playerHand.Contains(cardComponent.CardData)) {
                // Esto no debería pasar si el drag falló antes de removerla de la lógica de mano
                // Pero si se removió y luego falló, hay que re-agregarla
                // playerHand.Add(cardComponent.CardData); 
                // Considera si necesitas esto o si playerHand ya está bien.
            }
        }


        if (!handCardObjects.Contains(cardObject)) {
            handCardObjects.Add(cardObject);
        }
        
        // Si la carta ya es hija del handContainer (podría ser si solo se movió dentro del canvas)
        // o si viene de otro padre temporal.
        cardObject.transform.SetParent(handContainer, true); // true para mantener escala mundial y luego ajustar
        cardObject.transform.SetAsLastSibling(); // O usar el originalSiblingIndex si lo guardas y es relevante

        // En lugar de WaitForEndOfFrame, llama directamente a ArrangeHandVisuals
        // ArrangeHandVisuals se encargará de la posición y escala correcta.
        ArrangeHandVisuals(true); // Animar el retorno
        ActualizarCostosVisualesEnMano(); // Por si acaso
    }


    public bool TryPlayCardFromHandToBoard(GameObject cardHandGO, BoardPlayZone targetZone, PlayerStats ownerStats)
    {
        if (cardHandGO == null || targetZone == null || ownerStats == null)
        {
            Debug.LogError("[DeckManager] ¡Parámetros inválidos en TryPlayCardFromHandToBoard!");
            return false;
        }

        Card cardComponent = cardHandGO.GetComponent<Card>();
        if (cardComponent == null || cardComponent.CardData == null)
        {
            Debug.LogError("[DeckManager] La carta arrastrada no tiene Card component o CardData.");
            return false;
        }
        ScriptableCard cardDataToPlay = cardComponent.CardData;

        // 1. Validar costo
        if (!ownerStats.PuedePagar(cardDataToPlay))
        {
            UIManager.Instance?.ShowStatusMessage($"¡No tienes suficiente Poder Político para '{cardDataToPlay.NombreCarta}'!");
            return false; // No se puede pagar
        }

        // 2. (Opcional) Validar condiciones específicas de la carta o del tablero aquí
        //     (ej: si es carta de tipo Político y el tablero está lleno, ya se validó en BoardPlayZone.OnDrop)

        // 3. Pagar costo
        ownerStats.Pagar(cardDataToPlay);

        // 4. Remover de la mano (lógica y visual)
        //    RemoveCardFromHand se encarga de playerHand.Remove y Destroy(cardHandGO)
        RemoveCardFromHand(cardDataToPlay); // Esto destruirá cardHandGO

        // 5. Instanciar y añadir al tablero
        if (this.cardPrefab != null) { // Usar el cardPrefab del DeckManager
            GameObject newBoardCardInstance = Instantiate(this.cardPrefab);
            // targetZone se encarga de parentar, posicionar, escalar e inicializar BoardCardController
            targetZone.AddInstantiatedCardToBoardAndAnimate(newBoardCardInstance, cardDataToPlay);
            Debug.Log($"[DeckManager - {ownerStats.name}] Jugó '{cardDataToPlay.NombreCarta}' en {targetZone.name}.");
            return true; // Éxito
        } else {
            Debug.LogError($"[DeckManager - {this.name}] CardPrefab es null. No se puede instanciar la carta en el tablero.");
            // Revertir el pago de costo si es posible y deseado (más complejo)
            // ownerStats.ReembolsarCosto(cardDataToPlay); 
            // Devolver la carta a la mano lógicamente (ya fue destruida visualmente) - esto sería un bug.
            // Mejor asegurar que cardPrefab siempre esté asignado.
            return false; // Fracaso por no tener prefab
        }
    }

    public void RemoveCardFromHand(ScriptableCard carta)
    {
        if (carta == null) return;

        // Remover de la lista lógica (playerHand)
        // Si puede haber duplicados de ScriptableCards en mano, RemoveAll podría ser mejor, pero usualmente se quiere quitar una instancia.
        bool removedFromLogic = playerHand.Remove(carta); 
        // Si usas Ids únicos y puede haber múltiples copias del mismo ScriptableCard en la lista playerHand:
        // ScriptableCard cardToRemove = playerHand.FirstOrDefault(c => c.IdUnico == carta.IdUnico);
        // if (cardToRemove != null) removedFromLogic = playerHand.Remove(cardToRemove);


        if (removedFromLogic)
        {
            // Remover de la lista visual (handCardObjects) y destruir el GameObject
            for (int i = handCardObjects.Count - 1; i >= 0; i--)
            {
                GameObject currentGO = handCardObjects[i];
                if (currentGO != null)
                {
                    Card cardComp = currentGO.GetComponent<Card>();
                    // Compara por referencia de ScriptableObject o por IdUnico si lo tienes
                    if (cardComp != null && cardComp.CardData == carta) // O cardComp.CardData.IdUnico == carta.IdUnico
                    {
                        handCardObjects.RemoveAt(i);
                        Destroy(currentGO);
                        // Si solo esperas una instancia visual por ScriptableCard, puedes hacer break aquí.
                        // break; 
                    }
                }
                else
                {
                    handCardObjects.RemoveAt(i); // Remover nulos de la lista
                }
            }
            ArrangeHandVisuals(true); // Reorganizar la mano después de remover una carta
            ActualizarCostosVisualesEnMano();
        }
        else
        {
            // Debug.LogWarning($"[DeckManager - {this.name}] Se intentó remover '{carta.NombreCarta}' de la mano lógica, pero no se encontró.");
        }
    }
}