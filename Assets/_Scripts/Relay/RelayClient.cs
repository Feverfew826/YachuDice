using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using UnityEngine;

namespace YachuDice.Relay
{
    public class RelayClient : MonoBehaviour
    {
        private const string ConnectionType = "udp";

        private static JoinAllocation _joinAllocation;
        public static bool IsAllocationJoined => _joinAllocation != null;

        private static Nullable<RelayServerData> _relayServerData;
        public static Nullable<RelayServerData> RelayServerData => _relayServerData;

        public static async UniTask<bool> JoinAllocationAsync(string joinCode, CancellationToken cancellationToken)
        {
            if (Authentication.Authentication.PlayerId == null)
            {
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Relay_PreconditionFailed_Authentication", cancellationToken);
                return false;
            }

            try
            {
                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Relay_JoinAllocationAsync", cancellationToken);
                return false;
            }

            Debug.Log($"client: {_joinAllocation.ConnectionData[0]} {_joinAllocation.ConnectionData[1]}");
            Debug.Log($"client: {_joinAllocation.AllocationId}");

            _relayServerData = AllocationUtils.ToRelayServerData(_joinAllocation, ConnectionType);

            return true;
        }
    }
}
