using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoIsOutside.Data
{
  /// <summary>En qué capa de verificación se nota una discrepancia.</summary>
  public enum DiscrepancyCategory
  {
    Question,       // Una respuesta de control no coincide con los datos reales
    PhysicalTrait,  // Un rasgo físico no coincide con la ficha
    Sound,          // Sonido extraño
    Credential      // Un dato de la credencial no coincide
  }

  /// <summary>Nivel de anomalía del doble.</summary>
  public enum DoubleSeverity
  {
    Mild,       // Anomalía leve
    Moderate,   // Anomalía moderada
    Confirmed   // Anomalía confirmada
  }

  /// <summary>Pregunta de control con su respuesta correcta.</summary>
  [Serializable]
  public class ControlQuestion
  {
    [TextArea] public string question;
    public string correctAnswer;
  }

  /// <summary>
  /// Respuesta incorrecta que da un doble a una pregunta de control.
  /// questionIndex es la posición de la pregunta en ResidentData.ControlQuestions.
  /// </summary>
  [Serializable]
  public class AlteredAnswer
  {
    [Min(0)] public int questionIndex;
    public string wrongAnswer;
  }

  /// <summary>Descripción de una discrepancia.</summary>
  [Serializable]
  public class Discrepancy
  {
    public DiscrepancyCategory category;
    [TextArea] public string description;
  }

  /// <summary>Una de las variantes de doble de un residente.</summary>
  [Serializable]
  public class ResidentVariant
  {
    public DoubleSeverity severity;

    [Tooltip("Sprite de la persona en la ventanilla.")]
    public Sprite sprite;

    [Tooltip("Opcional: solo si el doble presenta una credencial distinta a la real.")]
    public Sprite credentialSpriteOverride;

    [Tooltip("Opcional: solo si el doble presenta una solicitud distinta a la real.")]
    public Sprite requestSpriteOverride;

    //public List<Discrepancy> discrepancies = new List<Discrepancy>();
    public List<AlteredAnswer> alteredAnswers = new List<AlteredAnswer>();

    /// <summary>
    /// Si este doble responde mal a la pregunta indicada, devuelve true
    /// y la respuesta incorrecta en 'answer'.
    /// </summary>
    public bool TryGetAlteredAnswer(int questionIndex, out string answer)
    {
      foreach (AlteredAnswer altered in alteredAnswers)
      {
        if (altered.questionIndex == questionIndex)
        {
          answer = altered.wrongAnswer;
          return true;
        }
      }

      answer = null;
      return false;
    }
  }
}