using System.Text;
using UnityEngine;
using WhoIsOutside.Data;

namespace WhoIsOutside.Verification
{
  /// <summary>
  /// Una llegada a la ventanilla: un residente real o un doble.
  /// Solo guarda datos; no es un componente de Unity.
  /// </summary>
  public class Arrival
  {
    public ResidentData Resident { get; }

    /// <summary>null = llega el residente real; si no, el doble que llega.</summary>
    public ResidentVariant Variant { get; }

    public Arrival(ResidentData resident, ResidentVariant variant)
    {
      Resident = resident;
      Variant = variant;
    }

    public bool IsImpostor => Variant != null;

    // Sprites ya resueltos (usan los metodos de ResidentData de la Tarea 2b).
    public Sprite VisitorSprite => Resident.GetVisitorSprite(Variant);
    public Sprite CredentialSprite => Resident.GetCredentialSprite(Variant);
    public Sprite RequestSprite => Resident.GetRequestSprite(Variant);

    public int DiscrepancyCount =>
        (IsImpostor && Variant.discrepancies != null) ? Variant.discrepancies.Count : 0;

    /// <summary>Texto para la Console: quien llega y, si es doble, que discrepancias tiene.</summary>
    public string Describe()
    {
      if (!IsImpostor)
        return $"{Resident.FullName} (REAL)";

      var builder = new StringBuilder();
      builder.Append(
          $"{Resident.FullName} (DOBLE {Variant.severity}, {DiscrepancyCount} discrepancia(s))");

      if (Variant.discrepancies != null)
      {
        foreach (Discrepancy discrepancy in Variant.discrepancies)
          builder.Append($"\n    - [{discrepancy.category}] {discrepancy.description}");
      }

      return builder.ToString();
    }
  }
}