using UnityEngine;
using WhoIsOutside.Core;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Tarea 11: muestra la pantalla final (screamer, despedida o victoria) cuando
  /// DecisionFlow avisa que la noche termino, y permite reintentar la noche.
  /// </summary>
  public class EndScreenController : MonoBehaviour
  {
    [Header("Referencias")]
    [SerializeField] private DecisionFlow decisionFlow;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnController turnController;

    [Header("Pantallas (deben empezar desactivadas)")]
    [SerializeField] private GameObject screamerScreen;
    [SerializeField] private GameObject firedScreen;
    [SerializeField] private GameObject victoryScreen;

    [SerializeField] private bool logFlow = true;

    private void Awake()
    {
      HideAll(); // Por si alguna pantalla quedo activa en el editor
    }

    private void OnEnable()
    {
      if (decisionFlow == null || gameManager == null || turnController == null)
      {
        Debug.LogError(
            "[EndScreen] Asigna DecisionFlow, GameManager y TurnController en el Inspector.", this);
        return;
      }

      decisionFlow.NightEnded += HandleNightEnded;
    }

    private void OnDisable()
    {
      if (decisionFlow != null) decisionFlow.NightEnded -= HandleNightEnded;
    }

    // DecisionFlow avisa que la noche termino y como.
    private void HandleNightEnded(NightEnding ending)
    {
      HideAll();

      switch (ending)
      {
        case NightEnding.DefeatScreamer:
          Show(screamerScreen, "screamer");
          break;

        case NightEnding.DefeatFired:
          Show(firedScreen, "despedida");
          break;

        default: // Victory
          Show(victoryScreen, "victoria");
          break;
      }
    }

    /// <summary>
    /// Se conecta desde On Click () de los botones "Reintentar" en el Inspector.
    /// Oculta la pantalla, reinicia el GameManager y arranca una noche nueva.
    /// </summary>
    public void Retry()
    {
      Log("Reintentar: reiniciando la noche.");
      HideAll();
      gameManager.RestartNight();
      turnController.BeginNight();
    }

    private void Show(GameObject screen, string label)
    {
      if (screen == null)
      {
        Debug.LogWarning($"[EndScreen] Falta asignar la pantalla de {label}.", this);
        return;
      }

      screen.SetActive(true);
      Log($"Mostrando pantalla de {label}.");
    }

    private void HideAll()
    {
      if (screamerScreen != null) screamerScreen.SetActive(false);
      if (firedScreen != null) firedScreen.SetActive(false);
      if (victoryScreen != null) victoryScreen.SetActive(false);
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[EndScreen] {message}", this);
    }

    // Pruebas rapidas: solo en Play, desde el menu de tres puntos del componente.
    [ContextMenu("Probar pantalla screamer")]
    private void TestScreamer() { TestScreen(NightEnding.DefeatScreamer); }

    [ContextMenu("Probar pantalla despedida")]
    private void TestFired() { TestScreen(NightEnding.DefeatFired); }

    [ContextMenu("Probar pantalla victoria")]
    private void TestVictory() { TestScreen(NightEnding.Victory); }

    private void TestScreen(NightEnding ending)
    {
      if (!Application.isPlaying)
      {
        Debug.LogWarning("[EndScreen] Esta prueba solo funciona en Play.", this);
        return;
      }
      HandleNightEnded(ending);
    }
  }
}