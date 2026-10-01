using Survival.InventorySystem;
using Survival.Items;

namespace Survival.Crafting
{
    /// <summary>Quién craftea y dónde: desbloqueos y estaciones cercanas.</summary>
    public interface ICraftingContext
    {
        TechTier CurrentTier { get; }
        bool IsUnlocked(RecipeDefinition recipe);
        bool HasStation(CraftingStation station);
    }

    public enum CraftResult
    {
        Success,
        InvalidRecipe,
        Locked,
        TierTooLow,
        MissingStation,
        MissingIngredients,
        NoSpaceForOutput,
        QueueFull
    }

    /// <summary>
    /// Reglas de crafteo sin estado. En multijugador SOLO el servidor llama a
    /// <see cref="TryCraft"/>/<see cref="TryConsumeIngredients"/>; el cliente usa
    /// <see cref="Check"/> para pintar la UI (llámalo al cambiar el inventario,
    /// no en cada frame: simula sobre una copia del inventario).
    /// </summary>
    public static class CraftingSystem
    {
        public static CraftResult Check(RecipeDefinition recipe, Inventory inventory, ICraftingContext context, bool requireOutputSpace = true)
        {
            var access = CheckAccess(recipe, context);
            if (access != CraftResult.Success) return access;

            // Simulamos sobre una copia: gestiona ingredientes repetidos y el
            // caso "el resultado cabe solo porque los ingredientes liberan un slot".
            var simulation = inventory.Clone();
            if (!RemoveIngredients(recipe, simulation)) return CraftResult.MissingIngredients;

            if (requireOutputSpace && !simulation.CanAdd(recipe.Output.Item, recipe.Output.Amount))
                return CraftResult.NoSpaceForOutput;

            return CraftResult.Success;
        }

        /// <summary>Crafteo instantáneo y atómico: o se hace todo o no cambia nada.</summary>
        public static CraftResult TryCraft(RecipeDefinition recipe, Inventory inventory, ICraftingContext context)
        {
            var result = Check(recipe, inventory, context);
            if (result != CraftResult.Success) return result;

            RemoveIngredients(recipe, inventory);
            inventory.Add(recipe.Output.Item, recipe.Output.Amount);
            return CraftResult.Success;
        }

        /// <summary>Para crafteo con tiempo: cobra los ingredientes al encolar (estilo Rust).</summary>
        public static CraftResult TryConsumeIngredients(RecipeDefinition recipe, Inventory inventory, ICraftingContext context)
        {
            var result = Check(recipe, inventory, context, requireOutputSpace: false);
            if (result != CraftResult.Success) return result;

            RemoveIngredients(recipe, inventory);
            return CraftResult.Success;
        }

        private static CraftResult CheckAccess(RecipeDefinition recipe, ICraftingContext context)
        {
            if (recipe == null || !recipe.IsValid) return CraftResult.InvalidRecipe;
            if (!context.IsUnlocked(recipe)) return CraftResult.Locked;
            if (context.CurrentTier < recipe.RequiredTier) return CraftResult.TierTooLow;
            if (!context.HasStation(recipe.RequiredStation)) return CraftResult.MissingStation;
            return CraftResult.Success;
        }

        // Puede dejar el inventario a medias si falla: úsalo sobre una copia o
        // después de que Check() haya validado sobre una copia.
        private static bool RemoveIngredients(RecipeDefinition recipe, Inventory inventory)
        {
            foreach (var ingredient in recipe.Ingredients)
                if (!inventory.Remove(ingredient.Item, ingredient.Amount))
                    return false;
            return true;
        }
    }
}
