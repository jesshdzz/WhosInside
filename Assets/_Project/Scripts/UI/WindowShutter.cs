using System.Collections;
using UnityEngine;
using WhoIsOutside.Core;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Tarea 11b (parte 1): al pulsar Emergencia, la persiana cae "de golpe", rebota,
  /// se queda cerrada unos segundos y sube sola. Hasta que no termina de subir, el
  /// TurnController no presenta al siguiente residente.
  /// La imagen cae desde arriba dentro de un contenedor con Rect Mask 2D.
  /// </summary>
  public class WindowShutter : MonoBehaviour
  {
    [Header("Referencias de juego")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnController turnController;
    [SerializeField] private DecisionInput decisionInput;

    [Header("Persiana")]
    [Tooltip("Contenedor con Rect Mask 2D: recorta la persiana al tamano de la ventana.")]
    [SerializeField] private RectTransform container;
    [Tooltip("Imagen de la persiana. En el editor se deja en su posicion CERRADA.")]
    [SerializeField] private RectTransform shutter;

    [Header("Tiempos")]
    [Tooltip("Segundos que tarda en caer (mas bajo = mas de golpe).")]
    [SerializeField, Min(0.05f)] private float closeDuration = 0.18f;
    [Tooltip("Segundos que se queda cerrada, contados desde que termina el rebote.")]
    [SerializeField, Min(0f)] private float holdSeconds = 3f;
    [Tooltip("Segundos que tarda en subir.")]
    [SerializeField, Min(0.1f)] private float openDuration = 0.6f;

    [Header("Rebote")]
    [Tooltip("Cuanto rebota al chocar, en unidades de Canvas. 0 = sin rebote.")]
    [SerializeField, Min(0f)] private float bounceHeight = 18f;
    [SerializeField, Min(0.05f)] private float bounceDuration = 0.25f;

    [SerializeField] private bool logFlow = true;

        /// <summary>Se dispara cuando la persiana empieza a caer.</summary>
    public event System.Action Closing;

    /// <summary>Se dispara cuando la persiana empieza a subir.</summary>
    public event System.Action Opening;

    private Vector2 closedPosition; // Donde la persiana tapa la ventana (la posicion del editor)
    private Vector2 openPosition;   // Arriba, fuera del recorte
    private bool isBusy;            // True durante todo el ciclo: cae, espera y sube
    private bool isSetUp;
    private Coroutine routine;

    private void Awake()
    {
      if (container == null || shutter == null)
      {
        Debug.LogError("[WindowShutter] Asigna el contenedor y la imagen de la persiana.", this);
        return;
      }

      // La posicion con la que dejaste la imagen en el editor es la posicion CERRADA.
      closedPosition = shutter.anchoredPosition;

      // Posicion abierta: subida lo suficiente para quedar fuera del recorte.
      float distance = container.rect.height + shutter.rect.height + 20f;
      openPosition = closedPosition + Vector2.up * distance;

      shutter.anchoredPosition = openPosition; // Empieza abierta
      isSetUp = true;
    }

    private void OnEnable()
    {
      if (gameManager == null || turnController == null || decisionInput == null)
      {
        Debug.LogError("[WindowShutter] Asigna GameManager, TurnController y DecisionInput.", this);
        return;
      }

      decisionInput.DecisionMade += HandleDecision;
      gameManager.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
      if (decisionInput != null) decisionInput.DecisionMade -= HandleDecision;
      if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;

      // Por seguridad, nunca dejamos la llegada retenida si este script se apaga.
      if (turnController != null) turnController.SetArrivalHold(false);
    }

    // ---------------- Eventos ----------------

    private void HandleDecision(Decision decision)
    {
      if (decision == Decision.Emergency) StartCycle();
    }

    private void HandleStateChanged(GameState previous, GameState current)
    {
      // La noche termino o se reinicio: la persiana se abre de inmediato, sin animacion,
      // y se libera cualquier espera. Asi nada se queda atascado.
      if (current == GameState.NightOver || current == GameState.NotStarted)
      {
        SnapOpen();
      }
    }

    // ---------------- Ciclo de la persiana ----------------

    private void StartCycle()
    {
      if (!isSetUp || isBusy) return;

      isBusy = true;
      turnController.SetArrivalHold(true); // El siguiente residente espera a que la persiana suba
      routine = StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
      // 1) Cae acelerando, como una persiana que se suelta.
      Log("Cerrando ventanilla.");
      Closing?.Invoke();
      Vector2 from = shutter.anchoredPosition;
      float t = 0f;

      while (t < 1f)
      {
        t += Time.deltaTime / closeDuration;
        float k = Mathf.Clamp01(t);
        float eased = k * k * k; // Empieza lento y termina muy rapido: "de golpe"
        shutter.anchoredPosition = Vector2.Lerp(from, closedPosition, eased);
        yield return null;
      }

      // 2) Rebote: sube un poco y vuelve a bajar, perdiendo fuerza.
      float b = 0f;
      while (bounceHeight > 0f && b < 1f)
      {
        b += Time.deltaTime / bounceDuration;
        float u = Mathf.Clamp01(b);
        float offset = bounceHeight * Mathf.Sin(u * Mathf.PI) * (1f - u);
        shutter.anchoredPosition = closedPosition + Vector2.up * offset;
        yield return null;
      }
      shutter.anchoredPosition = closedPosition;

      // 3) Se queda cerrada unos segundos.
      yield return new WaitForSeconds(holdSeconds);

      // 4) No sube hasta que el residente anterior se haya ido (empieza el turno nuevo).
      while (gameManager.CurrentState != GameState.ResidentArriving)
        yield return null;

      // 5) Sube suavemente.
      Log("Abriendo ventanilla.");
      Opening?.Invoke();
      from = shutter.anchoredPosition;
      t = 0f;

      while (t < 1f)
      {
        t += Time.deltaTime / openDuration;
        float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
        shutter.anchoredPosition = Vector2.Lerp(from, openPosition, eased);
        yield return null;
      }
      shutter.anchoredPosition = openPosition;

      // 6) Listo: ahora si puede llegar el siguiente residente.
      isBusy = false;
      routine = null;
      turnController.SetArrivalHold(false);
    }

    // Abre de golpe, sin animacion, y libera la espera de llegadas.
    private void SnapOpen()
    {
      if (!isSetUp) return;

      if (routine != null)
      {
        StopCoroutine(routine);
        routine = null;
      }

      shutter.anchoredPosition = openPosition;
      isBusy = false;
      turnController.SetArrivalHold(false);
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[WindowShutter] {message}", this);
    }
  }
}