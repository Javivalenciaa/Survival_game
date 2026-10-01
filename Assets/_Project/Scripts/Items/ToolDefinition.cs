using UnityEngine;

namespace Survival.Items
{
    /// <summary>
    /// Herramienta o arma cuerpo a cuerpo. La progresión madera → piedra → hierro
    /// es solo una cuestión de datos: cada tier es un asset con más
    /// gatherPower, mejor yieldMultiplier y más durabilidad.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Items/Tool", fileName = "Tool_")]
    public class ToolDefinition : ItemDefinition
    {
        [Header("Recolección")]
        [SerializeField] private ToolType toolType = ToolType.Axe;
        [SerializeField, Min(0f), Tooltip("Daño al nodo de recurso por golpe.")]
        private float gatherPower = 10f;
        [SerializeField, Min(0f), Tooltip("Multiplicador de recursos obtenidos (herramientas mejores extraen más).")]
        private float yieldMultiplier = 1f;

        [Header("Golpe")]
        [SerializeField, Min(0.05f)] private float swingCooldown = 0.8f;
        [SerializeField, Min(0.1f)] private float range = 2.5f;
        [SerializeField, Min(0f), Tooltip("Daño a jugadores (PvP).")]
        private float meleeDamage = 10f;

        [Header("Durabilidad (0 = indestructible, p.ej. puños)")]
        [SerializeField, Min(0)] private int maxDurability = 100;

        public ToolType ToolType => toolType;
        public float GatherPower => gatherPower;
        public float YieldMultiplier => yieldMultiplier;
        public float SwingCooldown => swingCooldown;
        public float Range => range;
        public float MeleeDamage => meleeDamage;

        public override bool HasDurability => maxDurability > 0;
        public override int MaxDurability => maxDurability;
    }
}
