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
        private void OnValidate()
        {
            if (doubles != null && doubles.Count != ExpectedDoubleCount)
            {
                Debug.LogWarning(
                    $"[{name}] Se esperaban {ExpectedDoubleCount} dobles y hay {doubles.Count}.",
                    this);
            }
        }
    }
}