using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NovaCore
{
    public enum RepairStationType
    {
        PistolGrip_WelderAndDrill,
        Hammer_ArmorStraightening,
        Ratchet_FlangeBolts,
        GreaseGun_PressureSealant
    }

    /// <summary>
    /// Mesa de reparación EVA — PORTADA de NASA-SPACE-APPS (Assets/Herramientas/Scripts/SpaceRepairStation.cs).
    ///
    /// Cambios respecto al original:
    ///   - Sin XR Interaction Toolkit: la herramienta es un SpaceTool (NovaInteractable) y
    ///     llama a TryApply* con el rayo del interactor (mando VR o centro de pantalla).
    ///   - Sin TextMeshPro: los textos son uGUI Text (no requiere TMP Essential Resources).
    ///   - Enlace con el Core de misiones: si linkToMission está activo, la mesa solo
    ///     progresa cuando missionAction es el objetivo actual, y al terminar la completa
    ///     (p. ej. SolarPanelRepaired, AirlockRepressurized).
    ///   - Caracteres de las barras/estado en ASCII (se ven con cualquier fuente).
    /// </summary>
    public class SpaceRepairStation : MonoBehaviour, IMissionTarget
    {
        public static readonly List<SpaceRepairStation> AllStations = new List<SpaceRepairStation>();

        [Header("Station Configuration")]
        public RepairStationType stationType = RepairStationType.PistolGrip_WelderAndDrill;
        public string stationTitle = "MESA DE REPARACIÓN EVA";
        public SpaceToolType requiredTool = SpaceToolType.PistolGripTool;
        public Color stationAccentColor = new Color(0.2f, 0.8f, 1.0f);

        [Header("Misión (Nova Core)")]
        [Tooltip("Si está activo, solo progresa cuando missionAction es el objetivo actual y al terminar lo completa")]
        public bool linkToMission = false;
        public MissionAction missionAction = MissionAction.SolarPanelRepaired;

        [Tooltip("Último aviso de alineación del taladro (lo muestra el panel)")]
        public string lastFeedback = "";
        private bool missionReported = false;
        private float nextGateWarn = 0f;

        [Header("Tool Docking Cradle")]
        public Transform toolDockTransform;
        public SpaceTool dockedTool;

        [Header("Holographic UI Panel")]
        public Text uiTitleText;
        public Text uiModeText;
        public Text uiStatusText;
        public Text uiProgressBarText;
        public MeshRenderer uiProgressFillRenderer;
        public GameObject uiHoloPanel;

        [Header("Audio Feedback")]
        public AudioSource stationAudioSource;
        public AudioClip sfxTaskComplete;
        public AudioClip sfxAlarmLeak;
        public AudioClip sfxMetallicClang;

        [Header("Common Interactive State")]
        public bool isTask1Complete = false;
        public bool isTask2Complete = false;
        public float task1Progress = 0f;
        public float task2Progress = 0f;

        [Header("1. PGT: Welder & Screw Elements")]
        public Transform weldLeftPlate;
        public Transform weldRightPlate;
        public MeshRenderer weldSeamRenderer;
        public ParticleSystem weldSparksParticles;
        public Transform precisionScrewTransform;
        public MeshRenderer screwIndicatorRenderer;
        private Vector3 screwInitialLocalPos;
        private float screwCurrentDepth = 0f;
        private const float ScrewMaxDepth = 0.085f;

        [Header("2. Hammer: Bent Armor Plate Elements")]
        public Transform bentArmorPlateTransform;
        public ParticleSystem hammerImpactSparks;
        private float armorBentAngle = 36f;
        private int hammerStrikesCount = 0;
        private const int HammerStrikesNeeded = 3;

        [Header("3. Ratchet: Pipe Flange Hex Bolts")]
        public Transform[] flangeBolts;
        public int[] boltTurnsRemaining;
        public MeshRenderer pipeFlowGlowRenderer;
        private const int TurnsPerBolt = 3;

        [Header("4. Grease Gun: Thermal Sealant & Pressure Leak")]
        public ParticleSystem pressureLeakParticles;
        public AudioSource leakAudioSource;
        public Transform foamSealantPlug;
        public MeshRenderer pressureGaugeBarRenderer;
        public float currentChamberPressure = 0.15f; // Starts depressurized at 0.15 bar, target 1.0 bar nominal

        private MaterialPropertyBlock propBlock;
        private float alertBlinkTimer = 0f;

        private void Awake()
        {
            if (!AllStations.Contains(this))
            {
                AllStations.Add(this);
            }
            propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()  { if (linkToMission) MissionGate.Register(this); }
        private void OnDisable() { MissionGate.Unregister(this); }

        private void OnDestroy()
        {
            AllStations.Remove(this);
            var m = MissionManager.Instance;
            if (m != null) m.OnMissionReset -= ResetStation;
        }

        private void Start()
        {
            InitializeStationElements();
            ResetStation();
            var m = MissionManager.Instance;
            if (m != null && linkToMission) m.OnMissionReset += ResetStation;

            // Venimos de otra escena con la reparación ya hecha
            if (linkToMission && MissionGate.IsDone(missionAction)) ForceCompleted();
        }

        /// <summary>Deja la mesa en estado "reparada" (sin sonidos ni aviso a la misión).</summary>
        public void ForceCompleted()
        {
            isTask1Complete = true;
            isTask2Complete = true;
            task1Progress = 1f;
            task2Progress = 1f;
            missionReported = true;
            hammerStrikesCount = HammerStrikesNeeded;
            currentChamberPressure = 1.01f;

            if (weldLeftPlate != null && weldRightPlate != null)
            {
                weldLeftPlate.localPosition = new Vector3(-0.001f, weldLeftPlate.localPosition.y, weldLeftPlate.localPosition.z);
                weldRightPlate.localPosition = new Vector3(0.001f, weldRightPlate.localPosition.y, weldRightPlate.localPosition.z);
            }
            if (precisionScrewTransform != null)
            {
                screwCurrentDepth = ScrewMaxDepth;
                precisionScrewTransform.localPosition = screwInitialLocalPos + Vector3.forward * ScrewMaxDepth;
            }
            if (bentArmorPlateTransform != null) bentArmorPlateTransform.localRotation = Quaternion.identity;
            if (boltTurnsRemaining != null) for (int i = 0; i < boltTurnsRemaining.Length; i++) boltTurnsRemaining[i] = 0;
            if (foamSealantPlug != null) foamSealantPlug.localScale = new Vector3(0.18f, 0.06f, 0.18f);
            if (pressureLeakParticles != null) pressureLeakParticles.Stop();
            if (weldSparksParticles != null) weldSparksParticles.Stop();
        }

        // ── IMissionTarget ───────────────────────────────────────────────────
        public MissionAction TargetAction => missionAction;
        public Vector3 MarkerPosition => transform.position + transform.up * 0.6f;
        public bool IsTargetAvailable => linkToMission && isActiveAndEnabled && !IsFullyRepaired;

        /// <summary>¿Están completas todas las tareas de esta mesa?</summary>
        public bool IsFullyRepaired =>
            isTask1Complete && (stationType == RepairStationType.Hammer_ArmorStraightening ||
                                stationType == RepairStationType.Ratchet_FlangeBolts ||
                                stationType == RepairStationType.GreaseGun_PressureSealant || isTask2Complete);

        /// <summary>Si la mesa está enlazada a la misión, ¿se puede trabajar ahora?</summary>
        public bool IsMissionGateOpen()
        {
            if (!linkToMission) return true;
            if (MissionGate.IsCurrent(missionAction)) return true;
            if (Time.unscaledTime >= nextGateWarn)
            {
                MissionGate.TryComplete(missionAction);   // muestra qué toca ahora
                nextGateWarn = Time.unscaledTime + 2f;
            }
            return false;
        }

        private void CheckMissionCompletion()
        {
            if (!linkToMission || missionReported || !IsFullyRepaired) return;
            if (MissionGate.TryComplete(missionAction)) missionReported = true;
        }

        private void InitializeStationElements()
        {
            if (precisionScrewTransform != null)
            {
                screwInitialLocalPos = precisionScrewTransform.localPosition;
            }

            if (stationAudioSource == null)
            {
                stationAudioSource = gameObject.AddComponent<AudioSource>();
                stationAudioSource.spatialBlend = 1.0f;
                stationAudioSource.minDistance = 1.5f;
                stationAudioSource.maxDistance = 12f;
                stationAudioSource.playOnAwake = false;
            }

            // Create procedural audio clips if not assigned
            if (sfxTaskComplete == null)
            {
                sfxTaskComplete = CreateProceduralChime();
            }
            if (sfxMetallicClang == null)
            {
                sfxMetallicClang = CreateProceduralClang();
            }
        }

        private void Update()
        {
            UpdateHolographicDisplay();

            // Leak audio volume tied to pressure if grease gun station
            if (stationType == RepairStationType.GreaseGun_PressureSealant && leakAudioSource != null)
            {
                if (!isTask1Complete)
                {
                    leakAudioSource.volume = Mathf.Lerp(0.8f, 0.0f, task1Progress);
                    if (!leakAudioSource.isPlaying) leakAudioSource.Play();
                }
                else
                {
                    leakAudioSource.volume = 0f;
                    if (leakAudioSource.isPlaying) leakAudioSource.Stop();
                }
            }
        }

        #region Holographic UI Display

        private void UpdateHolographicDisplay()
        {
            alertBlinkTimer += Time.deltaTime * 3.5f;
            bool blink = Mathf.Sin(alertBlinkTimer) > 0f;

            if (uiTitleText != null)
            {
                uiTitleText.text = stationTitle;
            }

            if (uiStatusText != null)
            {
                bool allDone = IsFullyRepaired;
                if (allDone)
                {
                    uiStatusText.text = "<color=#00FF88>* ESTADO: REPARACIÓN COMPLETADA (SISTEMA NOMINAL)</color>";
                }
                else
                {
                    string alertColor = blink ? "#FFCC00" : "#FF5533";
                    uiStatusText.text = $"<color={alertColor}>! ATENCIÓN: AVERÍA ACTIVA - REQUIERE {SpaceTool.DisplayName(requiredTool)}</color>";
                    if (!string.IsNullOrEmpty(lastFeedback)) uiStatusText.text += "\n" + lastFeedback;
                }
            }

            if (uiModeText != null)
            {
                switch (stationType)
                {
                    case RepairStationType.PistolGrip_WelderAndDrill:
                        uiModeText.text = isTask1Complete
                            ? "<color=#00FFCC>[MODO 2 ACTIVO]: ATORNILLAR PERNO (ALINEA EL TALADRO RECTO)</color>"
                            : "<color=#FF9933>[MODO 1 ACTIVO]: SOLDADOR MOLECULAR (UNIR PLACAS)</color>";
                        break;
                    case RepairStationType.Hammer_ArmorStraightening:
                        uiModeText.text = isTask1Complete
                            ? "<color=#00FF88>[BLINDAJE RECTIFICADO]: TOLERANCIA ESTÁNDAR 100%</color>"
                            : $"<color=#FF5533>[MODO IMPACTO]: GOLPEA LA PLACA DEFORMADA ({hammerStrikesCount}/{HammerStrikesNeeded})</color>";
                        break;
                    case RepairStationType.Ratchet_FlangeBolts:
                        int remaining = 0;
                        if (boltTurnsRemaining != null)
                        {
                            foreach (int t in boltTurnsRemaining) remaining += t;
                        }
                        uiModeText.text = remaining == 0
                            ? "<color=#00FF88>[BRIDA SELLADA]: FLUJO DE REFRIGERANTE NOMINAL</color>"
                            : $"<color=#00E5FF>[MODO APRIETE]: AJUSTA LOS PERNOS DE LA BRIDA ({remaining} VUELTAS RESTANTES)</color>";
                        break;
                    case RepairStationType.GreaseGun_PressureSealant:
                        uiModeText.text = isTask1Complete
                            ? "<color=#00FF88>[CÁMARA ESTABILIZADA]: PRESIÓN 1.01 bar (HERMÉTICO)</color>"
                            : $"<color=#FF3322>[FUGA DE PRESIÓN]: INYECTA SELLADOR TÉRMICO ({Mathf.RoundToInt(currentChamberPressure * 100f)}% / 100%)</color>";
                        break;
                }
            }

            if (uiProgressBarText != null)
            {
                float overallProgress = GetOverallProgress();
                int barCount = Mathf.RoundToInt(overallProgress * 20);
                string bar = new string('|', barCount) + new string('.', 20 - barCount);
                uiProgressBarText.text = $"PROGRESO: [{bar}] {Mathf.RoundToInt(overallProgress * 100f)}%";
            }

            if (uiProgressFillRenderer != null)
            {
                float overallProgress = GetOverallProgress();
                uiProgressFillRenderer.transform.localScale =
                    new Vector3(
                        Mathf.Max(0.01f, Mathf.Clamp01(overallProgress)),
                        1f,
                        1f
                    );
                if (propBlock != null)
                {
                    Color fillCol = Color.Lerp(Color.red, Color.green, overallProgress);
                    propBlock.SetColor("_BaseColor", fillCol);
                    propBlock.SetColor("_EmissionColor", fillCol * 1.5f);
                    uiProgressFillRenderer.SetPropertyBlock(propBlock);
                }
            }
        }

        public float GetOverallProgress()
        {
            switch (stationType)
            {
                case RepairStationType.PistolGrip_WelderAndDrill:
                    return (task1Progress + task2Progress) * 0.5f;
                case RepairStationType.Hammer_ArmorStraightening:
                    return task1Progress;
                case RepairStationType.Ratchet_FlangeBolts:
                    return task1Progress;
                case RepairStationType.GreaseGun_PressureSealant:
                    return task1Progress;
                default:
                    return 0f;
            }
        }

        #endregion

        #region Interactive Repair Methods

        // ==========================================
        // 1. PISTOL GRIP TOOL (Welder & Screwdriver)
        // ==========================================

        /// <summary>
        /// Mode 1: Molecular Welder - melts and joins separated hull plates along the fracture line.
        /// </summary>
        public bool TryApplyWelder(Vector3 hitPoint, Vector3 toolForward, float deltaTime)
        {
            if (isTask1Complete) return false;
            if (!IsMissionGateOpen()) return false;

            task1Progress = Mathf.Clamp01(task1Progress + deltaTime * 0.42f); // ~2.4 seconds to fully weld

            // Visual feedback: animate plates moving closer together
            if (weldLeftPlate != null && weldRightPlate != null)
            {
                float gap = Mathf.Lerp(0.045f, 0.001f, task1Progress);
                weldLeftPlate.localPosition = new Vector3(-gap, weldLeftPlate.localPosition.y, weldLeftPlate.localPosition.z);
                weldRightPlate.localPosition = new Vector3(gap, weldRightPlate.localPosition.y, weldRightPlate.localPosition.z);
            }

            // Seam glowing thermal effect
            if (weldSeamRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                Color weldColor = Color.Lerp(new Color(1f, 0.4f, 0.1f), new Color(0.2f, 0.8f, 1.0f), task1Progress);
                propBlock.SetColor("_BaseColor", weldColor);
                propBlock.SetColor("_EmissionColor", weldColor * (3.5f * (1f - task1Progress) + 0.5f));
                weldSeamRenderer.SetPropertyBlock(propBlock);
            }

            if (weldSparksParticles != null && !weldSparksParticles.isPlaying)
            {
                weldSparksParticles.Play();
            }

            if (task1Progress >= 1.0f && !isTask1Complete)
            {
                isTask1Complete = true;
                if (weldSparksParticles != null) weldSparksParticles.Stop();
                PlaySound(sfxTaskComplete, 1.0f, 1.1f);
                Debug.Log("[SpaceRepairStation] ¡Mesa 1: Placas soldadas con éxito!");
                CheckMissionCompletion();
            }

            return true;
        }

        /// <summary>
        /// Mode 2: Precision Alignment Screwdriver - Player must keep tool straight facing the screw (angle < 15 deg)
        /// while spinning until nominal torque depth is achieved.
        /// </summary>
        public bool TryApplyDrill(Vector3 hitPoint, Vector3 toolForward, float deltaTime, out string alignmentFeedback)
        {
            alignmentFeedback = "";

            if (!isTask1Complete)
            {
                alignmentFeedback = "ADVERTENCIA: PRIMERO DEBES COMPLETAR LA SOLDADURA (MODO 1)";
                return false;
            }

            if (isTask2Complete)
            {
                alignmentFeedback = "TORNILLO YA BLOQUEADO A TORQUE NOMINAL (65 Nm)";
                return false;
            }

            if (precisionScrewTransform == null) return false;
            if (!IsMissionGateOpen()) return false;

            // Alignment check: Tool forward must point almost parallel to screw penetration axis (-forward)
            Vector3 screwHoleDirection = -precisionScrewTransform.forward;
            float alignmentAngle = Vector3.Angle(toolForward, screwHoleDirection);

            // Distance check to screw head
            float dist = Vector3.Distance(hitPoint, precisionScrewTransform.position);

            if (alignmentAngle > 18f)
            {
                alignmentFeedback = $"<color=#FF4422>DESALINEADO ({Mathf.RoundToInt(alignmentAngle)}°): MANTÉN EL TALADRO RECTO FRENTE AL PERNO</color>";
                if (screwIndicatorRenderer != null && propBlock != null)
                {
                    propBlock.SetColor("_BaseColor", Color.red);
                    propBlock.SetColor("_EmissionColor", Color.red * 2f);
                    screwIndicatorRenderer.SetPropertyBlock(propBlock);
                }
                return false;
            }

            // Correctly aligned! Thread screw in and spin
            alignmentFeedback = $"<color=#00FF88>ALINEACIÓN CORRECTA ({Mathf.RoundToInt(alignmentAngle)}°): ATORNILLANDO...</color>";

            precisionScrewTransform.Rotate(Vector3.forward * (720f * deltaTime), Space.Self);
            screwCurrentDepth = Mathf.Clamp(screwCurrentDepth + deltaTime * 0.035f, 0f, ScrewMaxDepth);
            precisionScrewTransform.localPosition = screwInitialLocalPos + Vector3.forward * screwCurrentDepth;

            task2Progress = screwCurrentDepth / ScrewMaxDepth;

            if (screwIndicatorRenderer != null && propBlock != null)
            {
                Color okCol = Color.Lerp(Color.yellow, Color.cyan, task2Progress);
                propBlock.SetColor("_BaseColor", okCol);
                propBlock.SetColor("_EmissionColor", okCol * 2.5f);
                screwIndicatorRenderer.SetPropertyBlock(propBlock);
            }

            if (task2Progress >= 1.0f && !isTask2Complete)
            {
                isTask2Complete = true;
                PlaySound(sfxTaskComplete, 1.0f, 1.25f);
                alignmentFeedback = "<color=#00FF88>* TORQUE NOMINAL ALCANZADO (65 Nm) - PERNO BLOQUEADO</color>";
                Debug.Log("[SpaceRepairStation] ¡Mesa 1: Perno atornillado y fijado a 65 Nm!");
                CheckMissionCompletion();
            }

            return true;
        }

        // ==========================================
        // 2. HAMMER (Kinetic Armor Straightening)
        // ==========================================

        /// <summary>
        /// Mode 1: Kinetic Percussion - strike the bent micrometeorite armor plate with the EVA Hammer
        /// to flatten it back to nominal curvature tolerance.
        /// </summary>
        public bool TryApplyHammer(Vector3 hitPoint, float strikeForce)
        {
            if (isTask1Complete) return false;
            if (!IsMissionGateOpen()) return false;

            hammerStrikesCount++;
            task1Progress = Mathf.Clamp01((float)hammerStrikesCount / HammerStrikesNeeded);

            // Animate bending plate downwards
            float remainingAngle = Mathf.Lerp(armorBentAngle, 0f, task1Progress);
            if (bentArmorPlateTransform != null)
            {
                bentArmorPlateTransform.localRotation = Quaternion.Euler(remainingAngle, 0f, 0f);
            }

            if (hammerImpactSparks != null)
            {
                hammerImpactSparks.transform.position = hitPoint;
                hammerImpactSparks.Play();
            }

            PlaySound(sfxMetallicClang, 1.0f, Random.Range(0.95f, 1.08f));

            if (hammerStrikesCount >= HammerStrikesNeeded)
            {
                isTask1Complete = true;
                PlaySound(sfxTaskComplete, 1.0f, 1.0f);
                Debug.Log("[SpaceRepairStation] ¡Mesa 2: Placa de blindaje rectificada!");
                CheckMissionCompletion();
            }

            return true;
        }

        // ==========================================
        // 3. RATCHET (Flange Hex Bolts Tightening)
        // ==========================================

        /// <summary>
        /// Mode 1: Mechanical Ratchet - tighten each hex bolt around the pipe flange until fully seated.
        /// </summary>
        public bool TryApplyRatchet(Transform hitTransform, int directionSign)
        {
            if (isTask1Complete || flangeBolts == null || flangeBolts.Length == 0) return false;
            if (!IsMissionGateOpen()) return false;

            int targetIndex = -1;
            for (int i = 0; i < flangeBolts.Length; i++)
            {
                if (flangeBolts[i] == hitTransform || hitTransform.IsChildOf(flangeBolts[i]))
                {
                    targetIndex = i;
                    break;
                }
            }

            // Fallback: pick the closest bolt that needs turns
            if (targetIndex == -1)
            {
                float minDist = 999f;
                for (int i = 0; i < flangeBolts.Length; i++)
                {
                    if (flangeBolts[i] != null && boltTurnsRemaining != null && i < boltTurnsRemaining.Length && boltTurnsRemaining[i] > 0)
                    {
                        float d = Vector3.Distance(hitTransform.position, flangeBolts[i].position);
                        if (d < minDist)
                        {
                            minDist = d;
                            targetIndex = i;
                        }
                    }
                }
            }

            if (boltTurnsRemaining != null && targetIndex >= 0 && targetIndex < boltTurnsRemaining.Length)
            {
                if (directionSign > 0) // Tighten (CW)
                {
                    if (boltTurnsRemaining[targetIndex] > 0)
                    {
                        boltTurnsRemaining[targetIndex]--;
                        // Rotate and sink bolt slightly
                        flangeBolts[targetIndex].Rotate(Vector3.up * 60f, Space.Self);
                        flangeBolts[targetIndex].localPosition -= Vector3.up * 0.012f;
                    }
                }
                else // Loosen (CCW)
                {
                    if (boltTurnsRemaining[targetIndex] < TurnsPerBolt)
                    {
                        boltTurnsRemaining[targetIndex]++;
                        flangeBolts[targetIndex].Rotate(-Vector3.up * 60f, Space.Self);
                        flangeBolts[targetIndex].localPosition += Vector3.up * 0.012f;
                    }
                }

                // Compute total progress
                int totalTurnsNeeded = flangeBolts.Length * TurnsPerBolt;
                int remainingTurns = 0;
                foreach (int t in boltTurnsRemaining) remainingTurns += t;
                task1Progress = 1.0f - ((float)remainingTurns / totalTurnsNeeded);

                if (remainingTurns == 0 && !isTask1Complete)
                {
                    isTask1Complete = true;
                    if (pipeFlowGlowRenderer != null && propBlock != null)
                    {
                        propBlock.SetColor("_BaseColor", new Color(0f, 1f, 0.8f));
                        propBlock.SetColor("_EmissionColor", new Color(0f, 1f, 0.8f) * 3f);
                        pipeFlowGlowRenderer.SetPropertyBlock(propBlock);
                    }
                    PlaySound(sfxTaskComplete, 1.0f, 1.15f);
                    Debug.Log("[SpaceRepairStation] ¡Mesa 3: Brida de acoplamiento sellada!");
                CheckMissionCompletion();
                }

                return true;
            }

            return false;
        }

        // ==========================================
        // 4. GREASE GUN (Thermal Sealant & Pressure)
        // ==========================================

        /// <summary>
        /// Mode 1: Thermal Sealant Injector - deposit expanding polymer foam into leaking airlock breach
        /// to restore cabin pressure to 1.0 bar nominal.
        /// </summary>
        public bool TryApplySealant(Vector3 hitPoint, float deltaTime)
        {
            if (isTask1Complete) return false;
            if (!IsMissionGateOpen()) return false;

            task1Progress = Mathf.Clamp01(task1Progress + deltaTime * 0.38f);
            currentChamberPressure = Mathf.Lerp(0.15f, 1.01f, task1Progress);

            // Expand foam plug
            if (foamSealantPlug != null)
            {
                foamSealantPlug.localScale = Vector3.Lerp(Vector3.zero, new Vector3(0.18f, 0.06f, 0.18f), task1Progress);
            }

            // Diminish leak particles
            if (pressureLeakParticles != null)
            {
                if (task1Progress >= 0.95f)
                {
                    pressureLeakParticles.Stop();
                }
            }

            // Update pressure gauge
            if (pressureGaugeBarRenderer != null && propBlock != null)
            {
                Color gaugeCol = Color.Lerp(Color.red, Color.green, task1Progress);
                propBlock.SetColor("_BaseColor", gaugeCol);
                propBlock.SetColor("_EmissionColor", gaugeCol * 2.2f);
                pressureGaugeBarRenderer.SetPropertyBlock(propBlock);
            }

            if (task1Progress >= 1.0f && !isTask1Complete)
            {
                isTask1Complete = true;
                if (pressureLeakParticles != null) pressureLeakParticles.Stop();
                PlaySound(sfxTaskComplete, 1.0f, 1.2f);
                Debug.Log("[SpaceRepairStation] ¡Mesa 4: Fisura sellada con espuma térmica, presión nominal!");
                CheckMissionCompletion();
            }

            return true;
        }

        #endregion

        #region Reset & Procedural Audio Helpers

        public void ResetStation()
        {
            missionReported = false;
            lastFeedback = "";
            isTask1Complete = false;
            isTask2Complete = false;
            task1Progress = 0f;
            task2Progress = 0f;

            // Reset PGT
            if (weldLeftPlate != null && weldRightPlate != null)
            {
                weldLeftPlate.localPosition = new Vector3(-0.045f, weldLeftPlate.localPosition.y, weldLeftPlate.localPosition.z);
                weldRightPlate.localPosition = new Vector3(0.045f, weldRightPlate.localPosition.y, weldRightPlate.localPosition.z);
            }
            if (precisionScrewTransform != null)
            {
                screwCurrentDepth = 0f;
                precisionScrewTransform.localPosition = screwInitialLocalPos;
            }

            // Reset Hammer
            hammerStrikesCount = 0;
            if (bentArmorPlateTransform != null)
            {
                bentArmorPlateTransform.localRotation = Quaternion.Euler(armorBentAngle, 0f, 0f);
            }

            // Reset Ratchet
            if (flangeBolts != null)
            {
                boltTurnsRemaining = new int[flangeBolts.Length];
                for (int i = 0; i < boltTurnsRemaining.Length; i++)
                {
                    boltTurnsRemaining[i] = TurnsPerBolt;
                    if (flangeBolts[i] != null)
                    {
                        flangeBolts[i].localPosition = new Vector3(flangeBolts[i].localPosition.x, 0.05f, flangeBolts[i].localPosition.z);
                    }
                }
            }

            // Reset Grease Gun
            currentChamberPressure = 0.15f;
            if (foamSealantPlug != null)
            {
                foamSealantPlug.localScale = Vector3.zero;
            }
            if (pressureLeakParticles != null && !pressureLeakParticles.isPlaying)
            {
                pressureLeakParticles.Play();
            }
        }

        private void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (stationAudioSource != null && clip != null)
            {
                stationAudioSource.pitch = pitch;
                stationAudioSource.PlayOneShot(clip, volume);
            }
        }

        private static AudioClip CreateProceduralChime()
        {
            int sampleRate = 44100;
            float duration = 0.55f;
            int length = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 7f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 880f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.6f;
                float f3 = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.4f;
                data[i] = (f1 + f2 + f3) * decay * 0.5f;
            }

            AudioClip clip = AudioClip.Create("Station_TaskComplete_Chime", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralClang()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int length = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 14f);
                float c1 = Mathf.Sin(2f * Mathf.PI * 440f * t);
                float c2 = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f;
                float noise = (Random.value * 2f - 1f) * 0.3f;
                data[i] = (c1 + c2 + noise) * decay * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Station_Hammer_Clang", length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
