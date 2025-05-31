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
        Initialize();

        for (var i = 0; i < Constants.TurnNum; i++)
        {
            await PlayHostTurnAsync(cancellationToken);
            await WaitClientTurnAsync(cancellationToken);
        }
    }

    public async UniTask PlayHostTurnAsync(CancellationToken cancellationToken)
    {
        await PlayTrunAsync(_playerScoreBoards[0], cancellationToken);
    }

    public async UniTask WaitClientTurnAsync(CancellationToken cancellationToken)
    {
        // Simulate waiting for client turn logic
        await UniTask.Delay(10000, cancellationToken: cancellationToken);
    }

    public async UniTask PlayGameAsClientAsync(CancellationToken cancellationToken)
    {
        Initialize();

        for (var i = 0; i < Constants.TurnNum; i++)
        {
            await WaitHostTurnAsync(cancellationToken);
            await PlayClientTurnAsync(cancellationToken);
        }
    }

    public async UniTask WaitHostTurnAsync(CancellationToken cancellationToken)
    {
        // Simulate waiting for host turn logic
        await UniTask.Delay(10000, cancellationToken: cancellationToken);
    }

    public async UniTask PlayClientTurnAsync(CancellationToken cancellationToken)
    {
        // Simulate client turn logic
        await UniTask.Delay(10000, cancellationToken: cancellationToken);
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

