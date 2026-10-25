using System;
using System.Collections;
using UnityEngine;
using WhoIsOutside.Verification;

namespace WhoIsOutside.Core
{
  /// <summary>
  /// Coordina el turno: genera la noche, hace llegar a cada residente
  /// y avisa a la UI con eventos. No dibuja nada.
  /// </summary>
  public class TurnController : MonoBehaviour
  {
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ArrivalGenerator arrivalGenerator;

    [Tooltip("Segundos que tarda el residente en llegar a la ventanilla.")]
    [SerializeField, Min(0f)] private float arrivalDelay = 1f;

    /// <summary>La llegada que esta en la ventanilla ahora (null si no hay nadie).</summary>
    public Arrival CurrentArrival { get; private set; }

    /// <summary>Llego alguien: la UI debe mostrar sus sprites.</summary>
    public event Action<Arrival> ArrivalStarted;

    /// <summary>El visitante se fue: la UI debe limpiar la ventanilla y los documentos.</summary>
    public event Action ArrivalEnded;

    /// <summary>La noche termino (aun sin saber si fue victoria o derrota).</summary>
    public event Action NightFinished;

    private Coroutine arrivalRoutine;

    private void OnEnable()
    {
      if (gameManager == null || arrivalGenerator == null)
      {
        Debug.LogError(
            "[TurnController] Asigna el GameManager y el ArrivalGenerator en el Inspector.", this);
        return;
      }

      gameManager.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
      if (gameManager != null)
        gameManager.StateChanged -= HandleStateChanged;
    }

    // ---------------- Acciones publicas ----------------

    /// <summary>Genera la noche y empieza el primer turno.</summary>
    public void BeginNight()
    {
      if (gameManager == null || arrivalGenerator == null) return;

      if (gameManager.CurrentState != GameState.NotStarted)
      {
        Debug.LogWarning(
            $"[TurnController] No se puede iniciar desde {gameManager.CurrentState}.", this);
        return;
      }

      if (!arrivalGenerator.GenerateNight()) return;

      gameManager.StartNight();
    }

    /// <summary>
    /// Cierra el turno ya resuelto: pasa al siguiente residente
    /// o termina la noche si no quedan llegadas.
    /// </summary>
    public void ContinueAfterResult()
    {
      if (gameManager.CurrentState != GameState.ShowingResult)
      {
        Debug.LogWarning(
            $"[TurnController] ContinueAfterResult solo vale en ShowingResult (ahora: {gameManager.CurrentState}).",
            this);
        return;
      }

      if (arrivalGenerator.Remaining > 0)
        gameManager.NextResident();
      else
        gameManager.EndNight();
    }

    // ---------------- Logica interna ----------------

    private void HandleStateChanged(GameState previous, GameState current)
    {
      switch (current)
      {
        case GameState.ResidentArriving:
          ClearArrival();
          arrivalRoutine = StartCoroutine(PresentNextArrival());
          break;

        case GameState.NightOver:
          ClearArrival();
          NightFinished?.Invoke();
          break;

        case GameState.NotStarted:
          ClearArrival();
          break;
      }
    }

    // Corrutina: espera 'arrivalDelay' segundos y presenta la siguiente llegada.
    private IEnumerator PresentNextArrival()
    {
      if (arrivalDelay > 0f)
        yield return new WaitForSeconds(arrivalDelay);

      Arrival arrival;
      if (!arrivalGenerator.TryGetNext(out arrival))
      {
        Debug.LogError("[TurnController] No quedan llegadas por entregar.", this);
        yield break;
      }

      CurrentArrival = arrival;

      Debug.Log(
          $"[TurnController] Llega: {arrival.Describe()} | Quedan {arrivalGenerator.Remaining}", this);

      // Primero se avisa a la UI y despues se pasa a "Deciding",
      // para que los sprites ya esten listos cuando el jugador pueda decidir.
      ArrivalStarted?.Invoke(arrival);
      gameManager.ResidentReady();

      arrivalRoutine = null;
    }

    private void ClearArrival()
    {
      if (arrivalRoutine != null)
      {
        StopCoroutine(arrivalRoutine);
        arrivalRoutine = null;
      }

      if (CurrentArrival != null)
      {
        CurrentArrival = null;
        ArrivalEnded?.Invoke();
      }
    }

    // ---------------- Pruebas desde el Inspector ----------------

    [ContextMenu("Debug/1 Iniciar noche")]
    private void DebugBeginNight() => BeginNight();

    [ContextMenu("Debug/2 Simular decision y continuar")]
    private void DebugSimulateDecision()
    {
      if (gameManager == null) return;

      if (gameManager.CurrentState != GameState.Deciding)
      {
        Debug.LogWarning(
            $"[TurnController] Solo se puede decidir en Deciding (ahora: {gameManager.CurrentState}).",
            this);
        return;
      }

      gameManager.SubmitDecision();
      ContinueAfterResult();
    }

    [ContextMenu("Debug/3 Reiniciar noche")]
    private void DebugRestartNight()
    {
      if (gameManager != null) gameManager.RestartNight();
    }
  }
}