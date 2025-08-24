using UnityEngine;
using System.Collections.Generic;
using Aeloria.Core;

namespace Aeloria.Cards
{
    public class CardManager : MonoBehaviour
    {
        [Header("Card Slots")]
        [SerializeField] private CardData[] cardSlots = new CardData[4];
        [SerializeField] private int[] currentCharges = new int[4];

        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 2f;
        [SerializeField] private float spawnHeightOffset = 2f; // Height above player
        [SerializeField] private Transform player;

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

            // Check charges
            if (currentCharges[slotIndex] <= 0)
            {
                Debug.Log($"{card.cardName} has no charges left!");
                return;
            }

            // Cast the card
            CastCard(card, slotIndex);
        }

        void CastCard(CardData card, int slotIndex)
        {
            Debug.Log($"Casting {card.cardName}!");

            // Get spawn direction
            Vector3 spawnDirection = Vector3.forward; // Default forward

            // Try to use player's forward direction
            if (player.forward != Vector3.zero)
            {
                spawnDirection = player.forward;
            }
            else
            {
                // If no forward (2D sprite), use world forward
                spawnDirection = new Vector3(0, 0, 1);
            }

            // Calculate spawn position
            Vector3 spawnPos = player.position + (spawnDirection.normalized * spawnDistance);

            // Force spawn height to be well above grid
            spawnPos.y = 3f; // High enough to clear any floor

            Debug.Log($"Spawning {card.cardName} at position: {spawnPos}");

            // Spawn the entity
            if (card.prefabToSpawn != null)
            {
                GameObject spawned = Instantiate(card.prefabToSpawn, spawnPos, Quaternion.identity);

                // Ensure the spawned entity has proper physics setup
                if (spawned.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    Debug.Log($"Wolf Rigidbody - IsKinematic: {rb.isKinematic}, UseGravity: {rb.useGravity}");
                }

                // Use a charge
                currentCharges[slotIndex]--;
                Debug.Log($"{card.cardName} charges remaining: {currentCharges[slotIndex]}");

                // Trigger events
                EventManager.TriggerEvent("CardCast", card);
            }
            else
            {
                Debug.LogError($"Card {card.cardName} has no prefab assigned!");
            }
        }
    }
}