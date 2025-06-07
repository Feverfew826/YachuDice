using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

using YachuDice.AddressableWrapper;

namespace YachuDice.Utilities
{
    public class IngameDebugConsoleStarter
    {
        public static DisposableInstanceHandle<Transform> _ingameDebugConsoleLoads;

        public static async UniTask OpenIngameDebugConsoleAndDontDestroyAsync(CancellationToken cancellationToken)
        {
            if (YachuDice.Environment.Development.Development.IsDevelopment == false)
                return;

            _ingameDebugConsoleLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<Transform>("IngameDebugConsole.prefab", cancellationToken: cancellationToken);
        }
    }
}
