using System.Collections.Generic;
using UnityEngine;

namespace Aeloria.Core
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public bool expandable = true;
    }

    public class ObjectPooler : MonoBehaviour
    {
        private static ObjectPooler _instance;
        public static ObjectPooler Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[ObjectPooler]");
                    _instance = go.AddComponent<ObjectPooler>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Dictionary of all pools
        private Dictionary<string, Queue<GameObject>> poolDictionary;
        private Dictionary<string, Pool> poolConfigs;
        private Transform poolParent;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        private void InitializePools()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            poolConfigs = new Dictionary<string, Pool>();

            // Create parent for organization
            poolParent = new GameObject("PooledObjects").transform;
            poolParent.SetParent(transform);

            Debug.Log("Object Pooler Initialized");
        }

        // Register a pool at runtime
        public void CreatePool(string poolTag, GameObject prefab, int size, bool expandable = true)
        {
            if (poolDictionary.ContainsKey(poolTag))
            {
                Debug.LogWarning($"Pool {poolTag} already exists!");
                return;
            }

            // Store config for expansion
            Pool poolConfig = new Pool
            {
                tag = poolTag,
                prefab = prefab,
                size = size,
                expandable = expandable
            };
            poolConfigs[poolTag] = poolConfig;

            // Create the pool
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Create parent for this pool type
            GameObject poolTypeParent = new GameObject($"Pool_{poolTag}");
            poolTypeParent.transform.SetParent(poolParent);

            for (int i = 0; i < size; i++)
            {
                GameObject obj = CreatePooledObject(prefab, poolTypeParent.transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(poolTag, objectPool);

            Debug.Log($"Created pool: {poolTag} with {size} objects");
        }

        private GameObject CreatePooledObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);

            // Add poolable component if it doesn't have one
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable == null)
            {
                // Object doesn't implement IPoolable, but that's okay
                // Not all pooled objects need special reset logic
            }

            return obj;
        }

        // Get object from pool
        public GameObject SpawnFromPool(string poolTag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(poolTag))
            {
                Debug.LogError($"Pool {poolTag} doesn't exist!");
                return null;
            }

            Queue<GameObject> pool = poolDictionary[poolTag];
            GameObject objectToSpawn = null;

            // Check if we have available objects
            if (pool.Count > 0)
            {
                objectToSpawn = pool.Dequeue();
            }
            else if (poolConfigs[poolTag].expandable)
            {
                // Expand pool if allowed
                GameObject parent = GameObject.Find($"Pool_{poolTag}");
                objectToSpawn = CreatePooledObject(poolConfigs[poolTag].prefab, parent.transform);
                Debug.Log($"Expanded pool: {poolTag}");
            }
            else
            {
                Debug.LogWarning($"Pool {poolTag} is empty and not expandable!");
                return null;
            }

            // Set position and activate
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            // Call spawn callback if it exists
            IPoolable poolable = objectToSpawn.GetComponent<IPoolable>();
            poolable?.OnSpawnFromPool();

            // Trigger event
            EventManager.TriggerEvent("ObjectSpawned", new { tag = poolTag, obj = objectToSpawn });

            return objectToSpawn;
        }

        // Return object to pool
        public void ReturnToPool(string poolTag, GameObject objectToReturn)
        {
            if (!poolDictionary.ContainsKey(poolTag))
            {
                Debug.LogError($"Pool {poolTag} doesn't exist!");
                Destroy(objectToReturn);
                return;
            }

            // Call return callback if it exists
            IPoolable poolable = objectToReturn.GetComponent<IPoolable>();
            poolable?.OnReturnToPool();
            poolable?.ResetPoolObject();

            objectToReturn.SetActive(false);
            poolDictionary[poolTag].Enqueue(objectToReturn);

            // Trigger event
            EventManager.TriggerEvent("ObjectReturned", new { tag = poolTag, obj = objectToReturn });
        }

        // Helper method for easy returns
        public static void Return(string poolTag, GameObject obj)
        {
            Instance.ReturnToPool(poolTag, obj);
        }

        // Get pool size info
        public int GetPoolSize(string poolTag)
        {
            if (poolDictionary.ContainsKey(poolTag))
            {
                return poolDictionary[poolTag].Count;
            }
            return 0;
        }

        // Clean up
        private void OnDestroy()
        {
            if (_instance == this)
            {
                poolDictionary?.Clear();
                poolConfigs?.Clear();
                _instance = null;
            }
        }
    }
}