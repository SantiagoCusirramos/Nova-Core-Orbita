using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Ancla la tablet a la muneca y la muestra solo cuando Eva la mira (levanta el brazo).
/// Poner en el Canvas World Space de la tablet. Escala sugerida del Canvas: 0.0006 (1600x1000 px ~ 0.96 x 0.6 m; reducir a gusto).
/// </summary>
public class WristTabletAnchor : MonoBehaviour
{
    [Tooltip("Mano a la que se ancla. Si esta vacio, busca 'Left Hand' (la crea XRPlayerRig en runtime).")]
    [SerializeField] private Transform hand;
    [SerializeField] private Transform head;                       // vacio = Camera.main
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.06f, 0.08f);
    [SerializeField] private Vector3 localEuler = new Vector3(-60f, 0f, 0f);
    [Tooltip("Cuanto debe mirar la pantalla hacia la cabeza para aparecer (0-1).")]
    [Range(0f, 1f)] [SerializeField] private float showThreshold = 0.55f;
    [SerializeField] private GameObject visual;                    // contenido a mostrar/ocultar (hijo del canvas)

    [Header("Sin visor (escritorio)")]
    [Tooltip("Tecla para mostrar/ocultar la tablet delante de la camara cuando no hay visor")]
    [SerializeField] private Key desktopToggleKey = Key.Tab;
    [SerializeField] private Vector3 desktopOffset = new Vector3(-0.16f, -0.08f, 0.7f);

    [Header("Tamano (escala del Canvas de 1600x1000 px)")]
    [Tooltip("En la muneca: 0.00016 = unos 26 cm de ancho")]
    [SerializeField] private float wristScale = 0.00016f;
    [Tooltip("En escritorio: 0.0003 = unos 48 cm a 70 cm de la camara")]
    [SerializeField] private float desktopScale = 0.0003f;

    private bool _desktopShown;
    private bool _xWasPressed;
    private MissionTabletUI _ui;

    private void Awake() { _ui = GetComponent<MissionTabletUI>(); }

    private void LateUpdate()
    {
        if (!XRSettings.isDeviceActive) { DesktopUpdate(); return; }
        if (wristScale > 0f) transform.localScale = Vector3.one * wristScale;

        if (hand == null)
        {
            GameObject h = GameObject.Find("Left Hand");
            if (h != null) hand = h.transform;
        }
        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (hand == null || head == null) return;

        transform.SetPositionAndRotation(
            hand.TransformPoint(localOffset),
            hand.rotation * Quaternion.Euler(localEuler));

        Vector3 toHead = (head.position - transform.position).normalized;
        bool facing = Vector3.Dot(-transform.forward, toHead) > showThreshold;   // cara visible de un Canvas = -forward
        if (visual != null && visual.activeSelf != facing) visual.SetActive(facing);

        // Boton X del mando izquierdo: siguiente pestana (solo con la tablet a la vista)
        bool x = false;
        var dev = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (dev.isValid) dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out x);
        if (x && !_xWasPressed && facing && _ui != null) _ui.NextTab();
        _xWasPressed = x;
    }

    /// <summary>Sin visor: Tab muestra la tablet abajo a la izquierda; con la tablet abierta, Tab de nuevo la cierra y 1/2/3 cambian de pestana.</summary>
    private void DesktopUpdate()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb[desktopToggleKey].wasPressedThisFrame) _desktopShown = !_desktopShown;
            if (_desktopShown && _ui != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) _ui.ShowTab(0);
                if (kb.digit2Key.wasPressedThisFrame) _ui.ShowTab(1);
                if (kb.digit3Key.wasPressedThisFrame) _ui.ShowTab(2);
            }
        }
        if (visual != null && visual.activeSelf != _desktopShown) visual.SetActive(_desktopShown);
        if (!_desktopShown || head == null) return;
        if (desktopScale > 0f) transform.localScale = Vector3.one * desktopScale;

        // Canvas: la cara visible es -forward, por eso mira en la misma direccion que la camara
        transform.SetPositionAndRotation(head.TransformPoint(desktopOffset),
                                         Quaternion.LookRotation(head.forward, head.up));
    }
}
