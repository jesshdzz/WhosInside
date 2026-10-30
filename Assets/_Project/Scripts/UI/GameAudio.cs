using UnityEngine;
using WhoIsOutside.Core;
using WhoIsOutside.UI;
using WhoIsOutside.Verification;

namespace WhoIsOutside.Audio
{
  /// <summary>
  /// Tarea 11f: todos los sonidos del juego en un solo lugar.
  /// Solo ESCUCHA eventos que ya existen; no cambia la logica del juego.
  /// Si falta un clip o una referencia, ese sonido simplemente no suena.
  /// </summary>
  public class GameAudio : MonoBehaviour
  {
    [Header("Referencias de juego (cada una es opcional)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnController turnController;
    [SerializeField] private DecisionInput decisionInput;
    [SerializeField] private DecisionFlow decisionFlow;
    [SerializeField] private WindowShutter windowShutter;
    [SerializeField] private ChecklistController checklistController;

    [Header("Musica de fondo")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("Final de la noche")]
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField, Range(0f, 1f)] private float resultVolume = 1f;

    [Header("Pasos")]
    [Tooltip("Suena cuando el residente llega.")]
    [SerializeField] private AudioClip arrivalStepsClip;
    [Tooltip("Suena cuando se le deja pasar y se va.")]
    [SerializeField] private AudioClip exitStepsClip;
    [SerializeField, Range(0f, 1f)] private float stepsVolume = 0.8f;

    [Header("Ventanilla")]
    [SerializeField] private AudioClip shutterCloseClip;
    [SerializeField] private AudioClip shutterOpenClip;

    [Header("Papeles (si pones varios, elige uno al azar)")]
    [SerializeField] private AudioClip[] paperClips;

    [Header("Dialogos del residente")]
    [SerializeField] private AudioClip dialogueClip;
    [SerializeField, Range(0f, 1f)] private float dialogueVolume = 1f;

    [Header("Efectos en general")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool logFlow = true;

    // Una fuente por tipo de sonido, para poder detener cada uno por separado.
    private AudioSource musicSource;
    private AudioSource stepsSource;
    private AudioSource voiceSource;
    private AudioSource resultSource;
    private AudioSource sfxSource;

    private void Awake()
    {
      musicSource = CreateSource("Music", true);
      stepsSource = CreateSource("Steps", false);
      voiceSource = CreateSource("Voice", false);
      resultSource = CreateSource("Result", false);
      sfxSource = CreateSource("Sfx", false);
    }

    // Crea un AudioSource 2D en un objeto hijo, para no llenar el Inspector.
    private AudioSource CreateSource(string sourceName, bool loop)
    {
      GameObject child = new GameObject($"AudioSource_{sourceName}");
      child.transform.SetParent(transform, false);

      AudioSource source = child.AddComponent<AudioSource>();
      source.playOnAwake = false;
      source.loop = loop;
      source.spatialBlend = 0f; // 2D: suena igual en todo el juego
      return source;
    }

    private void OnEnable()
    {
      if (gameManager != null) gameManager.StateChanged += HandleStateChanged;
      else Warn("GameManager (musica y cortes)");

      if (turnController != null)
      {
        turnController.ArrivalStarted += HandleArrivalStarted;
        turnController.ArrivalEnded += HandleArrivalEnded;
      }
      else Warn("TurnController (pasos de llegada)");

      if (decisionInput != null) decisionInput.DecisionMade += HandleDecision;
      else Warn("DecisionInput (pasos de ida)");

      if (decisionFlow != null) decisionFlow.NightEnded += HandleNightEnded;
      else Warn("DecisionFlow (victoria y derrota)");

      if (windowShutter != null)
      {
        windowShutter.Closing += HandleShutterClosing;
        windowShutter.Opening += HandleShutterOpening;
      }
      else Warn("WindowShutter (ventanilla)");

      if (checklistController != null) checklistController.ResponseShown += HandleResponseShown;
      else Warn("ChecklistController (dialogos)");
    }

    private void OnDisable()
    {
      if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;

      if (turnController != null)
      {
        turnController.ArrivalStarted -= HandleArrivalStarted;
        turnController.ArrivalEnded -= HandleArrivalEnded;
      }

      if (decisionInput != null) decisionInput.DecisionMade -= HandleDecision;
      if (decisionFlow != null) decisionFlow.NightEnded -= HandleNightEnded;

      if (windowShutter != null)
      {
        windowShutter.Closing -= HandleShutterClosing;
        windowShutter.Opening -= HandleShutterOpening;
      }

      if (checklistController != null) checklistController.ResponseShown -= HandleResponseShown;
    }

    // ---------------- Eventos del juego ----------------

    private void HandleStateChanged(GameState previous, GameState current)
    {
      switch (current)
      {
        case GameState.ResidentArriving:
          // Viene de NotStarted: la noche acaba de empezar, arranca la musica.
          if (previous == GameState.NotStarted) PlayMusic();
          break;

        case GameState.NightOver:
          StopMusic();
          break;

        case GameState.NotStarted:
          // Noche reiniciada: se corta cualquier sonido de la noche anterior.
          StopMusic();
          resultSource.Stop();
          stepsSource.Stop();
          voiceSource.Stop();
          break;
      }

      // Fuera de "Deciding" ya no hay dialogo en curso.
      if (current != GameState.Deciding) voiceSource.Stop();
    }

    private void HandleArrivalStarted(Arrival arrival)
    {
      PlayLoop(stepsSource, arrivalStepsClip, stepsVolume);
    }

    private void HandleArrivalEnded()
    {
      stepsSource.Stop(); // Si quedaba algun paso sonando, se corta al irse el visitante
    }

    private void HandleDecision(Decision decision)
    {
      if (decision == Decision.Allow) PlayLoop(stepsSource, exitStepsClip, stepsVolume);
    }

    private void HandleNightEnded(NightEnding ending)
    {
      AudioClip clip = ending == NightEnding.Victory ? winClip : loseClip;
      PlayLoop(resultSource, clip, resultVolume);
      Log(ending == NightEnding.Victory ? "Suena victoria." : "Suena derrota.");
    }

    private void HandleShutterClosing() { PlaySfx(shutterCloseClip); }

    private void HandleShutterOpening() { PlaySfx(shutterOpenClip); }

    private void HandleResponseShown()
    {
      PlayLoop(voiceSource, dialogueClip, dialogueVolume);
    }

    // ---------------- Para los botones (On Click) ----------------

    /// <summary>
    /// Se conecta desde On Click () de los botones de papeles:
    /// ID, solicitud, lista del dia, checklist y carpetas de cada piso.
    /// </summary>
    public void PlayPaper()
    {
      if (paperClips == null || paperClips.Length == 0) return;

      AudioClip clip = paperClips[Random.Range(0, paperClips.Length)];
      PlaySfx(clip);
    }

    // ---------------- Utilidades ----------------

    private void PlayMusic()
    {
      if (backgroundMusic == null || musicSource.isPlaying) return;

      musicSource.clip = backgroundMusic;
      musicSource.volume = musicVolume;
      musicSource.Play();
      Log("Empieza la musica de fondo.");
    }

    private void StopMusic()
    {
      musicSource.Stop();
    }

    // Reproduce un clip en una fuente propia (reemplaza lo que sonaba antes ahi).
    private void PlayLoop(AudioSource source, AudioClip clip, float volume)
    {
      source.Stop();
      if (clip == null) return;

      source.clip = clip;
      source.volume = volume;
      source.Play();
    }

    // Efecto corto que puede solaparse con otros.
    private void PlaySfx(AudioClip clip)
    {
      if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void Warn(string what)
    {
      Debug.LogWarning($"[GameAudio] Falta asignar: {what}. Ese sonido no se reproducira.", this);
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[GameAudio] {message}", this);
    }
  }
}