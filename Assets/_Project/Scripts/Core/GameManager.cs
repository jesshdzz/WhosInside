using System;
using UnityEngine;

namespace WhoIsOutside.Core
{
  /// <summary>
  /// Controla las fases del turno. No sabe nada de UI ni de datos:
  /// solo cambia de estado y avisa mediante un evento.
  /// </summary>
  public class GameManager : MonoBehaviour
  {
    [SerializeField] private bool logTransitions = true;

    /// <summary>Estado actual. Se puede leer desde fuera, pero solo se cambia aqui.</summary>
    public GameState CurrentState { get; private set; } = GameState.NotStarted;

    /// <summary>Se dispara en cada cambio: (estado anterior, estado nuevo).</summary>
    public event Action<GameState, GameState> StateChanged;

    // ---- Acciones publicas: otros scripts llaman a estas ----

    public void StartNight() => TryChangeState(GameState.ResidentArriving);
    public void ResidentReady() => TryChangeState(GameState.Deciding);
    public void SubmitDecision() => TryChangeState(GameState.ShowingResult);
    public void NextResident() => TryChangeState(GameState.ResidentArriving);
    public void EndNight() => TryChangeState(GameState.NightOver);
    public void RestartNight() => TryChangeState(GameState.NotStarted);

    // ---- Logica interna ----

    private void TryChangeState(GameState newState)
    {
      if (!IsValidTransition(CurrentState, newState))
      {
        Debug.LogWarning(
            $"[GameManager] Transicion no permitida: {CurrentState} -> {newState}", this);
        return;
      }

      GameState previous = CurrentState;
      CurrentState = newState;

      if (logTransitions)
        Debug.Log($"[GameManager] {previous} -> {newState}", this);

      StateChanged?.Invoke(previous, newState);
    }

    private static bool IsValidTransition(GameState from, GameState to)
    {
      switch (from)
      {
        case GameState.NotStarted:
          return to == GameState.ResidentArriving;
        case GameState.ResidentArriving:
          return to == GameState.Deciding;
        case GameState.Deciding:
          return to == GameState.ShowingResult;
        case GameState.ShowingResult:
          return to == GameState.ResidentArriving || to == GameState.NightOver;
        case GameState.NightOver:
          return to == GameState.NotStarted;
        default:
          return false;
      }
    }

    // ---- Pruebas desde el Inspector (se borran o se dejan, no afectan al juego) ----

    [ContextMenu("Debug/1 StartNight")]
    private void DebugStartNight() => StartNight();

    [ContextMenu("Debug/2 ResidentReady")]
    private void DebugResidentReady() => ResidentReady();

    [ContextMenu("Debug/3 SubmitDecision")]
    private void DebugSubmitDecision() => SubmitDecision();

    [ContextMenu("Debug/4 NextResident")]
    private void DebugNextResident() => NextResident();

    [ContextMenu("Debug/5 EndNight")]
    private void DebugEndNight() => EndNight();

    [ContextMenu("Debug/6 RestartNight")]
    private void DebugRestartNight() => RestartNight();
  }
}