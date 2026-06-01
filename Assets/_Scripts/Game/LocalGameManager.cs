using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.Assertions;

public class LocalGameManager : MonoBehaviour
{
    [SerializeField] private GameElementContainer _gameElementContainer;
    [SerializeField] private List<EmotionButtonPanel> _emotionButtonPanels;

    public async UniTask<LocalGameResult> PlayGameAsync(LocalGameParameter gameParameters, CancellationToken cancellationToken)
    {
        _gameElementContainer.Initialize();

        _gameElementContainer.InitializePlayerBoard(new string[] { "Player 1", "Player 2" });

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken, _gameElementContainer.QuitCancellationToken);
        try
        {
            for (var i = 0; i < _emotionButtonPanels.Count; i++)
            {
                // Hard-coded!! But it's fine for now.
                var (xOffset, character) = i switch
                {
                    0 => (-375, EmotionNotifier.Character.Girl),
                    _ => (425, EmotionNotifier.Character.Boy),
                };
                PlayEmotionNotifierLoopAsync(_emotionButtonPanels[i], character, xOffset, linkedCancellationTokenSource.Token).Forget();
            }

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
        await GamePhaseNotifier.ShowGamePhaseNotifierAsync(GamePhaseNotifier.GamePhase.GameStart, cancellationToken);

        for (var turnCount = 0; turnCount < Constants.TurnNum; turnCount++)
        {
            for (var playerIndex = 0; playerIndex < _gameElementContainer.PlayerScoreBoards.Count; playerIndex++)
            {
                var playerScoreBoard = _gameElementContainer.PlayerScoreBoards[playerIndex];

                var gamePhase_turn = playerIndex switch
                {
                    0 => GamePhaseNotifier.GamePhase.Player1sTurn,
                    1 => GamePhaseNotifier.GamePhase.Player2sTurn,
                    _ => GamePhaseNotifier.GamePhase.Player1sTurn
                };

                await GamePhaseNotifier.ShowGamePhaseNotifierAsync(gamePhase_turn, cancellationToken);

                await PlayTrunAsync(_gameElementContainer, playerScoreBoard, cancellationToken);
            }
        }

        var maxIndex = 0;
        var maxScore = int.MinValue;
        for (var i = 0; i < _gameElementContainer.PlayerScoreBoards.Count; i++)
        {
            var playerScoreBoard = _gameElementContainer.PlayerScoreBoards[i];
            var playerTotalScore = playerScoreBoard.CalcTotalScore();
            if (playerTotalScore > maxScore)
            {
                maxScore = playerTotalScore;
                maxIndex = i;
            }
        }

        var gamePhase_winner = maxIndex switch
        {
            0 => GamePhaseNotifier.GamePhase.Player1Win,
            1 => GamePhaseNotifier.GamePhase.Player2Win,
            _ => GamePhaseNotifier.GamePhase.Player1Win
        };

        await GamePhaseNotifier.ShowGamePhaseNotifierAsync(gamePhase_winner, cancellationToken);

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

                var combinationScores = GameManagerCommonLogic.CalculateCombinationScores(rollResult);

                playerScoreBoard.SetPreviewScores(combinationScores);

                foreach (var combination in GameManagerCommonLogic.SpecialCombination.Reverse())
                {
                    if (combinationScores[combination] > 0)
                    {
                        gameElementContainer.PlayCombinationMelody();
                        await CombinationNotifier.ShowCombinationNotifierAsync(combination, cancellationToken);
                        break;
                    }
                }

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

    private async UniTask PlayEmotionNotifierLoopAsync(EmotionButtonPanel emotionButtonPanel, EmotionNotifier.Character character, int xOffset, CancellationToken cancellationToken)
    {
        while (true)
        {
            var emotion = await emotionButtonPanel.OnEmotionNotifierRequested.ToUniTask(true, cancellationToken);
            await EmotionNotifier.ShowEmotionNotifierAsync(emotion, character, xOffset, cancellationToken);
        }
    }

    public struct LocalGameParameter
    {

    }

    public struct LocalGameResult
    {

    }
}
