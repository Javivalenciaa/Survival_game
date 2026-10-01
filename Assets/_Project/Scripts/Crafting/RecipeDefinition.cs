using Survival.Items;
using UnityEngine;

namespace Survival.Crafting
{
    [CreateAssetMenu(menuName = "Survival/Crafting/Recipe", fileName = "Recipe_")]
    public class RecipeDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("ID estable para red y guardado.")]
        private int id;

        [SerializeField] private ItemAmount[] ingredients = new ItemAmount[0];
        [SerializeField] private ItemAmount output;
        [SerializeField, Min(0f)] private float craftSeconds = 2f;

        [Header("Requisitos de progresión")]
        [SerializeField] private CraftingStation requiredStation = CraftingStation.Hand;
        [SerializeField] private TechTier requiredTier = TechTier.Primitive;
        [SerializeField, Tooltip("Si es false hay que desbloquearla (plano, árbol tecnológico...).")]
        private bool unlockedByDefault = true;

        public int Id => id;
        public ItemAmount[] Ingredients => ingredients;
        public ItemAmount Output => output;
        public float CraftSeconds => craftSeconds;
        public CraftingStation RequiredStation => requiredStation;
        public TechTier RequiredTier => requiredTier;
        public bool UnlockedByDefault => unlockedByDefault;

        public bool IsValid => output.Item != null && output.Amount > 0;

        private void OnValidate()
        {
            if (output.Item != null && output.Item.Tier > requiredTier)
                Debug.LogWarning($"{name}: el resultado es de tier {output.Item.Tier} pero la receta solo exige {requiredTier}.", this);
        }
    }
}
