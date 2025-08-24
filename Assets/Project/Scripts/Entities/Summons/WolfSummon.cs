using UnityEngine;
using System.Collections.Generic;
using Aeloria.Core;

namespace Aeloria.Entities.Summons
{
    public class WolfSummon : SummonBase
    {
        [Header("Wolf Secondary - Pack Howl")]
        [SerializeField] private float howlRadius = 5f;
        [SerializeField] private float howlDamageBuff = 1.5f;
        [SerializeField] private float howlDuration = 3f;
        [SerializeField] private float howlHeal = 5f;

        private WolfAI wolfAI;

        protected override void Awake()
        {
            base.Awake();
            entityName = "Wolf Summon";

            wolfAI = GetComponent<WolfAI>();
            if (wolfAI == null)
            {
                wolfAI = gameObject.AddComponent<WolfAI>();
            }
        }

        protected override void ExecuteSecondaryEffect()
        {
            Debug.Log($"{entityName} uses Pack Howl!");

            Heal(howlHeal);

            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, howlRadius);
            List<SummonBase> buffedAllies = new List<SummonBase>();

            foreach (var col in nearbyColliders)
            {
                var summon = col.GetComponent<SummonBase>();

                if (summon != null && summon != this && summon.Owner == owner)
                {
                    ApplyHowlBuff(summon);
                    buffedAllies.Add(summon);
                }
            }

            CreateHowlEffect();
            Debug.Log($"Pack Howl buffed {buffedAllies.Count} allies!");
        }

        private void ApplyHowlBuff(SummonBase ally)
        {
            ally.Heal(howlHeal * 0.5f);
            Debug.Log($"Buffed {ally.EntityName}"); // FIX: Using EntityName property
        }

        private void CreateHowlEffect()
        {
            EventManager.TriggerEvent("PlayEffect", new EffectData
            {
                effectName = "HowlRing",
                position = transform.position,
                scale = howlRadius
            });

            EventManager.TriggerEvent("PlaySound", "WolfHowl");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, howlRadius);
        }
    }

    public class EffectData
    {
        public string effectName;
        public Vector3 position;
        public float scale;
    }
}