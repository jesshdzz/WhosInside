using System.Collections;
using UnityEngine;
using WhoIsOutside.Core;
using WhoIsOutside.Verification;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// PROVISIONAL (Tarea 8b): une los botones de decision con el GameManager y el
  /// TurnController para poder jugar una noche completa. Las reglas de resultado
  /// de aqui se reemplazan en la Tarea 10 y las pantallas finales en la Tarea 11.
  /// </summary>
  public class DecisionFlow : MonoBehaviour
  {
    private enum Defeat
    {
      None,
      Screamer,   // Dejo pasar a un impostor
      Fired       // Reporto a un residente real
    }

    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnController turnController;
    [SerializeField] private DecisionInput decisionInput;

    [Header("Ajustes")]
    [Tooltip("Empieza la noche solo al iniciar la escena.")]
    [SerializeField] private bool autoStartNight = true;
    [Tooltip("Segundos entre que se resuelve un turno y llega el siguiente (o termina la noche).")]
    [SerializeField, Min(0f)] private float resultDelay = 1.5f;
    [SerializeField] private bool logFlow = true;

    private Defeat pendingDefeat = Defeat.None;
    private Defeat nightDefeat = Defeat.None;
    private Coroutine resultRoutine;

    private void OnEnable()
    {
      if (gameManager == null || turnController == null || decisionInput == null)
      {
        Debug.LogError(
            "[DecisionFlow] Asigna GameManager, TurnController y DecisionInput en el Inspector.", this);
        return;
      }

      decisionInput.DecisionMade += HandleDecision;
      gameManager.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
      if (decisionInput != null) decisionInput.DecisionMade -= HandleDecision;
      if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
    }

    private void Start()
    {
      if (decisionInput == null || turnController == null) return;

      decisionInput.DisableAll(); // Sin residente, los botones no responden

      if (autoStartNight) turnController.BeginNight();
    }

    // ---------------- Decisiones del jugador ----------------

    private void HandleDecision(Decision decision)
    {
      Arrival arrival = turnController.CurrentArrival;
      if (arrival == null) return; // Nadie en la ventanilla

      switch (decision)
      {
        case Decision.Deny:
          // Ventanilla cerrada: el resultado se decide con el boton de Emergencia.
          Log("Rechazado: ventanilla cerrada, falta el reporte.");
          gameManager.RejectResident();
          return;

        case Decision.Allow:
          pendingDefeat = arrival.IsImpostor ? Defeat.Screamer : Defeat.None;
          break;

        case Decision.Emergency:
          pendingDefeat = arrival.IsImpostor ? Defeat.None : Defeat.Fired;
          break;
      }

      gameManager.SubmitDecision();
    }

    // ---------------- Cambios de estado ----------------

    private void HandleStateChanged(GameState previous, GameState current)
    {
      switch (current)
      {
        case GameState.Deciding:
          decisionInput.ShowDecidingMode();
          break;

        case GameState.Rejected:
          // DecisionInput ya paso por si solo a su modo "rechazado".
          break;

        case GameState.ShowingResult:
          decisionInput.DisableAll();
          Log(pendingDefeat == Defeat.None
              ? "Resultado: correcto."
              : $"Resultado: DERROTA ({pendingDefeat}).");
          resultRoutine = StartCoroutine(FinishTurn());
          break;

        case GameState.NightOver:
          decisionInput.DisableAll();
          Log(nightDefeat == Defeat.None
              ? "Noche terminada: VICTORIA."
              : $"Noche terminada: DERROTA ({nightDefeat}).");
          break;

        case GameState.NotStarted:
          pendingDefeat = Defeat.None;
          nightDefeat = Defeat.None;
          decisionInput.DisableAll();
          break;

        default: // ResidentArriving
          decisionInput.DisableAll();
          break;
      }
    }

    // Espera un momento y despues pasa al siguiente turno o termina la noche.
    // Siempre espera al menos un fotograma, para no cambiar de estado dentro del evento.
    private IEnumerator FinishTurn()
    {
      if (resultDelay > 0f) yield return new WaitForSeconds(resultDelay);
      else yield return null;

      resultRoutine = null;

      if (pendingDefeat != Defeat.None)
      {
        nightDefeat = pendingDefeat;
        gameManager.EndNight();
      }
      else
      {
        turnController.ContinueAfterResult();
      }
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[DecisionFlow] {message}", this);
    }
  }
}