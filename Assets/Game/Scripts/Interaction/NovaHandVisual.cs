using UnityEngine;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

namespace NovaCore
{
    /// <summary>
    /// Mano de guante de astronauta hecha con primitivas (sin modelos externos) que
    /// reemplaza al mando visible. Los dedos se cierran según los botones:
    ///   - Índice  → gatillo (usar / interactuar)
    ///   - Medio, anular y meñique → grip (agarrar)
    ///   - Pulgar → toca A/B/X/Y o el stick
    /// Se crea sola desde NovaInteractionRig; no hay que configurarla a mano.
    /// </summary>
    public class NovaHandVisual : MonoBehaviour
    {
        public bool isLeft;
        public Material gloveMaterial;
        [Tooltip("Velocidad con la que se cierran/abren los dedos")]
        public float curlSpeed = 14f;

        // Cada dedo: 3 falanges encadenadas. Se cierran rotando en X local.
        private Transform[][] _fingers;
        private Transform[] _thumb;
        private float _grip, _trigger, _thumbTouch;

        private static readonly float[] SegLen = { 0.034f, 0.024f, 0.020f };
        private const float FingerRadius = 0.0085f;

        public static NovaHandVisual Create(Transform hand, bool left, Material glove)
        {
            var go = new GameObject(left ? "Mano izquierda (guante)" : "Mano derecha (guante)");
            go.transform.SetParent(hand, false);
            var v = go.AddComponent<NovaHandVisual>();
            v.isLeft = left;
            v.gloveMaterial = glove;
            v.Build();
            return v;
        }

        private void Build()
        {
            float side = isLeft ? 1f : -1f;   // los dedos y el pulgar se reflejan entre manos

            // La pose del mando (grip) queda en el puño: la palma va un poco atrás y abajo
            var palmRoot = new GameObject("Palma").transform;
            palmRoot.SetParent(transform, false);
            palmRoot.localPosition = new Vector3(0.0f, -0.012f, -0.045f);
            palmRoot.localRotation = Quaternion.Euler(-20f, 0f, side * 8f);

            var palm = Part(PrimitiveType.Cube, "Dorso", palmRoot, new Vector3(0f, 0f, 0.02f), new Vector3(0.082f, 0.028f, 0.09f));
            palm.localRotation = Quaternion.identity;

            // Puño del traje (anillo de conexión)
            var cuff = Part(PrimitiveType.Cylinder, "Puño", palmRoot, new Vector3(0f, 0f, -0.04f), new Vector3(0.085f, 0.022f, 0.085f));
            cuff.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Cuatro dedos: índice (más hacia el pulgar) → meñique
            _fingers = new Transform[4][];
            for (int f = 0; f < 4; f++)
            {
                float x = side * (0.028f - f * 0.019f);
                float lenScale = f == 0 ? 0.95f : f == 1 ? 1.05f : f == 2 ? 1f : 0.8f;
                _fingers[f] = Finger(palmRoot, "Dedo " + (f + 1), new Vector3(x, 0f, 0.066f), lenScale, Quaternion.identity);
            }

            // Pulgar: sale del lateral de la palma, apuntando hacia delante y adentro
            _thumb = Finger(palmRoot, "Pulgar", new Vector3(side * 0.042f, -0.006f, 0.0f), 0.9f,
                            Quaternion.Euler(10f, -side * 40f, side * 50f));

            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (gloveMaterial != null) r.sharedMaterial = gloveMaterial;
            }
        }

        private Transform[] Finger(Transform parent, string name, Vector3 basePos, float lenScale, Quaternion baseRot)
        {
            var segs = new Transform[3];
            Transform p = parent;
            Vector3 pos = basePos;
            for (int s = 0; s < 3; s++)
            {
                float len = SegLen[s] * lenScale;
                var joint = new GameObject(name + " · " + (s + 1)).transform;
                joint.SetParent(p, false);
                joint.localPosition = pos;
                joint.localRotation = s == 0 ? baseRot : Quaternion.identity;

                var seg = Part(PrimitiveType.Capsule, "Falange", joint, new Vector3(0f, 0f, len * 0.5f),
                               new Vector3(FingerRadius * 2f, len * 0.5f + FingerRadius, FingerRadius * 2f));
                seg.localRotation = Quaternion.Euler(90f, 0f, 0f);

                segs[s] = joint;
                p = joint;
                pos = new Vector3(0f, 0f, len);
            }
            return segs;
        }

        private static Transform Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go.transform;
        }

        private void Update()
        {
            if (_fingers == null) return;

            XRNode node = isLeft ? XRNode.LeftHand : XRNode.RightHand;
            float grip = NovaXR.Axis(node, XRCommonUsages.grip);
            float trig = NovaXR.Axis(node, XRCommonUsages.trigger);
            bool thumb = NovaXR.Button(node, XRCommonUsages.primaryTouch)
                      || NovaXR.Button(node, XRCommonUsages.secondaryTouch)
                      || NovaXR.Button(node, XRCommonUsages.primary2DAxisTouch);

            float k = 1f - Mathf.Exp(-curlSpeed * Time.deltaTime);
            _grip = Mathf.Lerp(_grip, grip, k);
            _trigger = Mathf.Lerp(_trigger, trig, k);
            _thumbTouch = Mathf.Lerp(_thumbTouch, thumb ? 1f : 0f, k);

            // Mano relajada: los dedos ya vienen algo flexionados
            Curl(_fingers[0], 0.15f + 0.85f * _trigger);
            for (int f = 1; f < 4; f++) Curl(_fingers[f], 0.2f + 0.8f * _grip);
            CurlThumb(_thumb, 0.1f + 0.6f * Mathf.Max(_thumbTouch, _grip * 0.7f));
        }

        private static void Curl(Transform[] segs, float amount)
        {
            segs[0].localRotation = Quaternion.Euler(amount * 70f, 0f, 0f);
            segs[1].localRotation = Quaternion.Euler(amount * 90f, 0f, 0f);
            segs[2].localRotation = Quaternion.Euler(amount * 60f, 0f, 0f);
        }

        private void CurlThumb(Transform[] segs, float amount)
        {
            float side = isLeft ? 1f : -1f;
            segs[0].localRotation = Quaternion.Euler(10f + amount * 25f, -side * (40f - amount * 25f), side * 50f);
            segs[1].localRotation = Quaternion.Euler(amount * 35f, 0f, 0f);
            segs[2].localRotation = Quaternion.Euler(amount * 45f, 0f, 0f);
        }
    }
}
