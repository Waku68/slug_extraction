using UnityEngine;

namespace SlugMove
{
    /// <summary>Marks what a plain object is made of for the pickaxe (walls, ledges...). Without it, a surface is Default.</summary>
    public class PickaxeSurface : MonoBehaviour, IPickaxeSurface
    {
        public PickaxeMaterial material = PickaxeMaterial.Hard;
        public PickaxeMaterial PickaxeMaterial => material;
    }
}
