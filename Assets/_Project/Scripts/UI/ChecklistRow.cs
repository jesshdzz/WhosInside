using UnityEngine;
using UnityEngine.UI;

namespace WhoIsOutside.UI
{
  /// <summary>Marca posible de una fila del checklist.</summary>
  public enum RowMark
  {
    None,   // Sin marcar
    Check,  // Palomita: el jugador no ve discrepancia
    Cross   // X: el jugador sospecha de esta fila
  }

  /// <summary>
  /// Una fila del checklist (por ejemplo "Apariencia"): dos botones transparentes
  /// sobre las casillas y una imagen de marca sobre cada una.
  /// Pulsar una casilla la marca; pulsarla otra vez la desmarca;
  /// marcar una casilla desmarca la otra de la misma fila.
  /// </summary>
  public class ChecklistRow : MonoBehaviour
  {
    // Nombre del objeto hijo que dibuja la marca dentro de cada boton.
    private const string MarkChildName = "Mark";

    [SerializeField] private Button checkButton;
    [SerializeField] private Button crossButton;
    [Tooltip("Opcional: si se deja vacio, se usa el hijo llamado 'Mark' del boton.")]
    [SerializeField] private GameObject checkMark;
    [Tooltip("Opcional: si se deja vacio, se usa el hijo llamado 'Mark' del boton.")]
    [SerializeField] private GameObject crossMark;

    private bool isSetUp;

    /// <summary>Marca actual de la fila. Solo se modifica desde aqui.</summary>
    public RowMark Mark { get; private set; } = RowMark.None;

    private void Awake()
    {
      if (EnsureSetup()) Refresh();
    }

    /// <summary>Quita cualquier marca (se usa al llegar un residente nuevo).</summary>
    public void Clear()
    {
      Mark = RowMark.None;
      if (EnsureSetup()) Refresh();
    }

    // Prepara la fila UNA sola vez: resuelve los Mark y conecta los clics.
    // Se llama desde Awake y desde Clear, porque Clear puede ejecutarse ANTES de
    // Awake cuando la hoja todavia no se ha abierto (el objeto esta inactivo).
    private bool EnsureSetup()
    {
      if (isSetUp) return true;

      if (checkButton == null || crossButton == null)
      {
        Debug.LogError($"[ChecklistRow] Faltan los botones en {name}.", this);
        return false;
      }

      // Si el campo esta vacio o apunta al propio boton, busca el hijo "Mark".
      checkMark = ResolveMark(checkMark, checkButton);
      crossMark = ResolveMark(crossMark, crossButton);

      if (checkMark == null || crossMark == null)
      {
        Debug.LogError(
            $"[ChecklistRow] En {name}, cada boton necesita un hijo llamado '{MarkChildName}'.", this);
        return false;
      }

      checkButton.onClick.AddListener(() => Click(RowMark.Check));
      crossButton.onClick.AddListener(() => Click(RowMark.Cross));

      isSetUp = true;
      return true;
    }

    private void Click(RowMark clicked)
    {
      Debug.Log($"[ChecklistRow] Clic {clicked} en {name}", this); // Puedes borrar esta linea.

      // Si ya estaba marcada esa casilla, se desmarca; si no, se marca.
      Mark = (Mark == clicked) ? RowMark.None : clicked;
      Refresh();
    }

    // Devuelve la marca valida: la asignada, o el hijo "Mark" del boton.
    private static GameObject ResolveMark(GameObject current, Button button)
    {
      if (current != null && current != button.gameObject) return current;

      Transform child = button.transform.Find(MarkChildName);
      return child != null ? child.gameObject : null;
    }

    // Muestra solo la imagen de la marca actual.
    private void Refresh()
    {
      if (checkMark != null) checkMark.SetActive(Mark == RowMark.Check);
      if (crossMark != null) crossMark.SetActive(Mark == RowMark.Cross);
    }
  }
}