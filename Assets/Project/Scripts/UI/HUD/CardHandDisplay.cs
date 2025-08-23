using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Aeloria.Core;

namespace Aeloria.UI
{
    /// <summary>
    /// Manages the display of cards in player's hand
    /// Coordinates between card system and UI
    /// </summary>
    public class CardHandDisplay : MonoBehaviour
    {
        [Header("Card Slots")]
        [SerializeField] private CardSlotUI[] cardSlots = new CardSlotUI[4];  // 4 slots for ABXY
        [SerializeField] private Transform cardSlotContainer;

        [Header("Next Card Preview")]
        [SerializeField] private GameObject nextCardPreview;
        [SerializeField] private Image nextCardArt;
        [SerializeField] private Text nextCardName;

        [Header("Deck Counter")]
        [SerializeField] private Text deckCountText;
        [SerializeField] private Text discardCountText;

        [Header("Visual Settings")]
        [SerializeField] private float slotSpacing = 150f;
        [SerializeField] private bool autoArrangeSlots = true;

        // State tracking
        private float currentEnergy = 0f;
        private Dictionary<int, object> slotCardMap;

        private void Awake()
        {
            // Initialize collections
            slotCardMap = new Dictionary<int, object>();

            // Find or create card slots if not assigned
            if (cardSlots[0] == null)
            {
                FindOrCreateCardSlots();
            }

            // Subscribe to events
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Find existing card slots or create them
        /// </summary>
        private void FindOrCreateCardSlots()
        {
            // Try to find existing slots
            CardSlotUI[] foundSlots = cardSlotContainer != null
                ? cardSlotContainer.GetComponentsInChildren<CardSlotUI>()
                : GetComponentsInChildren<CardSlotUI>();

            if (foundSlots.Length >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    cardSlots[i] = foundSlots[i];
                }
            }
            else
            {
                Debug.LogWarning("Not enough CardSlotUI components found! Need 4 slots.");
            }

            // Arrange slots if needed
            if (autoArrangeSlots)
            {
                ArrangeSlots();
            }
        }

        /// <summary>
        /// Arrange card slots horizontally
        /// </summary>
        private void ArrangeSlots()
        {
            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                {
                    RectTransform rect = cardSlots[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float xPos = (i - 1.5f) * slotSpacing;  // Center around 0
                        rect.anchoredPosition = new Vector2(xPos, rect.anchoredPosition.y);
                    }
                }
            }
        }

        /// <summary>
        /// Subscribe to game events
        /// </summary>
        private void SubscribeToEvents()
        {
            UIEvents.OnCardDrawn += OnCardDrawn;
            UIEvents.OnCardPlayed += OnCardPlayed;
            UIEvents.OnCardChargesChanged += OnCardChargesChanged;
            UIEvents.OnEnergyChanged += OnEnergyChanged;
        }

        /// <summary>
        /// Unsubscribe from game events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            UIEvents.OnCardDrawn -= OnCardDrawn;
            UIEvents.OnCardPlayed -= OnCardPlayed;
            UIEvents.OnCardChargesChanged -= OnCardChargesChanged;
            UIEvents.OnEnergyChanged -= OnEnergyChanged;
        }

        /// <summary>
        /// Handle card drawn event
        /// </summary>
        private void OnCardDrawn(int slotIndex, object cardData)
        {
            if (slotIndex >= 0 && slotIndex < cardSlots.Length)
            {
                if (cardSlots[slotIndex] != null)
                {
                    // Use SetCard method that exists in CardSlotUI
                    cardSlots[slotIndex].SetCard(cardData);
                    slotCardMap[slotIndex] = cardData;

                    // Update availability based on current energy
                    cardSlots[slotIndex].UpdateAvailability(currentEnergy);
                }
            }
        }

        /// <summary>
        /// Handle card played event
        /// </summary>
        private void OnCardPlayed(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < cardSlots.Length)
            {
                if (cardSlots[slotIndex] != null)
                {
                    // Use PlayCard method that exists in CardSlotUI
                    cardSlots[slotIndex].PlayCard();
                    slotCardMap.Remove(slotIndex);
                }
            }
        }

        /// <summary>
        /// Handle card charges changed event
        /// </summary>
        private void OnCardChargesChanged(int slotIndex, int charges)
        {
            if (slotIndex >= 0 && slotIndex < cardSlots.Length)
            {
                if (cardSlots[slotIndex] != null)
                {
                    if (charges <= 0)
                    {
                        // Card exhausted - clear the slot
                        cardSlots[slotIndex].SetEmpty();
                        slotCardMap.Remove(slotIndex);
                    }
                    else
                    {
                        // Update charges display
                        cardSlots[slotIndex].UpdateCharges(charges);
                    }
                }
            }
        }

        /// <summary>
        /// Handle energy changed event
        /// </summary>
        private void OnEnergyChanged(float current, float max)
        {
            currentEnergy = current;

            // Update all card slots availability
            foreach (var slot in cardSlots)
            {
                if (slot != null)
                {
                    slot.UpdateAvailability(currentEnergy);
                }
            }
        }

        /// <summary>
        /// Update deck counter display
        /// </summary>
        public void UpdateDeckCount(int deckCount, int discardCount)
        {
            if (deckCountText) deckCountText.text = deckCount.ToString();
            if (discardCountText) discardCountText.text = discardCount.ToString();
        }

        /// <summary>
        /// Update next card preview
        /// </summary>
        public void UpdateNextCardPreview(object cardData)
        {
            if (nextCardPreview == null) return;

            if (cardData != null)
            {
                nextCardPreview.SetActive(true);
                // Update preview visuals when we have CardData class
                if (nextCardName) nextCardName.text = "Next Card";
            }
            else
            {
                nextCardPreview.SetActive(false);
            }
        }

        /// <summary>
        /// Get card slot at index
        /// </summary>
        public CardSlotUI GetSlot(int index)
        {
            if (index >= 0 && index < cardSlots.Length)
            {
                return cardSlots[index];
            }
            return null;
        }

        /// <summary>
        /// Highlight slot when hovering or targeting
        /// </summary>
        public void HighlightSlot(int slotIndex, bool highlight)
        {
            if (slotIndex >= 0 && slotIndex < cardSlots.Length)
            {
                if (cardSlots[slotIndex] != null)
                {
                    cardSlots[slotIndex].SetHighlight(highlight);
                }
            }
        }
    }
}