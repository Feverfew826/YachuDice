using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;

using YachuDice.Relay;

internal static class NetworkGameManagerHelpers
{
    private const int MaxConnections = 2;

    public static async UniTask<bool> AutoConnectIfNotStartedNetworkAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        if (networkManager.IsHost == false && networkManager.IsClient == false)
        {
            var mppmTag = CurrentPlayer.ReadOnlyTags();
            if (mppmTag.Contains("Host"))
            {
                var startHostResult = await StartHostAsync(networkManager, unityTransport, cancellationToken);
                if (startHostResult == false)
                    return false;

                await JoinCodeDisplayModal.OpenJoinCodeDisplayModalAsync(RelayHost.JoinCode, () => networkManager.ConnectedClients.Count > 1, cancellationToken);
                return true;
            }
            else if (mppmTag.Contains("Client"))
            {
                var joinCode = await JoinCodeInputModal.OpenJoinCodeInputModalAsync(cancellationToken);
                return await StartClientAsync(networkManager, unityTransport, joinCode, cancellationToken);
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    private static async UniTask<bool> StartHostAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        if (RelayHost.IsAllocationCreated == false)
        {
            var createAllocationResult = await RelayHost.CreateAllocationAsync(MaxConnections, cancellationToken);
            if (createAllocationResult == false)
                return false;
        }

        unityTransport.SetRelayServerData(RelayHost.RelayServerData.Value);

        if (RelayHost.JoinCode == null)
        {
            var createJoinCodeResult = await RelayHost.CreateJoinCodeAsync(cancellationToken);
            if (createJoinCodeResult == false)
                return false;
        }

        return networkManager.StartHost();
    }

    private static async UniTask<bool> StartClientAsync(NetworkManager networkManager, UnityTransport unityTransport, string joinCode, CancellationToken cancellationToken)
    {
        if (RelayClient.IsAllocationJoined == false)
        {
            var createAllocationResult = await RelayClient.JoinAllocationAsync(joinCode, cancellationToken);
            if (createAllocationResult == false)
                return false;
        }

        unityTransport.SetRelayServerData(RelayClient.RelayServerData.Value);

        return networkManager.StartClient();
    }

    public static void ShowNetworkStatus()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        StatusLabels();
        GUILayout.EndArea();
    }

    private static void StatusLabels()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        var mode = "None";
        if (networkManager.IsHost)
        {
            mode = "Host";
        }
        else if (networkManager.IsClient)
        {
            mode = "Client";
        }

        GUILayout.Label("Transport: " + networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}