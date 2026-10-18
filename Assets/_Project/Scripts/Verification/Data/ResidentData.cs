using System.Collections.Generic;
using UnityEngine;

namespace WhoIsOutside.Data
{
  /// <summary>
  /// Ficha de un residente: datos reales, aspecto, preguntas de control y sus dobles.
  /// Cada residente es un archivo .asset (ScriptableObject).
  /// </summary>
  [CreateAssetMenu(fileName = "Resident_", menuName = "WhoIsOutside/Resident Data", order = 0)]
  public class ResidentData : ScriptableObject
  {
    public const int ExpectedDoubleCount = 3;

    [Header("Identidad")]
    [SerializeField] private string residentId;
    [SerializeField] private string firstName;
    [SerializeField] private string lastName;
    [SerializeField, Min(0)] private int age;
    [SerializeField] private string birthDate;        // Texto, por ejemplo "12/03/1921"
    [SerializeField] private string apartment;        // Por ejemplo "F01-01"
    [SerializeField] private string credentialExpiry; // Caducidad de la credencial, texto

    [Header("Aspecto")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private List<string> mainTraits = new List<string>();
    [SerializeField] private List<string> secondaryTraits = new List<string>();
    [SerializeField, TextArea] private string behavior;

    [Header("Preguntas de control")]
    [SerializeField] private List<ControlQuestion> controlQuestions = new List<ControlQuestion>();

    [Header("Dobles (se esperan 3: leve, moderado, confirmado)")]
    [SerializeField] private List<ResidentVariant> doubles = new List<ResidentVariant>();

    // Propiedades de solo lectura: otros scripts leen, pero no modifican.
    public string ResidentId => residentId;
    public string FirstName => firstName;
    public string LastName => lastName;
    public string FullName => $"{firstName} {lastName}";
    public int Age => age;
    public string BirthDate => birthDate;
    public string Apartment => apartment;
    public string CredentialExpiry => credentialExpiry;
    public Sprite NormalSprite => normalSprite;
    public IReadOnlyList<string> MainTraits => mainTraits;
    public IReadOnlyList<string> SecondaryTraits => secondaryTraits;
    public string Behavior => behavior;
    public IReadOnlyList<ControlQuestion> ControlQuestions => controlQuestions;
    public IReadOnlyList<ResidentVariant> Doubles => doubles;

    // Unity llama a este método en el editor cuando cambia algo en el Inspector.
    // Unity llama a este método en el editor cuando cambias algo en el Inspector.
    private void OnValidate()
    {
      if (normalSprite == null)
        Debug.LogWarning($"[{name}] Falta el sprite normal.", this);

      if (doubles == null) return;

      if (doubles.Count != ExpectedDoubleCount)
      {
        Debug.LogWarning(
            $"[{name}] Se esperaban {ExpectedDoubleCount} dobles y hay {doubles.Count}.",
            this);
      }

      for (int i = 0; i < doubles.Count; i++)
      {
        ResidentVariant variant = doubles[i];
        if (variant == null) continue;

        // Se asume el orden del enum: 0 = Mild, 1 = Moderate, 2 = Confirmed.
        if (i < ExpectedDoubleCount && variant.severity != (DoubleSeverity)i)
        {
          Debug.LogWarning(
              $"[{name}] El doble #{i} deberia ser {(DoubleSeverity)i} pero es {variant.severity}.",
              this);
        }

        if (variant.sprite == null)
          Debug.LogWarning($"[{name}] El doble #{i} no tiene sprite.", this);

        if (variant.discrepancies == null || variant.discrepancies.Count == 0)
          Debug.LogWarning($"[{name}] El doble #{i} no tiene discrepancias.", this);

        if (variant.alteredAnswers == null) continue;

        foreach (AlteredAnswer altered in variant.alteredAnswers)
        {
          if (altered.questionIndex >= controlQuestions.Count)
          {
            Debug.LogWarning(
                $"[{name}] El doble #{i} tiene una respuesta alterada para la pregunta {altered.questionIndex}, que no existe.",
                this);
          }
        }
      }
    }
  }
}