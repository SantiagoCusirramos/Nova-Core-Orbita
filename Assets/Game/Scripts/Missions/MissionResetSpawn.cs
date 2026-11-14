using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Al reiniciar la secuencia, devuelve al jugador al punto de spawn (por si estaba
    /// fuera de la estación cuando se cierra la esclusa).
    /// </summary>
    public class MissionResetSpawn : MonoBehaviour
    {
        public PlayerSpawnBay spawnBay;
        public Transform player;

        private void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset += HandleReset;
        }

        private void OnDestroy()
        {
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset -= HandleReset;
        }

        private void HandleReset()
        {
            if (spawnBay == null || player == null) return;
            spawnBay.TeleportPlayer(player);
            var fly = player.GetComponent<OrbitFreeFly>();
            if (fly != null) fly.StopMotion();
        }
    }
}
