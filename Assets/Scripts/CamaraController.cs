using UnityEngine;

namespace PerceptronSimulator
{
    /// <summary>
    /// Zoom con scroll del mouse, rotar mundo con click izquierdo y arrastrar,
    /// reset de cámara con tecla R. Órbita alrededor del centro de los puntos.
    /// </summary>
    public class CamaraController : MonoBehaviour
    {
        [Header("Objetivo")]
        public Transform target;

        [Header("Distancia y zoom")]
        public float distance = 15f;
        public float minDistance = 2f;
        public float maxDistance = 50f;
        public float zoomSpeed = 2f;

        [Header("Rotación órbita")]
        public float orbitSpeedX = 120f;
        public float orbitSpeedY = 80f;
        public float minY = -80f;
        public float maxY = 80f;

        [Header("Reset")]
        public KeyCode resetKey = KeyCode.R;
        public float resetDistance = 15f;
        public float resetAngleX = 20f;
        public float resetAngleY = 0f;

        private float _angleX;
        private float _angleY;

        private void Start()
        {
            if (target == null) target = transform.parent;
            Vector3 angles = transform.eulerAngles;
            _angleX = angles.y;
            _angleY = angles.x;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Input.GetKeyDown(resetKey))
            {
                distance = resetDistance;
                _angleX = resetAngleY;
                _angleY = resetAngleX;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                distance -= scroll * zoomSpeed * distance;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            if (Input.GetMouseButton(0))
            {
                _angleX += Input.GetAxis("Mouse X") * orbitSpeedX * 0.02f;
                _angleY -= Input.GetAxis("Mouse Y") * orbitSpeedY * 0.02f;
                _angleY = Mathf.Clamp(_angleY, minY, maxY);
            }

            Quaternion rot = Quaternion.Euler(_angleY, _angleX, 0);
            Vector3 offset = rot * new Vector3(0, 0, -distance);
            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }
}
