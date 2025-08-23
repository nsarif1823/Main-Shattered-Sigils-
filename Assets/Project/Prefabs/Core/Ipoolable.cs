namespace Aeloria.Core
{
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
        void ResetPoolObject();
    }
}