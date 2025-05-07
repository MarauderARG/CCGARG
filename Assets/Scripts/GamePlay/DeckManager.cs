using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Mazo asignado")]
    public CardCollection mazoAsignado; // ← Arrastrá aquí tu ScriptableObject de mazo (ej: Mazo Basico)

    [Header("Prefab de carta y referencias visuales")]
    public GameObject cardPrefab;
    public Transform handContainer;
    public int cartasEnManoInicial = 8;

    [Header("Zona de juego")]
    public BoardPlayZone boardPlayZone;

    [Header("Lógico")]
    public List<ScriptableCard> mazo = new List<ScriptableCard>();
    public List<ScriptableCard> playerHand = new List<ScriptableCard>();
    public List<GameObject> handCardObjects = new List<GameObject>();

    void Awake()
    {
        CrearMazos();
    }

    public void CrearMazos()
    {
        mazo.Clear();
        if (mazoAsignado != null && mazoAsignado.Cartas != null)
        {
            mazo.AddRange(mazoAsignado.Cartas);
            Debug.Log($"✅ Mazo lógico creado con {mazo.Count} cartas desde {mazoAsignado.name}.");
        }
        else
        {
            Debug.LogError("❌ No hay mazoAsignado o no tiene cartas.");
        }
    }

    public void BarajarMazo()
    {
        for (int i = 0; i < mazo.Count; i++)
        {
            ScriptableCard temp = mazo[i];
            int randomIndex = Random.Range(i, mazo.Count);
            mazo[i] = mazo[randomIndex];
            mazo[randomIndex] = temp;
        }
        Debug.Log($"Mazo barajado: {mazo.Count} cartas.");
    }

    public IEnumerator SetupPlayerDeckAndDealInitialHand()
    {
        if (mazo == null || mazo.Count == 0)
        {
            Debug.LogError("SetupPlayerDeck: mazoJugador vacío.");
        }
        BarajarMazo();
        yield return StartCoroutine(DealInitialHand(cartasEnManoInicial));
        Debug.Log("Setup del jugador completo.");
    }

    public IEnumerator DealInitialHand(int cantidad)
    {
        Debug.Log($"Repartiendo mano inicial de {cantidad} cartas...");
        for (int i = 0; i < cantidad; i++)
        {
            yield return StartCoroutine(RobarCartaVisual());
        }
        Debug.Log($"Mano inicial repartida con {handCardObjects.Count} cartas.");
        ArrangeHandVisuals(false);
    }

    public IEnumerator RobarCartaVisual()
    {
        if (mazo.Count == 0)
        {
            Debug.LogWarning("No quedan cartas para robar.");
            yield break;
        }

        ScriptableCard carta = mazo[0];
        mazo.RemoveAt(0);
        playerHand.Add(carta);

        GameObject nuevaCarta = Instantiate(cardPrefab, handContainer);
        nuevaCarta.name = carta.NombreCarta + " (Mano)";
        Card cardComponent = nuevaCarta.GetComponent<Card>();
        if (cardComponent != null)
        {
            cardComponent.SetCardData(carta);
        }
        handCardObjects.Add(nuevaCarta);

        yield return null;
    }

    public ScriptableCard EncontrarCartaJugable(int poderDisponible, BoardPlayZone zonaTablero)
    {
        foreach (var carta in playerHand)
        {
            if (carta.CostoPoderPolitico <= poderDisponible && zonaTablero.CartasEnZona().Count < zonaTablero.maxSlots)
                return carta;
        }
        return null;
    }

    public void JugarCartaDesdeMano(ScriptableCard carta, BoardPlayZone boardPlayZone, PlayerStats jugador)
    {
        GameObject nuevaCarta = Instantiate(cardPrefab, boardPlayZone.playedCardContainer);
        nuevaCarta.name = carta.NombreCarta + " (Jugada)";

        Card cardComponent = nuevaCarta.GetComponent<Card>();
        if (cardComponent != null)
            cardComponent.SetCardData(carta);

        if (nuevaCarta.GetComponent<BoardCardController>() == null)
            nuevaCarta.AddComponent<BoardCardController>().cardData = carta;

        boardPlayZone.AgregarCartaAlTablero(nuevaCarta);

        Debug.Log($"[DeckManager] {jugador.gameObject.name} jugó {carta.NombreCarta} en {boardPlayZone.name}");

        RemoveCardFromHand(carta);
    }

    public void RemoveCardFromHand(ScriptableCard carta)
    {
        int index = playerHand.IndexOf(carta);
        if (index >= 0)
        {
            playerHand.RemoveAt(index);

            if (handCardObjects.Count > index && handCardObjects[index] != null)
            {
                GameObject cartaGO = handCardObjects[index];
                handCardObjects.RemoveAt(index);
                Destroy(cartaGO);
            }
        }
        ArrangeHandVisuals(true);
    }

    public void RemoveCardFromHand(GameObject cardGO)
    {
        int index = handCardObjects.IndexOf(cardGO);
        if (index >= 0)
        {
            handCardObjects.RemoveAt(index);
            if (playerHand.Count > index)
                playerHand.RemoveAt(index);

            Destroy(cardGO);
        }
        ArrangeHandVisuals(true);
    }

    public void ArrangeHandVisuals(bool animate)
    {
        int cardCount = handCardObjects.Count;
        float spread = 120f;
        float totalWidth = (cardCount - 1) * spread;
        Vector3 targetScale = Vector3.one;

        for (int i = 0; i < cardCount; i++)
        {
            GameObject cartaGO = handCardObjects[i];
            if (cartaGO == null) continue;
            Vector3 pos = new Vector3(-totalWidth / 2f + i * spread, 0, 0);

            if (animate)
                LeanTween.moveLocal(cartaGO, pos, 0.2f).setEaseOutExpo();
            else
                cartaGO.transform.localPosition = pos;

            cartaGO.transform.localScale = targetScale;
        }
        Debug.Log($"ArrangeHandVisuals: Reorganizando {cardCount} cartas. Animar: {animate}");
    }
}