// BoardCardController.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Necesario para usar TextMeshProUGUI
using static LeanTween;

public class BoardCardController : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Datos base de la carta")]
    public ScriptableCard cardData;

    [Header("Estadísticas en Juego")]
    public int influencia;
    public int resistencia;
    public bool yaAtaco;
    public bool enCooldown;

    [Header("UI de la Carta en Tablero")]
    [Tooltip("Referencia al TextMeshProUGUI para la Influencia en esta carta")]
    public TextMeshProUGUI influenciaText;
    [Tooltip("Referencia al TextMeshProUGUI para la Resistencia en esta carta")]
    public TextMeshProUGUI resistenciaText;

    [Header("Feedback Visual - Atacante en Tablero")]
    public GameObject attackerHighlightObject;

    private BoardPlayZone parentBoardZone;
    public bool isDying = false;
    private CanvasGroup _canvasGroup;

    public void InitializeOnBoard(ScriptableCard newCardData, BoardPlayZone assignedZone)
    {
        this.cardData = newCardData;
        this.parentBoardZone = assignedZone;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;
        isDying = false;

        if (this.cardData != null)
        {
            this.name = this.cardData.NombreCarta + " (En Juego)";
            influencia = this.cardData.Influencia;
            resistencia = this.cardData.Resistencia;
            Debug.Log($"[BCC Init - {this.cardData.NombreCarta}] Stats enteros fijados: I={this.influencia}, R={this.resistencia}");

            enCooldown = !this.cardData.AccionInmediata;
            yaAtaco = false;

            ActualizarStatsVisuales();
            
            if (influenciaText != null && resistenciaText != null) {
                Debug.Log($"[BCC Init - {this.cardData.NombreCarta}] Después de ActualizarVisuales: InfluenciaText='{influenciaText.text}', ResistenciaText='{resistenciaText.text}'");
            } else {
                // Este warning es el que veíamos si las referencias son null en la instancia
                Debug.LogWarning($"[BCC Init - {this.cardData.NombreCarta}] influenciaText ({ (influenciaText == null ? "NULL" : "OK") }) o resistenciaText ({ (resistenciaText == null ? "NULL" : "OK") }) es NULL.");
            }

            if (this.parentBoardZone == null) Debug.LogError($"InitializeOnBoard: parentBoardZone para '{this.cardData.NombreCarta}' es null.");
        }
        else
        {
            Debug.LogError($"InitializeOnBoard ({this.name}): fue llamado con newCardData null.");
            if (influenciaText != null) influenciaText.text = "-";
            if (resistenciaText != null) resistenciaText.text = "-";
        }

        Card cardComponentVisual = GetComponent<Card>();
        if (cardComponentVisual != null)
        {
            cardComponentVisual.SetCardData(this.cardData); // Para que CardUI muestre nombre, desc, arte, etc.
            cardComponentVisual.ShowAsCardBack(false);
            cardComponentVisual.isInteractable = true; // Clicks serán manejados por este OnPointerClick
            var hover = GetComponent<CardHoverEffect>();
            if(hover != null) hover.enabled = false;
        } else {
            Debug.LogWarning($"BoardCardController en '{gameObject.name}' no encontró componente Card.");
        }
        
        if (attackerHighlightObject == null) {
            Transform foundHighlight = transform.Find("AttackerHighlightBorder");
            if (foundHighlight != null) attackerHighlightObject = foundHighlight.gameObject;
        }
        if (attackerHighlightObject != null) attackerHighlightObject.SetActive(false);

        if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null) {
            Debug.LogWarning($"La carta en tablero '{this.name}' no tiene Collider. Añadiendo BoxCollider2D por defecto.");
            gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        }
    }

    public void ResetTurnStatus()
    {
        if (!isDying) { enCooldown = false; yaAtaco = false; ToggleAttackerHighlight(false); }
    }

    public void ActualizarStatsVisuales()
    {
        string cardNameForLog = cardData != null ? cardData.NombreCarta : this.name;
        //Debug.Log($"[BCC ActualizarStats - {cardNameForLog}] Intentando actualizar. I Entera={influencia}, R Entera={resistencia}");
        
        if (influenciaText != null) {
            influenciaText.text = influencia.ToString();
        } else {
             //Debug.LogWarning($"'{cardNameForLog}': influenciaText no asignado en BoardCardController al intentar actualizar visual.");
        }

        if (resistenciaText != null) {
            resistenciaText.text = resistencia.ToString();
            if (cardData != null) {
                if (resistencia < cardData.Resistencia) resistenciaText.color = Color.red;
                else if (resistencia > cardData.Resistencia) resistenciaText.color = Color.green;
                else resistenciaText.color = Color.white;
            }
        } else {
            //Debug.LogWarning($"'{cardNameForLog}': resistenciaText no asignado en BoardCardController al intentar actualizar visual.");
        }
    }

    public void ToggleAttackerHighlight(bool isOn)
    {
        if (isDying) return;
        if (attackerHighlightObject != null) attackerHighlightObject.SetActive(isOn);
    }

    public bool PuedeAtacar() { return !enCooldown && !yaAtaco && !isDying; }

    public void Atacar(BoardCardController objetivo)
    {
        // ... (El método Atacar como lo tenías y te pasé antes, con sus logs) ...
        if (isDying || !PuedeAtacar()) { return; }
        if (objetivo == null || objetivo.isDying || objetivo.cardData == null) { Debug.LogWarning($"[{this.name}] Objetivo inválido o muriendo."); return; }
        if (cardData == null) { Debug.LogError($"[{this.name}] Error: Atacante sin cardData."); return; }

        Debug.Log($"INICIANDO ATAQUE: '{cardData.NombreCarta}' ({influencia} / {resistencia}) ataca a '{objetivo.cardData.NombreCarta}' ({objetivo.influencia} / {objetivo.resistencia})");
        this.yaAtaco = true;
        ToggleAttackerHighlight(false);

        Vector3 originalPosition = transform.localPosition;
        float movementDuration = 0.4f;
        float returnDuration = 0.4f;
        float impactPause = 0.1f;
        Vector3 targetAttackPosition = objetivo.transform.localPosition;

        LeanTween.moveLocal(gameObject, targetAttackPosition, movementDuration)
            .setEaseOutCubic()
            .setOnComplete(() => {
                if (this == null || !this.gameObject.activeInHierarchy || isDying) return;
                if (objetivo == null || !objetivo.gameObject.activeInHierarchy || objetivo.isDying) {
                    Debug.LogWarning($"[{this.name}] Objetivo '{objetivo?.cardData?.NombreCarta ?? "NULL"}' inválido o muriendo al momento del impacto.");
                    if (this != null && this.gameObject.activeInHierarchy && !this.isDying) {
                        LeanTween.moveLocal(gameObject, originalPosition, returnDuration).setEaseInCubic().setDelay(impactPause);
                    }
                    return;
                }

                Debug.Log($"APLICANDO DAÑO: '{cardData.NombreCarta}'({influencia}) ataca a '{objetivo.cardData.NombreCarta}'({objetivo.influencia} / {objetivo.resistencia})");
                int resistenciaOriginalObjetivo = objetivo.resistencia;
                objetivo.RecibirDaño(this.influencia);

                if (this != null && this.gameObject.activeInHierarchy && !this.isDying &&
                    objetivo != null && objetivo.gameObject.activeInHierarchy && !objetivo.isDying && objetivo.influencia > 0)
                {
                    Debug.Log($"APLICANDO CONTRAATAQUE: '{objetivo.cardData.NombreCarta}'({objetivo.influencia}) contraataca a '{cardData.NombreCarta}'({resistencia})");
                    this.RecibirDaño(objetivo.influencia);
                }
                
                if (objetivo == null || !objetivo.gameObject.activeInHierarchy) 
                {
                    int dañoSobrante = this.influencia - resistenciaOriginalObjetivo;
                    if (dañoSobrante > 0)
                    {
                        Debug.Log($"ARROLLAR: Daño sobrante {dañoSobrante} de '{cardData.NombreCarta}'. Aplicando al jugador oponente.");
                        PlayerStats jugadorOponente = GameManager.Instance.GetOpponentPlayer(); 
                        if (jugadorOponente != null) {
                            jugadorOponente.RecibirDaño(dañoSobrante); 
                        } else {
                            Debug.LogError("PlayerStats del oponente es null al intentar aplicar daño de Arrollar.");
                        }
                    }
                }

                if (this != null && this.gameObject.activeInHierarchy && !this.isDying)
                {
                    LeanTween.moveLocal(gameObject, originalPosition, returnDuration).setEaseInCubic().setDelay(impactPause);
                }
            });
    }
    
    public void AtacarJugador(PlayerStats jugadorOponente)
    {
        if (isDying || !PuedeAtacar() || cardData == null || jugadorOponente == null) { return; }
        Debug.Log($"INICIANDO ATAQUE AL JUGADOR: '{cardData.NombreCarta}' ({influencia}) ataca a '{jugadorOponente.name}'");
        this.yaAtaco = true;
        ToggleAttackerHighlight(false);

        Vector3 originalPosition = transform.localPosition;
        float movementAmount = 80f; 
        float movementDuration = 0.4f;
        float returnDuration = 0.4f;
        float impactPause = 0.1f;
        Vector3 targetAttackPosition = originalPosition + transform.up * movementAmount;

        LeanTween.moveLocal(gameObject, targetAttackPosition, movementDuration)
            .setEaseOutCubic()
            .setOnComplete(() => {
                if (this == null || !this.gameObject.activeInHierarchy || isDying || jugadorOponente == null) return;
                Debug.Log($"APLICANDO DAÑO AL JUGADOR: '{cardData.NombreCarta}' ({influencia}). '{jugadorOponente.name}' recibe {this.influencia} daño.");
                jugadorOponente.RecibirDaño(this.influencia);
                if (this != null && this.gameObject.activeInHierarchy && !this.isDying)
                {
                    LeanTween.moveLocal(gameObject, originalPosition, returnDuration).setEaseInCubic().setDelay(impactPause);
                }
            });
    }

    public void RecibirDaño(int cantidad)
    {
        if (isDying || cardData == null || cantidad <= 0) return;

        string cardNameForLog = cardData.NombreCarta;
        Debug.Log($"[BCC RecibirDaño - {cardNameForLog}] Antes del daño: Resistencia Entera={resistencia}, Texto Resistencia='{resistenciaText?.text ?? "NULL TEXT REF"}'");
        
        resistencia -= cantidad;
        Debug.Log($"[BCC RecibirDaño - {cardNameForLog}] Después del daño: Resistencia Entera={resistencia}");

        ActualizarStatsVisuales();

        if (resistenciaText != null) {
            Debug.Log($"[BCC RecibirDaño - {cardNameForLog}] Después de ActualizarVisuales: Texto Resistencia='{resistenciaText.text}'");
        } else {
            Debug.LogWarning($"[BCC RecibirDaño - {cardNameForLog}] resistenciaText es NULL al intentar loggear después de ActualizarVisuales.");
        }

        if (resistencia <= 0 && !isDying)
        {
            isDying = true;
            StartCoroutine(PlayDeathAnimationAndDestroy());
        }
    }

    private IEnumerator PlayDeathAnimationAndDestroy()
    {
        Debug.Log($"'{cardData?.NombreCarta}' iniciando animación de destrucción.");
        Collider2D col2D = GetComponent<Collider2D>(); if (col2D != null) col2D.enabled = false;
        Collider col3D = GetComponent<Collider>(); if (col3D != null) col3D.enabled = false;
        ToggleAttackerHighlight(false);

        float deathAnimDuration = 0.6f; 
        if (_canvasGroup != null)
        {
            LeanTween.alphaCanvas(_canvasGroup, 0f, deathAnimDuration).setEaseInExpo(); 
        }
        LeanTween.scale(gameObject, transform.localScale * 0.1f, deathAnimDuration) 
            .setEaseInBack() 
            .setOnComplete(FinalizeDestruction); 
        yield return null; 
    }
    
    private void FinalizeDestruction()
    {
        string cardNameForLog = cardData?.NombreCarta ?? "Carta (ya nula o sin datos)";
        Debug.Log($"'{cardNameForLog}' Finalizando destrucción real.");

        if (cardData != null && GameManager.Instance != null && parentBoardZone != null)
        {
            DeckManager ownerDeckManager = null;
            if (parentBoardZone == GameManager.Instance.boardPlayZoneJugador1) ownerDeckManager = GameManager.Instance.deckManagerJugador1;
            else if (parentBoardZone == GameManager.Instance.boardPlayZoneJugador2) ownerDeckManager = GameManager.Instance.deckManagerJugador2;

            if (ownerDeckManager != null) {
                ownerDeckManager.AddToDiscard(this.cardData);
            } else { 
                Debug.LogError($"No se encontró DeckManager dueño para '{cardNameForLog}' al ser destruida. No se añadió a la cripta.");
            }
        } else {
            Debug.LogWarning($"FinalizeDestruction para '{cardNameForLog}': Faltan referencias clave para añadir a la cripta. CardData: {cardData != null}, GameManager: {GameManager.Instance != null}, ParentBoardZone: {parentBoardZone != null}");
        }

        if (parentBoardZone != null) {
            parentBoardZone.NotifyCardRemovedFromBoard(this.gameObject);
        } else {
            Debug.LogError($"ParentBoardZone es null para '{cardNameForLog}' al finalizar destrucción. No se notificó al tablero.");
        }
        Destroy(gameObject);
    }

    // --- MANEJO DE CLICKS CORRECTO PARA BoardCardController ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // Si la carta no está en una zona del tablero (ej. aún en mano y este script está activo por error allí)
        // O si no tiene datos, o el GameManager no está listo, ignorar.
        // El chequeo de parentBoardZone es crucial para distinguir una carta en mano de una en tablero.
        if (parentBoardZone == null) 
        {
            // Esto puede pasar si el BoardCardController está en un prefab que también se usa para la mano
            // y el click lo procesa Card.cs. Si Card.cs no maneja el click, o si queremos que BCC
            // tenga un comportamiento si se clickea en mano (lo cual es raro), se manejaría aquí.
            // Por ahora, si no tiene parentBoardZone, no es una carta activa en el tablero para BCC.
            // El error "Faltan referencias esenciales" aparecerá si cardData es null (porque InitializeOnBoard no se llamó)
            // Debug.LogWarning($"OnPointerClick en {this.name} ignorado: parentBoardZone es null.");
            return; 
        }

        if (isDying || (GameManager.Instance != null && GameManager.Instance.IsGameOver)) return;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Estas referencias SÍ son esenciales para una carta en tablero
            if (GameManager.Instance == null || cardData == null /* parentBoardZone ya chequeado arriba */) 
            {
                Debug.LogError($"BoardCardController.OnPointerClick para '{this.name}': Faltan GameManager o cardData. (GM: {GameManager.Instance != null}, CD: {cardData != null}, PBZ: {parentBoardZone != null})");
                return;
            }

            bool isCurrentPlayerCard = (parentBoardZone == GameManager.Instance.GetCurrentPlayerBoardZone());

            if (isCurrentPlayerCard) // Click en una de nuestras cartas en el tablero
            {
                if (GameManager.Instance.selectedCardAttacker == this) { // Si ya estaba seleccionada, deseleccionarla
                    GameManager.Instance.ClearAttackerSelection();
                } else if (PuedeAtacar()) { // Si puede atacar, seleccionarla
                    GameManager.Instance.ClearAttackerSelection(); // Deseleccionar cualquier otra
                    GameManager.Instance.selectedCardAttacker = this;
                    ToggleAttackerHighlight(true);
                    UIManager.Instance?.ShowStatusMessage($"'{cardData.NombreCarta}' seleccionado. ¡Selecciona un objetivo enemigo!");
                } else { // No puede atacar
                    if (UIManager.Instance != null) {
                        string razon = "";
                        if (enCooldown) razon = " (Acaba de entrar al tablero)";
                        else if (yaAtaco) razon = " (Ya atacó este turno)";
                        else if (isDying) razon = " (Se está muriendo)";
                        UIManager.Instance.ShowStatusMessage($"'{cardData.NombreCarta}' no puede atacar" + razon);
                    }
                    if(GameManager.Instance.selectedCardAttacker != null) GameManager.Instance.ClearAttackerSelection();
                }
            }
            else // Click en una carta del oponente en el tablero
            {
                if (GameManager.Instance.selectedCardAttacker != null) // Si tenemos un atacante nuestro seleccionado
                {
                    BoardCardController atacante = GameManager.Instance.selectedCardAttacker;
                    BoardCardController objetivoPotencial = this; // 'this' es la carta oponente clickeada
                    BoardPlayZone zonaOponente = GameManager.Instance.GetOpponentPlayerBoardZone();

                    // Doble chequeo de que realmente está en la zona del oponente
                    if (parentBoardZone != null && parentBoardZone == zonaOponente)
                    {
                        // Lógica de "TieneFueros" (Guardaespaldas)
                        bool hayFuerosEnemigos = zonaOponente.HayCartasConFuerosActivas();
                        if (hayFuerosEnemigos && (objetivoPotencial.cardData == null || !objetivoPotencial.cardData.TieneFueros))
                        {
                            Debug.Log($"Ataque inválido: '{atacante.cardData?.NombreCarta}' no puede atacar a '{objetivoPotencial.cardData?.NombreCarta}' porque hay Políticos con Fueros.");
                            if (UIManager.Instance != null) UIManager.Instance.ShowStatusMessage("¡Debes atacar a un Político con Fueros!");
                            return; // No proceder con el ataque
                        }
                        else // Objetivo válido (no hay Fueros, o este objetivo tiene Fueros)
                        {
                            Debug.Log($"'{atacante.cardData?.NombreCarta}' ATACA A CARTA Enemiga: '{objetivoPotencial.cardData?.NombreCarta}'.");
                            atacante.Atacar(objetivoPotencial);
                            GameManager.Instance.ClearAttackerSelection(); // Limpiar selección después del ataque
                        }
                    }
                }
            }
        }
    }
}