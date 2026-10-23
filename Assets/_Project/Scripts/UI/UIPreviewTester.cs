using UnityEngine;
using WhoIsOutside.Data;

namespace WhoIsOutside.UI
{
    /// <summary>
    /// SOLO PARA PRUEBAS: muestra residentes en las vistas sin necesitar GameManager.
    /// Se borra en la Tarea 8.
    /// </summary>
    public class UIPreviewTester : MonoBehaviour
    {
        [SerializeField] private ResidentData[] residents;
        [SerializeField] private Sprite expectedListSprite;

        [SerializeField] private SpriteView window;
        [SerializeField] private SpriteView credential;
        [SerializeField] private SpriteView request;
        [SerializeField] private SpriteView expectedList;

        private int residentIndex;
        private int doubleIndex;

        [ContextMenu("Preview/Mostrar siguiente residente (normal)")]
        private void ShowNextNormal()
        {
            ResidentData resident = NextResident();
            if (resident == null) return;

            ShowVisitor(resident, null);
        }

        [ContextMenu("Preview/Mostrar siguiente residente (doble)")]
        private void ShowNextDouble()
        {
            ResidentData resident = NextResident();
            if (resident == null) return;

            if (resident.Doubles.Count == 0)
            {
                Debug.LogWarning($"[UIPreviewTester] {resident.name} no tiene dobles.", this);
                return;
            }

            ResidentVariant variant = resident.Doubles[doubleIndex % resident.Doubles.Count];
            doubleIndex++;

            Debug.Log($"[UIPreviewTester] Doble {variant.severity} de {resident.name}", this);
            ShowVisitor(resident, variant);
        }

        [ContextMenu("Preview/Mostrar lista de esperados")]
        private void ShowExpected()
        {
            if (expectedList != null) expectedList.Show(expectedListSprite);
        }

        [ContextMenu("Preview/Limpiar todo")]
        private void ClearAll()
        {
            if (window != null) window.Hide();
            if (credential != null) credential.Hide();
            if (request != null) request.Hide();
            if (expectedList != null) expectedList.Hide();
        }

        private ResidentData NextResident()
        {
            if (residents == null || residents.Length == 0)
            {
                Debug.LogWarning("[UIPreviewTester] Asigna los ResidentData en el Inspector.", this);
                return null;
            }

            ResidentData resident = residents[residentIndex % residents.Length];
            residentIndex++;
            return resident;
        }

        // 'variant' null significa "llega el residente real".
        private void ShowVisitor(ResidentData resident, ResidentVariant variant)
        {
            if (window != null) window.Show(resident.GetVisitorSprite(variant));
            if (credential != null) credential.Show(resident.GetCredentialSprite(variant));
            if (request != null) request.Show(resident.GetRequestSprite(variant));
        }
    }
}