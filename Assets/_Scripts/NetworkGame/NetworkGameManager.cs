using System.Threading;

using Cysharp.Threading.Tasks;

using Unity.Netcode;

public class NetworkGameManager : GameManager
{
    public async UniTask<NetworkGameResult> PlayGameAsync(NetworkGameParameter gameParameters, CancellationToken cancellationToken)
    {
        var networkManager = NetworkManager.Singleton;
        NetworkGameManagerHelpers.AutoConnectIfNotStartedNetwork(networkManager);

        if (networkManager.IsHost)
        {
            await PlayGameAsHostAsync(cancellationToken);
        }
        else if (networkManager.IsClient)
        {
            await PlayGameAsClientAsync(cancellationToken);
        }

        return new NetworkGameResult();
    }

    public async UniTask PlayGameAsHostAsync(CancellationToken cancellationToken)
    {
        await UniTask.Delay(10000);
    }

    public async UniTask PlayGameAsClientAsync(CancellationToken cancellationToken)
    {
        await UniTask.Delay(10000);
    }

    public struct NetworkGameParameter
    {

    }

    public struct NetworkGameResult
    {

    }

    private void OnGUI()
    {
        NetworkGameManagerHelpers.ShowNetworkStatus();
    }
}

