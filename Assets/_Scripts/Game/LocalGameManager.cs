using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

public class LocalGameManager : MonoBehaviour
{
    [SerializeField] private GameElementContainer _gameElementContainer;

    public async UniTask<LocalGameResult> PlayGameAsync(LocalGameParameter gameParameters, CancellationToken cancellationToken)
    {
        _gameElementContainer.Initialize();

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken, _gameElementContainer.QuitCancellationToken);
        try
        {
            await PlayGameAsync(linkedCancellationTokenSource.Token);
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
                await PlayTrunAsync(_gameElementContainer, playerScoreBoard, cancellationToken);
            }
        }
    }

    public static async UniTask PlayTrunAsync(GameElementContainer gameElementContainer, PlayerScoreBoard playerScoreBoard, CancellationToken cancellationToken)
    {
        playerScoreBoard.Highlight(true);
        gameElementContainer.ResetKeepFlags();

        var rollCount = 0;
        while (true)
        {
            var hasRolled = rollCount > 0;
            var canRollMore = rollCount < Constants.RollNum;

            // UI 업데이트
            gameElementContainer.UpdateRollButtonState(canRollMore);

            gameElementContainer.UpdateConfirmButtons(playerScoreBoard, hasRolled);

            var canKeep = hasRolled && canRollMore;
            gameElementContainer.UpdateKeepButtons(canKeep, true);

            var userChoice = await gameElementContainer.WaitUserChoiceRollOrConfirmAsync(cancellationToken);

            // 입력 처리 동안 UI 요소 비활성화
            gameElementContainer.MakeUIElementsUninteractable();

            // 사용자 입력 처리(돌리거나, 멈추거나)
            if (userChoice.choiceType == ChoiceType.Roll)
            {
                var rollResult = await gameElementContainer.RollDicesAsync(cancellationToken);

                playerScoreBoard.SetPreviewScores(GameManagerCommonLogic.CalculateCombinationScores(rollResult));

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                GameManagerCommonLogic.ProcessUserChoiceConfirm(gameElementContainer, playerScoreBoard, userChoice.combination);

                await CombinationNotifier.ShowCombinationNotifierAsync(userChoice.combination, cancellationToken);

                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }

    public struct LocalGameParameter
    {

    }

    public struct LocalGameResult
    {

    }
}
