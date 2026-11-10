using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace NovaCore
{
    /// <summary>
    /// Rig VR mínimo para Meta Quest 2 usando solo OpenXR + XR Management (sin XR Interaction Toolkit).
    ///
    /// Qué hace:
    ///   - Detecta cuándo hay un HMD activo y configura el TrackedPoseDriver de la cámara.
    ///   - Crea los TrackedPoseDrivers para los mandos izquierdo y derecho.
    ///   - Gestiona la altura de cámara (Floor tracking o fallback).
    ///   - Habilita el InputActionAsset de XRI si se asigna.
    ///
    /// Dependencias: solo UnityEngine.XR, UnityEngine.XR.Management, UnityEngine.InputSystem.
    /// NO requiere XR Interaction Toolkit.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public class XRPlayerRig : MonoBehaviour
    {
        [Header("Input Actions (opcional)")]
        [Tooltip("Arrastra aquí el asset 'XRI Default Input Actions' si lo tienes. " +
                 "Si está vacío el rig usará bindings directos.")]
        [SerializeField] private InputActionAsset xriInputActions;

        [Header("Modelos visuales de mandos (opcional)")]
        [SerializeField] private GameObject leftControllerModelPrefab;
        [SerializeField] private GameObject rightControllerModelPrefab;

        [Header("Opciones")]
        [SerializeField] private bool logDeviceInfo = true;
        [SerializeField] private float fallbackCameraHeight = 1.6f;
        [SerializeField] private float floorRaycastDistance = 5f;

        // ── Bindings por defecto (OpenXR) ────────────────────────────────────
        const string HeadPos   = "<XRHMD>/centerEyePosition";
        const string HeadRot   = "<XRHMD>/centerEyeRotation";
        const string HeadTrack = "<XRHMD>/trackingState";
        const string LeftPos   = "<XRController>{LeftHand}/devicePosition";
        const string LeftRot   = "<XRController>{LeftHand}/deviceRotation";
        const string LeftTrack = "<XRController>{LeftHand}/trackingState";
        const string RightPos  = "<XRController>{RightHand}/devicePosition";
        const string RightRot  = "<XRController>{RightHand}/deviceRotation";
        const string RightTrack= "<XRController>{RightHand}/trackingState";

        // ── Estado ────────────────────────────────────────────────────────────
        private Camera              headCamera;
        private TrackedPoseDriver   headDriver;
        private Transform           cameraOffset;
        private CharacterController cc;
        private XRInputSubsystem    inputSubsystem;
        private bool rigBuilt;
        private bool floorConfirmed;
        private bool floorUnsupported;
        private float nextFloorRetry;
        const float FLOOR_RETRY = 1f;

        // ── Ciclo ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            headCamera = GetComponentInChildren<Camera>(true);
            if (headCamera == null)
            {
                Debug.LogError("[XRPlayerRig] No se encontró Camera hija. Desactivando.", this);
                enabled = false;
                return;
            }
            headDriver = headCamera.GetComponent<TrackedPoseDriver>();
            cc         = GetComponent<CharacterController>();

            if (IsXRActive()) BuildRig();
        }

        private void Update()
        {
            if (!rigBuilt)
            {
                if (IsXRActive()) BuildRig();
                return;
            }
            if (!floorConfirmed && !floorUnsupported)
                TryFloorTracking();

            SyncCameraOffset();
        }

        // ════════════════════════════════════════════════════════════════════
        // CONSTRUCCIÓN DEL RIG
        // ════════════════════════════════════════════════════════════════════

        private void BuildRig()
        {
            if (rigBuilt) return;
            rigBuilt = true;

            EnableInputActions();
            SnapToFloor();
            CreateCameraOffset();
            SetupHeadTracking();
            CreateHands();
            SyncCameraOffset();
            TryFloorTracking();

            if (logDeviceInfo) LogDevices();
            Debug.Log("[XRPlayerRig] Rig VR construido correctamente.", this);
        }

        // ── Input Actions ────────────────────────────────────────────────────

        private void EnableInputActions()
        {
            if (xriInputActions == null) return;
            xriInputActions.Enable();
        }

        // ── Snap al suelo ────────────────────────────────────────────────────

        private void SnapToFloor()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            var hits = Physics.RaycastAll(origin, Vector3.down, floorRaycastDistance,
                                          ~0, QueryTriggerInteraction.Ignore);
            float closest = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (cc != null && hit.collider == cc) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance < closest) closest = hit.distance;
            }
            if (closest == float.MaxValue) return;

            var pos = transform.position;
            pos.y   = origin.y - closest;
            if (cc != null) { cc.enabled = false; transform.position = pos; cc.enabled = true; }
            else transform.position = pos;
        }

        // ── Camera Offset ────────────────────────────────────────────────────

        private void CreateCameraOffset()
        {
            if (cameraOffset != null) return;
            var go      = new GameObject("Camera Offset");
            cameraOffset = go.transform;
            cameraOffset.SetParent(transform, false);
            cameraOffset.localPosition = Vector3.zero;
            cameraOffset.localRotation = Quaternion.identity;

            headCamera.transform.SetParent(cameraOffset, false);
            headCamera.transform.localPosition = Vector3.zero;
            headCamera.transform.localRotation = Quaternion.identity;
        }

        // ── Head tracking ────────────────────────────────────────────────────

        private void SetupHeadTracking()
        {
            if (headDriver == null)
                headDriver = headCamera.gameObject.AddComponent<TrackedPoseDriver>();

            SetDriverBindings(headDriver, HeadPos, HeadRot, HeadTrack);
            headDriver.enabled = true;
        }

        // ── Mandos ────────────────────────────────────────────────────────────

        private void CreateHands()
        {
            var parent = cameraOffset != null ? cameraOffset : transform;
            CreateHand("Left Hand",  parent, LeftPos,  LeftRot,  LeftTrack,  leftControllerModelPrefab);
            CreateHand("Right Hand", parent, RightPos, RightRot, RightTrack, rightControllerModelPrefab);
        }

        private void CreateHand(string handName, Transform parent,
                                 string posBinding, string rotBinding, string trackBinding,
                                 GameObject modelPrefab)
        {
            var go = new GameObject(handName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var driver = go.AddComponent<TrackedPoseDriver>();
            SetDriverBindings(driver, posBinding, rotBinding, trackBinding);

            if (modelPrefab != null)
                Instantiate(modelPrefab, go.transform, false).name = handName + " Visual";
        }

        // ── TrackedPoseDriver helper ─────────────────────────────────────────

        private void SetDriverBindings(TrackedPoseDriver driver,
                                        string posBinding, string rotBinding, string trackBinding)
        {
            driver.trackingType        = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType          = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.ignoreTrackingState = false;

            driver.positionInput      = MakeProperty(posBinding,   "Vector3");
            driver.rotationInput      = MakeProperty(rotBinding,   "Quaternion");
            driver.trackingStateInput = MakeProperty(trackBinding, "Integer");
        }

        private InputActionProperty MakeProperty(string binding, string controlType)
        {
            // Intentar encontrar la acción en el asset asignado
            if (xriInputActions != null)
            {
                // Buscar por binding path
                foreach (var action in xriInputActions)
                {
                    foreach (var b in action.bindings)
                    {
                        if (b.path == binding)
                            return new InputActionProperty(InputActionReference.Create(action));
                    }
                }
            }

            // Acción directa como fallback
            var direct = new InputAction(binding, InputActionType.Value,
                                         binding, expectedControlType: controlType);
            return new InputActionProperty(direct);
        }

        // ── Floor tracking ───────────────────────────────────────────────────

        private void TryFloorTracking()
        {
            if (floorConfirmed || floorUnsupported) return;
            if (Time.unscaledTime < nextFloorRetry) return;
            nextFloorRetry = Time.unscaledTime + FLOOR_RETRY;

            var subs = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subs);
            inputSubsystem = null;
            bool anyFloor  = false;

            foreach (var sub in subs)
            {
                if (sub == null) continue;
                inputSubsystem = sub;
                try
                {
                    var modes = sub.GetSupportedTrackingOriginModes();
                    if ((modes & TrackingOriginModeFlags.Floor) == 0) continue;
                    anyFloor = true;
                    if (sub.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                    { floorConfirmed = true; return; }
                }
                catch { inputSubsystem = null; }
            }

            if (inputSubsystem != null && !anyFloor)
            {
                floorUnsupported = true;
                Debug.LogWarning($"[XRPlayerRig] El runtime no soporta Floor tracking. Fallback: {fallbackCameraHeight} m.", this);
            }
        }

        // ── Camera offset height ─────────────────────────────────────────────

        private void SyncCameraOffset()
        {
            if (cameraOffset == null) return;
            bool floor = false;
            if (inputSubsystem != null)
            {
                try { floor = (inputSubsystem.GetTrackingOriginMode() & TrackingOriginModeFlags.Floor) != 0; }
                catch { inputSubsystem = null; }
            }
            var lp = cameraOffset.localPosition;
            float target = floor ? 0f : fallbackCameraHeight;
            if (!Mathf.Approximately(lp.y, target)) { lp.y = target; cameraOffset.localPosition = lp; }
        }

        // ── Log ──────────────────────────────────────────────────────────────

        private void LogDevices()
        {
            LogDev("HMD",            XRNode.Head);
            LogDev("Mando izquierdo",XRNode.LeftHand);
            LogDev("Mando derecho",  XRNode.RightHand);
        }

        private static void LogDev(string label, XRNode node)
        {
            var dev = InputDevices.GetDeviceAtXRNode(node);
            if (!dev.isValid) Debug.LogWarning($"[XRPlayerRig] {label}: no detectado.");
            else              Debug.Log($"[XRPlayerRig] {label}: {dev.name} ({dev.manufacturer})");
        }

        // ── Util ─────────────────────────────────────────────────────────────

        private static bool IsXRActive()
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            return loader != null || XRSettings.isDeviceActive;
        }
    }
}
