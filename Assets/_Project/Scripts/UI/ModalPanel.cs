using UnityEngine;
using UnityEngine.EventSystems;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Ventana modal: se abre, se cierra, se arrastra y viene al frente al tocarla.
  /// Va en cada modal (CredentialModal, RequestModal, etc.).
  /// </summary>
  public class ModalPanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
  {
    private RectTransform modalRect;
    private RectTransform parentRect;
    private Vector2 dragOffset;

    // ---- Metodos que llaman los botones (desde On Click) ----

    /// <summary>Abre el modal y lo trae al frente.</summary>
    public void Open()
    {
      gameObject.SetActive(true);
      transform.SetAsLastSibling();
    }

    /// <summary>Cierra el modal.</summary>
    public void Close()
    {
      gameObject.SetActive(false);
    }

    // ---- Arrastre y traer al frente ----

    private void Awake()
    {
      // Awake corre la primera vez que el modal se activa.
      modalRect = (RectTransform)transform;
      parentRect = modalRect.parent as RectTransform;
    }

    // Al tocar el modal (antes de arrastrar) pasa al frente.
    public void OnPointerDown(PointerEventData eventData)
    {
      transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
      // Guarda la distancia entre donde tocaste y el centro del modal,
      // para que no "salte" al empezar a arrastrar.
      if (ScreenToParent(eventData, out Vector2 localPoint))
        dragOffset = modalRect.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
      if (ScreenToParent(eventData, out Vector2 localPoint))
        modalRect.anchoredPosition = localPoint + dragOffset;
    }

    // Convierte la posicion del raton (en pixeles de pantalla) a la posicion
    // dentro del Canvas, teniendo en cuenta el escalado del Canvas Scaler.
    private bool ScreenToParent(PointerEventData eventData, out Vector2 localPoint)
    {
      return RectTransformUtility.ScreenPointToLocalPointInRectangle(
          parentRect, eventData.position, eventData.pressEventCamera, out localPoint);
    }
  }
}