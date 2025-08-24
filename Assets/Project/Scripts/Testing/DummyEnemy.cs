using UnityEngine;
using Aeloria.Entities;

public class DummyEnemy : EntityBase
{
    protected override void Start()
    {
        base.Start();
        entityName = "Dummy";
        maxHealth = 30;
        CurrentHealth = maxHealth;
    }

    protected override void HandleDeath()
    {
        Debug.Log("Enemy defeated!");
        Destroy(gameObject);
    }
}