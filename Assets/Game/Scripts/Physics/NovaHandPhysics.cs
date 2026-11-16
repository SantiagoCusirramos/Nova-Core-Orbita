using System.Collections.Generic;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Cuerpo físico de la mano (VR): palma y punta del índice con colisión. Empuja los
    /// objetos sueltos (gravedad cero) con la velocidad real de la mano, como si los
    /// golpearas. Es un objeto aparte que sigue a la mano con MovePosition en FixedUpdate,
    /// así Unity calcula bien la velocidad del choque. No choca con el propio jugador.
    /// </summary>
    public class NovaHandPhysics : MonoBehaviour
    {
        /// <summary>Cuerpos de las manos (el láser y los agarres los ignoran).</summary>
        public static readonly HashSet<Rigidbody> HandBodies = new HashSet<Rigidbody>();

        public Transform hand;
        public Vector3 palmOffset = new Vector3(0f, -0.01f, -0.02f);
        public float palmRadius = 0.045f;
        public Vector3 fingertipOffset = new Vector3(0f, -0.025f, 0.085f);
        public float fingertipRadius = 0.018f;

        private Rigidbody _rb;

        public static NovaHandPhysics Create(Transform hand, Vector3 fingertip)
        {
            var go = new GameObject("Mano física · " + hand.name);
            var hp = go.AddComponent<NovaHandPhysics>();
            hp.hand = hand;
            hp.fingertipOffset = fingertip;
            return hp;
        }

        private void Start()
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.None;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            HandBodies.Add(_rb);

            AddSphere("Palma", palmOffset, palmRadius);
            AddSphere("Índice", fingertipOffset, fingertipRadius);

            // No chocar con la cápsula del jugador (las manos están dentro de ella a veces)
            var cc = hand != null ? hand.GetComponentInParent<CharacterController>() : null;
            if (cc != null)
                foreach (var c in GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(c, cc);

            if (hand != null) transform.SetPositionAndRotation(hand.position, hand.rotation);
        }

        private void AddSphere(string name, Vector3 offset, float r)
        {
            var c = new GameObject(name);
            c.transform.SetParent(transform, false);
            c.transform.localPosition = offset;
            var s = c.AddComponent<SphereCollider>();
            s.radius = r;
        }

        private void FixedUpdate()
        {
            if (hand == null) { Destroy(gameObject); return; }
            bool active = hand.gameObject.activeInHierarchy;
            if (_rb.detectCollisions != active) _rb.detectCollisions = active;
            _rb.MovePosition(hand.position);
            _rb.MoveRotation(hand.rotation);
        }

        private void OnDestroy() { if (_rb != null) HandBodies.Remove(_rb); }
    }
}
