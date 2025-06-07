using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;

using YachuDice.AddressableWrapper;
using YachuDice.Authentication;
using YachuDice.Relay;
using YachuDice.UnityServices;

public class AuthenticatedRelayNetworkFacade : IDisposable
{
    private static bool _isExistCurrentlyWorkingInstance;
    public static bool IsExistCurrentlyWorkingInstance => _isExistCurrentlyWorkingInstance;

    private const int MaxConnections = 2;

    private bool _isInitialized;
    private bool _isDisposed;

    private DisposableInstanceHandle<NetworkManager> _networkManagerLoad;
    private NetworkManager _networkManager;
    private UnityTransport _unityTransport;

    public NetworkManager NetworkManager => _networkManager;

    public bool IsHost => _networkManager.IsHost;
    public bool IsClient => _networkManager.IsClient;

    // 인스턴스 수를 관리하기 위해 생성자 감춤.
    // 싱글톤으로 만들지 않는 이유는 아무렇게나 인스턴스에 접근하는 것을 허용하지 않기 위해서.
    private AuthenticatedRelayNetworkFacade() { }

    public static AuthenticatedRelayNetworkFacade GetDisposableInstance()
    {
        if (_isExistCurrentlyWorkingInstance)
            return null;

        return new AuthenticatedRelayNetworkFacade();
    }

    public async UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        if (_isExistCurrentlyWorkingInstance)
            return false;

        if (_isDisposed)
            return false;

        if (_isInitialized)
            return true;

        _isExistCurrentlyWorkingInstance = true;
        _isInitialized = true;

        try
        {
            _networkManagerLoad = await AddressableWrapper.DisposableInstantiateAsync<NetworkManager>("NetworkManager.prefab", cancellationToken: cancellationToken);
            _networkManager = _networkManagerLoad.Instance;
            _unityTransport = _networkManager.GetComponent<UnityTransport>();

            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogException(e);

            _isExistCurrentlyWorkingInstance = false;
            _isInitialized = false;

            return false;
        }
    }

    public void Dispose()
    {
        if (_isDisposed == false)
        {
            _isDisposed = true;

            if (_networkManager != null)
            {
                _networkManager.Shutdown();
                _networkManagerLoad.Dispose();
                _networkManager = null;
                _unityTransport = null;
            }

            _isExistCurrentlyWorkingInstance = false;
        }
    }

    public async UniTask<bool> AutoConnectIfNotStartedNetworkAsync(CancellationToken cancellationToken)
    {
        var initializationResult = await InitializeAsync(cancellationToken);
        if (initializationResult == false)
            return false;

        return await AutoConnectIfNotStartedNetworkAsync(_networkManager, _unityTransport, cancellationToken);
    }

    public async UniTask<bool> StartHostThenShowJoinCodeAsync(CancellationToken cancellationToken)
    {
        var initializationResult = await InitializeAsync(cancellationToken);
        if (initializationResult == false)
            return false;

        return await StartHostThenShowJoinCodeAsync(_networkManager, _unityTransport, cancellationToken);
    }

    public async UniTask<bool> RetrieveJoinCodeThenConnectToHostAsync(CancellationToken cancellationToken)
    {
        var initializationResult = await InitializeAsync(cancellationToken);
        if (initializationResult == false)
            return false;

        return await RetrieveJoinCodeThenConnectToHostAsync(_networkManager, _unityTransport, cancellationToken);
    }

    private static async UniTask<bool> AutoConnectIfNotStartedNetworkAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
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

    private static async UniTask<bool> StartHostThenShowJoinCodeAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        var startHostResult = await StartHostAsync(networkManager, unityTransport, cancellationToken);
        if (startHostResult == false)
            return false;

        return await JoinCodeDisplayModal.OpenJoinCodeDisplayModalAsync(RelayHost.JoinCode, () => networkManager.ConnectedClients.Count > 1, cancellationToken);
    }

    private static async UniTask<bool> RetrieveJoinCodeThenConnectToHostAsync(NetworkManager networkManager, UnityTransport unityTransport, CancellationToken cancellationToken)
    {
        while (true)
        {
            var joinCode = await JoinCodeInputModal.OpenJoinCodeInputModalAsync(cancellationToken);
            if (string.IsNullOrEmpty(joinCode))
                return false;

            var startClientResult = await StartClientAsync(networkManager, unityTransport, joinCode, cancellationToken);
            if (startClientResult)
            {
                await UniTask.WaitUntil(() => networkManager.IsConnectedClient, cancellationToken: cancellationToken);
                return true;
            }
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

    [Conditional("DEBUG")]
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