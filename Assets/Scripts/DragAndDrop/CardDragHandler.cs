// CardDragHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Vector3 offset; // Considera si realmente necesitas este offset o si eventData.position es suficiente.
    private Vector3 originalScale;
    private int originalSiblingIndex;

    public bool dropSuccessful = false; // Bandera para saber si el drop fue exitoso

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dropSuccessful = false; // Reiniciar al comenzar a arrastrar

        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Mover a un canvas temporal manteniendo escala
        transform.SetParent(GetComponentInParent<Canvas>().transform, true);
        transform.localScale = originalScale * 1.1f; // Ligero aumento visual

        canvasGroup.blocksRaycasts = false; // Para que el raycast pase a través de la carta y detecte la DropZone
        canvasGroup.alpha = 0.8f;

        // Calcular offset si es necesario (ejemplo: para que el cursor no esté justo en la esquina)
        // offset = transform.position - (Vector3)eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Mover la carta siguiendo el cursor
        transform.position = (Vector3)eventData.position; // + offset; (si usas offset)
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (dropSuccessful)
        {
            // La carta fue jugada exitosamente.
            // DeckManager.TryPlayCardFromHandToBoard destruyó este GameObject (la carta en mano)
            // y creó una nueva instancia en el tablero.
            // No hay nada más que hacer con este GameObject.
        }
        else
        {
            // El drop no fue exitoso o la carta se soltó en un lugar no válido.
            // Devolver la carta a su mano original.
            if (this.gameObject != null && originalParent != null)
            {
                // Determinar a qué DeckManager pertenece esta mano
                DeckManager ownerDeckManager = null;
                if (GameManager.Instance != null) {
                    // Intenta obtener el DeckManager del jugador actual si el originalParent es su handContainer
                    DeckManager currentDm = GameManager.Instance.GetCurrentPlayerDeckManager();
                    if (currentDm != null && currentDm.handContainer == originalParent) {
                        ownerDeckManager = currentDm;
                    } else {
                        // Intenta con el oponente (menos probable para drag del jugador, pero por si acaso)
                        DeckManager opponentDm = GameManager.Instance.GetOpponentPlayerDeckManager();
                         if (opponentDm != null && opponentDm.handContainer == originalParent) {
                            ownerDeckManager = opponentDm;
                        }
                    }
                }
                
                // Si no se pudo determinar por el jugador actual, intentar por el componente padre
                if (ownerDeckManager == null) {
                     ownerDeckManager = originalParent.GetComponentInParent<DeckManager>();
                }


                if (ownerDeckManager != null)
                {
                    ownerDeckManager.ReturnCardToHand(this.gameObject); // DeckManager se encarga de reparentar y organizar
                }
                else
                {
                    // Fallback si no se encuentra el DeckManager (esto no debería ocurrir en una configuración normal)
                    Debug.LogError($"CardDragHandler ({this.gameObject.name}): No se pudo encontrar DeckManager para devolver la carta a la mano. Reparentando manualmente.");
                    transform.SetParent(originalParent, true);
                    transform.SetSiblingIndex(originalSiblingIndex);
                    transform.localScale = originalScale;
                    // Considera llamar a ArrangeHandVisuals directamente si es posible o necesario como fallback.
                }
            }
            else if (this.gameObject == null)
            {
                Debug.LogWarning("CardDragHandler: OnEndDrag llamado pero gameObject es null y dropSuccessful era false. Esto indica un problema previo.");
            }
        }
    }
}