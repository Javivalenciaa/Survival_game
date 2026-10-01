using System.Collections.Generic;
using Survival.Items;
using UnityEngine;

namespace Survival.InventorySystem
{
    /// <summary>Envoltorio de escena para <see cref="Inventory"/> (jugador, cofre, horno...).</summary>
    [DisallowMultipleComponent]
    public class InventoryComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 30;
        [SerializeField, Tooltip("Primeros N slots que forman la hotbar.")]
        private int hotbarSize = 6;
        [SerializeField] private List<ItemStack> startingItems = new List<ItemStack>();

        public Inventory Inventory { get; private set; }
        public int HotbarSize => Mathf.Min(hotbarSize, capacity);

        private void Awake()
        {
            Inventory = new Inventory(capacity);
            foreach (var stack in startingItems)
            {
                int overflow = Inventory.Add(stack);
                if (overflow > 0)
                    Debug.LogWarning($"{name}: no caben {overflow} de {stack.Item.DisplayName} en el inventario inicial.", this);
            }
        }
    }
}
