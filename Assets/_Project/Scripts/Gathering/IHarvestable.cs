using System.Collections.Generic;
using Survival.Items;

namespace Survival.Gathering
{
    public enum HarvestStatus
    {
        Success,
        Depleted,
        WrongTool,
        ToolTierTooLow
    }

    public interface IHarvestable
    {
        bool IsDepleted { get; }

        /// <summary>
        /// Aplica un golpe y escribe el botín en <paramref name="results"/>
        /// (lista reutilizada por el llamante para no generar basura).
        /// </summary>
        HarvestStatus Harvest(ToolDefinition tool, List<ItemStack> results);
    }
}
