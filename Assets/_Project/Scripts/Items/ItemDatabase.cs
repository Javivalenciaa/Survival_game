using System.Collections.Generic;
using Survival.Crafting;
using UnityEngine;

namespace Survival.Items
{
    /// <summary>
    /// Registro central de ítems y recetas. Por red y en el guardado solo viajan
    /// IDs enteros; este asset los traduce de vuelta a ScriptableObjects.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
        [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        private Dictionary<int, ItemDefinition> _itemsById;
        private Dictionary<int, RecipeDefinition> _recipesById;

        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<RecipeDefinition> Recipes => recipes;

        public ItemDefinition GetItem(int id)
        {
            EnsureLookups();
            return _itemsById.TryGetValue(id, out var item) ? item : null;
        }

        public RecipeDefinition GetRecipe(int id)
        {
            EnsureLookups();
            return _recipesById.TryGetValue(id, out var recipe) ? recipe : null;
        }

        private void OnEnable() => _itemsById = null;

        private void EnsureLookups()
        {
            if (_itemsById != null) return;

            _itemsById = new Dictionary<int, ItemDefinition>(items.Count);
            foreach (var item in items)
                if (item != null) _itemsById[item.Id] = item;

            _recipesById = new Dictionary<int, RecipeDefinition>(recipes.Count);
            foreach (var recipe in recipes)
                if (recipe != null) _recipesById[recipe.Id] = recipe;
        }

        private void OnValidate()
        {
            _itemsById = null;
            WarnDuplicates(items, i => i.Id, "ítem");
            WarnDuplicates(recipes, r => r.Id, "receta");
        }

        private void WarnDuplicates<T>(List<T> list, System.Func<T, int> getId, string label) where T : Object
        {
            var seen = new Dictionary<int, T>();
            foreach (var entry in list)
            {
                if (entry == null) continue;
                int id = getId(entry);
                if (seen.TryGetValue(id, out var other) && other != entry)
                    Debug.LogError($"ID de {label} duplicado {id}: {other.name} y {entry.name}", this);
                else
                    seen[id] = entry;
            }
        }
    }
}
