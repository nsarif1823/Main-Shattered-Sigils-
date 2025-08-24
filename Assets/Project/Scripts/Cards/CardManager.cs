using UnityEngine;
using System.Collections.Generic;
using Aeloria.Core;
using Aeloria.Entities.Summons;

namespace Aeloria.Cards
{
    public class CardManager : MonoBehaviour
    {
        [Header("Card Slots")]
        [SerializeField] private CardData[] cardSlots = new CardData[4];
        [SerializeField] private int[] currentCharges = new int[4];

        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 2f;
        [SerializeField] private float spawnHeightOffset = 2f;
        [SerializeField] private Transform player;

        // Track active summons for secondary effects
        private Dictionary<CardData, SummonBase> activeSummons = new Dictionary<CardData, SummonBase>();
        private int nextSummonID = 0;

        void Start()
        {
            // Initialize charges
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                    currentCharges[i] = cardSlots[i].maxCharges;
            }

            // Find player if not assigned
            if (player == null)
                player = GameObject.FindWithTag("Player")?.transform;
        }

        void Update()
        {
            // Check for card inputs (1-4 keys)
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    TryCastCard(i);
                }
            }
        }

        void TryCastCard(int slotIndex)
        {
            if (cardSlots[slotIndex] == null)
            {
                Debug.Log($"No card in slot {slotIndex + 1}");
                return;
            }

            CardData card = cardSlots[slotIndex];

            // CHECK FOR SECONDARY EFFECT FIRST
            if (activeSummons.TryGetValue(card, out SummonBase existingSummon))
            {
                if (existingSummon != null && existingSummon.HasSecondaryEffect)
                {
                    // Try to use secondary effect instead of spawning new summon
                    if (existingSummon.TrySecondaryEffect())
                    {
                        Debug.Log($"Used secondary effect for {card.cardName}!");
                        EventManager.TriggerEvent("SecondaryEffectUsed", card);
                        return; // Don't spawn new summon
                    }
                }
            }

            // No active summon or secondary failed - check charges for primary cast
            if (currentCharges[slotIndex] <= 0)
            {
                Debug.Log($"{card.cardName} has no charges left!");
                return;
            }

            // Cast the primary card effect
            CastCard(card, slotIndex);
        }

        void CastCard(CardData card, int slotIndex)
        {
            Debug.Log($"Casting {card.cardName}!");

            // Get spawn direction based on player facing or use default
            Vector3 spawnDirection = player.forward;
            if (spawnDirection.magnitude < 0.1f)
            {
                // If player has no forward (2D sprite), use a default direction
                spawnDirection = new Vector3(0, 0, 1);
            }

            // Calculate spawn position
            Vector3 spawnPos = player.position + (spawnDirection.normalized * spawnDistance);

            // CRITICAL FIX: Set Y to exactly 1 unit above ground
            // This ensures summons spawn at correct height regardless of grid
            spawnPos.y = 1f;

            Debug.Log($"Spawning {card.cardName} at position: {spawnPos}");

            if (card.prefabToSpawn != null)
            {
                GameObject spawned = Instantiate(card.prefabToSpawn, spawnPos, Quaternion.identity);

                // Initialize summon if it has SummonBase
                var summonBase = spawned.GetComponent<SummonBase>();
                if (summonBase != null)
                {
                    summonBase.InitializeSummon(player.gameObject, card, nextSummonID++);

                    if (activeSummons.ContainsKey(card))
                    {
                        CleanupSummon(card);
                    }
                    activeSummons[card] = summonBase;

                    summonBase.OnSummonExpired += (summon) => OnSummonExpired(card, summon);
                }

                // Ensure physics setup for future-proofing
                if (spawned.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = true; // Let gravity handle positioning
                    rb.constraints = RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ;
                }

                currentCharges[slotIndex]--;
                Debug.Log($"{card.cardName} charges remaining: {currentCharges[slotIndex]}");

                EventManager.TriggerEvent("CardCast", card);
            }
            else
            {
                Debug.LogError($"Card {card.cardName} has no prefab assigned!");
            }
        }

        /// <summary>
        /// Handle when a summon expires or dies
        /// </summary>
        void OnSummonExpired(CardData card, SummonBase summon)
        {
            if (activeSummons.ContainsKey(card) && activeSummons[card] == summon)
            {
                activeSummons.Remove(card);
                Debug.Log($"{card.cardName} summon expired - card can spawn new one");
            }
        }

        /// <summary>
        /// Clean up old summon reference
        /// </summary>
        void CleanupSummon(CardData card)
        {
            if (activeSummons.TryGetValue(card, out var oldSummon))
            {
                if (oldSummon != null)
                {
                    // Optionally destroy old summon when spawning new one
                    // Destroy(oldSummon.gameObject);
                }
                activeSummons.Remove(card);
            }
        }

        /// <summary>
        /// Check if a card has an active summon
        /// </summary>
        public bool HasActiveSummon(CardData card)
        {
            return activeSummons.ContainsKey(card) &&
                   activeSummons[card] != null &&
                   activeSummons[card].IsAlive;
        }

        /// <summary>
        /// Get active summon for a card (for UI purposes)
        /// </summary>
        public SummonBase GetActiveSummon(CardData card)
        {
            activeSummons.TryGetValue(card, out var summon);
            return summon;
        }
    }
}