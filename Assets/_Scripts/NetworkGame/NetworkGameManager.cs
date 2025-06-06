using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UniRx;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Assertions;

using YachuDice.Utilities;

public class NetworkGameManager : NetworkBehaviour
{
    [SerializeField] private GameElementContainer _gameElementContainer;

    private ReactiveCommand<bool> _isHostContinueTurn = new();
    private ReactiveCommand _hostTurnFinished = new();
    private ReactiveCommand<List<int>> _clientRollResult = new();
    private ReactiveCommand<UserChoice> _clientUserChoice = new();

    public async UniTask<NetworkGameResult> PlayGameAsync(NetworkGameParameter gameParameters, AuthenticatedRelayNetworkFacade authenticatedRelayNetworkFacade, CancellationToken cancellationToken)
    {
        _gameElementContainer.Initialize();

        var buttonClicks = _gameElementContainer.KeepButtons.Select(elmt => elmt.OnClickAsObservable());
        buttonClicks.Zip(Enumerable.Range(0, Constants.DiceNum), (buttonClick, index) => buttonClick.Subscribe(_ => UpdateKeepButtonsRpc(index, _gameElementContainer.KeepFlags[index])).AddTo(this)).Consume();

        if (authenticatedRelayNetworkFacade.IsHost)
        {
            await PlayGameAsHostAsync(cancellationToken);
        }
        else if (authenticatedRelayNetworkFacade.IsClient)
        {
            await PlayGameAsClientAsync(cancellationToken);
        }

        return new NetworkGameResult();
    }

    private async UniTask PlayGameAsHostAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < Constants.TurnNum; i++)
        {
            await PlayHostTurnAsync(cancellationToken);
            HostTurnFinishedRpc();
            await WaitClientTurnAsync(cancellationToken);
        }
    }

    private async UniTask PlayHostTurnAsync(CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[0];

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

                var previewScores = GameManagerTemp.CalculateCombinationScores(rollResult);

                playerScoreBoard.SetPreviewScores(previewScores);

                HostRollResultRpc(rollResult.ToArray());

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                var updatedScore = GameManagerTemp.ProcessUserChoiceConfirm(gameElementContainer, playerScoreBoard, userChoice.combination);

                HostUpdatedScoreRpc(updatedScore.combination, updatedScore.score);

                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }

    private async UniTask WaitClientTurnAsync(CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[1];

        playerScoreBoard.Highlight(true);
        gameElementContainer.ResetKeepFlags();

        gameElementContainer.MakeUIElementsUninteractable();

        var rollCount = 0;
        while (true)
        {
            var hasRolled = rollCount > 0;
            var canRollMore = rollCount < Constants.RollNum;

            var canKeep = hasRolled && canRollMore;
            gameElementContainer.UpdateKeepButtons(canKeep, false);

            var userChoice = await _clientUserChoice.ToUniTask(true, cancellationToken);

            // 사용자 입력 처리(돌리거나, 멈추거나)
            if (userChoice.choiceType == ChoiceType.Roll)
            {
                var rollResult = await gameElementContainer.RollDicesAsync(cancellationToken);

                playerScoreBoard.SetPreviewScores(GameManagerTemp.CalculateCombinationScores(rollResult));

                ClientRollResultRpc(rollResult.ToArray());

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                var updatedScore = GameManagerTemp.ProcessUserChoiceConfirm(gameElementContainer, playerScoreBoard, userChoice.combination);

                ClientUpdatedScoreRpc(updatedScore.combination, updatedScore.score);

                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }

    private async UniTask PlayGameAsClientAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < Constants.TurnNum; i++)
        {
            await WaitHostTurnAsync(cancellationToken);
            await PlayClientTurnAsync(cancellationToken);
        }
    }

    private async UniTask WaitHostTurnAsync(CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[0];

        playerScoreBoard.Highlight(true);
        gameElementContainer.ResetKeepFlags();

        gameElementContainer.MakeUIElementsUninteractable();

        var rollCount = 0;
        while (true)
        {
            var hasRolled = rollCount > 0;
            var canRollMore = rollCount < Constants.RollNum;

            var canKeep = hasRolled && canRollMore;
            gameElementContainer.UpdateKeepButtons(canKeep, false);

            var isHostContinueTurn = await _isHostContinueTurn;
            if (isHostContinueTurn)
                rollCount++;
            else
                break;
        }

        playerScoreBoard.Highlight(false);
    }

    private async UniTask PlayClientTurnAsync(CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[1];

        playerScoreBoard.Highlight(true);
        gameElementContainer.ResetKeepFlags();

        gameElementContainer.MakeUIElementsUninteractable();

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

            ClientUserChoiceRpc(new NetworkUserChoice(userChoice));

            // 사용자 입력 처리(돌리거나, 멈추거나)
            if (userChoice.choiceType == ChoiceType.Roll)
            {
                var rollResult = await _clientRollResult.ToUniTask(true, cancellationToken);

                playerScoreBoard.SetPreviewScores(GameManagerTemp.CalculateCombinationScores(rollResult));

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }

    [Rpc(SendTo.NotServer)]
    private void HostTurnFinishedRpc()
    {
        _hostTurnFinished.Execute();
    }

    [Rpc(SendTo.NotServer)]
    private void ClientRollResultRpc(int[] rollResult)
    {
        _clientRollResult.Execute(rollResult.ToList());
    }

    [Rpc(SendTo.NotServer)]
    private void HostRollResultRpc(int[] rollResult)
    {
        _gameElementContainer.PlayerScoreBoards[0].SetPreviewScores(GameManagerTemp.CalculateCombinationScores(rollResult.ToList()));

        _isHostContinueTurn.Execute(true);
    }

    [Rpc(SendTo.NotServer)]
    private void HostUpdatedScoreRpc(Combination combination, int score)
    {
        foreach (var elmt in Constants.AllCombinations)
            _gameElementContainer.PlayerScoreBoards[0].ResetText(elmt);

        _gameElementContainer.PlayerScoreBoards[0].SetConfirmedScore(combination, score);

        _isHostContinueTurn.Execute(false);
    }

    [Rpc(SendTo.NotServer)]
    private void ClientUpdatedScoreRpc(Combination combination, int score)
    {
        foreach (var elmt in Constants.AllCombinations)
            _gameElementContainer.PlayerScoreBoards[1].ResetText(elmt);

        _gameElementContainer.PlayerScoreBoards[1].SetConfirmedScore(combination, score);
    }

    [Rpc(SendTo.Server)]
    private void ClientUserChoiceRpc(NetworkUserChoice userChoice)
    {
        _clientUserChoice.Execute(userChoice.ToUserChoice());
    }

    [Rpc(SendTo.NotMe)]
    private void UpdateKeepButtonsRpc(int index, bool isKeep)
    {
        _gameElementContainer.KeepFlags[index] = isKeep;
    }

    public struct NetworkUserChoice : INetworkSerializable
    {
        public ChoiceType choiceType;
        public Combination combination;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref choiceType);
        }

        public NetworkUserChoice(UserChoice userChoice)
        {
            choiceType = userChoice.choiceType;
            combination = userChoice.combination;
        }

        public UserChoice ToUserChoice()
        {
            return new UserChoice() { choiceType = choiceType, combination = combination };
        }
    }

    public struct NetworkGameParameter
    {

    }

    public struct NetworkGameResult
    {

    }

    private void OnGUI()
    {
        AuthenticatedRelayNetworkFacade.ShowNetworkStatus();
    }
}

