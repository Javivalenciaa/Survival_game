using System.Collections.Generic;
using UnityEngine;

namespace Survival.Crafting
{
    /// <summary>
    /// Progresión del jugador (tier y recetas desbloqueadas) + detección de
    /// estaciones cercanas. En multijugador este estado vive en el servidor.
    /// </summary>
    public class PlayerCraftingContext : MonoBehaviour, ICraftingContext
    {
        [SerializeField] private TechTier currentTier = TechTier.Primitive;
        [SerializeField] private LayerMask stationMask = ~0;
        [SerializeField, Min(1f)] private float searchRadius = 4f;

        private readonly HashSet<int> _unlockedRecipeIds = new HashSet<int>();
        private readonly Collider[] _overlapBuffer = new Collider[16];

        public TechTier CurrentTier => currentTier;

        public void SetTier(TechTier tier)
        {
            if (tier > currentTier) currentTier = tier;
        }

        public void Unlock(RecipeDefinition recipe) => _unlockedRecipeIds.Add(recipe.Id);

        public bool IsUnlocked(RecipeDefinition recipe) =>
            recipe.UnlockedByDefault || _unlockedRecipeIds.Contains(recipe.Id);

        public bool HasStation(CraftingStation station)
        {
            if (station == CraftingStation.Hand) return true;

            Vector3 origin = transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, searchRadius, _overlapBuffer, stationMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var marker = _overlapBuffer[i].GetComponentInParent<CraftingStationMarker>();
                if (marker == null || marker.Station != station) continue;

                float maxDistance = marker.UseRadius;
                if ((marker.transform.position - origin).sqrMagnitude <= maxDistance * maxDistance)
                    return true;
            }
            return false;
        }
    }
}
