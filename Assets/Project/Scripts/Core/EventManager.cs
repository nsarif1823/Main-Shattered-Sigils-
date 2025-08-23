using UnityEngine;
using System;
using System.Collections.Generic;

namespace Aeloria.Core
{
    public class EventManager : MonoBehaviour
    {
        private static EventManager _instance;
        public static EventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[EventManager]");
                    _instance = go.AddComponent<EventManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Dictionary to hold all events
        private Dictionary<string, Action<object>> eventDictionary = new Dictionary<string, Action<object>>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Subscribe to events
        public static void StartListening(string eventName, Action<object> listener)
        {
            if (Instance.eventDictionary.TryGetValue(eventName, out Action<object> thisEvent))
            {
                thisEvent += listener;
                Instance.eventDictionary[eventName] = thisEvent;
            }
            else
            {
                thisEvent += listener;
                Instance.eventDictionary.Add(eventName, thisEvent);
            }

            Debug.Log($"Started listening to: {eventName}");
        }

        // Unsubscribe from events
        public static void StopListening(string eventName, Action<object> listener)
        {
            if (_instance == null) return;

            if (Instance.eventDictionary.TryGetValue(eventName, out Action<object> thisEvent))
            {
                thisEvent -= listener;
                Instance.eventDictionary[eventName] = thisEvent;

                if (thisEvent == null)
                {
                    Instance.eventDictionary.Remove(eventName);
                }
            }
        }

        // Trigger events
        public static void TriggerEvent(string eventName, object data = null)
        {
            if (Instance.eventDictionary.TryGetValue(eventName, out Action<object> thisEvent))
            {
                thisEvent?.Invoke(data);
                Debug.Log($"Event Triggered: {eventName}");
            }
        }

        // Clean up on destroy
        private void OnDestroy()
        {
            if (_instance == this)
            {
                eventDictionary.Clear();
                _instance = null;
            }
        }
    }

    // Event name constants to avoid typos
    public static class GameEvents
    {
        // Core game events
        public const string GAME_STARTED = "GameStarted";
        public const string GAME_PAUSED = "GamePaused";
        public const string GAME_RESUMED = "GameResumed";
        public const string GAME_OVER = "GameOver";

        // Combat events (for later)
        public const string ENEMY_KILLED = "EnemyKilled";
        public const string PLAYER_DAMAGED = "PlayerDamaged";
        public const string CARD_PLAYED = "CardPlayed";
        public const string SUMMON_SPAWNED = "SummonSpawned";
        public const string ROOM_COMPLETED = "RoomCompleted";
    }
}