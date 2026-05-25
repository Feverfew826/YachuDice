using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace YachuDice.PlayStore
{
    // Google Play Services 초기화 진입점. 현재 통합 범위는 빌드/배포(AAB) 까지로,
    // 로그인·리더보드·도전과제는 미통합. 통합 절차: PLAYSTORE_INTEGRATION.md 참고.
    public static class PlayStoreInitializer
    {
        private static bool _isInitialized;
        public static bool IsInitialized => _isInitialized;

        public static UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
                return UniTask.FromResult(true);

            // TODO: Play Games Plugin 통합 시 PlayGamesPlatform.Activate() 등 호출.
            Debug.Log("[PlayStore] PlayStoreInitializer is a stub. See PLAYSTORE_INTEGRATION.md.");

            _isInitialized = true;
            return UniTask.FromResult(true);
        }
    }
}
