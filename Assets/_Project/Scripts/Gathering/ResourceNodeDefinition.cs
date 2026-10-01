using System;
using Survival.Items;
using UnityEngine;

namespace Survival.Gathering
{
    [Serializable]
    public struct ResourceYield
    {
        public ItemDefinition Item;
        [Min(0f), Tooltip("Total que suelta el nodo a lo largo de su vida (con multiplicador 1).")]
        public float TotalAmount;
    }

    /// <summary>
    /// Tipo de nodo: árbol, roca, veta de hierro... Un árbol pequeño y uno
    /// grande son el mismo script con distinto asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Gathering/Resource Node", fileName = "Node_")]
    public class ResourceNodeDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;

        [Header("Herramienta")]
        [SerializeField] private ToolType effectiveTools = ToolType.Axe;
        [SerializeField, Tooltip("Tier mínimo de herramienta (p.ej. hierro requiere pico de piedra).")]
        private TechTier minToolTier = TechTier.Primitive;
        [SerializeField, Range(0f, 1f), Tooltip("Eficiencia con herramienta no adecuada. 0 = no se puede.")]
        private float wrongToolEfficiency = 0.25f;

        [Header("Vida y botín")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private ResourceYield[] yields = new ResourceYield[0];
        [SerializeField, Tooltip("Botín extra al agotarlo (último golpe).")]
        private ItemAmount[] depletionBonus = new ItemAmount[0];
        [SerializeField, Min(0f)] private float respawnSeconds = 300f;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public ResourceYield[] Yields => yields;
        public ItemAmount[] DepletionBonus => depletionBonus;
        public float RespawnSeconds => respawnSeconds;

        /// <summary>Decide si la herramienta sirve y con qué eficiencia (0..1).</summary>
        public HarvestStatus Evaluate(ToolDefinition tool, out float efficiency)
        {
            efficiency = 0f;
            if (tool == null) return HarvestStatus.WrongTool;
            if (tool.Tier < minToolTier) return HarvestStatus.ToolTierTooLow;

            if ((effectiveTools & tool.ToolType) != 0)
            {
                efficiency = 1f;
                return HarvestStatus.Success;
            }

            if (wrongToolEfficiency <= 0f) return HarvestStatus.WrongTool;

            efficiency = wrongToolEfficiency;
            return HarvestStatus.Success;
        }
    }
}
