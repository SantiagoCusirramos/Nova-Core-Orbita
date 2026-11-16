using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Va en el Player (junto a OrbitFreeFly y XRPlayerRig). Crea los interactores:
    ///   - Escritorio: uno en la cámara (rayo desde el centro de la pantalla + mira).
    ///   - VR: uno en cada mando, cuando XRPlayerRig termina de construir el rig.
    /// Cambia solo entre uno y otro según haya visor activo o no.
    /// También pone manos con guante (o un mando simple) si XRPlayerRig no tiene prefabs asignados.
    /// </summary>
    [DisallowMultipleComponent]
    public class NovaInteractionRig : MonoBehaviour
    {
        [Header("Materiales (los asigna el constructor de escena)")]
        public Material laserMaterial;
        public Material controllerMaterial;

        [Header("Alcance del rayo (metros)")]
        public float desktopRayLength = 3.0f;
        public float vrRayLength = 4.0f;

        [Header("Opciones")]
        [Tooltip("Crea un mando simple visible en cada mano si no hay modelo asignado")]
        public bool createControllerVisuals = true;
        public bool showDesktopCrosshair = true;
        [Tooltip("Muestra manos con guante en lugar de mandos (los dedos siguen al gatillo y al grip)")]
        public bool showHands = true;

        public NovaInteractor Desktop { get; private set; }
        public NovaInteractor Left    { get; private set; }
        public NovaInteractor Right   { get; private set; }

        private XRPlayerRig _rig;
        private Camera _cam;

        private void Start()
        {
            _rig = GetComponent<XRPlayerRig>();
            _cam = GetComponentInChildren<Camera>(true);
            if (_cam == null) _cam = Camera.main;

            if (_cam != null)
            {
                Desktop = _cam.gameObject.AddComponent<NovaInteractor>();
                Desktop.source = NovaInteractor.Source.Desktop;
                Desktop.rayLength = desktopRayLength;
            }
            else
            {
                Debug.LogWarning("[NovaInteractionRig] No hay cámara en el Player; sin interacción de escritorio.", this);
            }
        }

        private void Update()
        {
            bool vr = NovaXR.IsActive;

            if (vr && Left == null && _rig != null && _rig.IsBuilt)
                CreateHandInteractors();

            if (Desktop != null) Desktop.enabled = !vr;
            if (Left  != null) Left.enabled  = vr;
            if (Right != null) Right.enabled = vr;
        }

        private void CreateHandInteractors()
        {
            if (_rig.LeftHand == null || _rig.RightHand == null) return;

            Left  = AddHand(_rig.LeftHand,  NovaInteractor.Source.LeftHand);
            Right = AddHand(_rig.RightHand, NovaInteractor.Source.RightHand);
            Debug.Log("[NovaInteractionRig] Interactores VR listos (gatillo = usar, grip = agarrar).", this);
        }

        private NovaInteractor AddHand(Transform hand, NovaInteractor.Source src)
        {
            var it = hand.gameObject.AddComponent<NovaInteractor>();
            it.source = src;
            it.rayLength = vrRayLength;
            it.laserMaterial = laserMaterial;

            // Manos con colisión: empujan los objetos sueltos
            NovaHandPhysics.Create(hand, it.fingertipOffset);

            if (createControllerVisuals && hand.childCount == 0)
            {
                if (showHands) NovaHandVisual.Create(hand, src == NovaInteractor.Source.LeftHand, controllerMaterial);
                else CreateControllerVisual(hand, src == NovaInteractor.Source.LeftHand);
            }

            return it;
        }

        private void CreateControllerVisual(Transform hand, bool left)
        {
            var root = new GameObject("Controller Visual").transform;
            root.SetParent(hand, false);

            // Mango
            var grip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            grip.name = "Grip";
            Object.Destroy(grip.GetComponent<Collider>());
            grip.transform.SetParent(root, false);
            grip.transform.localScale = new Vector3(0.035f, 0.055f, 0.035f);
            grip.transform.localPosition = new Vector3(0f, -0.025f, -0.03f);
            grip.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);

            // Anillo de tracking
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(root, false);
            ring.transform.localScale = new Vector3(0.075f, 0.006f, 0.075f);
            ring.transform.localPosition = new Vector3(left ? 0.01f : -0.01f, 0.02f, 0.015f);

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (controllerMaterial != null) r.sharedMaterial = controllerMaterial;
            }
        }

        // ── Mira de escritorio ───────────────────────────────────────────────
        private void OnGUI()
        {
            if (!showDesktopCrosshair || NovaXR.IsActive || Desktop == null || !Desktop.enabled) return;

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            bool hover = Desktop.Hovered != null;
            var prev = GUI.color;
            GUI.color = hover ? new Color(0.4f, 1f, 0.5f, 0.95f) : new Color(1f, 1f, 1f, 0.7f);
            float s = hover ? 6f : 4f;
            GUI.DrawTexture(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
