using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace YachuDice.Steam
{
    // Steamworks 초기화 진입점. 현재는 스카래폴딩 단계로 noop.
    // 통합 절차: STEAM_INTEGRATION.md 참고.
    public static class SteamInitializer
    {
        private static bool _isInitialized;
        public static bool IsInitialized => _isInitialized;

        public static UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
                return UniTask.FromResult(true);

            // TODO: Steamworks.NET 또는 Facepunch.Steamworks 통합 시 SteamAPI.Init() 호출.
            Debug.Log("[Steam] SteamInitializer is a stub. See STEAM_INTEGRATION.md.");

            _isInitialized = true;
            return UniTask.FromResult(true);
        }

        public static void Shutdown()
        {
            if (_isInitialized == false)
                return;

            // TODO: SteamAPI.Shutdown() 호출.
            _isInitialized = false;
        }
    }
}
