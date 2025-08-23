using System;
using UnityEngine;

namespace Aeloria.UI
{
    /// <summary>
    /// Centralized UI event definitions for decoupled UI updates
    /// All UI elements subscribe to these events rather than directly referencing game objects
    /// </summary>
    public static class UIEvents
    {
        // ===== HEALTH EVENTS =====
        public static event Action<float, float, GameObject> OnHealthChanged;  // current, max, entity
        public static event Action<float, GameObject> OnDamageReceived;        // amount, entity
        public static event Action<float, GameObject> OnHealReceived;          // amount, entity

        // ===== ENERGY EVENTS =====
        public static event Action<float, float> OnEnergyChanged;              // current, max
        public static event Action<float> OnEnergyConsumed;                    // amount
        public static event Action<float> OnEnergyRegenerated;                 // amount

        // ===== CARD EVENTS =====
        public static event Action<int, object> OnCardDrawn;                   // slotIndex, cardData
        public static event Action<int> OnCardPlayed;                          // slotIndex
        public static event Action<int, int> OnCardChargesChanged;             // slotIndex, charges
        public static event Action<object> OnCardExhausted;                    // cardData

        // ===== CORRUPTION EVENTS =====
        public static event Action<int, int> OnCorruptionChanged;              // current, max
        public static event Action<string> OnCorruptionStateChanged;           // state name

        // ===== ROOM/COMBAT EVENTS =====
        public static event Action<string> OnRoomEntered;                      // room name
        public static event Action OnCombatStarted;
        public static event Action OnCombatEnded;

        // ===== CURRENCY EVENTS =====
        public static event Action<string, int> OnCurrencyChanged;             // currency type, amount

        // ===== NOTIFICATION EVENTS =====
        public static event Action<string, float> OnNotificationRequested;     // message, duration
        public static event Action<string, string, float> OnTooltipRequested;  // title, description, duration

        // ===== TRIGGER METHODS =====
        // These are called by game systems to notify UI

        public static void TriggerHealthChanged(float current, float max, GameObject entity)
        {
            OnHealthChanged?.Invoke(current, max, entity);
        }

        public static void TriggerDamageReceived(float amount, GameObject entity)
        {
            OnDamageReceived?.Invoke(amount, entity);
        }

        public static void TriggerHealReceived(float amount, GameObject entity)
        {
            OnHealReceived?.Invoke(amount, entity);
        }

        public static void TriggerEnergyChanged(float current, float max)
        {
            OnEnergyChanged?.Invoke(current, max);
        }

        public static void TriggerCardDrawn(int slotIndex, object cardData)
        {
            OnCardDrawn?.Invoke(slotIndex, cardData);
        }

        public static void TriggerCardPlayed(int slotIndex)
        {
            OnCardPlayed?.Invoke(slotIndex);
        }

        public static void TriggerCorruptionChanged(int current, int max)
        {
            OnCorruptionChanged?.Invoke(current, max);
        }

        // Clear all subscriptions (useful for scene changes)
        public static void ClearAllSubscriptions()
        {
            OnHealthChanged = null;
            OnDamageReceived = null;
            OnHealReceived = null;
            OnEnergyChanged = null;
            OnCardDrawn = null;
            OnCardPlayed = null;
            OnCardChargesChanged = null;
            OnCardExhausted = null;
            OnCorruptionChanged = null;
            OnCorruptionStateChanged = null;
            OnRoomEntered = null;
            OnCombatStarted = null;
            OnCombatEnded = null;
            OnCurrencyChanged = null;
            OnNotificationRequested = null;
            OnTooltipRequested = null;
        }
    }
}