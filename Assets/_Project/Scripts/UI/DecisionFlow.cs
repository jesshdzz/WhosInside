using System;
using System.Collections;
using UnityEngine;
using WhoIsOutside.Core;
using WhoIsOutside.Verification;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Une los botones de decision con el GameManager y el TurnController.
  /// Tarea 10: las reglas de resultado viven en ResolutionRules; este script solo
  /// las consulta y avisa con eventos (TurnResolved, NightEnded) para que la
  /// Tarea 11 muestre las pantallas finales.
  /// </summary>
  public class DecisionFlow : MonoBehaviour
  {
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

    /// <summary>Se dispara cuando una decision final resuelve el turno actual.</summary>
    public event Action<TurnResult> TurnResolved;

    /// <summary>Se dispara una vez cuando la noche termina (victoria o derrota).</summary>
    public event Action<NightEnding> NightEnded;

    private TurnResult pendingResult = TurnResult.Correct; // Resultado del turno en curso
    private TurnResult nightResult = TurnResult.Correct;   // Derrota que termino la noche (si hubo)
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

      // Deny solo cierra la ventanilla: el resultado se decide con Emergencia.
      if (decision == Decision.Deny)
      {
        Log("Rechazado: ventanilla cerrada, falta el reporte.");
        gameManager.RejectResident();
        return;
      }

      // Allow y Emergency resuelven el turno: las reglas viven en ResolutionRules.
      if (!ResolutionRules.TryResolve(decision, arrival.IsImpostor, out TurnResult result))
      {
        return; // Decision que no resuelve nada
      }

      pendingResult = result;
      TurnResolved?.Invoke(result);
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
          Log(pendingResult == TurnResult.Correct
              ? "Resultado: correcto."
              : $"Resultado: DERROTA ({pendingResult}).");
          resultRoutine = StartCoroutine(FinishTurn());
          break;

        case GameState.NightOver:
        {
          decisionInput.DisableAll();
          NightEnding ending = ResolutionRules.ToEnding(nightResult);
          Log(ending == NightEnding.Victory
              ? "Noche terminada: VICTORIA."
              : $"Noche terminada: DERROTA ({ending}).");
          NightEnded?.Invoke(ending);
          break;
        }

        case GameState.NotStarted:
          pendingResult = TurnResult.Correct;
          nightResult = TurnResult.Correct;
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

      if (ResolutionRules.IsDefeat(pendingResult))
      {
        nightResult = pendingResult;
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

    // Prueba rapida de la tabla de reglas, sin necesidad de dar Play.
    // Se ejecuta desde el menu de tres puntos del componente en el Inspector.
    [ContextMenu("Probar reglas de resolucion")]
    private void TestRules()
    {
      int fails = 0;
      fails += CheckRule(Decision.Allow,     false, TurnResult.Correct);
      fails += CheckRule(Decision.Allow,     true,  TurnResult.DefeatScreamer);
      fails += CheckRule(Decision.Emergency, true,  TurnResult.Correct);
      fails += CheckRule(Decision.Emergency, false, TurnResult.DefeatFired);

      if (ResolutionRules.TryResolve(Decision.Deny, true, out _))
      {
        Debug.LogError("[DecisionFlow] FALLA: Deny no deberia resolver el turno.", this);
        fails++;
      }
      else
      {
        Debug.Log("[DecisionFlow] PASA: Deny no resuelve el turno.", this);
      }

      Debug.Log($"[DecisionFlow] Prueba de reglas terminada. Fallos: {fails}", this);
    }

    private int CheckRule(Decision decision, bool isImpostor, TurnResult expected)
    {
      ResolutionRules.TryResolve(decision, isImpostor, out TurnResult actual);
      string who = isImpostor ? "impostor" : "real";

      if (actual == expected)
      {
        Debug.Log($"[DecisionFlow] PASA: {decision} / {who} -> {actual}", this);
        return 0;
      }

      Debug.LogError($"[DecisionFlow] FALLA: {decision} / {who} dio {actual} y se esperaba {expected}", this);
      return 1;
    }
  }
}