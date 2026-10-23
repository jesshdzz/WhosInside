using UnityEngine;
using UnityEngine.UI;

namespace WhoIsOutside.UI
{
    /// <summary>
    /// Muestra la lista de residentes del piso elegido (una imagen por piso).
    /// Los botones de piso viven en el mismo prefab.
    /// </summary>
    public class FloorRegistryView : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [Tooltip("Indice 0 = piso 1, indice 1 = piso 2, indice 2 = piso 3.")]
        [SerializeField] private Sprite[] floorSprites = new Sprite[3];
        [SerializeField] private Button[] floorButtons = new Button[3];

        private void Awake()
        {
            for (int i = 0; i < floorButtons.Length; i++)
            {
                if (floorButtons[i] == null) continue;

                // Copia local: sin ella, todos los botones usarian el mismo valor final de i.
                int floorIndex = i;
                floorButtons[i].onClick.AddListener(() => ShowFloor(floorIndex));
            }
        }

        private void Start()
        {
            ShowFloor(0); // Empieza en el piso 1
        }

        /// <summary>Muestra el piso indicado (0 = piso 1).</summary>
        public void ShowFloor(int floorIndex)
        {
            if (floorSprites == null || floorIndex < 0 || floorIndex >= floorSprites.Length)
            {
                Debug.LogWarning($"[FloorRegistryView] Piso invalido: {floorIndex}", this);
                return;
            }

            if (targetImage == null) return;

            Sprite sprite = floorSprites[floorIndex];
            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
        }
    }
}