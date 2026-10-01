using System;

namespace Survival
{
    /// <summary>
    /// Escalón tecnológico. Gobierna qué recetas se pueden fabricar y qué
    /// herramientas pueden extraer qué recursos. Añadir un tier nuevo es
    /// añadir un valor aquí y crear los assets correspondientes.
    /// </summary>
    public enum TechTier
    {
        Primitive = 0, // manos, piedra suelta, madera
        Stone = 1,     // hachas/picos de piedra, arco, lanza
        Iron = 2,      // herramientas y armas de hierro (requiere fundición)
        Steel = 3      // acero / metal de alta calidad
    }

    [Flags]
    public enum ToolType
    {
        None = 0,
        Hand = 1 << 0,
        Axe = 1 << 1,
        Pickaxe = 1 << 2,
        Knife = 1 << 3,
        Hammer = 1 << 4
    }

    public enum ItemCategory
    {
        Resource,
        Component,
        Tool,
        Weapon,
        Ammo,
        Consumable,
        Deployable
    }

    /// <summary>Estaciones de crafteo. "Hand" = inventario del jugador.</summary>
    public enum CraftingStation
    {
        Hand,
        Campfire,
        Workbench,
        Furnace,
        Anvil
    }
}
