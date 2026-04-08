using UnityEngine;

namespace PerceptronSimulator
{
    /// <summary>
    /// Hace que este objeto mire siempre en la misma dirección que la cámara como un cartel
    /// paralelo a la pantalla. Se usa en la UI 3D del Canvas de las gráficas (error, pesos)
    /// para que RawImages se lean de frente aunque gires la vista.
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        [Tooltip("Si está vacío, se usa Camera.main.")]
        public Camera cam;

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            // Misma orientación que la cámara (no LookAt: el objeto no “apunta” al ojo, copia el forward)
            transform.forward = cam.transform.forward;
        }
    }
}
