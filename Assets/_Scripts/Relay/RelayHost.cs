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

        private Allocation _allocation;
        public bool IsAllocationCreated => _allocation != null;

        private Nullable<RelayServerData> _relayServerData;
        public Nullable<RelayServerData> RelayServerData => _relayServerData;

        private string _joinCode;
        public string JoinCode => _joinCode;

        public async UniTask<bool> CreateAllocationAsync(int maxConnections, CancellationToken cancellationToken)
        {
            if (Authentication.Authentication.PlayerId == null)
            {
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Relay_PreconditionFailed_Authentication", cancellationToken);
                return false;
            }

            try
            {
                _allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Relay_CreateAllocationFailed", cancellationToken);
                return false;
            }

            Debug.Log($"server: {_allocation.ConnectionData[0]} {_allocation.ConnectionData[1]}");
            Debug.Log($"server: {_allocation.AllocationId}");

            _relayServerData = AllocationUtils.ToRelayServerData(_allocation, ConnectionType);

            return true;
        }

        public async UniTask<bool> CreateJoinCodeAsync(CancellationToken cancellationToken)
        {
            if (_allocation == null)
            {
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Relay_PreconditionFailed_Allocation", cancellationToken);
                return false;
            }

            try
            {
                _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await Utilities.ErrorMessageModal.OpenErrorMessageModalAsync("Relay_CreateJoinCodeFailed", cancellationToken);
                return false;
            }
        }
    }
}
