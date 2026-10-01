using System;
using System.Collections.Generic;
using Survival.InventorySystem;
using Survival.Items;
using UnityEngine;

namespace Survival.Gathering
{
    /// <summary>
    /// Golpea lo que hay delante de la cámara con la herramienta equipada y
    /// mete el botín en el inventario.
    ///
    /// Multijugador: el cliente solo envía "quiero golpear" (origen + dirección)
    /// por RPC; el servidor ejecuta <see cref="TrySwing(Ray)"/> con su propia
    /// copia del inventario y valida cooldown y alcance. Nunca confíes en el
    /// cliente para decidir qué recursos recibe.
    /// </summary>
    public class PlayerHarvester : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField, Tooltip("Herramienta usada con la mano vacía (ToolType.Hand, durabilidad 0).")]
        private ToolDefinition fists;
        [SerializeField] private LayerMask harvestMask = ~0;
        [SerializeField, Min(0f)] private float aimRadius = 0.15f;
        [SerializeField, Min(0)] private int durabilityPerHit = 1;

        private readonly List<ItemStack> _loot = new List<ItemStack>(8);
        private float _nextSwingTime;

        /// <summary>Slot equipado de la hotbar; -1 = mano vacía.</summary>
        public int EquippedSlot { get; set; } = -1;

        public event Action<ItemDefinition, int> Gathered;
        public event Action<HarvestStatus> HarvestFailed;
        /// <summary>Botín que no cabe en el inventario: suéltalo como bolsa en el suelo.</summary>
        public event Action<ItemStack> Overflow;
        public event Action<ToolDefinition> ToolBroken;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (inventory == null) inventory = GetComponent<InventoryComponent>();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        // Solo para prototipar. En producción enlázalo desde el Input System.
        private void Update()
        {
            if (Input.GetMouseButton(0)) TrySwing();
        }
#endif

        public ToolDefinition GetEquippedTool()
        {
            if (EquippedSlot >= 0 && EquippedSlot < inventory.Inventory.Capacity)
            {
                var stack = inventory.Inventory[EquippedSlot];
                if (!stack.IsEmpty && stack.Item is ToolDefinition tool) return tool;
            }
            return fists;
        }

        public bool TrySwing() =>
            TrySwing(viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));

        public bool TrySwing(Ray aim)
        {
            if (Time.time < _nextSwingTime) return false;

            var tool = GetEquippedTool();
            if (tool == null) return false;

            _nextSwingTime = Time.time + tool.SwingCooldown;

            if (!Physics.SphereCast(aim, aimRadius, out RaycastHit hit, tool.Range, harvestMask, QueryTriggerInteraction.Ignore))
                return false;

            var target = hit.collider.GetComponentInParent<IHarvestable>();
            if (target == null) return false;

            _loot.Clear();
            var status = target.Harvest(tool, _loot);
            if (status != HarvestStatus.Success)
            {
                HarvestFailed?.Invoke(status);
                return false;
            }

            var inv = inventory.Inventory;
            foreach (var stack in _loot)
            {
                int overflow = inv.Add(stack);
                if (stack.Amount > overflow) Gathered?.Invoke(stack.Item, stack.Amount - overflow);
                if (overflow > 0) Overflow?.Invoke(new ItemStack(stack.Item, overflow));
            }

            WearEquippedTool(tool);
            return true;
        }

        private void WearEquippedTool(ToolDefinition tool)
        {
            if (tool == fists || EquippedSlot < 0 || durabilityPerHit <= 0) return;

            if (inventory.Inventory.ReduceDurability(EquippedSlot, durabilityPerHit))
                ToolBroken?.Invoke(tool);
        }
    }
}
