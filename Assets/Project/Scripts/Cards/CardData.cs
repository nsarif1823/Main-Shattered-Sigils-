using UnityEngine;

namespace Aeloria.Cards
{
    [CreateAssetMenu(fileName = "New Card", menuName = "Aeloria/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        public string cardName = "Unnamed Card";
        public Sprite cardIcon;
        public string description;

        [Header("Card Type")]
        public CardType cardType;
        public GameObject prefabToSpawn; // What this card spawns

        [Header("Cost & Charges")]
        public int energyCost = 2;
        public int maxCharges = 3;
        public float cooldown = 1f;

        [Header("Stats (for summons)")]
        public float health = 10f;
        public float damage = 5f;
        public float moveSpeed = 3f;
    }

    public enum CardType
    {
        Summon,
        Spell,
        Weapon,
        Enhancement
    }
}