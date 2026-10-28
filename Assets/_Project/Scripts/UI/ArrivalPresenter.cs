using UnityEngine;
using UnityEngine.UI;
using WhoIsOutside.Core;
using WhoIsOutside.Data;
using WhoIsOutside.Verification;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Muestra en la pantalla de trabajo a quien llega: el visitante en la ventanilla,
  /// la credencial y la solicitud (en el escritorio y en sus modales).
  /// Escucha los eventos del TurnController; no decide nada.
  /// </summary>
  public class ArrivalPresenter : MonoBehaviour
  {
    // Un modal con su imagen y la caja maxima en la que debe caber el sprite.
    private struct ModalSlot
    {
      public ModalPanel modal;
      public Image image;
      public Vector2 maxSize;
    }

    [Header("Logica")]
    [SerializeField] private TurnController turnController;
    [Tooltip("Opcional: si se asigna, la imagen de la lista de esperados sale de aqui.")]
    [SerializeField] private NightConfig nightConfig;

    [Header("Ventanilla")]
    [SerializeField] private SpriteView residentWindow;

    [Header("Escritorio (solo se ven cuando hay alguien en la ventanilla)")]
    [SerializeField] private GameObject deskCredential;
    [SerializeField] private GameObject deskRequest;

    [Header("Modales")]
    [SerializeField] private ModalPanel credentialModal;
    [SerializeField] private ModalPanel requestModal;
    [SerializeField] private ModalPanel expectedListModal;

    private ModalSlot credentialSlot;
    private ModalSlot requestSlot;
    private ModalSlot expectedListSlot;

    private void Awake()
    {
      // Se guarda el tamano que se le dio a cada modal en la escena:
      // sera la caja maxima en la que cabe cualquier sprite, conservando su proporcion.
      credentialSlot = CreateSlot(credentialModal);
      requestSlot = CreateSlot(requestModal);
      expectedListSlot = CreateSlot(expectedListModal);
    }

    private void OnEnable()
    {
      if (turnController == null)
      {
        Debug.LogError("[ArrivalPresenter] Asigna el TurnController en el Inspector.", this);
        return;
      }

      turnController.ArrivalStarted += HandleArrivalStarted;
      turnController.ArrivalEnded += HandleArrivalEnded;
    }

    private void OnDisable()
    {
      if (turnController == null) return;

      turnController.ArrivalStarted -= HandleArrivalStarted;
      turnController.ArrivalEnded -= HandleArrivalEnded;
    }

    private void Start()
    {
      if (nightConfig != null && nightConfig.ExpectedListSprite != null)
        SetModalSprite(expectedListSlot, nightConfig.ExpectedListSprite);

      HideArrival(); // Al empezar no hay nadie en la ventanilla
    }

    // ---------------- Eventos del TurnController ----------------

    private void HandleArrivalStarted(Arrival arrival)
    {
      if (residentWindow != null) residentWindow.Show(arrival.VisitorSprite);

      if (deskCredential != null) deskCredential.SetActive(true);
      if (deskRequest != null) deskRequest.SetActive(true);

      SetModalSprite(credentialSlot, arrival.CredentialSprite);
      SetModalSprite(requestSlot, arrival.RequestSprite);
    }

    private void HandleArrivalEnded()
    {
      HideArrival();
    }

    // ---------------- Logica interna ----------------

    private void HideArrival()
    {
      if (residentWindow != null) residentWindow.Hide();

      if (deskCredential != null) deskCredential.SetActive(false);
      if (deskRequest != null) deskRequest.SetActive(false);

      // Los documentos del visitante que se fue se cierran.
      if (credentialModal != null) credentialModal.Close();
      if (requestModal != null) requestModal.Close();
    }

    private static ModalSlot CreateSlot(ModalPanel modal)
    {
      var slot = new ModalSlot { modal = modal };

      if (modal != null)
      {
        slot.image = modal.GetComponent<Image>();
        if (slot.image != null) slot.maxSize = slot.image.rectTransform.sizeDelta;
      }

      return slot;
    }

    // Pone el sprite y ajusta el tamano del modal a la proporcion del sprite,
    // sin salirse de la caja original.
    private static void SetModalSprite(ModalSlot slot, Sprite sprite)
    {
      if (slot.image == null) return;

      slot.image.sprite = sprite;
      if (sprite == null) return;

      Vector2 spriteSize = sprite.rect.size;
      if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

      float scale = Mathf.Min(slot.maxSize.x / spriteSize.x, slot.maxSize.y / spriteSize.y);
      slot.image.rectTransform.sizeDelta = spriteSize * scale;
    }
  }
}