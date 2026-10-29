namespace WhoIsOutside.Core
{
  /// <summary>
  /// Resultado de UN turno (una llegada).
  /// Se llama TurnResult y no "Resolution" porque UnityEngine ya tiene un tipo
  /// llamado Resolution y el compilador se confundiria.
  /// </summary>
  public enum TurnResult
  {
    Correct,         // El jugador acerto: el turno termina bien
    DefeatScreamer,  // Dejo pasar a un impostor: derrota con screamer
    DefeatFired      // Reporto a un residente real: derrota por despedida
  }

  /// <summary>
  /// Como termina la NOCHE completa.
  /// </summary>
  public enum NightEnding
  {
    Victory,
    DefeatScreamer,
    DefeatFired
  }

  /// <summary>
  /// Reglas de resolucion del juego. No depende de la escena ni de MonoBehaviour:
  /// solo recibe datos y devuelve un resultado.
  /// </summary>
  public static class ResolutionRules
  {
    /// <summary>
    /// Calcula el resultado de una decision FINAL.
    /// Devuelve false si la decision no resuelve el turno (Deny solo cierra la
    /// ventanilla; el turno se resuelve despues con Emergency).
    /// </summary>
    public static bool TryResolve(Decision decision, bool isImpostor, out TurnResult result)
    {
      switch (decision)
      {
        case Decision.Allow:
          // Dejar pasar: bien si era real, screamer si era impostor.
          result = isImpostor ? TurnResult.DefeatScreamer : TurnResult.Correct;
          return true;

        case Decision.Emergency:
          // Emergencia: bien si era impostor (la D.D.D. se lo lleva),
          // despedida si era un residente real.
          result = isImpostor ? TurnResult.Correct : TurnResult.DefeatFired;
          return true;

        default:
          // Decision.Deny: no resuelve el turno todavia.
          result = TurnResult.Correct;
          return false;
      }
    }

    /// <summary>True si el resultado termina la noche con derrota.</summary>
    public static bool IsDefeat(TurnResult result)
    {
      return result != TurnResult.Correct;
    }

    /// <summary>Convierte el ultimo resultado de turno en el final de la noche.</summary>
    public static NightEnding ToEnding(TurnResult lastResult)
    {
      switch (lastResult)
      {
        case TurnResult.DefeatScreamer: return NightEnding.DefeatScreamer;
        case TurnResult.DefeatFired:    return NightEnding.DefeatFired;
        default:                        return NightEnding.Victory;
      }
    }
  }
}