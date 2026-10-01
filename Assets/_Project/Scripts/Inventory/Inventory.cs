using System;
using Survival.Items;

namespace Survival.InventorySystem
{
    /// <summary>
    /// Inventario por slots en C# puro (sin MonoBehaviour). Así la misma lógica
    /// corre en el servidor autoritativo, en el cliente para predicción/UI y
    /// en tests de EditMode sin escena.
    /// </summary>
    public sealed class Inventory
    {
        private readonly ItemStack[] _slots;

        /// <summary>Se lanza con el índice del slot modificado. Úsalo para refrescar UI o marcar sync de red.</summary>
        public event Action<int> SlotChanged;

        public Inventory(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _slots = new ItemStack[capacity];
        }

        public int Capacity => _slots.Length;

        public ItemStack this[int index] => _slots[index];

        /// <summary>Añade ítems apilando primero en stacks existentes. Devuelve la cantidad que NO cupo.</summary>
        public int Add(ItemDefinition item, int amount) => Add(new ItemStack(item, amount));

        /// <inheritdoc cref="Add(ItemDefinition,int)"/>
        public int Add(ItemStack stack)
        {
            if (stack.IsEmpty) return 0;

            var item = stack.Item;
            int remaining = stack.Amount;

            if (item.IsStackable)
            {
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

                    int moved = Math.Min(item.MaxStack - _slots[i].Amount, remaining);
                    if (moved <= 0) continue;

                    _slots[i].Amount += moved;
                    remaining -= moved;
                    SlotChanged?.Invoke(i);
                }
            }

            // Un ítem roto se destruye al llegar a 0, así que 0 aquí significa "nuevo, a tope".
            int durability = item.HasDurability
                ? (stack.Durability > 0 ? Math.Min(stack.Durability, item.MaxDurability) : item.MaxDurability)
                : 0;

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (!_slots[i].IsEmpty) continue;

                int moved = Math.Min(item.MaxStack, remaining);
                _slots[i] = new ItemStack(item, moved, durability);
                remaining -= moved;
                SlotChanged?.Invoke(i);
            }

            return remaining;
        }

        /// <summary>Cuántas unidades de <paramref name="item"/> caben todavía.</summary>
        public int GetSpaceFor(ItemDefinition item)
        {
            if (item == null) return 0;

            int space = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty)
                    space += item.MaxStack;
                else if (_slots[i].Item == item)
                    space += item.MaxStack - _slots[i].Amount;
            }
            return space;
        }

        public bool CanAdd(ItemDefinition item, int amount) => GetSpaceFor(item) >= amount;

        public int Count(ItemDefinition item)
        {
            if (item == null) return 0;

            int total = 0;
            for (int i = 0; i < _slots.Length; i++)
                if (!_slots[i].IsEmpty && _slots[i].Item == item)
                    total += _slots[i].Amount;
            return total;
        }

        public bool Has(ItemDefinition item, int amount) => Count(item) >= amount;

        /// <summary>Quita <paramref name="amount"/> unidades. Todo o nada: si no hay suficientes no toca nada.</summary>
        public bool Remove(ItemDefinition item, int amount)
        {
            if (amount <= 0) return true;
            if (!Has(item, amount)) return false;

            // Desde el final para vaciar antes los slots de la mochila que los de la hotbar.
            for (int i = _slots.Length - 1; i >= 0 && amount > 0; i--)
            {
                if (_slots[i].IsEmpty || _slots[i].Item != item) continue;

                int taken = Math.Min(_slots[i].Amount, amount);
                SetAmount(i, _slots[i].Amount - taken);
                amount -= taken;
            }
            return true;
        }

        /// <summary>Quita hasta <paramref name="amount"/> de un slot concreto y devuelve lo extraído.</summary>
        public ItemStack RemoveAt(int index, int amount)
        {
            var slot = _slots[index];
            if (slot.IsEmpty || amount <= 0) return ItemStack.Empty;

            int taken = Math.Min(slot.Amount, amount);
            SetAmount(index, slot.Amount - taken);
            return new ItemStack(slot.Item, taken, slot.Durability);
        }

        /// <summary>Gasta durabilidad del ítem del slot. Devuelve true si se ha roto (y se elimina).</summary>
        public bool ReduceDurability(int index, int amount)
        {
            var slot = _slots[index];
            if (slot.IsEmpty || !slot.Item.HasDurability || amount <= 0) return false;

            slot.Durability -= amount;
            if (slot.Durability <= 0)
            {
                _slots[index] = ItemStack.Empty;
                SlotChanged?.Invoke(index);
                return true;
            }

            _slots[index] = slot;
            SlotChanged?.Invoke(index);
            return false;
        }

        /// <summary>Arrastrar y soltar: fusiona si es el mismo ítem apilable, si no intercambia.</summary>
        public void Move(int from, int to)
        {
            if (from == to) return;

            var source = _slots[from];
            var target = _slots[to];
            if (source.IsEmpty) return;

            if (!target.IsEmpty && target.Item == source.Item && source.Item.IsStackable)
            {
                int moved = Math.Min(source.Item.MaxStack - target.Amount, source.Amount);
                if (moved <= 0) return;

                _slots[to].Amount += moved;
                SetAmount(from, source.Amount - moved);
                SlotChanged?.Invoke(to);
                return;
            }

            _slots[from] = target;
            _slots[to] = source;
            SlotChanged?.Invoke(from);
            SlotChanged?.Invoke(to);
        }

        /// <summary>Sobrescribe un slot (carga de partida, sync de red desde el servidor).</summary>
        public void SetSlot(int index, ItemStack stack)
        {
            _slots[index] = stack.IsEmpty ? ItemStack.Empty : stack;
            SlotChanged?.Invoke(index);
        }

        /// <summary>Copia sin eventos, para simular operaciones (p.ej. "¿cabe el resultado tras gastar ingredientes?").</summary>
        public Inventory Clone()
        {
            var copy = new Inventory(_slots.Length);
            Array.Copy(_slots, copy._slots, _slots.Length);
            return copy;
        }

        private void SetAmount(int index, int amount)
        {
            if (amount <= 0)
                _slots[index] = ItemStack.Empty;
            else
                _slots[index].Amount = amount;
            SlotChanged?.Invoke(index);
        }
    }
}
