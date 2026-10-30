using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhoIsOutside.Core;
using WhoIsOutside.Data;
using WhoIsOutside.Verification;

namespace WhoIsOutside.UI
{
  /// <summary>
  /// Tarea 11c: controla el checklist.
  /// - El boton del escritorio solo funciona mientras hay un residente esperando decision.
  /// - Al cerrar la hoja, el residente responde las preguntas de las filas marcadas con X.
  /// - El boton Allow solo se habilita cuando las 4 filas tienen palomita.
  /// - El dialogo del residente se oculta solo despues de unos segundos.
  /// </summary>
  public class ChecklistController : MonoBehaviour
  {
    [Header("Referencias de juego")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnController turnController;

    [Header("Interfaz")]
    [Tooltip("Boton transparente sobre el papel del escritorio.")]
    [SerializeField] private Button checklistButton;
    [SerializeField] private ModalPanel checklistModal;
    [Tooltip("Filas EN ORDEN: 0 ID, 1 Apariencia, 2 Solicitud, 3 Lista del dia. " +
             "El orden coincide con el indice de las preguntas de control.")]
    [SerializeField] private ChecklistRow[] rows;
    [SerializeField] private GameObject responsePanel;
    [SerializeField] private TMP_Text responseText;

    [Header("Validacion")]
    [Tooltip("Boton de dejar pasar: solo se habilita si las 4 filas tienen palomita.")]
    [SerializeField] private Button allowButton;

    [Header("Ajustes")]
    [Tooltip("Segundos que el dialogo del residente se queda en pantalla.")]
    [SerializeField, Min(0.5f)] private float responseSeconds = 5f;
    [SerializeField] private bool logFlow = true;

    private Coroutine hideRoutine;

    /// <summary>True si TODAS las filas tienen palomita.</summary>
    public bool AllRowsChecked
    {
      get
      {
        if (rows == null || rows.Length == 0) return false;

        foreach (ChecklistRow row in rows)
        {
          if (row == null || row.Mark != RowMark.Check) return false;
        }
        return true;
      }
    }

    private void OnEnable()
    {
      if (gameManager == null || turnController == null || checklistButton == null)
      {
        Debug.LogError(
            "[Checklist] Asigna GameManager, TurnController y el boton del checklist.", this);
        return;
      }

      gameManager.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
      if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
    }

    private void Start()
    {
      if (allowButton == null)
      {
        Debug.LogWarning("[Checklist] Falta asignar el boton Allow: no se validara con el checklist.", this);
      }

      // Al empezar no hay residente: boton apagado y sin respuestas en pantalla.
      if (checklistButton != null) checklistButton.interactable = false;
      HideResponse();
    }

    // Se ejecuta despues de todos los Update y de los eventos del cuadro actual.
    // Asi, cuando DecisionInput habilita Allow al pasar a "Deciding", aqui lo
    // volvemos a bloquear si el checklist aun no esta completo.
    private void LateUpdate()
    {
      if (allowButton == null || gameManager == null) return;
      if (gameManager.CurrentState != GameState.Deciding) return; // En otros estados manda DecisionInput

      bool open = AllRowsChecked;
      if (allowButton.interactable != open)
      {
        allowButton.interactable = open;
        Log(open ? "Checklist completo: Allow habilitado." : "Checklist incompleto: Allow bloqueado.");
      }
    }

    // ---------------- Estado del juego ----------------

    private void HandleStateChanged(GameState previous, GameState current)
    {
      bool canUse = current == GameState.Deciding;
      checklistButton.interactable = canUse;

      // Residente nuevo: la hoja empieza en blanco.
      if (current == GameState.ResidentArriving) ResetChecklist();

      // Fuera de "Deciding" (rechazado, resultado, fin...) se cierra la hoja y se limpia.
      if (!canUse)
      {
        if (checklistModal != null) checklistModal.Close();
        HideResponse();
      }
    }

    // ---------------- Cierre de la hoja ----------------

    /// <summary>
    /// Se conecta desde On Click () del CloseButton de ChecklistModal, DESPUES de
    /// ModalPanel.Close(). Lee las filas marcadas con X y muestra las respuestas.
    /// </summary>
    public void OnChecklistClosed()
    {
      // Solo se responde si hay un residente esperando decision.
      if (gameManager.CurrentState != GameState.Deciding) return;

      Arrival arrival = turnController.CurrentArrival;
      if (arrival == null) return;

      ResidentData resident = arrival.Resident;
      ResidentVariant variant = arrival.Variant; // null = residente real
      Log($"Hoja cerrada. {arrival.Describe()}");

      StringBuilder text = new StringBuilder();
      int asked = 0;

      for (int i = 0; i < rows.Length; i++)
      {
        // Solo preguntan las filas marcadas con X.
        if (rows[i] == null || rows[i].Mark != RowMark.Cross) continue;

        if (i >= resident.ControlQuestions.Count)
        {
          Debug.LogWarning(
              $"[Checklist] {resident.name} no tiene la pregunta de control #{i}.", this);
          continue;
        }

        ControlQuestion question = resident.ControlQuestions[i];

        // Respuesta correcta, salvo que el doble tenga una alterada para esta pregunta.
        string answer = question.correctAnswer;
        bool altered = false;
        if (variant != null && variant.TryGetAlteredAnswer(i, out string wrongAnswer))
        {
          answer = wrongAnswer;
          altered = true;
        }

        text.AppendLine($"P: {question.question}");
        text.AppendLine($"R: {answer}");
        asked++;

        Log($"Pregunta #{i}: {(altered ? "RESPUESTA ALTERADA" : "respuesta correcta")}");
      }

      if (asked == 0)
      {
        HideResponse(); // No hay ninguna X marcada: nada que preguntar.
        return;
      }

      ShowResponse(text.ToString().TrimEnd());
    }

    // ---------------- Utilidades ----------------

    private void ResetChecklist()
    {
      foreach (ChecklistRow row in rows)
      {
        if (row != null) row.Clear();
      }
    }

    private void ShowResponse(string text)
    {
      if (responseText != null) responseText.text = text;
      if (responsePanel != null) responsePanel.SetActive(true);

      // Reinicia el temporizador: el dialogo se oculta solo tras responseSeconds.
      if (hideRoutine != null) StopCoroutine(hideRoutine);
      hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
      yield return new WaitForSeconds(responseSeconds);
      hideRoutine = null;
      HideResponse();
    }

    private void HideResponse()
    {
      if (hideRoutine != null)
      {
        StopCoroutine(hideRoutine);
        hideRoutine = null;
      }

      if (responsePanel != null) responsePanel.SetActive(false);
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[Checklist] {message}", this);
    }
  }
}