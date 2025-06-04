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
                var authenticationResult = await Authentication.Authentication.AuthenticatingAPlayerAsync(cancellationToken);
                if (authenticationResult == false)
                    return false;
            }

            try
            {
                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Relay join allocation request failed {e.Message}");
                return false;
            }

            Debug.Log($"client: {_joinAllocation.ConnectionData[0]} {_joinAllocation.ConnectionData[1]}");
            Debug.Log($"client: {_joinAllocation.AllocationId}");

            _relayServerData = AllocationUtils.ToRelayServerData(_joinAllocation, ConnectionType);

            return true;
        }
    }
}
