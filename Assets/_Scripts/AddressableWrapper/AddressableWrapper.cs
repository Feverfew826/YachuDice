using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace YachuDice.AddressableWrapper
{
    public class DisposableAssetHandle<T> : System.IDisposable
    {
        public AsyncOperationHandle<T> Handle { get; }
        public T Asset => Handle.Status == AsyncOperationStatus.Succeeded ? Handle.Result : default;

        public DisposableAssetHandle(AsyncOperationHandle<T> handle)
        {
            Handle = handle;
        }

        public void Dispose()
        {
            if (Handle.IsValid())
            {
                Addressables.Release(Handle);
            }
        }
    }

    public class DisposableInstanceHandle<T> : System.IDisposable where T : Component
    {
        public AsyncOperationHandle<GameObject> Handle { get; }
        public T Instance => Handle.Status == AsyncOperationStatus.Succeeded ? Handle.Result.GetComponent<T>() : null;

        public DisposableInstanceHandle(AsyncOperationHandle<GameObject> handle)
        {
            Handle = handle;
        }

        public void Dispose()
        {
            if (Handle.IsValid())
            {
                Addressables.ReleaseInstance(Handle);
            }
        }
    }

    public static class AddressableWrapper
    {
        public static async UniTask<DisposableAssetHandle<T>> DisposableLoadAssetAsync<T>(object key, CancellationToken cancellationToken = default)
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.ToUniTask(cancellationToken: cancellationToken);
            return new DisposableAssetHandle<T>(handle);
        }

        public static async UniTask<DisposableInstanceHandle<T>> DisposableInstantiateAsync<T>(object key, Transform parent = null, bool instantiateInWorldSpace = false, CancellationToken cancellationToken = default) where T : Component
        {
            var handle = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace);
            await handle.ToUniTask(cancellationToken: cancellationToken);
            return new DisposableInstanceHandle<T>(handle);
        }

        public static async UniTask<DisposableInstanceHandle<T>> DisposableInstantiateAsync<T>(object key, Vector3 position, Quaternion rotation, Transform parent = null, CancellationToken cancellationToken = default) where T : Component
        {
            var handle = Addressables.InstantiateAsync(key, position, rotation, parent);
            await handle.ToUniTask(cancellationToken: cancellationToken);
            return new DisposableInstanceHandle<T>(handle);
        }
    }
}
