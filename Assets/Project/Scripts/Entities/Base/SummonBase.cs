using Aeloria.Cards;
using Aeloria.Core;
using System.Collections;
using UnityEngine;

namespace Aeloria.Entities.Summons
{
    public abstract class SummonBase : EntityBase
    {
        [Header("Summon Core")]
        [SerializeField] protected float lifetime = 10f;
        [SerializeField] protected bool destroyOnOwnerDeath = true;

        [Header("Secondary Effect")]
        [SerializeField] protected bool hasSecondaryEffect = true;
        [SerializeField] protected float secondaryCooldown = 2f;
        [SerializeField] protected float secondaryEnergyCost = 1f;

        protected GameObject owner;
        protected CardData sourceCard;
        protected int summonID;

        // Public accessors
        public GameObject Owner => owner;
        public CardData SourceCard => sourceCard;
        public int SummonID => summonID;
        public bool HasSecondaryEffect => hasSecondaryEffect;
        public float SecondaryCooldown => secondaryCooldown;
        public new string EntityName => entityName; // FIX: Added this property

        protected float timeAlive = 0f;
        protected float lastSecondaryUse = -999f;
        protected bool isExpired = false;

        public System.Action<SummonBase> OnSummonExpired;
        public System.Action<SummonBase> OnSecondaryActivated;

        public virtual void InitializeSummon(GameObject summoner, CardData card, int id)
        {
            owner = summoner;
            sourceCard = card;
            summonID = id;

            EventManager.TriggerEvent("SummonCreated", new SummonEventData
            {
                summon = this,
                owner = summoner,
                card = card
            });

            if (lifetime > 0)
            {
                StartCoroutine(LifetimeCountdown());
            }
        }

        public virtual bool TrySecondaryEffect()
        {
            if (!hasSecondaryEffect)
            {
                Debug.Log($"{entityName} has no secondary effect");
                return false;
            }

            if (!IsAlive || isExpired)
            {
                Debug.Log($"{entityName} is dead/expired");
                return false;
            }

            if (Time.time - lastSecondaryUse < secondaryCooldown)
            {
                float remaining = secondaryCooldown - (Time.time - lastSecondaryUse);
                Debug.Log($"Secondary on cooldown: {remaining:F1}s");
                return false;
            }

            // FIX: Simplified energy check - removed PlayerController dependency
            // Energy is now handled by CardManager

            ExecuteSecondaryEffect();
            lastSecondaryUse = Time.time;
            OnSecondaryActivated?.Invoke(this);

            EventManager.TriggerEvent("SecondaryEffectUsed", new SummonEventData
            {
                summon = this,
                owner = owner,
                card = sourceCard
            });

            return true;
        }

        protected abstract void ExecuteSecondaryEffect();

        protected virtual IEnumerator LifetimeCountdown()
        {
            while (timeAlive < lifetime && IsAlive && !isExpired)
            {
                timeAlive += Time.deltaTime;
                yield return null;
            }

            if (IsAlive && !isExpired)
            {
                ExpireSummon();
            }
        }

        protected virtual void ExpireSummon()
        {
            isExpired = true;
            OnSummonExpired?.Invoke(this);

            EventManager.TriggerEvent("SummonExpired", new SummonEventData
            {
                summon = this,
                owner = owner,
                card = sourceCard
            });

            StartCoroutine(FadeAndDestroy());
        }

        protected override void HandleDeath()
        {
            isExpired = true;

            EventManager.TriggerEvent("SummonDied", new SummonEventData
            {
                summon = this,
                owner = owner,
                card = sourceCard
            });

            OnSummonExpired?.Invoke(this);
            Destroy(gameObject, 0.5f);
        }

        protected virtual IEnumerator FadeAndDestroy()
        {
            float fadeTime = 1f;
            float elapsed = 0;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            while (elapsed < fadeTime)
            {
                float alpha = 1f - (elapsed / fadeTime);

                foreach (var rend in renderers)
                {
                    if (rend.material.HasProperty("_Color"))
                    {
                        Color c = rend.material.color;
                        c.a = alpha;
                        rend.material.color = c;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        public float GetLifetimePercent()
        {
            if (lifetime <= 0) return 1f;
            return Mathf.Clamp01(1f - (timeAlive / lifetime));
        }

        public bool IsSecondaryReady()
        {
            return hasSecondaryEffect &&
                   IsAlive &&
                   !isExpired &&
                   (Time.time - lastSecondaryUse >= secondaryCooldown);
        }

        protected virtual void OnDestroy()
        {
            OnSummonExpired = null;
            OnSecondaryActivated = null;
        }
    }

    public class SummonEventData
    {
        public SummonBase summon;
        public GameObject owner;
        public CardData card;
    }
}