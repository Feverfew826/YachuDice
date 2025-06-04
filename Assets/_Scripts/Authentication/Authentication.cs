using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Services.Authentication;
using Unity.Services.Core;

using UnityEngine;

namespace YachuDice.Authentication
{
    public class Authentication
    {
        private static string _playerId;

        public static string PlayerId => _playerId;


        public static async UniTask<bool> AuthenticatingAPlayerAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();

                if (UnityServices.State == ServicesInitializationState.Initializing)
                    await UniTask.WaitWhile(() => UnityServices.State == ServicesInitializationState.Initializing, cancellationToken: cancellationToken);

                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    _playerId = AuthenticationService.Instance.PlayerId;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }
    }
}
