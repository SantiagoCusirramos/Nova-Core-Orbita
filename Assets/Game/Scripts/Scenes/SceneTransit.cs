using UnityEngine;
using UnityEngine.SceneManagement;

namespace NovaCore
{
    /// <summary>
    /// Cambio de escena entre módulos de la ISS. Guarda a qué punto de llegada
    /// (PlayerSpawnBay.spawnId) debe aparecer el jugador y usa el ScreenFader si existe
    /// (fundido a negro: más cómodo en VR y oculta el tirón de carga).
    /// </summary>
    public static class SceneTransit
    {
        /// <summary>Punto de llegada pendiente para la próxima escena (lo consume SceneEntry).</summary>
        public static string PendingSpawn { get; private set; } = "";

        /// <summary>Escena desde la que venimos (informativo).</summary>
        public static string PreviousScene { get; private set; } = "";

        public static bool IsLoading { get; private set; }

        public static void Go(string sceneName, string spawnId)
        {
            if (IsLoading || string.IsNullOrEmpty(sceneName)) return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("[SceneTransit] La escena '" + sceneName + "' no está en Build Settings. " +
                                 "Ejecuta Nova Core → Configurar proyecto.");
                MissionHUD.Feedback("Falta la escena '" + sceneName + "' en Build Settings.", MissionHUD.Tone.Warning);
                return;
            }

            IsLoading = true;
            PendingSpawn = spawnId ?? "";
            PreviousScene = SceneManager.GetActiveScene().name;

            var fader = ScreenFader.Instance;
            if (fader != null) fader.FadeOutThen(() => Load(sceneName));
            else Load(sceneName);
        }

        private static void Load(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null) { IsLoading = false; return; }
            op.completed += _ => IsLoading = false;
        }

        /// <summary>Lo llama SceneEntry al usar el punto de llegada.</summary>
        public static string ConsumeSpawn()
        {
            string s = PendingSpawn;
            PendingSpawn = "";
            return s;
        }
    }
}
