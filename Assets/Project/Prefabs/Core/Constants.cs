using UnityEngine;

namespace Aeloria.Core
{
    public static class Constants
    {
        // ===== PLAYER STATS =====
        public const float PLAYER_BASE_HEALTH = 10f;
        public const float PLAYER_MOVE_SPEED = 5f;
        public const float PLAYER_DODGE_COOLDOWN = 1f;
        public const float PLAYER_DODGE_DISTANCE = 3f;
        public const float PLAYER_DODGE_DURATION = 0.3f;

        // ===== ENERGY SYSTEM =====
        public const float ENERGY_MAX = 10f;
        public const float ENERGY_REGEN_RATE = 2f; // per second
        public const float ENERGY_MIN_TO_CAST = 1f;

        // ===== CARD SYSTEM =====
        public const int HAND_SIZE = 4;
        public const int STARTING_DECK_SIZE = 10;
        public const int MAX_DECK_SIZE = 20;
        public const float CARD_DRAW_ANIMATION_TIME = 0.3f;

        // ===== SUMMON DEFAULTS =====
        public const float SUMMON_DEFAULT_LIFETIME = 10f;
        public const int SUMMON_DEFAULT_MAX_HITS = 3;
        public const float SUMMON_SPAWN_OFFSET = 1.5f;

        // ===== COMBAT =====
        public const float DAMAGE_NUMBER_FLOAT_SPEED = 2f;
        public const float DAMAGE_NUMBER_LIFETIME = 1f;
        public const float HIT_FLASH_DURATION = 0.1f;

        // ===== CORRUPTION =====
        public const int CORRUPTION_FORCE_DISCARD = 5;
        public const int CORRUPTION_FULL_SHUFFLE = 15;
        public const int CORRUPTION_REMOVE_CARD = 20;

        // Corruption thresholds
        public const int CORRUPTION_PURE_MAX = 25;
        public const int CORRUPTION_TAINTED_MAX = 50;
        public const int CORRUPTION_CORRUPTED_MAX = 75;
        public const int CORRUPTION_CONSUMED_MAX = 99;

        // Enemy stat multipliers
        public const float CORRUPTION_TAINTED_MULTIPLIER = 1.1f;    // +10%
        public const float CORRUPTION_CORRUPTED_MULTIPLIER = 1.25f; // +25%
        public const float CORRUPTION_CONSUMED_MULTIPLIER = 1.4f;   // +40%

        // ===== ROOMS =====
        public const float ROOM_TRANSITION_TIME = 1f;
        public const int ROOMS_PER_FLOOR = 3;
        public const float ROOM_COMPLETE_DELAY = 1.5f;

        // ===== OBJECT POOL SIZES =====
        public const int POOL_SIZE_SUMMONS = 20;
        public const int POOL_SIZE_PROJECTILES = 50;
        public const int POOL_SIZE_VFX = 30;
        public const int POOL_SIZE_DAMAGE_NUMBERS = 20;

        // ===== LAYERS =====
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_ENEMY = "Enemy";
        public const string LAYER_SUMMON = "Summon";
        public const string LAYER_PROJECTILE = "Projectile";

        // ===== TAGS =====
        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY = "Enemy";
        public const string TAG_SUMMON = "Summon";

        // ===== DEBUG =====
        public const bool DEBUG_MODE = true;
        public const bool SHOW_DAMAGE_NUMBERS = true;
        public const bool IMMORTAL_MODE = false;
        public const bool INFINITE_ENERGY = false;
    }
}