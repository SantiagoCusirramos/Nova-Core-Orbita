using System.Collections.Generic;
using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Puntuación de la partida (gamificación). Va en el MissionManager persistente, así
    /// sobrevive a los cambios de módulo.
    ///   +100 por objetivo, +hasta 100 de bonus si se hace rápido (baja en 'bonusWindow' s),
    ///   -25 si hizo falta la pista automática, +50 por cada insignia escondida.
    /// Al terminar da un rango (S, A, B, C) que muestran el HUD y la tablet.
    /// </summary>
    public class MissionScore : MonoBehaviour
    {
        public static MissionScore Instance { get; private set; }

        public int pointsPerObjective = 100;
        public int maxTimeBonus = 100;
        [Tooltip("Segundos en los que el bonus de rapidez llega a 0")]
        public float bonusWindow = 120f;
        public int hintPenalty = 25;
        public int pointsPerPatch = 50;
        [Tooltip("Insignias escondidas en total (una por módulo)")]
        public int totalPatches = 5;

        public int Score { get; private set; }
        public int PatchesFound => _patches.Count;
        public int HintsUsed { get; private set; }
        public float ElapsedSeconds => _running ? Time.time - _startTime : _finalTime;

        public event System.Action OnScoreChanged;

        private readonly HashSet<string> _patches = new HashSet<string>();
        private float _objectiveStart, _startTime, _finalTime;
        private bool _hintThisObjective, _running, _bound;

        private void Awake()
        {
            // Solo vale el del MissionManager vivo (los duplicados se destruyen con él)
            var mm = GetComponent<MissionManager>();
            if (mm != null && MissionManager.Instance != null && MissionManager.Instance != mm) { enabled = false; return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (_bound || Instance != this) return;
            var m = MissionManager.Instance;
            if (m == null) return;
            _bound = true;
            m.OnMissionStarted += HandleStart;
            m.OnActionCompleted += HandleAction;
            m.OnMissionCompleted += HandleCompleted;
            m.OnMissionReset += HandleReset;
        }

        private void HandleStart()
        {
            if (_running) return;   // la segunda misión también lanza "started"
            _running = true;
            _startTime = _objectiveStart = Time.time;
        }

        private void HandleAction(MissionAction a)
        {
            if (!_running) { _running = true; _startTime = Time.time; }
            float t = Time.time - _objectiveStart;
            int bonus = Mathf.RoundToInt(maxTimeBonus * Mathf.Clamp01(1f - t / bonusWindow));
            int gained = pointsPerObjective + bonus - (_hintThisObjective ? hintPenalty : 0);
            Score += Mathf.Max(10, gained);
            _objectiveStart = Time.time;
            _hintThisObjective = false;
            OnScoreChanged?.Invoke();
            if (bonus >= maxTimeBonus * 0.6f) MissionHUD.Feedback("+" + gained + "  ¡Rápido!", MissionHUD.Tone.Success, 1.6f);
        }

        private void HandleCompleted()
        {
            var m = MissionManager.Instance;
            if (m == null || m.CurrentState != MissionState.Completed) return;
            _running = false;
            _finalTime = Time.time - _startTime;
            OnScoreChanged?.Invoke();
            MissionHUD.Feedback("ESTACIÓN ESTABILIZADA\nPuntos: " + Score + "   Rango: " + Rank()
                                + "\nInsignias: " + PatchesFound + "/" + totalPatches, MissionHUD.Tone.Success, 12f);
        }

        private void HandleReset()
        {
            Score = 0; HintsUsed = 0; _patches.Clear(); _running = false; _hintThisObjective = false;
            OnScoreChanged?.Invoke();
        }

        /// <summary>La guía avisa cuando tuvo que dar una pista automática.</summary>
        public void NotifyHint()
        {
            if (_hintThisObjective) return;
            _hintThisObjective = true;
            HintsUsed++;
        }

        public bool HasPatch(string id) => _patches.Contains(id);

        public bool CollectPatch(string id)
        {
            if (string.IsNullOrEmpty(id) || !_patches.Add(id)) return false;
            Score += pointsPerPatch;
            OnScoreChanged?.Invoke();
            return true;
        }

        public string Rank()
        {
            int max = 16 * (pointsPerObjective + maxTimeBonus) + totalPatches * pointsPerPatch;
            float p = max > 0 ? (float)Score / max : 0f;
            return p >= 0.8f ? "S" : p >= 0.62f ? "A" : p >= 0.45f ? "B" : "C";
        }
    }
}
