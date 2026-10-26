using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WhoIsOutside.Core;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Convierte los clics de los botones de decision en un evento y controla
  /// cuales responden segun la fase del turno.
  /// No decide si la decision fue correcta: solo avisa que se tomo.
  /// </summary>
  public class DecisionInput : MonoBehaviour
  {
    // Fases internas del panel de botones.
    private enum Mode
    {
      Disabled,   // Ningun boton responde
      Deciding,   // Dejar pasar y Rechazar activos; Emergencia apagada
      Rejected    // Solo Emergencia activa
    }

    [Header("Botones")]
    [SerializeField] private Button allowButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private Button emergencyButton;

    [Header("Mensaje que obliga a usar Emergencia")]
    [Tooltip("Objeto (por ejemplo un texto) que se activa pasado el tiempo. Debe empezar desactivado.")]
    [SerializeField] private GameObject emergencyPrompt;
    [Tooltip("Segundos tras Rechazar antes de mostrar el mensaje.")]
    [SerializeField, Min(0f)] private float emergencyPromptDelay = 5f;

    [Header("Pruebas")]
    [Tooltip("Si esta marcado, al iniciar los botones quedan en modo 'decidiendo'.")]
    [SerializeField] private bool startInDecidingMode = true;
    [Tooltip("Escribe cada decision en la Console.")]
    [SerializeField] private bool logDecisions = true;

    /// <summary>Se dispara cada vez que el jugador pulsa un boton valido.</summary>
    public event Action<Decision> DecisionMade;

    private Mode mode = Mode.Disabled;
    private Coroutine promptRoutine;

    private void Awake()
    {
      Bind(allowButton, Decision.Allow);
      Bind(denyButton, Decision.Deny);
      Bind(emergencyButton, Decision.Emergency);

      if (startInDecidingMode) ShowDecidingMode();
      else DisableAll();
    }

    // ---------------- Acciones publicas ----------------

    /// <summary>Nuevo turno: Dejar pasar y Rechazar activos, Emergencia apagada.</summary>
    public void ShowDecidingMode() => SetMode(Mode.Deciding);

    /// <summary>Tras Rechazar: solo Emergencia activa y empieza la cuenta del mensaje.</summary>
    public void ShowRejectedMode() => SetMode(Mode.Rejected);

    /// <summary>Turno resuelto o sin residente: ningun boton responde.</summary>
    public void DisableAll() => SetMode(Mode.Disabled);

    // ---------------- Logica interna ----------------

    private void SetMode(Mode newMode)
    {
      mode = newMode;

      StopPrompt(); // Cancela la cuenta y oculta el mensaje

      SetButtonInteractable(allowButton, newMode == Mode.Deciding);
      SetButtonInteractable(denyButton, newMode == Mode.Deciding);
      SetButtonInteractable(emergencyButton, newMode == Mode.Rejected);

      if (newMode == Mode.Rejected && emergencyPrompt != null)
        promptRoutine = StartCoroutine(ShowPromptAfterDelay());
    }

    private void Bind(Button button, Decision decision)
    {
      if (button == null)
      {
        Debug.LogWarning($"[DecisionInput] Falta el boton para {decision}.", this);
        return;
      }

      // La lambda recuerda la decision de este boton.
      button.onClick.AddListener(() => Raise(decision));
    }

    private void Raise(Decision decision)
    {
      // Proteccion extra: ignora clics que no corresponden a la fase actual.
      if (!IsAllowedInCurrentMode(decision)) return;

      if (logDecisions)
        Debug.Log($"[DecisionInput] Decision: {decision}", this);

      // Primero se cambia el modo y despues se avisa: asi, si quien escucha
      // vuelve a cambiar el modo (por ejemplo al empezar otro turno), no se pisa.
      if (decision == Decision.Deny)
        ShowRejectedMode();   // Se cierra la ventanilla: ahora solo Emergencia
      else
        DisableAll();         // Dejar pasar o Emergencia: el turno queda resuelto

      DecisionMade?.Invoke(decision);
    }

    private bool IsAllowedInCurrentMode(Decision decision)
    {
      switch (decision)
      {
        case Decision.Allow:
        case Decision.Deny:
          return mode == Mode.Deciding;
        case Decision.Emergency:
          return mode == Mode.Rejected;
        default:
          return false;
      }
    }

    // Corrutina: espera y muestra el mensaje.
    private IEnumerator ShowPromptAfterDelay()
    {
      if (emergencyPromptDelay > 0f)
        yield return new WaitForSeconds(emergencyPromptDelay);

      emergencyPrompt.SetActive(true);
      promptRoutine = null;
    }

    private void StopPrompt()
    {
      if (promptRoutine != null)
      {
        StopCoroutine(promptRoutine);
        promptRoutine = null;
      }

      if (emergencyPrompt != null)
        emergencyPrompt.SetActive(false);
    }

    private static void SetButtonInteractable(Button button, bool value)
    {
      if (button != null) button.interactable = value;
    }

    // ---------------- Pruebas desde el Inspector ----------------

    [ContextMenu("Debug/Volver a modo decidiendo")]
    private void DebugBackToDeciding() => ShowDecidingMode();
  }
}