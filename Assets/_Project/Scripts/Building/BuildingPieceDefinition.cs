using System.Collections.Generic;
using Survival.Items;
using UnityEngine;

namespace Survival.Building
{
    public enum BuildingPieceType
    {
        Foundation,
        Wall,
        Doorway,
        Door,
        Floor,
        Roof,
        Stairs
    }

    /// <summary>Forma de una pieza modular. El material lo aporta un <see cref="BuildingTierDefinition"/>.</summary>
    [CreateAssetMenu(menuName = "Survival/Building/Piece", fileName = "Piece_")]
    public class BuildingPieceDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("ID estable para red y guardado.")]
        private int id;
        [SerializeField] private BuildingPieceType pieceType;
        [SerializeField, Tooltip("Prefab con LODGroup y sockets (Transforms hijos) para encajar.")]
        private GameObject prefab;
        [SerializeField, Min(1f)] private float baseHealth = 250f;
        [SerializeField, Min(0.1f), Tooltip("Una pared = 1, un cimiento = 2...")]
        private float costUnits = 1f;
        [SerializeField] private BuildingMaterialTier minTier = BuildingMaterialTier.Twig;
        [SerializeField] private BuildingMaterialTier maxTier = BuildingMaterialTier.Metal;

        public int Id => id;
        public BuildingPieceType PieceType => pieceType;
        public GameObject Prefab => prefab;

        public bool SupportsTier(BuildingTierDefinition tier) =>
            tier != null && tier.Tier >= minTier && tier.Tier <= maxTier;

        public float GetMaxHealth(BuildingTierDefinition tier) => baseHealth * tier.HealthMultiplier;

        /// <summary>Coste de colocar/mejorar esta pieza al tier indicado.</summary>
        public void GetCost(BuildingTierDefinition tier, List<ItemAmount> result)
        {
            result.Clear();
            foreach (var unit in tier.CostPerUnit)
                result.Add(new ItemAmount(unit.Item, Mathf.CeilToInt(unit.Amount * costUnits)));
        }
    }
}
