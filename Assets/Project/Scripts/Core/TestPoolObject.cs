using UnityEngine;
using Aeloria.Core;

public class TestPoolObject : MonoBehaviour, IPoolable
{
    private float lifetime = 2f;
    private float currentLife;

    public void OnSpawnFromPool()
    {
        Debug.Log($"{gameObject.name} spawned from pool!");
        currentLife = lifetime;
    }

    public void OnReturnToPool()
    {
        Debug.Log($"{gameObject.name} returned to pool!");
    }

    public void ResetPoolObject()
    {
        // Reset any values
        transform.localScale = Vector3.one;
        currentLife = lifetime;
    }

    private void Update()
    {
        currentLife -= Time.deltaTime;

        // Auto-return to pool after lifetime
        if (currentLife <= 0)
        {
            ObjectPooler.Return("TestObject", gameObject);
        }

        // Visual feedback - spin and shrink
        transform.Rotate(Vector3.up, 180 * Time.deltaTime);
        transform.localScale = Vector3.one * (currentLife / lifetime);
    }
}