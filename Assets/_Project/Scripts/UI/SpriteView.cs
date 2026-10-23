using UnityEngine;
using UnityEngine.UI;

namespace WhoIsOutside.UI
{
    /// <summary>
    /// Muestra un sprite en un Image. Sirve para la ventanilla, la credencial,
    /// la solicitud y la lista de esperados.
    /// </summary>
    public class SpriteView : MonoBehaviour
    {
        [SerializeField] private Image targetImage;

        // Unity llama a Reset cuando agregas el componente: intenta rellenar el campo solo.
        private void Reset()
        {
            targetImage = GetComponent<Image>();
        }

        /// <summary>Muestra el sprite. Con null, oculta la imagen.</summary>
        public void Show(Sprite sprite)
        {
            if (targetImage == null) return;

            targetImage.sprite = sprite;
            // Un Image sin sprite dibuja un rectangulo blanco, asi que se desactiva.
            targetImage.enabled = sprite != null;
        }

        public void Hide() => Show(null);
    }
}