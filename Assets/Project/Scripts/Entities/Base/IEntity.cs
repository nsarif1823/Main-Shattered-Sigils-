using UnityEngine;

namespace Aeloria.Entities
{
    public interface IEntity
    {
        string EntityName { get; }
        Transform Transform { get; }
        bool IsAlive { get; }
        void Initialize();
    }

    public interface IDamageable
    {
        float MaxHealth { get; }
        float CurrentHealth { get; }
        void TakeDamage(float damage, GameObject source);
        void Heal(float amount);
        void Die();
    }

    public interface ITargetable
    {
        bool CanBeTargeted { get; }
        Vector3 GetTargetPosition();
        float GetTargetPriority();
    }

    public interface IMoveable
    {
        float MoveSpeed { get; }
        void Move(Vector3 direction);
        void StopMovement();
    }
}