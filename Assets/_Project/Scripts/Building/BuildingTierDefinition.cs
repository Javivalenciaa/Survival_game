using System;
using Survival.Items;
using UnityEngine;

namespace Survival.Building
{
    public enum BuildingMaterialTier
    {
        Twig = 0,  // paja/ramas: se coloca gratis o casi, se rompe a puñetazos
        Wood = 1,
        Stone = 2,
        Metal = 3
    }

    public enum DamageType
    {
        Melee,
        Arrow,
        Bullet,
        Explosive,
        Fire
    }

    [Serializable]
    public struct DamageResistance
    {
        public DamageType Type;
        [Range(0f, 1f), Tooltip("0 = daño completo, 1 = inmune.")]
        public float Resistance;
    }

    /// <summary>
    /// Material de construcción. La pieza (pared, cimiento...) define la forma y
    /// este asset define vida, coste y resistencias: N piezas × M tiers sin
    /// duplicar prefabs ni lógica.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Building/Material Tier", fileName = "BuildTier_")]
    public class BuildingTierDefinition : ScriptableObject
    {
        [SerializeField] private BuildingMaterialTier tier;
        [SerializeField, Min(0.01f)] private float healthMultiplier = 1f;
        [SerializeField, Tooltip("Coste por unidad; se multiplica por BuildingPieceDefinition.CostUnits.")]
        private ItemAmount[] costPerUnit = new ItemAmount[0];
        [SerializeField] private DamageResistance[] resistances = new DamageResistance[0];
        [SerializeField, Tooltip("Material URP compartido (atlas) para no romper el batching.")]
        private Material material;

        public BuildingMaterialTier Tier => tier;
        public float HealthMultiplier => healthMultiplier;
        public ItemAmount[] CostPerUnit => costPerUnit;
        public Material Material => material;

        public float GetResistance(DamageType type)
        {
            foreach (var r in resistances)
                if (r.Type == type) return r.Resistance;
            return 0f;
        }

        public float ApplyResistance(float rawDamage, DamageType type) =>
            rawDamage * (1f - GetResistance(type));
    }
}
