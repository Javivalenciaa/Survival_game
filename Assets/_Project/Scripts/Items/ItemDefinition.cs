using UnityEngine;

namespace Survival.Items
{
    /// <summary>
    /// Datos inmutables de un tipo de ítem. Nunca guardes estado de partida
    /// aquí: un ScriptableObject es compartido por todas las instancias.
    /// </summary>
    [CreateAssetMenu(menuName = "Survival/Items/Item", fileName = "Item_")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("ID estable para red y guardado. No lo cambies tras publicar.")]
        private int id;

        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemCategory category;
        [SerializeField] private TechTier tier;
        [SerializeField, Min(1)] private int maxStack = 1;

        public int Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public TechTier Tier => tier;
        public int MaxStack => maxStack;
        public bool IsStackable => maxStack > 1;

        public virtual bool HasDurability => false;
        public virtual int MaxDurability => 0;

        protected virtual void OnValidate()
        {
            if (HasDurability && maxStack != 1)
            {
                Debug.LogWarning($"{name}: los ítems con durabilidad no pueden apilarse. maxStack forzado a 1.", this);
                maxStack = 1;
            }
        }
    }
}
