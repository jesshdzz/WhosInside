using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using WhoIsOutside.Core;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Tarea 11d: menu de pausa con Continuar, Reiniciar y Salir.
  /// Pausar = Time.timeScale en 0 (congela corrutinas y animaciones).
  /// </summary>
  public class PauseController : MonoBehaviour
  {
    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [Tooltip("Panel de pantalla completa con la capa oscura y la ventanita. Debe empezar DESACTIVADO.")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Ajustes")]
    [SerializeField] private bool allowEscapeKey = true;
    [SerializeField] private bool logFlow = true;

    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Start()
    {
      if (pauseMenu != null) pauseMenu.SetActive(false);
    }

    private void Update()
    {
      // Esc pausa y reanuda (Input System nuevo).
      if (!allowEscapeKey || Keyboard.current == null) return;

      if (Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        if (isPaused) Resume();
        else Pause();
      }
    }

    private void OnDisable()
    {
      // Si este objeto se apaga mientras esta en pausa, nunca dejamos el tiempo detenido.
      if (isPaused) RestoreTime();
    }

    // ---------------- Opciones del menu ----------------

    /// <summary>Se conecta desde el boton Pause del escritorio.</summary>
    public void Pause()
    {
      if (isPaused || !CanPause()) return;

      isPaused = true;
      previousTimeScale = Time.timeScale;
      Time.timeScale = 0f;
      AudioListener.pause = true;
      if (pauseMenu != null) pauseMenu.SetActive(true);
      Log("Juego en pausa.");
    }

    /// <summary>Boton Continuar.</summary>
    public void Resume()
    {
      if (!isPaused) return;

      RestoreTime();
      if (pauseMenu != null) pauseMenu.SetActive(false);
      Log("Juego reanudado.");
    }

    /// <summary>Boton Reiniciar: recarga la escena y empieza una noche nueva.</summary>
        /// <summary>Boton Reiniciar: recarga la escena y empieza una noche nueva directo.</summary>
    public void Restart()
    {
      ReloadScene(true);
    }

    /// <summary>
    /// Boton Salir: recarga la escena y vuelve a la pantalla de inicio.
    /// Se llama Quit para no romper la conexion del boton en el Inspector.
    /// </summary>
    public void Quit()
    {
      ReloadScene(false);
    }

    // Recarga la escena. Si skipStartScreen es true, la noche empieza sin pasar
    // por la pantalla de inicio; si es false, se muestra la pantalla de inicio.
    private void ReloadScene(bool skipStartScreen)
    {
      Scene scene = SceneManager.GetActiveScene();

      // LoadScene solo funciona con escenas agregadas a la lista de compilacion.
      if (scene.buildIndex < 0)
      {
        Debug.LogError(
            "[Pause] La escena no esta en la lista de compilacion. " +
            "Agregala en File > Build Profiles (Scene List).", this);
        return;
      }

      RestoreTime(); // Nunca se carga una escena con el tiempo detenido

      if (skipStartScreen) StartScreenController.SkipStartScreenOnNextLoad();

      SceneManager.LoadScene(scene.buildIndex);
    }

    // ---------------- Utilidades ----------------

    // Solo se puede pausar con la noche en curso (no en el inicio ni en las pantallas finales).
    private bool CanPause()
    {
      if (gameManager == null) return false;

      GameState state = gameManager.CurrentState;
      return state != GameState.NotStarted && state != GameState.NightOver;
    }

    private void RestoreTime()
    {
      isPaused = false;
      Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
      AudioListener.pause = false;
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[Pause] {message}", this);
    }
  }
}