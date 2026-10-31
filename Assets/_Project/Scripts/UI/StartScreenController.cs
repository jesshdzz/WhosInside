using System.Collections;
using UnityEngine;
using WhoIsOutside.Core;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Tarea 11d: pantalla de inicio. Mientras esta visible, la noche no empieza;
  /// el boton Iniciar llama a StartGame().
  /// </summary>
  public class StartScreenController : MonoBehaviour
  {
    // Se conserva entre cargas de escena: "Reiniciar" del menu de pausa lo activa
    // para que la noche empiece directo, sin volver a pasar por la pantalla de inicio.
    private static bool skipStartScreenOnce;

    [Header("Referencias")]
    [SerializeField] private TurnController turnController;
    [Tooltip("Panel de pantalla completa con la imagen de inicio. Debe empezar ACTIVO.")]
    [SerializeField] private GameObject startScreen;

    [SerializeField] private bool logFlow = true;

    /// <summary>Se dispara cuando el jugador pulsa el boton Iniciar.</summary>
    public event System.Action GameStarted;

    // Evita que el valor se quede guardado entre sesiones de Play cuando el editor
    // no recarga el dominio (Enter Play Mode Options).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
      skipStartScreenOnce = false;
    }

    /// <summary>La proxima vez que se cargue la escena, se salta la pantalla de inicio.</summary>
    public static void SkipStartScreenOnNextLoad()
    {
      skipStartScreenOnce = true;
    }

    private void Start()
    {
      if (turnController == null)
      {
        Debug.LogError("[StartScreen] Asigna el TurnController en el Inspector.", this);
        return;
      }

      bool screenVisible = startScreen != null && startScreen.activeSelf;

      // Reiniciado desde la pausa, o pantalla desactivada para probar: empezar directo.
      if (skipStartScreenOnce || !screenVisible)
      {
        skipStartScreenOnce = false;
        if (startScreen != null) startScreen.SetActive(false);
        StartCoroutine(BeginNightNextFrame());
      }
    }

    /// <summary>Se conecta desde On Click () del boton Iniciar.</summary>
    public void StartGame()
    {
      if (startScreen != null) startScreen.SetActive(false);
      Log("Boton Iniciar: empieza la noche.");
      GameStarted?.Invoke();
      turnController.BeginNight();
    }

    // Espera un cuadro para que todos los Start de la escena ya hayan corrido.
    private IEnumerator BeginNightNextFrame()
    {
      yield return null;
      turnController.BeginNight();
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[StartScreen] {message}", this);
    }
  }
}