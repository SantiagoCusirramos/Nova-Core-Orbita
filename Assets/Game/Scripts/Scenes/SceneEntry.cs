using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Va en el Player. Al cargar la escena, lo coloca en el PlayerSpawnBay cuyo spawnId
    /// coincide con el que pidió SceneTransit (p. ej. "from_nodo"). Si no hay ninguno,
    /// usa el punto por defecto (spawnId vacío) o se queda donde está.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneEntry : MonoBehaviour
    {
        private void Start()
        {
            string id = SceneTransit.ConsumeSpawn();
            var bays = FindObjectsByType<PlayerSpawnBay>();

            PlayerSpawnBay target = null, fallback = null;
            foreach (var b in bays)
            {
                if (b == null) continue;
                if (!string.IsNullOrEmpty(id) && b.spawnId == id) target = b;
                if (string.IsNullOrEmpty(b.spawnId)) fallback = b;
            }
            if (target == null) target = fallback;
            if (target == null) return;

            target.TeleportPlayer(transform);
            var fly = GetComponent<OrbitFreeFly>();
            if (fly != null) fly.StopMotion();
        }
    }
}
