using UnityEngine;

namespace NovaCore
{
    /// <summary>Hace que un cartel/sprite lejano (p. ej. la Luna) mire siempre a la cámara.</summary>
    public class FaceCamera : MonoBehaviour
    {
        [Tooltip("Mantiene la misma distancia y dirección respecto a la cámara (objetos 'en el cielo')")]
        public bool followCamera = true;

        private Vector3 _offset;
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
            if (_cam != null) _offset = transform.position - _cam.transform.position;
        }

        private void LateUpdate()
        {
            if (_cam == null || !_cam.isActiveAndEnabled) { _cam = Camera.main; if (_cam == null) return; }
            if (followCamera) transform.position = _cam.transform.position + _offset;
            Vector3 to = transform.position - _cam.transform.position;
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to, Vector3.up);
        }
    }
}
