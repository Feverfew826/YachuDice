using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace YachuDice.UnityServices
{
    public class UnityServices
    {
        private static bool _isInitialized;

        public static bool IsInitialized => _isInitialized;

        public static async UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized)
                    await Unity.Services.Core.UnityServices.InitializeAsync();

                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initializing)
                    await UniTask.WaitWhile(() => Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initializing, cancellationToken: cancellationToken);

                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized)
                {
                    _isInitialized = true;
                    return true;
                }
                else
                {
                    await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("UnityServices_InitializeFailed", cancellationToken);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("UnityServices_InitializeFailed", cancellationToken);
                return false;
            }
        }
    }
}
