// ActionEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ActionEffectManager : MonoBehaviour
{
    private static ActionEffectManager _instance;
    public static ActionEffectManager Instance {
        get {
            if (_instance == null) {
                _instance = Object.FindFirstObjectByType<ActionEffectManager>();
                if (_instance == null) {
                    GameObject singletonObject = new GameObject("ActionEffectManager_Singleton");
                    _instance = singletonObject.AddComponent<ActionEffectManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake() {
        if (_instance != null && _instance != this) { Destroy(gameObject); }
        else if (_instance == null) { _instance = this; }
    }

    public void ExecuteEffect(ScriptableCard cardWithEffect, PlayerStats playerCaster, PlayerStats playerTarget, BoardPlayZone casterBoardZone, BoardPlayZone targetBoardZone)
    {
        if (cardWithEffect == null || playerCaster == null || playerTarget == null) { // casterBoardZone y targetBoardZone pueden ser null para algunos efectos
            Debug.LogError("ActionEffectManager.ExecuteEffect: Parámetros cardWithEffect, playerCaster o playerTarget son null.");
            return;
        }

        string tipoCartaNormalizado = cardWithEffect.TipoCarta?.Trim().ToLowerInvariant() ?? "";
        bool isEffectCard = (tipoCartaNormalizado == "accion" || tipoCartaNormalizado == "acción" || tipoCartaNormalizado == "evento");

        if (!isEffectCard) {
            return;
        }

        Debug.Log($"ActionEffectManager: Procesando efecto '{cardWithEffect.effectType}' para '{cardWithEffect.NombreCarta}' (Jugador: {playerCaster.name}). " +
                  $"GenAmount: {cardWithEffect.effectAmount}, DrawP: {cardWithEffect.ParamCartasARobar}, CostRedP: {cardWithEffect.ParamReduccionDeCosto}, DuraP: {cardWithEffect.ParamDuracionTurnos}, DmgP: {cardWithEffect.ParamDaño}, HealP: {cardWithEffect.ParamCuracion}");

        if (UIManager.Instance != null && !string.IsNullOrEmpty(cardWithEffect.NombreCarta)) {
             UIManager.Instance.ShowStatusMessage($"'{playerCaster.name}' usó '{cardWithEffect.NombreCarta}'");
        }

        switch (cardWithEffect.effectType)
        {
            case ActionEffectType.DamageBothPlayers:
                int damageToBoth = cardWithEffect.ParamDaño > 0 ? cardWithEffect.ParamDaño : cardWithEffect.effectAmount;
                if (damageToBoth > 0) {
                    Debug.Log($"Efecto: Daño a ambos jugadores por {damageToBoth}.");
                    playerCaster.RecibirDaño(damageToBoth);
                    playerTarget.RecibirDaño(damageToBoth);
                } else { Debug.LogWarning($"DamageBothPlayers: cantidad de daño es 0 para '{cardWithEffect.NombreCarta}'.");}
                break;

            case ActionEffectType.DamageTargetPlayer:
                int damageToTarget = cardWithEffect.ParamDaño > 0 ? cardWithEffect.ParamDaño : cardWithEffect.effectAmount;
                 if (damageToTarget > 0) {
                    Debug.Log($"Efecto: Daño al jugador oponente por {damageToTarget}.");
                    playerTarget.RecibirDaño(damageToTarget);
                } else { Debug.LogWarning($"DamageTargetPlayer: cantidad de daño es 0 para '{cardWithEffect.NombreCarta}'.");}
                break;

            case ActionEffectType.HealCasterPlayer:
                int healToCaster = cardWithEffect.ParamCuracion > 0 ? cardWithEffect.ParamCuracion : cardWithEffect.effectAmount;
                if (healToCaster > 0) {
                    Debug.Log($"Efecto: Curar al lanzador por {healToCaster}.");
                    // playerCaster.Curar(healToCaster); // TODO: Implementa PlayerStats.Curar()
                    Debug.LogWarning("ActionEffectManager: PlayerStats.Curar() no implementado.");
                } else { Debug.LogWarning($"HealCasterPlayer: cantidad de curación es 0 para '{cardWithEffect.NombreCarta}'.");}
                break;

            case ActionEffectType.HealTargetPlayer:
                int healToTarget = cardWithEffect.ParamCuracion > 0 ? cardWithEffect.ParamCuracion : cardWithEffect.effectAmount;
                if (healToTarget > 0) {
                    Debug.Log($"Efecto: Curar al oponente por {healToTarget}.");
                    // playerTarget.Curar(healToTarget); // TODO: Implementa PlayerStats.Curar()
                    Debug.LogWarning("ActionEffectManager: PlayerStats.Curar() no implementado.");
                } else { Debug.LogWarning($"HealTargetPlayer: cantidad de curación es 0 para '{cardWithEffect.NombreCarta}'.");}
                break;

            case ActionEffectType.DrawCards:
                int cardsToDraw = cardWithEffect.ParamCartasARobar > 0 ? cardWithEffect.ParamCartasARobar : cardWithEffect.effectAmount;
                if (cardsToDraw > 0) {
                    Debug.Log($"[ActionEffectManager] Efecto: {playerCaster.name} roba {cardsToDraw} cartas.");
                    if (playerCaster.myDeckManager != null) {
                        for (int i = 0; i < cardsToDraw; i++) {
                            playerCaster.myDeckManager.StartCoroutine(playerCaster.myDeckManager.RobarCartaVisual());
                        }
                    } else {
                        Debug.LogError($"[ActionEffectManager] myDeckManager es null para '{playerCaster.name}'. No se pueden robar cartas.");
                    }
                } else { Debug.LogWarning($"DrawCards: cantidad de robo es 0 para '{cardWithEffect.NombreCarta}'."); }
                break;

            case ActionEffectType.DrawAndReduceCost:
                if (cardWithEffect.ParamCartasARobar > 0) {
                    Debug.Log($"[ActionEffectManager] Efecto: {playerCaster.name} roba {cardWithEffect.ParamCartasARobar} y reduce costo en {cardWithEffect.ParamReduccionDeCosto} por {cardWithEffect.ParamDuracionTurnos}t.");
                    if (playerCaster.myDeckManager != null && playerCaster != null) {
                        StartCoroutine(RobarYColectarParaReduccion(playerCaster, cardWithEffect.ParamCartasARobar, cardWithEffect.ParamReduccionDeCosto, cardWithEffect.ParamDuracionTurnos));
                    } else {
                        Debug.LogError($"[ActionEffectManager] myDeckManager o PlayerStats es null para '{playerCaster.name}'. No se puede ejecutar DrawAndReduceCost.");
                    }
                } else { Debug.LogWarning($"DrawAndReduceCost: ParamCartasARobar es 0 para '{cardWithEffect.NombreCarta}'.");}
                break;

            case ActionEffectType.None:
                 Debug.Log($"Efecto: Carta '{cardWithEffect.NombreCarta}' tiene effectType.None. No se ejecuta acción.");
                 break;

            default:
                Debug.Log($"Efecto: Carta '{cardWithEffect.NombreCarta}' tiene effectType '{cardWithEffect.effectType}'. Tipo no manejado.");
                break;
        }
    }

    private IEnumerator RobarYColectarParaReduccion(PlayerStats caster, int cantidadARobarProgramada, int reduccionCosto, int duracionTurnos)
    {
        // ... (La corutina RobarYColectarParaReduccion que te pasé en el mensaje anterior, ID output_message_ID: assistant_message_ID: content/gemini-pro-dev-api/01683122-463d-4e35-9c56-03168e46f6cc , se mantiene igual)
        if (caster.myDeckManager == null) { Debug.LogError($"[AEM RobarYColectar] DeckManager de {caster.name} es null."); yield break; }
        if (cantidadARobarProgramada <= 0) { Debug.LogWarning($"[AEM RobarYColectar] cantidadARobarProgramada es {cantidadARobarProgramada}. No se robarán cartas."); yield break; }

        HashSet<string> idsEnManoAntesDelEfecto = new HashSet<string>(caster.myDeckManager.playerHand.Select(c => c.IdUnico));
        int contadorRobadasExitosamente = 0;

        for (int i = 0; i < cantidadARobarProgramada; i++) {
            if (caster.myDeckManager.mazo.Count == 0) { Debug.LogWarning($"[AEM RobarYColectar] Mazo vacío en iteración {i + 1}."); break; }
            yield return caster.myDeckManager.StartCoroutine(caster.myDeckManager.RobarCartaVisual());
            contadorRobadasExitosamente++;
        }
        yield return new WaitForSeconds(0.1f); 

        List<ScriptableCard> cartasRecienAñadidasALaMano = new List<ScriptableCard>();
        if (contadorRobadasExitosamente > 0) {
            foreach(ScriptableCard cartaEnManoAhora in caster.myDeckManager.playerHand) {
                if (cartaEnManoAhora != null && !idsEnManoAntesDelEfecto.Contains(cartaEnManoAhora.IdUnico)) {
                    if (!cartasRecienAñadidasALaMano.Any(c => c.IdUnico == cartaEnManoAhora.IdUnico)) { // Evitar duplicados si la lógica de ids no es perfecta
                        cartasRecienAñadidasALaMano.Add(cartaEnManoAhora);
                    }
                }
            }
             // Si la heurística de arriba falló en obtener la cantidad correcta, tomar las últimas N como fallback
            if (cartasRecienAñadidasALaMano.Count != contadorRobadasExitosamente && contadorRobadasExitosamente <= caster.myDeckManager.playerHand.Count) {
                Debug.LogWarning($"[AEM RobarYColectar] Discrepancia en cartas identificadas ({cartasRecienAñadidasALaMano.Count}) vs robadas ({contadorRobadasExitosamente}). Usando fallback de últimas cartas.");
                cartasRecienAñadidasALaMano.Clear();
                int indiceInicio = Mathf.Max(0, caster.myDeckManager.playerHand.Count - contadorRobadasExitosamente);
                for (int i = indiceInicio; i < caster.myDeckManager.playerHand.Count; i++) {
                    cartasRecienAñadidasALaMano.Add(caster.myDeckManager.playerHand[i]);
                }
            }
        }
        
        if (cartasRecienAñadidasALaMano.Count > 0) {
            Debug.Log($"[AEM RobarYColectar] Aplicando reducción a {cartasRecienAñadidasALaMano.Count} cartas: " + string.Join(", ", cartasRecienAñadidasALaMano.Select(c => c?.NombreCarta ?? "NULL")));
            caster.AplicarModificadorDeCostoTemporalALista(cartasRecienAñadidasALaMano, reduccionCosto, duracionTurnos);
        } else if (cantidadARobarProgramada > 0) {
            Debug.LogWarning($"[AEM RobarYColectar] Se intentaron robar {cantidadARobarProgramada} pero no se coleccionaron para reducción.");
        }
    }
}