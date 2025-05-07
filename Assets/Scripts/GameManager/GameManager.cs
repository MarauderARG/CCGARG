using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject selectedCardObject = null;

    [Header("Jugadores")]
    public PlayerStats jugador1; // Humano
    public PlayerStats jugador2; // IA

    [Header("Decks")]
    public DeckManager deckManagerJugador1;
    public DeckManager deckManagerJugador2;

    [Header("Tableros")]
    public BoardPlayZone boardPlayZoneJugador1;
    public BoardPlayZone boardPlayZoneJugador2;

    [Header("IA")]
    public IAController iaController; // Asigna el objeto IAController aquí

    private bool turnoJugador1 = true;
    private bool gameSetupComplete = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (jugador1 == null) Debug.LogError("GameManager: Falta asignar Jugador 1!");
        if (jugador2 == null) Debug.LogError("GameManager: Falta asignar Jugador 2!");
        if (deckManagerJugador1 == null) Debug.LogError("GameManager: Falta asignar DeckManager Jugador 1!");
        if (deckManagerJugador2 == null) Debug.LogError("GameManager: Falta asignar DeckManager Jugador 2!");
        if (boardPlayZoneJugador1 == null) Debug.LogError("GameManager: Falta asignar BoardPlayZone Jugador 1!");
        if (boardPlayZoneJugador2 == null) Debug.LogError("GameManager: Falta asignar BoardPlayZone Jugador 2!");
        if (iaController == null) Debug.LogWarning("GameManager: Falta asignar IAController (solo necesario si usas IA).");
    }

    private void Start()
    {
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        Debug.Log("Iniciando secuencia del juego...");
        gameSetupComplete = false;

        if (deckManagerJugador1 != null)
            yield return StartCoroutine(deckManagerJugador1.SetupPlayerDeckAndDealInitialHand());
        if (deckManagerJugador2 != null)
            yield return StartCoroutine(deckManagerJugador2.SetupPlayerDeckAndDealInitialHand());

        Debug.Log("Setup inicial completado. Iniciando primer turno.");
        gameSetupComplete = true;
        IniciarTurno();
    }

    public void IniciarTurno()
    {
        if (!gameSetupComplete) return;

        if (turnoJugador1)
        {
            Debug.Log("Iniciando turno del Jugador 1 (HUMANO)");
            jugador1.IniciarTurno();
            // Aquí puedes habilitar la UI del jugador, mostrar mensaje, etc.
        }
        else
        {
            Debug.Log("Iniciando turno del Jugador 2 (IA)");
            jugador2.IniciarTurno();
            if (iaController != null)
            {
                StartCoroutine(iaController.TomarTurno());
            }
            else
            {
                Debug.LogWarning("No hay IAController asignado. Turno de Jugador 2 será manual.");
            }
        }
    }

    // Llama esto desde el botón "Fin de Turno" del jugador, o al terminar la IA su turno
    public void FinalizarTurno()
    {
        if (!gameSetupComplete) return;

        Debug.Log($"Finalizando turno de {(turnoJugador1 ? "Jugador 1" : "Jugador 2")}");
        // Coloca aquí lógica adicional de fin de turno si la hay

        turnoJugador1 = !turnoJugador1;
        IniciarTurno();
    }
}