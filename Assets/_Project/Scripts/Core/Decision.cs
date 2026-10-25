namespace WhoIsOutside.Core
{
  /// <summary>Las tres decisiones que puede tomar el jugador.</summary>
  public enum Decision
  {
    Allow,      // Candado verde: dejar pasar
    Deny,       // Candado rojo: rechazar
    Emergency   // Boton de emergencia bajo el vidrio
  }
}