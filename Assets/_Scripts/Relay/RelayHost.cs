using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

using UnityEngine;

namespace YachuDice.Relay
{
    public class RelayHost
    {
        private const string ConnectionType = "udp";

        private static Allocation _allocation;
        public static bool IsAllocationCreated => _allocation != null;

        private static Nullable<RelayServerData> _relayServerData;
        public static Nullable<RelayServerData> RelayServerData => _relayServerData;

        private static string _joinCode;
        public static string JoinCode => _joinCode;

        public static async UniTask<bool> CreateAllocationAsync(int maxConnections, CancellationToken cancellationToken)
        {
            if (Authentication.Authentication.PlayerId == null)
            {
                var authenticationResult = await Authentication.Authentication.AuthenticatingAPlayerAsync(cancellationToken);
                if (authenticationResult == false)
                    return false;
            }

            try
            {
                _allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            }
            catch (Exception e)
            {
                Debug.LogError($"Relay create allocation request failed {e.Message}");
                return false;
            }

            Debug.Log($"server: {_allocation.ConnectionData[0]} {_allocation.ConnectionData[1]}");
            Debug.Log($"server: {_allocation.AllocationId}");

            _relayServerData = AllocationUtils.ToRelayServerData(_allocation, ConnectionType);

            return true;
        }

        public static async UniTask<bool> CreateJoinCodeAsync(CancellationToken cancellationToken)
        {
            if (_allocation == null)
                return false;

            try
            {
                _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                return true;
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                return false;
            }
        }
    }
}
