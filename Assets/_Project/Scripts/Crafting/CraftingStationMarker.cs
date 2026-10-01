using UnityEngine;

namespace Survival.Crafting
{
    /// <summary>Marca un objeto colocado (banco, horno, yunque) como estación de crafteo.</summary>
    public class CraftingStationMarker : MonoBehaviour
    {
        [SerializeField] private CraftingStation station = CraftingStation.Workbench;
        [SerializeField, Min(0.5f)] private float useRadius = 3f;

        public CraftingStation Station => station;
        public float UseRadius => useRadius;
    }
}
