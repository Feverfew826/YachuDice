using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;

using YachuDice.Authentication;
using YachuDice.Relay;
using YachuDice.UnityServices;

public static class AuthenticatedRelayNetworkFacade
{
    private const int MaxConnections = 2;

    public static async UniTask<bool> AutoConnectIfNotStartedNetworkAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        if (networkManager.IsHost == false && networkManager.IsClient == false)
        {
            var mppmTag = CurrentPlayer.ReadOnlyTags();
            if (mppmTag.Contains("Host"))
            {
                return await StartHostThenShowJoinCodeAsync(networkManager, unityTransport, cancellationToken);
            }
            else if (mppmTag.Contains("Client"))
            {
                return await RetrieveJoinCodeThenConnectToHostAsync(networkManager, unityTransport, cancellationToken);
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

    public static async UniTask<bool> StartHostThenShowJoinCodeAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        var startHostResult = await StartHostAsync(networkManager, unityTransport, cancellationToken);
        if (startHostResult == false)
            return false;

        return await JoinCodeDisplayModal.OpenJoinCodeDisplayModalAsync(RelayHost.JoinCode, () => networkManager.ConnectedClients.Count > 1, cancellationToken);
    }

    public static async UniTask<bool> RetrieveJoinCodeThenConnectToHostAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        while (true)
        {
            var joinCode = await JoinCodeInputModal.OpenJoinCodeInputModalAsync(cancellationToken);
            if (joinCode == null)
                return false;

            var startClientResult = await StartClientAsync(networkManager, unityTransport, joinCode, cancellationToken);
            if (startClientResult)
                return true;
        }
    }

    private static async UniTask<bool> StartHostAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        if (UnityServices.IsInitialized == false)
        {
            var unityServicesInitializationResult = await UnityServices.InitializeAsync(cancellationToken);
            if (unityServicesInitializationResult == false)
                return false;
        }

        if (Authentication.PlayerId == null)
        {
            var authenticationResult = await Authentication.SignInAnonymouslyAsync(cancellationToken);
            if (authenticationResult == false)
                return false;
        }

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
        if (UnityServices.IsInitialized == false)
        {
            var unityServicesInitializationResult = await UnityServices.InitializeAsync(cancellationToken);
            if (unityServicesInitializationResult == false)
                return false;
        }

        if (Authentication.PlayerId == null)
        {
            var authenticationResult = await Authentication.SignInAnonymouslyAsync(cancellationToken);
            if (authenticationResult == false)
                return false;
        }

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