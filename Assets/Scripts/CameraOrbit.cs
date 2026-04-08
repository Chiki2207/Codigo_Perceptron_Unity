using UnityEngine;

namespace PerceptronSimulator
{
    /// <summary>
    /// Zoom con scroll y rotar mundo con click y arrastrar.
    /// </summary>
    public class CameraOrbit : MonoBehaviour
    {
        public Transform target;
        public float distance = 15f;
        public float minDistance = 0.75f;
        public float maxDistance = 250f;
        public float zoomSpeed = 2.5f;
        public float orbitSpeedX = 120f;
        public float orbitSpeedY = 80f;
        public float minY = -80f;
        public float maxY = 80f;

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
