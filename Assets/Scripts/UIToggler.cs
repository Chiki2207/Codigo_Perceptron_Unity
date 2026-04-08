using UnityEngine;
using UnityEngine.EventSystems;

namespace PerceptronSimulator
{
    /// <summary>
    /// Tab muestra/oculta el Canvas. Al ocultarlo, quita el foco de la UI para evitar
    /// NullReferenceException en InputField.GenerateCaret (parpadeo del cursor ~1 s).
    /// </summary>
    public class UIToggler : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            if (canvas == null) return;

            bool wasVisible = canvas.enabled;
            canvas.enabled = !canvas.enabled;

            if (wasVisible && !canvas.enabled)
                ClearUiSelection();
        }

        private void LateUpdate()
        {
            if (canvas == null || canvas.enabled) return;
            ClearIfSelectionUnderThisCanvas();
        }

        private void ClearIfSelectionUnderThisCanvas()
        {
            if (EventSystem.current == null) return;
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current == null) return;
            if (current.transform.IsChildOf(transform))
                ClearUiSelection();
        }

        private static void ClearUiSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
