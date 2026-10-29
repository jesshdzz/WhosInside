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
    [SerializeField] private Button checkButton;
    [SerializeField] private Button crossButton;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private GameObject crossMark;

    /// <summary>Marca actual de la fila. Solo se modifica desde aqui.</summary>
    public RowMark Mark { get; private set; } = RowMark.None;

    private void Awake()
    {
      if (checkButton == null || crossButton == null || checkMark == null || crossMark == null)
      {
        Debug.LogError($"[ChecklistRow] Faltan referencias en {name}.", this);
        return;
      }

      // Conectamos los clics desde codigo: asi cada fila duplicada funciona sola.
      checkButton.onClick.AddListener(() => Click(RowMark.Check));
      crossButton.onClick.AddListener(() => Click(RowMark.Cross));
      Refresh();
    }

    /// <summary>Quita cualquier marca (se usa al llegar un residente nuevo).</summary>
    public void Clear()
    {
      Mark = RowMark.None;
      Refresh();
    }

    private void Click(RowMark clicked)
    {
      // Si ya estaba marcada esa casilla, se desmarca; si no, se marca.
      Mark = (Mark == clicked) ? RowMark.None : clicked;
      Refresh();
    }

    // Muestra solo la imagen de la marca actual.
    private void Refresh()
    {
      if (checkMark != null) checkMark.SetActive(Mark == RowMark.Check);
      if (crossMark != null) crossMark.SetActive(Mark == RowMark.Cross);
    }
  }
}