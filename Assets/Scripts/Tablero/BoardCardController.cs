using UnityEngine;

public class BoardCardController : MonoBehaviour
{
    public ScriptableCard cardData;
    public int vidaActual = 1; // Ajustá según corresponda
    public bool yaAtacoEsteTurno = false; // Controla si la carta ya atacó

    // Permite que la IA determine si la carta puede atacar
    public bool PuedeAtacar()
    {
        // Lógica básica: puede atacar si no atacó y está viva
        return !yaAtacoEsteTurno && vidaActual > 0;
    }

    // Llamar para animar shake al atacar
    public void AnimarShakeAtaque()
    {
        Vector3 originalPos = transform.localPosition;
        float shakeAmount = 15f;
        float shakeTime = 0.1f;

        LeanTween.moveLocalX(gameObject, originalPos.x + shakeAmount, shakeTime)
            .setLoopPingPong(2)
            .setOnComplete(() => transform.localPosition = originalPos);
    }

    // Ejemplo de método de ataque a otra carta
    public void Atacar(BoardCardController objetivo, PlayerStats oponente)
    {
        // Lógica de ataque aquí...
        AnimarShakeAtaque();
        yaAtacoEsteTurno = true;
        // Tu lógica de daño y efectos
    }

    // Ejemplo de ataque directo a jugador
    public void AtacarJugador(PlayerStats oponente)
    {
        // Lógica de ataque a jugador...
        AnimarShakeAtaque();
        yaAtacoEsteTurno = true;
        // Tu lógica de daño al jugador
    }
}