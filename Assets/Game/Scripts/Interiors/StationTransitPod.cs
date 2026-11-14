using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// PORTADO de NASA-SPACE-APPS. Pod/ascensor que lleva al jugador entre dos puntos (opcional).
    /// Transit elevator / pod transporting the player between Hub and Ring or along Spine.
    /// </summary>
    public class StationTransitPod : MonoBehaviour
    {
        public Vector3 startPosition;
        public Vector3 endPosition;
        public float speed = 25f;
        public bool isMoving = false;
        public bool targetIsEnd = true;

        private void Start()
        {
            if (startPosition == Vector3.zero && endPosition == Vector3.zero)
            {
                startPosition = transform.position;
                endPosition = transform.position + transform.forward * 400f;
            }
        }

        private void Update()
        {
            if (!isMoving) return;

            Vector3 target = targetIsEnd ? endPosition : startPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                isMoving = false;
                targetIsEnd = !targetIsEnd;
            }
        }

        public void ActivatePod()
        {
            isMoving = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.transform.SetParent(transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.transform.SetParent(null);
            }
        }
    }
}
