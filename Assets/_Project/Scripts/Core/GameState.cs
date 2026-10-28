namespace WhoIsOutside.Core
{
  /// <summary>Fases del turno de una noche.</summary>
  public enum GameState
  {
    NotStarted,       // La noche aun no empieza
    ResidentArriving, // Se prepara y muestra al residente que llega
    Deciding,         // El jugador compara y decide
    Rejected,         // El jugador rechazo: ventanilla cerrada, solo falta usar Emergencia
    ShowingResult,    // Se muestra el resultado del turno
    NightOver         // La noche termino (victoria o derrota)
  }
}