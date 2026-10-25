using System.Collections.Generic;
using UnityEngine;

namespace WhoIsOutside.Data
{
  /// <summary>
  /// Parametros de una noche: quien se espera, cuantas llegadas hay
  /// y cuantas de ellas son impostores. Es un archivo .asset.
  /// </summary>
  [CreateAssetMenu(fileName = "Night_", menuName = "WhoIsOutside/Night Config", order = 1)]
  public class NightConfig : ScriptableObject
  {
    [Header("Residentes esperados esta noche")]
    [SerializeField] private List<ResidentData> expectedResidents = new List<ResidentData>();

    [Header("Turnos")]
    [SerializeField, Min(1)] private int arrivalsCount = 6;
    [SerializeField, Min(0)] private int impostorCount = 2;

    [Header("Arte")]
    [Tooltip("Imagen de la lista de esperados pegada en la pared.")]
    [SerializeField] private Sprite expectedListSprite;

    public IReadOnlyList<ResidentData> ExpectedResidents => expectedResidents;
    public int ArrivalsCount => arrivalsCount;
    public int ImpostorCount => impostorCount;
    public Sprite ExpectedListSprite => expectedListSprite;

    // Unity llama a este metodo en el editor cuando cambias algo en el Inspector.
    private void OnValidate()
    {
      if (impostorCount > arrivalsCount)
      {
        Debug.LogWarning(
            $"[{name}] Hay mas impostores ({impostorCount}) que llegadas ({arrivalsCount}).",
            this);
      }
    }
  }
}