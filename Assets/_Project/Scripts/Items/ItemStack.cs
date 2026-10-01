using System;
using UnityEngine;

namespace Survival.Items
{
    /// <summary>Instancia de ítem en un slot: tipo + cantidad (+ durabilidad si aplica).</summary>
    [Serializable]
    public struct ItemStack
    {
        public ItemDefinition Item;
        [Min(0)] public int Amount;
        [Tooltip("Solo para ítems con durabilidad. 0 al crear = durabilidad máxima.")]
        public int Durability;

        public static readonly ItemStack Empty = default;

        public ItemStack(ItemDefinition item, int amount, int durability = 0)
        {
            Item = item;
            Amount = amount;
            Durability = durability;
        }

        public bool IsEmpty => Item == null || Amount <= 0;

        public override string ToString() => IsEmpty ? "(vacío)" : $"{Item.DisplayName} x{Amount}";
    }

    /// <summary>Par ítem/cantidad usado por recetas, costes y botines.</summary>
    [Serializable]
    public struct ItemAmount
    {
        public ItemDefinition Item;
        [Min(1)] public int Amount;

        public ItemAmount(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }
}
