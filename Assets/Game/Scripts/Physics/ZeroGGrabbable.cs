using UnityEngine;

namespace NovaCore
{
    /// <summary>
    /// Objeto suelto de la estación que se puede agarrar y lanzar (gravedad cero).
    /// VR: acercar la mano y mantener el grip (o apuntar con el láser); soltar = lanzar
    /// con la velocidad de la mano. Escritorio: F agarra / suelta, R lanza.
    /// </summary>
    [RequireComponent(typeof(ZeroGBody))]
    public class ZeroGGrabbable : NovaInteractable
    {
        public Vector3 holdOffset = new Vector3(0f, 0f, 0.08f);
        public override bool CanGrab => true;
        public override Vector3 HoldLocalPosition => holdOffset;
    }
}
