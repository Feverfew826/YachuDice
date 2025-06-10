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

#pragma warning disable CS0162 // 접근할 수 없는 코드가 있습니다.
            _ingameDebugConsoleLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<Transform>("IngameDebugConsole.prefab", cancellationToken: cancellationToken);
#pragma warning restore CS0162 // 접근할 수 없는 코드가 있습니다.
        }
    }
}
