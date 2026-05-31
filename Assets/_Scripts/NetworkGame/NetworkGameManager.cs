using System;
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
    [SerializeField] private EmotionButtonPanel _emotionButtonPanel;

    private ReactiveCommand<bool> _isHostContinueTurn = new();
    private ReactiveCommand<bool> _isOtherPlayerContinueTurn = new();
    private ReactiveCommand _hostTurnFinished = new();
    private ReactiveCommand<List<int>> _clientRollResult = new();
    private ReactiveCommand<UserChoice> _clientUserChoice = new();

    private int _playerCount = 0;
    private bool _isHost;
    private int _myIndex = 0;
    private UniTaskCompletionSource<int> _myIndexAssigned = new();

    public async UniTask<NetworkGameResult> PlayGameAsync(NetworkGameParameter gameParameters, AuthenticatedRelayNetworkFacade authenticatedRelayNetworkFacade, CancellationToken cancellationToken)
    {
        _gameElementContainer.Initialize();

        var buttonClicks = _gameElementContainer.KeepButtons.Select(elmt => elmt.OnClickAsObservable());
        buttonClicks.Zip(Enumerable.Range(0, Constants.DiceNum), (buttonClick, index) => buttonClick.Subscribe(_ => UpdateKeepButtonsRpc(index, _gameElementContainer.KeepFlags[index])).AddTo(this)).Consume();

        authenticatedRelayNetworkFacade.NetworkManager.OnClientDisconnectCallback += OnClientDisconnectHandler;

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken, _gameElementContainer.QuitCancellationToken);
        try
        {
            if (authenticatedRelayNetworkFacade.IsHost)
            {
                _isHost = true;
                var playerOrder = authenticatedRelayNetworkFacade.PlayerOrder.ToArray();
                _myIndex = Array.IndexOf(playerOrder, authenticatedRelayNetworkFacade.NetworkManager.LocalClientId);

                _playerCount = playerOrder.Length;
                _myIndex = 0;
                AssignClientIndicesRpc(playerOrder);

                var playerCount = _playerCount;
                var playerNames = new string[playerCount];

                playerNames[0] = $"You";
                for (var i = 1; i < playerCount; i++)
                {
                    playerNames[i] = $"Player {i + 1}";
                }

                _gameElementContainer.InitializePlayerBoard(playerNames);

                if (_emotionButtonPanel != null)
                    PlayEmotionNotifierLoopAsync(_emotionButtonPanel, linkedCancellationTokenSource.Token).Forget();
                await PlayGameAsHostAsync(playerCount, linkedCancellationTokenSource.Token);
            }
            else if (authenticatedRelayNetworkFacade.IsClient)
            {
                _isHost = false;
                _myIndex = await _myIndexAssigned.Task.AttachExternalCancellation(linkedCancellationTokenSource.Token);

                var playerCount = _playerCount;
                var playerNames = new string[playerCount];

                playerNames[0] = $"Player 1";
                for (var i = 1; i < playerCount; i++)
                {
                    if (i == _myIndex)
                        playerNames[i] = $"You";
                    else
                        playerNames[i] = $"Player {i + 1}";
                }

                _gameElementContainer.InitializePlayerBoard(playerNames);

                if (_emotionButtonPanel != null)
                    PlayEmotionNotifierLoopAsync(_emotionButtonPanel, linkedCancellationTokenSource.Token).Forget();
                await PlayGameAsClientAsync(playerCount, linkedCancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            if (_gameElementContainer.QuitCancellationToken.IsCancellationRequested)
                return new NetworkGameResult();
            else
                throw;
        }
        finally
        {
            authenticatedRelayNetworkFacade.NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectHandler;
        }

        return new NetworkGameResult();
    }

    private void OnClientDisconnectHandler(ulong clientId)
    {
        OnClientDisconnectHandlerAsync(destroyCancellationToken).Forget();
    }

    private async UniTask OnClientDisconnectHandlerAsync(CancellationToken cancellationToken)
    {
        // If _isHost is true, it means the client is disconnected.
        // If _isHost is false, it means the host is disconnected.
        var isClientDisconnected = _isHost;
        await DisconnectionNotifyModal.OpenDisconnectionNotifyModalAsync(isClientDisconnected, cancellationToken);

        _gameElementContainer.Quit();
    }

    private async UniTask PlayGameAsHostAsync(int playerCount, CancellationToken cancellationToken)
    {
        for (var turnIndex = 0; turnIndex < Constants.TurnNum; turnIndex++)
        {
            await PlayHostTurnAsync(cancellationToken);
            HostTurnFinishedRpc();

            for (var playerIndex = 1; playerIndex < playerCount; playerIndex++)
            {
                await WaitClientTurnAsHostAsync(playerIndex, cancellationToken);
            }
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

                var previewScores = GameManagerCommonLogic.CalculateCombinationScores(rollResult);

                playerScoreBoard.SetPreviewScores(previewScores);

                HostRollResultRpc(rollResult.ToArray());

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                var updatedScore = GameManagerCommonLogic.ProcessUserChoiceConfirm(gameElementContainer, playerScoreBoard, userChoice.combination);

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

    private async UniTask WaitClientTurnAsHostAsync(int playerIndex, CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[playerIndex];

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

                var combinationScores = GameManagerCommonLogic.CalculateCombinationScores(rollResult);

                playerScoreBoard.SetPreviewScores(combinationScores);

                ClientRollResultRpc(playerIndex, rollResult.ToArray());

                foreach (var combination in GameManagerCommonLogic.SpecialCombination.Reverse())
                {
                    if (combinationScores[combination] > 0)
                    {
                        await CombinationNotifier.ShowCombinationNotifierAsync(combination, cancellationToken);
                        break;
                    }
                }

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                var updatedScore = GameManagerCommonLogic.ProcessUserChoiceConfirm(gameElementContainer, playerScoreBoard, userChoice.combination);

                ClientUpdatedScoreRpc(playerIndex, updatedScore.combination, updatedScore.score);

                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }

    private async UniTask PlayGameAsClientAsync(int playerCount, CancellationToken cancellationToken)
    {
        for (var turnIndex = 0; turnIndex < Constants.TurnNum; turnIndex++)
        {
            await WaitHostTurnAsync(0, cancellationToken);
            for (var playerIndex = 1; playerIndex < playerCount; playerIndex++)
            {
                if (_myIndex == playerIndex)
                    await PlayClientTurnAsync(cancellationToken);
                else
                    await WaitClientTurnAsClientAsync(playerIndex, cancellationToken);
            }
        }
    }

    private async UniTask WaitHostTurnAsync(int playerIndex, CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[playerIndex];

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

            var isHostContinueTurn = await _isHostContinueTurn.ToUniTask(true, cancellationToken);
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
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[_myIndex];

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

                var combinationScores = GameManagerCommonLogic.CalculateCombinationScores(rollResult);

                playerScoreBoard.SetPreviewScores(combinationScores);

                foreach (var combination in GameManagerCommonLogic.SpecialCombination.Reverse())
                {
                    if (combinationScores[combination] > 0)
                    {
                        await CombinationNotifier.ShowCombinationNotifierAsync(combination, cancellationToken);
                        break;
                    }
                }

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

    private async UniTask WaitClientTurnAsClientAsync(int playerIndex, CancellationToken cancellationToken)
    {
        var gameElementContainer = _gameElementContainer;
        var playerScoreBoard = gameElementContainer.PlayerScoreBoards[playerIndex];

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

            var isHostContinueTurn = await _isOtherPlayerContinueTurn.ToUniTask(true, cancellationToken);
            if (isHostContinueTurn)
                rollCount++;
            else
                break;
        }

        playerScoreBoard.Highlight(false);
    }

    [Rpc(SendTo.NotServer)]
    private void AssignClientIndicesRpc(ulong[] playerOrder)
    {
        _playerCount = playerOrder.Length;

        for (var i = 0; i < playerOrder.Length; i++)
        {
            if (playerOrder[i] == NetworkManager.LocalClientId)
            {
                _myIndex = i;
                _myIndexAssigned.TrySetResult(i);
                break;
            }
        }
    }

    [Rpc(SendTo.NotServer)]
    private void HostTurnFinishedRpc()
    {
        _hostTurnFinished.Execute();
    }

    [Rpc(SendTo.NotServer)]
    private void ClientRollResultRpc(int playerIndex, int[] rollResult)
    {
        _gameElementContainer.PlayerScoreBoards[playerIndex].SetPreviewScores(GameManagerCommonLogic.CalculateCombinationScores(rollResult.ToList()));

        if (playerIndex == _myIndex)
            _clientRollResult.Execute(rollResult.ToList());
        else
            _isOtherPlayerContinueTurn.Execute(true);
    }

    [Rpc(SendTo.NotServer)]
    private void HostRollResultRpc(int[] rollResult)
    {
        _gameElementContainer.PlayerScoreBoards[0].SetPreviewScores(GameManagerCommonLogic.CalculateCombinationScores(rollResult.ToList()));

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
    private void ClientUpdatedScoreRpc(int playerIndex, Combination combination, int score)
    {
        foreach (var elmt in Constants.AllCombinations)
            _gameElementContainer.PlayerScoreBoards[playerIndex].ResetText(elmt);

        _gameElementContainer.PlayerScoreBoards[playerIndex].SetConfirmedScore(combination, score);

        if (playerIndex != _myIndex)
            _isOtherPlayerContinueTurn.Execute(false);
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

    private static EmotionNotifier.Character GetCharacterForIndex(int index) => index switch
    {
        0 => EmotionNotifier.Character.Girl,
        1 => EmotionNotifier.Character.Boy,
        2 => EmotionNotifier.Character.Dog,
        _ => EmotionNotifier.Character.Cat,
    };

    // Hard-coded!! 4인 기준 균등 분할: canvas 위치 -100, 167, 433, 700 → xOffset = canvas - 275
    private static int GetXOffsetForIndex(int index) => index switch
    {
        0 => -375,
        1 => -108,
        2 => 158,
        _ => 425,
    };

    private async UniTask PlayEmotionNotifierLoopAsync(EmotionButtonPanel emotionButtonPanel, CancellationToken cancellationToken)
    {
        var myCharacter = GetCharacterForIndex(_myIndex);
        var myXOffset = GetXOffsetForIndex(_myIndex);
        while (true)
        {
            var emotion = await emotionButtonPanel.OnEmotionNotifierRequested.ToUniTask(true, cancellationToken);
            EmotionNotifierRequestRpc(emotion, _myIndex);
            await EmotionNotifier.ShowEmotionNotifierAsync(emotion, myCharacter, myXOffset, cancellationToken);
        }
    }

    [Rpc(SendTo.NotMe)]
    private void EmotionNotifierRequestRpc(EmotionButtonPanel.Emotion emotion, int senderIndex)
    {
        var character = GetCharacterForIndex(senderIndex);
        var xOffset = GetXOffsetForIndex(senderIndex);
        EmotionNotifier.ShowEmotionNotifierAsync(emotion, character, xOffset, destroyCancellationToken).Forget();
    }

    public struct NetworkUserChoice : INetworkSerializable
    {
        public ChoiceType choiceType;
        public Combination combination;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref choiceType);
            serializer.SerializeValue(ref combination);
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

