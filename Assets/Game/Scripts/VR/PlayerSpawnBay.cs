using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Punto de spawn interactivo / teleportador.
    /// Muestra un anillo holográfico pulsante y una plataforma de llegada.
    ///
    /// Uso:
    ///   1. Crear un GameObject vacío en la escena y añadir este componente.
    ///   2. Asignar un Transform hijo como spawnPoint (si no se asigna, usa este objeto).
    ///   3. Llamar a GetSpawnPosition() / GetSpawnRotation() al teletransportar al jugador.
    ///
    /// Portado de SpaceStation5.PlayerSpawnBay → namespace NovaCore.
    /// </summary>
    public class PlayerSpawnBay : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("Nombre descriptivo del punto (solo para debug/editor)")]
        public string sectorName = "Punto de observación principal";
        [Tooltip("Transform hijo que marca la posición exacta de spawn. Si no se asigna, usa este objeto.")]
        public Transform spawnPoint;

        [Header("Visuales")]
        public Light       beaconLight;
        public MeshRenderer holographicRing;
        public ParticleSystem arrivalParticles;

        [Header("Animación")]
        public float pulseSpeed    = 2.0f;
        public float rotationSpeed = 30.0f;

        private Material ringMaterial;
        private Color    baseEmissionColor;

        // ────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (spawnPoint == null) spawnPoint = transform;

            if (holographicRing != null)
            {
                ringMaterial = Application.isPlaying
                    ? holographicRing.material
                    : holographicRing.sharedMaterial;

                if (ringMaterial != null && ringMaterial.HasProperty("_EmissionColor"))
                    baseEmissionColor = ringMaterial.GetColor("_EmissionColor");
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Rotar y pulsar el anillo holográfico
            if (holographicRing != null)
            {
                holographicRing.transform.Rotate(Vector3.up * (rotationSpeed * dt));

                if (ringMaterial != null && ringMaterial.HasProperty("_EmissionColor"))
                {
                    float glow = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
                    ringMaterial.SetColor("_EmissionColor", baseEmissionColor * glow);
                }
            }

            // Pulsar la luz del beacon
            if (beaconLight != null)
                beaconLight.intensity = 1.8f + 0.6f * Mathf.Sin(Time.time * pulseSpeed);
        }

        // ────────────────────────────────────────────────────────────────────

        public Vector3    GetSpawnPosition() => spawnPoint != null ? spawnPoint.position    : transform.position + Vector3.up * 0.1f;
        public Quaternion GetSpawnRotation() => spawnPoint != null ? spawnPoint.rotation    : transform.rotation;

        /// <summary>
        /// Mueve el jugador a este spawn bay.
        /// Desactiva el CharacterController durante el teletransporte para evitar física residual.
        /// </summary>
        public void TeleportPlayer(Transform player)
        {
            if (player == null) return;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());

            if (cc != null) cc.enabled = true;

            if (arrivalParticles != null) arrivalParticles.Play();

            Debug.Log($"[PlayerSpawnBay] Jugador teletransportado a '{sectorName}'.");
        }

        // ────────────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.7f);
            Vector3 pos  = spawnPoint != null ? spawnPoint.position : transform.position;
            Gizmos.DrawWireSphere(pos + Vector3.up * 0.9f, 0.5f);
            Gizmos.DrawLine(pos, pos + Vector3.up * 1.8f);
            Gizmos.DrawRay(pos + Vector3.up * 1.6f, transform.forward * 1.2f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetSpawnPosition(), 0.3f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(GetSpawnPosition() + Vector3.up * 0.4f, sectorName);
#endif
        }
    }
}
