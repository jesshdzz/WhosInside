using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private StartScreenController startScreenController;

    [Header("Musica de fondo")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("Pantalla de inicio")]
    [Tooltip("Suena al pulsar el boton Iniciar.")]
    [SerializeField] private AudioClip startClip;

    [Header("Botones de decision")]
    [SerializeField] private AudioClip allowClip;
    [SerializeField] private AudioClip denyClip;

    [Header("Emergencia")]
    [Tooltip("Suena al pulsar Emergencia y dura lo que la ventanilla esta cerrada.")]
    [SerializeField] private AudioClip emergencyClip;
    [SerializeField, Range(0f, 1f)] private float emergencyVolume = 1f;
    [Tooltip("Segundos que tarda en bajar el volumen cuando la persiana empieza a subir.")]
    [SerializeField, Min(0f)] private float emergencyFadeOut = 0.3f;

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
    [Tooltip("Opcional: botones a los que se les conecta el sonido de papel POR CODIGO. " +
             "Sirve para los que no dejan anadir entradas en On Click (). " +
             "No pongas aqui los que ya tienen PlayPaper en On Click, o sonara doble.")]
    [SerializeField] private Button[] paperButtons;

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
    private AudioSource emergencySource;
    private AudioSource sfxSource;

    private Coroutine emergencyFadeRoutine;

    private void Awake()
    {
      musicSource = CreateSource("Music", true);
      stepsSource = CreateSource("Steps", false);
      voiceSource = CreateSource("Voice", false);
      resultSource = CreateSource("Result", false);
      emergencySource = CreateSource("Emergency", false);
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
      else Warn("DecisionInput (botones Allow, Deny y Emergencia)");

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

      if (startScreenController != null) startScreenController.GameStarted += HandleGameStarted;
      else Warn("StartScreenController (boton Iniciar)");

      // Botones de papel conectados por codigo (opcional).
      if (paperButtons != null)
      {
        foreach (Button button in paperButtons)
        {
          if (button != null) button.onClick.AddListener(PlayPaper);
        }
      }
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
      if (startScreenController != null) startScreenController.GameStarted -= HandleGameStarted;

      if (paperButtons != null)
      {
        foreach (Button button in paperButtons)
        {
          if (button != null) button.onClick.RemoveListener(PlayPaper);
        }
      }
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
          StopEmergencyNow();
          break;

        case GameState.NotStarted:
          // Noche reiniciada: se corta cualquier sonido de la noche anterior.
          StopMusic();
          resultSource.Stop();
          stepsSource.Stop();
          voiceSource.Stop();
          StopEmergencyNow();
          break;
      }

      // Fuera de "Deciding" ya no hay dialogo en curso.
      if (current != GameState.Deciding) voiceSource.Stop();
    }

    private void HandleGameStarted()
    {
      PlaySfx(startClip);
    }

    private void HandleArrivalStarted(Arrival arrival)
    {
      PlayOn(stepsSource, arrivalStepsClip, stepsVolume);
    }

    private void HandleArrivalEnded()
    {
      stepsSource.Stop(); // Si quedaba algun paso sonando, se corta al irse el visitante
    }

    private void HandleDecision(Decision decision)
    {
      switch (decision)
      {
        case Decision.Allow:
          PlaySfx(allowClip);
          PlayOn(stepsSource, exitStepsClip, stepsVolume);
          break;

        case Decision.Deny:
          PlaySfx(denyClip);
          break;

        case Decision.Emergency:
          StopEmergencyFade();
          PlayOn(emergencySource, emergencyClip, emergencyVolume);
          Log("Suena emergencia.");
          break;
      }
    }

    private void HandleNightEnded(NightEnding ending)
    {
      AudioClip clip = ending == NightEnding.Victory ? winClip : loseClip;
      PlayOn(resultSource, clip, resultVolume);
      Log(ending == NightEnding.Victory ? "Suena victoria." : "Suena derrota.");
    }

    private void HandleShutterClosing()
    {
      PlaySfx(shutterCloseClip);
    }

    private void HandleShutterOpening()
    {
      PlaySfx(shutterOpenClip);

      // La emergencia dura lo que la ventanilla esta cerrada: al subir, se apaga suavemente.
      if (emergencySource.isPlaying)
      {
        StopEmergencyFade();
        emergencyFadeRoutine = StartCoroutine(FadeOutEmergency());
      }
    }

    private void HandleResponseShown()
    {
      PlayOn(voiceSource, dialogueClip, dialogueVolume);
    }

    // ---------------- Para los botones (On Click) ----------------

    /// <summary>
    /// Sonido de papel. Se conecta desde On Click () o desde la lista Paper Buttons:
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
    private void PlayOn(AudioSource source, AudioClip clip, float volume)
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

    // Baja el volumen de la emergencia y la detiene.
    private IEnumerator FadeOutEmergency()
    {
      float startVolume = emergencySource.volume;
      float t = 0f;

      while (emergencyFadeOut > 0f && t < 1f)
      {
        t += Time.deltaTime / emergencyFadeOut;
        emergencySource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(t));
        yield return null;
      }

      emergencySource.Stop();
      emergencyFadeRoutine = null;
    }

    private void StopEmergencyFade()
    {
      if (emergencyFadeRoutine == null) return;
      StopCoroutine(emergencyFadeRoutine);
      emergencyFadeRoutine = null;
    }

    // Corta la emergencia de golpe (fin de la noche o reinicio).
    private void StopEmergencyNow()
    {
      StopEmergencyFade();
      emergencySource.Stop();
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