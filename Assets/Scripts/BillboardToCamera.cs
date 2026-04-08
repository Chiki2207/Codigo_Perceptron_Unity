using UnityEngine;

namespace PerceptronSimulator
{
    /// <summary>
    /// Orienta TextMeshPro 3D hacia la cámara sin espejo ni boca abajo (LookAt + corrección TMP).
    /// </summary>
    public class BillboardToCamera : MonoBehaviour
    {
        public Camera cam;

        [Tooltip("Corrección local después de mirar a la cámara. Si el texto sigue al revés, prueba (180,0,0) o (0,0,180).")]
        public Vector3 extraEulerDegrees = new Vector3(0f, 180f, 0f);

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 toCamera = cam.transform.position - transform.position;
            if (toCamera.sqrMagnitude < 1e-10f)
                return;

            Vector3 forward = toCamera.normalized;
            // "Up" estable: preferimos el up de la cámara proyectado en el plano perpendicular al forward
            Vector3 up = cam.transform.up;
            Vector3 projectedUp = Vector3.ProjectOnPlane(up, forward);
            if (projectedUp.sqrMagnitude < 1e-6f)
                projectedUp = Vector3.up;
            else
                projectedUp.Normalize();

            Quaternion look = Quaternion.LookRotation(forward, projectedUp);
            transform.rotation = look * Quaternion.Euler(extraEulerDegrees);
        }
    }
}
