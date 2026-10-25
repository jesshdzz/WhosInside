using System;
using UnityEngine;
using UnityEngine.UI;
using WhoIsOutside.Core;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Convierte los clics de los tres botones de decision en un evento.
  /// No decide nada: solo avisa que decision se tomo.
  /// </summary>
  public class DecisionInput : MonoBehaviour
  {
    [SerializeField] private Button allowButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private Button emergencyButton;

    [Tooltip("Si esta desmarcado, los botones empiezan sin responder al clic.")]
    [SerializeField] private bool startInteractable = true;

    [Tooltip("Escribe cada decision en la Console (util para probar).")]
    [SerializeField] private bool logDecisions = true;

    /// <summary>Se dispara cada vez que el jugador pulsa un boton de decision.</summary>
    public event Action<Decision> DecisionMade;

    private void Awake()
    {
      Bind(allowButton, Decision.Allow);
      Bind(denyButton, Decision.Deny);
      Bind(emergencyButton, Decision.Emergency);

      SetInteractable(startInteractable);
    }

    /// <summary>Activa o desactiva la respuesta de los tres botones.</summary>
    public void SetInteractable(bool value)
    {
      SetButtonInteractable(allowButton, value);
      SetButtonInteractable(denyButton, value);
      SetButtonInteractable(emergencyButton, value);
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
      if (logDecisions)
        Debug.Log($"[DecisionInput] Decision: {decision}", this);

      DecisionMade?.Invoke(decision);
    }

    private static void SetButtonInteractable(Button button, bool value)
    {
      if (button != null) button.interactable = value;
    }
  }
}