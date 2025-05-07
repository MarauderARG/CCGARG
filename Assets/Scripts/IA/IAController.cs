using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAController : MonoBehaviour
{
    [Header("Referencias de la IA")]
    public DeckManager deckManagerIA;
    public PlayerStats playerStatsIA;
    public BoardPlayZone boardPlayZoneIA;

    [Header("Referencias del jugador humano")]
    public BoardPlayZone boardPlayZoneHumano;
    public PlayerStats playerStatsHumano;

    public IEnumerator TomarTurno()
    {
        // 1. Jugar carta si puede
        ScriptableCard cartaAJugar = deckManagerIA.EncontrarCartaJugable(playerStatsIA.poderActual, boardPlayZoneIA);
        if (cartaAJugar != null)
        {
            deckManagerIA.JugarCartaDesdeMano(cartaAJugar, boardPlayZoneIA, playerStatsIA);
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Atacar con todas las cartas en el tablero (usando copia de la lista para evitar errores)
        var cartasEnTablero = boardPlayZoneIA.CartasEnZona();
        var copiaCartas = new List<GameObject>(cartasEnTablero); // Copia para evitar InvalidOperationException
        foreach (var carta in copiaCartas)
        {
            var boardCard = carta.GetComponent<BoardCardController>();
            if (boardCard != null && boardCard.PuedeAtacar())
            {
                var cartasEnemigas = boardPlayZoneHumano.CartasEnZona();
                if (cartasEnemigas.Count > 0)
                {
                    // Ataca a una carta enemiga al azar
                    var objetivo = cartasEnemigas[Random.Range(0, cartasEnemigas.Count)];
                    var boardObjetivo = objetivo.GetComponent<BoardCardController>();
                    if (boardObjetivo != null)
                    {
                        boardCard.Atacar(boardObjetivo, playerStatsHumano);
                    }
                }
                else
                {
                    // Si no hay enemigos, ataca al jugador
                    boardCard.AtacarJugador(playerStatsHumano);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 3. Finaliza turno
        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.FinalizarTurno();
    }
}