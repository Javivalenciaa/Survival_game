using System;
using System.Collections.Generic;
using Survival.InventorySystem;
using Survival.Items;
using UnityEngine;

namespace Survival.Crafting
{
    /// <summary>
    /// Cola de crafteo con tiempo. Los ingredientes se cobran al encolar y se
    /// devuelven al cancelar. Lo que no quepa al terminar se emite en
    /// <see cref="Overflow"/> para soltarlo al suelo como bolsa de botín.
    /// </summary>
    public class CraftingQueue : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;
        [SerializeField, Min(1)] private int maxJobs = 8;

        private ICraftingContext _context;
        private readonly List<Job> _jobs = new List<Job>();

        public event Action<RecipeDefinition> JobStarted;
        public event Action<RecipeDefinition> JobCompleted;
        public event Action<ItemStack> Overflow;

        public int JobCount => _jobs.Count;
        public RecipeDefinition CurrentRecipe => _jobs.Count > 0 ? _jobs[0].Recipe : null;

        /// <summary>Progreso 0..1 del trabajo en curso (para la barra de la UI).</summary>
        public float CurrentProgress =>
            _jobs.Count == 0 || _jobs[0].Recipe.CraftSeconds <= 0f
                ? 0f
                : 1f - _jobs[0].Remaining / _jobs[0].Recipe.CraftSeconds;

        private struct Job
        {
            public RecipeDefinition Recipe;
            public float Remaining;
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventoryComponent>();
            _context = GetComponent<ICraftingContext>();
            if (_context == null)
                Debug.LogError($"{name}: CraftingQueue necesita un componente que implemente ICraftingContext.", this);
            enabled = false; // solo hace Update mientras hay trabajos
        }

        public CraftResult Enqueue(RecipeDefinition recipe)
        {
            if (_jobs.Count >= maxJobs) return CraftResult.QueueFull;

            var result = CraftingSystem.TryConsumeIngredients(recipe, inventory.Inventory, _context);
            if (result != CraftResult.Success) return result;

            _jobs.Add(new Job { Recipe = recipe, Remaining = recipe.CraftSeconds });
            if (_jobs.Count == 1) JobStarted?.Invoke(recipe);
            enabled = true;
            return CraftResult.Success;
        }

        public void Cancel(int index)
        {
            if (index < 0 || index >= _jobs.Count) return;

            var recipe = _jobs[index].Recipe;
            _jobs.RemoveAt(index);
            foreach (var ingredient in recipe.Ingredients)
                Give(ingredient.Item, ingredient.Amount);

            if (index == 0 && _jobs.Count > 0) JobStarted?.Invoke(_jobs[0].Recipe);
            enabled = _jobs.Count > 0;
        }

        private void Update()
        {
            if (_jobs.Count == 0)
            {
                enabled = false;
                return;
            }

            var job = _jobs[0];
            job.Remaining -= Time.deltaTime;
            if (job.Remaining > 0f)
            {
                _jobs[0] = job;
                return;
            }

            _jobs.RemoveAt(0);
            Give(job.Recipe.Output.Item, job.Recipe.Output.Amount);
            JobCompleted?.Invoke(job.Recipe);

            if (_jobs.Count > 0) JobStarted?.Invoke(_jobs[0].Recipe);
            else enabled = false;
        }

        private void Give(ItemDefinition item, int amount)
        {
            int overflow = inventory.Inventory.Add(item, amount);
            if (overflow > 0) Overflow?.Invoke(new ItemStack(item, overflow));
        }
    }
}
