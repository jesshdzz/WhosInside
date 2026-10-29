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
  /// Tarea 11c: controla el checklist. El boton del escritorio solo funciona mientras
  /// hay un residente esperando decision. Al cerrar la hoja, el residente responde
  /// las preguntas de control de las filas marcadas.
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

    [Header("Ajustes")]
    [Tooltip("Activo: el residente solo responde las filas con X. " +
             "Apagado: responde todas las filas que tengan alguna marca (palomita o X).")]
    [SerializeField] private bool answerOnlyCrossed = false;
    [SerializeField] private bool logFlow = true;

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
      // Al empezar no hay residente: boton apagado y sin respuestas en pantalla.
      if (checklistButton != null) checklistButton.interactable = false;
      HideResponse();
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
    /// ModalPanel.Close(). Lee las marcas y muestra las respuestas del residente.
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
        if (rows[i] == null) continue;

        RowMark mark = rows[i].Mark;
        bool ask = answerOnlyCrossed ? mark == RowMark.Cross : mark != RowMark.None;
        if (!ask) continue;

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

        Log($"Pregunta #{i} ({mark}): {(altered ? "RESPUESTA ALTERADA" : "respuesta correcta")}");
      }

      if (asked == 0)
      {
        HideResponse(); // No se marco nada que preguntar.
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
    }

    private void HideResponse()
    {
      if (responsePanel != null) responsePanel.SetActive(false);
    }

    private void Log(string message)
    {
      if (logFlow) Debug.Log($"[Checklist] {message}", this);
    }
  }
}