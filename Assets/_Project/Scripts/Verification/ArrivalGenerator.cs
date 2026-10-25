using System.Collections.Generic;
using UnityEngine;
using WhoIsOutside.Data;

namespace WhoIsOutside.Verification
{
  /// <summary>
  /// Prepara las llegadas de una noche y las entrega de una en una.
  /// No conoce la UI ni el GameManager.
  /// </summary>
  public class ArrivalGenerator : MonoBehaviour
  {
    [SerializeField] private NightConfig nightConfig;

    [Header("Pruebas")]
    [Tooltip("Con esto activado, la misma semilla da siempre la misma noche.")]
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int seed = 12345;

    private readonly List<Arrival> arrivals = new List<Arrival>();
    private int nextIndex;

    public IReadOnlyList<Arrival> Arrivals => arrivals;

    /// <summary>La llegada que esta actualmente en la ventanilla (null si no hay).</summary>
    public Arrival Current { get; private set; }

    /// <summary>Cuantas llegadas faltan por entregar.</summary>
    public int Remaining => arrivals.Count - nextIndex;

    /// <summary>
    /// Arma la lista completa de llegadas de la noche.
    /// Devuelve false (y escribe un error en la Console) si la configuracion es invalida.
    /// </summary>
    public bool GenerateNight()
    {
      arrivals.Clear();
      nextIndex = 0;
      Current = null;

      if (!ValidateConfig()) return false;

      // System.Random permite usar semilla. Se usa el nombre completo
      // para no confundirlo con UnityEngine.Random.
      System.Random rng = useFixedSeed ? new System.Random(seed) : new System.Random();

      int total = nightConfig.ArrivalsCount;
      int impostors = Mathf.Min(nightConfig.ImpostorCount, total);

      // 1) Decide en que turnos llega un impostor (los demas son reales).
      var impostorSlots = new List<bool>(total);
      for (int i = 0; i < total; i++)
        impostorSlots.Add(i < impostors);
      Shuffle(impostorSlots, rng);

      // 2) Para cada turno elige un residente (bolsa barajada) y, si toca impostor, un doble.
      var bag = new List<ResidentData>();
      ResidentData previous = null;

      for (int i = 0; i < total; i++)
      {
        ResidentData resident = TakeFromBag(bag, previous, rng);
        previous = resident;

        ResidentVariant variant = null;
        if (impostorSlots[i])
        {
          List<ResidentVariant> validDoubles = GetValidDoubles(resident);
          variant = validDoubles[rng.Next(validDoubles.Count)];
        }

        arrivals.Add(new Arrival(resident, variant));
      }

      return true;
    }

    /// <summary>Entrega la siguiente llegada. Devuelve false si ya no quedan.</summary>
    public bool TryGetNext(out Arrival arrival)
    {
      if (nextIndex >= arrivals.Count)
      {
        arrival = null;
        Current = null;
        return false;
      }

      arrival = arrivals[nextIndex];
      nextIndex++;
      Current = arrival;
      return true;
    }

    // ---------------- Logica interna ----------------

    private bool ValidateConfig()
    {
      if (nightConfig == null)
      {
        Debug.LogError("[ArrivalGenerator] Asigna un NightConfig en el Inspector.", this);
        return false;
      }

      if (nightConfig.ExpectedResidents.Count == 0)
      {
        Debug.LogError("[ArrivalGenerator] El NightConfig no tiene residentes esperados.", this);
        return false;
      }

      if (nightConfig.ArrivalsCount <= 0)
      {
        Debug.LogError("[ArrivalGenerator] ArrivalsCount debe ser mayor que 0.", this);
        return false;
      }

      if (nightConfig.ImpostorCount > nightConfig.ArrivalsCount)
      {
        Debug.LogWarning(
            "[ArrivalGenerator] Hay mas impostores que llegadas; se usaran solo las llegadas disponibles.",
            this);
      }

      foreach (ResidentData resident in nightConfig.ExpectedResidents)
      {
        if (resident == null)
        {
          Debug.LogError("[ArrivalGenerator] Hay un residente vacio en la lista de esperados.", this);
          return false;
        }

        if (GetValidDoubles(resident).Count == 0)
        {
          Debug.LogError(
              $"[ArrivalGenerator] {resident.name} no tiene ningun doble valido " +
              "(necesita sprite y al menos una discrepancia).",
              this);
          return false;
        }
      }

      return true;
    }

    // Dobles que se pueden usar: tienen sprite y al menos una discrepancia.
    private static List<ResidentVariant> GetValidDoubles(ResidentData resident)
    {
      var valid = new List<ResidentVariant>();

      foreach (ResidentVariant variant in resident.Doubles)
      {
        bool hasSprite = variant != null && variant.sprite != null;
        bool hasDiscrepancy =
            variant != null && variant.discrepancies != null && variant.discrepancies.Count > 0;

        if (hasSprite && hasDiscrepancy)
          valid.Add(variant);
      }

      return valid;
    }

    // Saca un residente de la bolsa; si esta vacia, la rellena y la baraja.
    private ResidentData TakeFromBag(List<ResidentData> bag, ResidentData previous, System.Random rng)
    {
      if (bag.Count == 0)
      {
        bag.AddRange(nightConfig.ExpectedResidents);
        Shuffle(bag, rng);

        // Se saca del final: si el ultimo es el mismo que acaba de llegar,
        // se cambia por el primero para que no lleguen dos veces seguidas.
        int last = bag.Count - 1;
        if (bag.Count > 1 && bag[last] == previous)
        {
          ResidentData temp = bag[0];
          bag[0] = bag[last];
          bag[last] = temp;
        }
      }

      int lastIndex = bag.Count - 1;
      ResidentData resident = bag[lastIndex];
      bag.RemoveAt(lastIndex);
      return resident;
    }

    // Baraja una lista (algoritmo Fisher-Yates).
    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
      for (int i = list.Count - 1; i > 0; i--)
      {
        int j = rng.Next(i + 1);
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
      }
    }

    // ---------------- Pruebas desde el Inspector ----------------

    [ContextMenu("Debug/1 Generar noche y mostrar en Console")]
    private void DebugGenerateNight()
    {
      if (!GenerateNight()) return;

      int impostors = 0;
      for (int i = 0; i < arrivals.Count; i++)
      {
        Arrival arrival = arrivals[i];
        if (arrival.IsImpostor) impostors++;
        Debug.Log($"[ArrivalGenerator] Turno {i + 1}/{arrivals.Count}: {arrival.Describe()}", this);
      }

      Debug.Log(
          $"[ArrivalGenerator] RESUMEN: {arrivals.Count} llegadas, {impostors} impostor(es).", this);
    }

    [ContextMenu("Debug/2 Pedir siguiente llegada")]
    private void DebugNextArrival()
    {
      if (TryGetNext(out Arrival arrival))
      {
        Debug.Log(
            $"[ArrivalGenerator] Llega: {arrival.Describe()} | Quedan {Remaining}", this);
      }
      else
      {
        Debug.Log("[ArrivalGenerator] No quedan llegadas (genera la noche primero).", this);
      }
    }
  }
}