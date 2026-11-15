using UnityEngine;

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

    private void LateUpdate()
    {
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
    }
}
