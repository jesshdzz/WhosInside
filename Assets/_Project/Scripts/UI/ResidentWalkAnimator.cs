using System.Collections;
using UnityEngine;
using WhoIsOutside.Core;
using WhoIsOutside.Verification;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Tarea 11e (parte 1): el visitante entra por la izquierda dando saltitos que simulan
  /// caminar, y sale por la derecha con la misma animacion cuando se le deja pasar.
  /// Solo mueve la imagen del residente; no cambia ningun otro script.
  /// </summary>
  public class ResidentWalkAnimator : MonoBehaviour
  {
    [Header("Referencias de juego")]
    [SerializeField] private TurnController turnController;
    [SerializeField] private DecisionInput decisionInput;

    [Header("Imagen del residente")]
    [Tooltip("La imagen que muestra al visitante (hija de ResidentWindow).")]
    [SerializeField] private RectTransform residentImage;
    [Tooltip("Opcional: la ventana que recorta. Si se deja vacio, se usa el padre de la imagen.")]
    [SerializeField] private RectTransform window;

    [Header("Entrada (desde la izquierda)")]
    [SerializeField, Min(0.1f)] private float enterDuration = 1.2f;
    [Tooltip("Cuantos saltitos da al entrar.")]
    [SerializeField, Min(1)] private int enterHops = 5;

    [Header("Salida (hacia la derecha, al dejarlo pasar)")]
    [SerializeField, Min(0.1f)] private float exitDuration = 1f;
    [SerializeField, Min(1)] private int exitHops = 4;

    [Header("Saltitos")]
    [Tooltip("Altura de cada saltito, en unidades de Canvas.")]
    [SerializeField, Min(0f)] private float hopHeight = 18f;
    [Tooltip("Cuanto se inclina a cada lado, en grados.")]
    [SerializeField, Min(0f)] private float tiltDegrees = 2.5f;

    [SerializeField] private bool logFlow = true;

    private Vector2 restPosition;   // Posicion con la que dejaste la imagen en el editor
    private Vector3 restEuler;      // Rotacion original
    private float offsetX;          // Cuanto esta desplazada ahora en horizontal
    private bool isSetUp;
    private Coroutine routine;

    private void Awake()
    {
      if (residentImage == null)
      {
        Debug.LogError("[ResidentWalk] Asigna la imagen del residente (ResidentImage).", this);
        return;
      }

      restPosition = residentImage.anchoredPosition;
      restEuler = residentImage.localEulerAngles;
      isSetUp = true;
    }

    private void OnEnable()
    {
      if (turnController == null || decisionInput == null)
      {
        Debug.LogError("[ResidentWalk] Asigna TurnController y DecisionInput.", this);
        return;
      }

      turnController.ArrivalStarted += HandleArrivalStarted;
      turnController.ArrivalEnded += HandleArrivalEnded;
      decisionInput.DecisionMade += HandleDecision;
    }

    private void OnDisable()
    {
      if (turnController != null)
      {
        turnController.ArrivalStarted -= HandleArrivalStarted;
        turnController.ArrivalEnded -= HandleArrivalEnded;
      }
      if (decisionInput != null) decisionInput.DecisionMade -= HandleDecision;
    }

    // ---------------- Eventos ----------------

    // Llega alguien: aparece fuera de la ventana por la izquierda y camina hasta su sitio.
    private void HandleArrivalStarted(Arrival arrival)
    {
      if (!isSetUp) return;

      Log("Entra caminando desde la izquierda.");
      StartWalk(-HiddenDistance(), 0f, enterDuration, enterHops, true);
    }

    // Se le deja pasar: sale caminando hacia la derecha.
    private void HandleDecision(Decision decision)
    {
      if (!isSetUp || decision != Decision.Allow) return;

      Log("Sale caminando hacia la derecha.");
      StartWalk(offsetX, HiddenDistance(), exitDuration, exitHops, false);
    }

    // El visitante se fue: la imagen vuelve a su posicion original, lista para el siguiente.
    private void HandleArrivalEnded()
    {
      if (!isSetUp) return;

      StopWalk();
      offsetX = 0f;
      ApplyPose(0f, 0f);
    }

    // ---------------- Animacion ----------------

    private void StartWalk(float fromX, float toX, float duration, int hops, bool easeOut)
    {
      StopWalk();

      // La posicion inicial se aplica YA, en el mismo cuadro, para que no haya parpadeo.
      offsetX = fromX;
      ApplyPose(fromX, 0f);

      routine = StartCoroutine(WalkRoutine(fromX, toX, duration, hops, easeOut));
    }

    private void StopWalk()
    {
      if (routine == null) return;
      StopCoroutine(routine);
      routine = null;
    }

    private IEnumerator WalkRoutine(float fromX, float toX, float duration, int hops, bool easeOut)
    {
      float t = 0f;

      while (t < 1f)
      {
        t += Time.deltaTime / duration;
        float p = Mathf.Clamp01(t);

        // Entrar: frena al llegar. Salir: acelera al irse.
        float eased = easeOut ? 1f - (1f - p) * (1f - p) : p * p;
        offsetX = Mathf.Lerp(fromX, toX, eased);

        // El ritmo de los pasos sigue el avance lineal, para que sea parejo.
        ApplyPose(offsetX, p * hops);
        yield return null;
      }

      offsetX = toX;
      ApplyPose(offsetX, 0f); // Termina apoyado y derecho
      routine = null;
    }

    // phase = numero de saltitos completados. Cada salto es un arco de |sin|, que toca
    // el suelo en cero; el balanceo usa sin, asi se inclina a un lado y luego al otro.
    private void ApplyPose(float x, float phase)
    {
      float s = Mathf.Sin(Mathf.PI * phase);
      float hop = hopHeight * Mathf.Abs(s);
      float tilt = tiltDegrees * s;

      residentImage.anchoredPosition = restPosition + new Vector2(x, hop);
      residentImage.localEulerAngles = new Vector3(restEuler.x, restEuler.y, restEuler.z + tilt);
    }

    // Distancia a la que el visitante queda completamente fuera de la ventana.
    private float HiddenDistance()
    {
      RectTransform area = window != null ? window : residentImage.parent as RectTransform;
      float windowWidth = area != null ? area.rect.width : 800f;

      return (windowWidth + residentImage.rect.width) * 0.5f
             + Mathf.Abs(restPosition.x) + 80f;
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[ResidentWalk] {message}", this);
    }
  }
}