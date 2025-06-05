using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Services.Authentication;

using UnityEngine;

namespace YachuDice.Authentication
{
    public class Authentication
    {
        private static string _playerId;

        public static string PlayerId => _playerId;


        public static async UniTask<bool> SignInAnonymouslyAsync(CancellationToken cancellationToken)
        {
            if (UnityServices.UnityServices.IsInitialized == false)
            {
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Authentication_PreconditionFailed_UnityServiceInitialization", cancellationToken);
                return false;
            }

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                _playerId = AuthenticationService.Instance.PlayerId;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Authentication_SignInAnonymouslyFailed", cancellationToken);
                return false;
            }
        }
    }
}
