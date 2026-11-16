using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Física de gravedad cero para objetos sueltos dentro de la estación.
    ///   - Sin gravedad: al soltarlo o lanzarlo sigue en línea recta a velocidad casi
    ///     constante (1ª ley de Newton) y gira sobre sí mismo.
    ///   - Rozamiento muy bajo con el aire de la cabina (en la ISS hay aire y ventilación:
    ///     los objetos se frenan muy despacio, en decenas de segundos).
    ///   - Rebota contra paredes y racks (coeficiente de restitución ~0.5) y conserva
    ///     el momento al chocar con otros objetos según su masa.
    /// Lo usan herramientas, piezas y objetos sueltos. Mientras se sujeta es cinemático.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ZeroGBody : MonoBehaviour
    {
        [Header("Física (valores aproximados a la ISS)")]
        [Tooltip("Masa en kg")]
        public float mass = 1.5f;
        [Tooltip("Frenado lineal por el aire de la cabina (1/s). 0.03 ≈ pierde la mitad de velocidad en ~23 s")]
        public float airDrag = 0.03f;
        [Tooltip("Frenado del giro (1/s)")]
        public float spinDrag = 0.05f;
        [Tooltip("Rebote: 0 = no rebota, 1 = rebote perfecto")]
        [Range(0f, 1f)] public float bounciness = 0.5f;
        [Range(0f, 1f)] public float friction = 0.25f;
        [Tooltip("Límite de velocidad al lanzar (m/s), para que no atraviese nada")]
        public float maxThrowSpeed = 8f;

        [Header("Si se pierde")]
        [Tooltip("Si se aleja más de esto de su posición inicial (p. ej. lanzado al espacio en la EVA), vuelve")]
        public float returnHomeDistance = 15f;
        /// <summary>Se alejó demasiado. Si nadie lo atiende, vuelve solo a su sitio inicial.</summary>
        public event System.Action OnLost;

        [Header("Inicio")]
        [Tooltip("Empieza quieto y cinemático (p. ej. herramientas en su soporte)")]
        public bool startFrozen = true;
        [Tooltip("Deriva inicial (m/s) para objetos que flotan desde el principio")]
        public Vector3 initialVelocity;
        public Vector3 initialSpin;

        public Rigidbody Body { get; private set; }
        private Vector3 _homePos;
        private Quaternion _homeRot;
        public bool Frozen { get; private set; }

        private static PhysicsMaterial s_material;
        private AudioSource _audio;
        private static AudioClip s_bump;
        private float _nextBump;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            _homePos = transform.position;
            _homeRot = transform.rotation;
            Body.useGravity = false;
            Body.mass = Mathf.Max(0.05f, mass);
            Body.linearDamping = airDrag;
            Body.angularDamping = spinDrag;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.maxAngularVelocity = 20f;

            var mat = new PhysicsMaterial("ZeroG")
            {
                bounciness = bounciness,
                dynamicFriction = friction,
                staticFriction = friction,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average
            };
            foreach (var c in GetComponentsInChildren<Collider>(true))
                if (!c.isTrigger) c.sharedMaterial = mat;

            if (startFrozen) Freeze();
            else
            {
                Body.isKinematic = false;
                Body.linearVelocity = initialVelocity;
                Body.angularVelocity = initialSpin;
            }
        }

        /// <summary>Se sujeta en la mano: deja de simularse.</summary>
        public void Hold()
        {
            Frozen = false;
            Body.isKinematic = true;
            Body.detectCollisions = false;
            // IMPORTANTE: un Rigidbody cinemático con interpolación no sigue a su padre (la mano):
            // se quedaba "suspendido" en el aire. Mientras se sujeta, sin interpolación.
            Body.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>Queda fijo (en su soporte, o desactivado).</summary>
        public void Freeze()
        {
            Frozen = true;
            Body.interpolation = RigidbodyInterpolation.None;
            if (!Body.isKinematic) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
            Body.isKinematic = true;
            Body.detectCollisions = true;
        }

        /// <summary>Se suelta con la velocidad de la mano (lanzamiento).</summary>
        public void Launch(Vector3 velocity, Vector3 angularVelocity)
        {
            if (Frozen) return;
            Body.detectCollisions = true;
            Body.isKinematic = false;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.linearVelocity = Vector3.ClampMagnitude(velocity, maxThrowSpeed);
            Body.angularVelocity = Vector3.ClampMagnitude(angularVelocity, 15f);
            Body.WakeUp();
        }

        private void FixedUpdate()
        {
            if (Body.isKinematic || returnHomeDistance <= 0f) return;
            if ((transform.position - _homePos).sqrMagnitude < returnHomeDistance * returnHomeDistance) return;
            if (OnLost != null) { OnLost(); return; }
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = _homePos;
            Body.rotation = _homeRot;
            transform.SetPositionAndRotation(_homePos, _homeRot);
        }

        private void OnCollisionEnter(Collision c)
        {
            // Golpe suave con volumen según la velocidad del impacto
            float v = c.relativeVelocity.magnitude;
            if (v < 0.25f || Time.time < _nextBump) return;
            _nextBump = Time.time + 0.08f;
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.spatialBlend = 1f;
                _audio.minDistance = 0.3f;
                _audio.maxDistance = 6f;
                _audio.playOnAwake = false;
            }
            if (s_bump == null) s_bump = MakeBump();
            _audio.pitch = Mathf.Lerp(1.3f, 0.8f, Mathf.Clamp01(mass / 5f));
            _audio.PlayOneShot(s_bump, Mathf.Clamp01(v / 3f));
        }

        private static AudioClip MakeBump()
        {
            const int rate = 22050;
            int n = rate / 6;
            var d = new float[n];
            var rnd = new System.Random(11);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.5f + ((float)rnd.NextDouble() * 2f - 1f) * 0.3f) * Mathf.Exp(-t * 35f);
            }
            var clip = AudioClip.Create("Bump", n, 1, rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
