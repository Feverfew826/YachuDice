using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

public class LocalGameManager : MonoBehaviour
{
    [SerializeField] private GameElementContainer _gameElementContainer;

    public async UniTask<LocalGameResult> PlayGameAsync(LocalGameParameter gameParameters, CancellationToken cancellationToken)
    {
        _gameElementContainer.Initialize();

        using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken, _gameElementContainer.QuitCancellationToken);
        try
        {
            await PlayGameAsync(linkedCancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            if (_gameElementContainer.QuitCancellationToken.IsCancellationRequested)
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
            foreach (var playerScoreBoard in _gameElementContainer.PlayerScoreBoards)
            {
                await GameManagerTemp.PlayTrunAsync(_gameElementContainer, playerScoreBoard, cancellationToken);
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
