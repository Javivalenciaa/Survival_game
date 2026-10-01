using System;
using System.Collections.Generic;
using Survival.Items;
using UnityEngine;

namespace Survival.Gathering
{
    /// <summary>
    /// Árbol, roca o veta. El botín es proporcional al daño infligido: una
    /// herramienta mejor agota el nodo en menos golpes y, con su
    /// yieldMultiplier, saca más recursos del mismo nodo.
    ///
    /// Rendimiento: el componente está desactivado (sin Update) salvo mientras
    /// espera a reaparecer, así miles de nodos en el mapa no cuestan CPU.
    /// En multijugador, Harvest() solo se ejecuta en el servidor y la vida /
    /// estado agotado se sincroniza a los clientes.
    /// </summary>
    [DisallowMultipleComponent]
    public class ResourceNode : MonoBehaviour, IHarvestable
    {
        [SerializeField] private ResourceNodeDefinition definition;
        [SerializeField, Tooltip("Malla visible; se oculta al agotarse.")]
        private GameObject visualRoot;
        [SerializeField] private Collider hitCollider;

        private float _health;
        private float[] _yieldRemainders;
        private float _respawnAt;

        /// <summary>(nodo, daño aplicado) — para VFX/SFX de impacto.</summary>
        public event Action<ResourceNode, float> Hit;
        public event Action<ResourceNode> Depleted;
        public event Action<ResourceNode> Respawned;

        public ResourceNodeDefinition Definition => definition;
        public bool IsDepleted { get; private set; }
        public float Health01 => definition != null ? _health / definition.MaxHealth : 0f;

        private void Awake()
        {
            if (hitCollider == null) hitCollider = GetComponentInChildren<Collider>();
            if (definition == null)
            {
                Debug.LogError($"{name}: ResourceNode sin ResourceNodeDefinition.", this);
                enabled = false;
                return;
            }

            _yieldRemainders = new float[definition.Yields.Length];
            ResetNode();
        }

        private void Update()
        {
            if (Time.time >= _respawnAt) Respawn();
        }

        public HarvestStatus Harvest(ToolDefinition tool, List<ItemStack> results)
        {
            if (definition == null || IsDepleted) return HarvestStatus.Depleted;

            var status = definition.Evaluate(tool, out float efficiency);
            if (status != HarvestStatus.Success) return status;

            float damage = Mathf.Min(tool.GatherPower * efficiency, _health);
            if (damage <= 0f) return HarvestStatus.WrongTool;

            _health -= damage;
            bool depleting = _health <= 0.001f;

            float fraction = damage / definition.MaxHealth;
            var yields = definition.Yields;
            for (int i = 0; i < yields.Length; i++)
            {
                if (yields[i].Item == null) continue;

                // Acumulamos decimales entre golpes para no perder recursos por redondeo.
                _yieldRemainders[i] += yields[i].TotalAmount * fraction * tool.YieldMultiplier;
                int whole = depleting
                    ? Mathf.RoundToInt(_yieldRemainders[i])
                    : Mathf.FloorToInt(_yieldRemainders[i]);
                _yieldRemainders[i] -= whole;

                if (whole > 0) results.Add(new ItemStack(yields[i].Item, whole));
            }

            Hit?.Invoke(this, damage);

            if (depleting)
            {
                foreach (var bonus in definition.DepletionBonus)
                    if (bonus.Item != null && bonus.Amount > 0)
                        results.Add(new ItemStack(bonus.Item, bonus.Amount));
                Deplete();
            }

            return HarvestStatus.Success;
        }

        private void Deplete()
        {
            IsDepleted = true;
            _health = 0f;
            SetVisible(false);
            Depleted?.Invoke(this);

            if (definition.RespawnSeconds > 0f)
            {
                _respawnAt = Time.time + definition.RespawnSeconds;
                enabled = true;
            }
        }

        private void Respawn()
        {
            ResetNode();
            Respawned?.Invoke(this);
        }

        private void ResetNode()
        {
            _health = definition.MaxHealth;
            Array.Clear(_yieldRemainders, 0, _yieldRemainders.Length);
            IsDepleted = false;
            SetVisible(true);
            enabled = false;
        }

        private void SetVisible(bool visible)
        {
            if (visualRoot != null) visualRoot.SetActive(visible);
            if (hitCollider != null) hitCollider.enabled = visible;
        }
    }
}
