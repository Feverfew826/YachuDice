using System;
using System.Threading;

using Cysharp.Threading.Tasks;

public class LocalGameManager : GameManager
{
    public async UniTask<LocalGameResult> PlayGameAsync(LocalGameParameter gameParameters, CancellationToken cancellationToken)
    {
        Initialize();

        using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken, QuitCancellationToken);
        try
        {
            await PlayGameAsync(linkedCancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            if (QuitCancellationToken.IsCancellationRequested)
                return new LocalGameResult();
            else
                throw;
        }

        return new LocalGameResult();
    }

    private async UniTask PlayGameAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < Constants.TurnNum; i++)
        {
            foreach (var playerScoreBoard in _playerScoreBoards)
            {
                await PlayTrunAsync(playerScoreBoard, cancellationToken);
            }
        }
    }

    public struct LocalGameParameter
    {

    }

    public struct LocalGameResult
    {

    }
}
