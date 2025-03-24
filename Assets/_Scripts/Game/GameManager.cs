using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UniRx;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Utilities;

public class GameManager : MonoBehaviour
{
    private const int DiceNum = 5;
    private const int TurnNum = 12;

    private const int YatchScore = 50;
    private const int LargeStraightScore = 30;
    private const int SmallStraightScore = 15;

    [Header("Game Settings")]
    [SerializeField] private int _rollNum = 3;

    [SerializeField] private string[] _playerNames;

    [Header("Dices")]
    [SerializeField] private Dice[] _dices;

    [Header("UIs")]
    [SerializeField] private RectTransform _playerParent;
    [SerializeField] private PlayerScoreBoard _playerPrefab;
    [SerializeField] private Image[] _keepImages;
    [SerializeField] private Button[] _keepButtons;
    [SerializeField] private Button[] _confirmButtons;
    [SerializeField] private Button _rollButton;
    [SerializeField] private Button _quitButton;

    [Header("Dice Roll Settings")]
    [SerializeField] private float _diceRollDuration;
    [SerializeField] private float _diceRollForce;
    [SerializeField] private float _diceRollTorque;
    [SerializeField] private float _diceRecallDuration;

    [Header("Pause Menu")]
    [SerializeField] private GameObject _pauseMenuParent;
    [SerializeField] private Button _pauseMenuResumeButton;
    [SerializeField] private Button _pauseMenuQuitButton;

    private List<PlayerScoreBoard> _playerScoreBoards = new List<PlayerScoreBoard>();
    private ReactiveCollection<bool> _keepFlags = new ReactiveCollection<bool>();
    private List<Combination> _allCombinations = new List<Combination>();
    private List<Vector3> _diceInitialPositions = new List<Vector3>();

    private CancellationTokenSource _quitCancellationTokenSource = new();

    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
            OnQuitButton(Unit.Default);
    }

    public async UniTask<GameResult> PlayGameAsync(GameParameter gameParameters, CancellationToken cancellationToken)
    {
        foreach (Combination combination in System.Enum.GetValues(typeof(Combination)))
            _allCombinations.Add(combination);

        Assert.AreEqual(_dices.Length, DiceNum, $"Should set {DiceNum} dices.");
        Assert.AreEqual(_keepImages.Length, DiceNum, $"Should set {DiceNum} keep images.");
        Assert.AreEqual(_keepButtons.Length, DiceNum, $"Should set {DiceNum} keep buttons.");
        Assert.AreEqual(_confirmButtons.Length, _allCombinations.Count, $"Should set {_allCombinations.Count} confirm buttons.");

        foreach (var playerName in _playerNames)
        {
            var newPlayer = Instantiate(_playerPrefab, _playerParent);
            newPlayer.SetName(playerName);
            _playerScoreBoards.Add(newPlayer);
        }

        foreach (var dice in _dices)
        {
            _diceInitialPositions.Add(dice.transform.position);
            _keepFlags.Add(false);
        }

        _keepFlags.ObserveReplace().Subscribe(data => _keepImages[data.Index].gameObject.SetActive(data.NewValue)).AddTo(this);

        var buttonClicks = _keepButtons.Select(elmt => elmt.OnClickAsObservable());
        buttonClicks.Zip(Enumerable.Range(0, DiceNum), (buttonClick, index) => buttonClick.Subscribe(_ => OnKeepButtonChanged(index)).AddTo(this)).Consume();

        _quitButton.OnClickAsObservable().Subscribe(OnQuitButton).AddTo(this);

        using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, destroyCancellationToken, _quitCancellationTokenSource.Token);
        try
        {
            await PlayGameAsync(linkedCancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            if (_quitCancellationTokenSource.IsCancellationRequested)
                return new GameResult();
            else
                throw;
        }

        return new GameResult();
    }

    public void OnKeepButtonChanged(int keepButtonIndex)
    {
        _keepFlags[keepButtonIndex] = _keepFlags[keepButtonIndex] == false;
        var hasRollableDice = _keepFlags.Any(elmt => elmt == false);
        _rollButton.interactable = hasRollableDice;
    }

    private async UniTask PlayGameAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < TurnNum; i++)
        {
            foreach (var playerScoreBoard in _playerScoreBoards)
            {
                await PlayTrunAsync(playerScoreBoard, cancellationToken);
            }
        }
    }

    private async UniTask PlayTrunAsync(PlayerScoreBoard playerScoreBoard, CancellationToken cancellationToken)
    {
        playerScoreBoard.Highlight(true);

        for (var i = 0; i < DiceNum; i++)
            _keepFlags[i] = false;

        var rollCount = 0;
        while (true)
        {
            var hasRolled = rollCount > 0;
            var canRollMore = rollCount < _rollNum;

            // UI 업데이트
            _rollButton.gameObject.SetActive(canRollMore);
            if (canRollMore)
                EventSystem.current.SetSelectedGameObject(_rollButton.gameObject);

            UpdateConfirmButtons(playerScoreBoard, hasRolled);

            var canKeep = hasRolled && canRollMore;
            foreach (var button in _keepButtons)
                button.gameObject.SetActive(canKeep);

            var userChoice = await WaitUserChoiceRollOrConfirmAsync(cancellationToken);

            // 입력 처리 동안 UI 요소 비활성화
            MakeUIElementsUninteractable();

            // 사용자 입력 처리(돌리거나, 멈추거나)
            if (userChoice.choiceType == ChoiceType.Roll)
            {
                await ProcessUserChoiceRollAsync(playerScoreBoard, cancellationToken);

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                ProcessUserChoiceConfirm(playerScoreBoard, userChoice.combination);

                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }

    private void UpdateConfirmButtons(PlayerScoreBoard playerScoreBoard, bool hasRolled)
    {
        var canConfirm = hasRolled;
        if (canConfirm)
        {
            foreach (var combination in _allCombinations)
            {
                var index = (int)combination;
                _confirmButtons[index].interactable = playerScoreBoard.HasConfirmedScore(combination) == false;
            }
        }
        else
        {
            foreach (var button in _confirmButtons)
                button.interactable = false;
        }
    }

    private async UniTask<UserChoice> WaitUserChoiceRollOrConfirmAsync(CancellationToken cancellationToken)
    {
        using var whenAnyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var whenAnyCancellationToken = whenAnyCancellationTokenSource.Token;

        var rollButtonTask = _rollButton.OnClickAsync(whenAnyCancellationToken);
        var confirmButtonTask = _confirmButtons.OnAnyClickAsync(whenAnyCancellationToken);

        var whenAnyResult = await UniTask.WhenAny(confirmButtonTask, rollButtonTask);
        whenAnyCancellationTokenSource.Cancel();

        if (whenAnyResult.hasResultLeft)
        {
            Assert.IsTrue(typeof(Combination).IsEnumDefined(whenAnyResult.result));
            return new UserChoice { choiceType = ChoiceType.Confirm, combination = (Combination)whenAnyResult.result };
        }
        else
        {
            return new UserChoice { choiceType = ChoiceType.Roll };
        }
    }

    private void MakeUIElementsUninteractable()
    {
        _rollButton.gameObject.SetActive(false);

        foreach (var button in _confirmButtons)
            button.interactable = false;

        foreach (var button in _keepButtons)
            button.gameObject.SetActive(false);
    }

    private async UniTask ProcessUserChoiceRollAsync(PlayerScoreBoard playerScoreBoard, CancellationToken cancellationToken)
    {
        var rollResult = await RollDicesAsync(cancellationToken);

        playerScoreBoard.SetPreviewScores(CalculateCombinationScores(rollResult));
    }

    private void ProcessUserChoiceConfirm(PlayerScoreBoard playerScoreBoard, Combination confirmedCombination)
    {
        foreach (var combination in _allCombinations)
            playerScoreBoard.ResetText(combination);

        var scores = CalculateCombinationScores(GetCurrentDiceValues());

        playerScoreBoard.SetConfirmedScore(confirmedCombination, scores[confirmedCombination]);
    }

    private async UniTask<List<int>> RollDicesAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<UniTask>();
        for (var i = 0; i < DiceNum; i++)
        {
            if (_keepFlags[i] == false)
                tasks.Add(_dices[i].RollAsync(_diceRollForce, _diceRollTorque, _diceRollDuration, cancellationToken));
        }

        await UniTask.WhenAll(tasks);

        foreach (var dice in _dices)
            dice.Stop();

        await MoveDicesToInitialPositionAsync(cancellationToken);

        var rollResult = new List<int>();

        foreach (var dice in _dices)
            rollResult.Add(dice.GetResult());

        return rollResult;
    }

    private List<int> GetCurrentDiceValues()
    {
        var values = new List<int>();
        foreach (var dice in _dices)
            values.Add(dice.GetResult());
        return values;
    }

    private async UniTask MoveDicesToInitialPositionAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<UniTask>();
        for (var i = 0; i < DiceNum; i++)
        {
            var moveTask = MoveAsync(_dices[i].transform, _diceInitialPositions[i], _diceRecallDuration, cancellationToken);
            var rotateTask = _dices[i].RotateToNumberAsync(_diceRecallDuration, cancellationToken);
            tasks.Add(moveTask);
            tasks.Add(rotateTask);
        }

        await UniTask.WhenAll(tasks);
    }

    private async UniTask MoveAsync(Transform targetTransform, Vector3 destination, float duration, CancellationToken cancellationToken)
    {
        var targetTime = Time.fixedTime + duration;
        var delta = (destination - targetTransform.position).magnitude / duration * Time.fixedDeltaTime;
        while (Time.fixedTime < targetTime)
        {
            targetTransform.position = Vector3.MoveTowards(targetTransform.position, destination, delta);
            await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate, cancellationToken: cancellationToken);
        }
        targetTransform.position = destination;
    }

    private Dictionary<Combination, int> CalculateCombinationScores(List<int> numbers)
    {
        var scoreDictionary = new Dictionary<Combination, int>();
        foreach (var jokbo in _allCombinations)
            scoreDictionary.Add(jokbo, 0);

        var counts = new Dictionary<int, int>();
        for (var i = 1; i <= 6; i++)
            counts.Add(i, numbers.Count(elmt => elmt == i));

        var numbersSum = numbers.Sum();

        // 주사위 눈 별 점수 계산
        scoreDictionary[Combination.Aces] = counts[1] * 1;
        scoreDictionary[Combination.Deuces] = counts[2] * 2;
        scoreDictionary[Combination.Threes] = counts[3] * 3;
        scoreDictionary[Combination.Fours] = counts[4] * 4;
        scoreDictionary[Combination.Fives] = counts[5] * 5;
        scoreDictionary[Combination.Sixes] = counts[6] * 6;

        // Yacht 점수 계산
        for (var i = 1; i <= 6; i++)
        {
            if (counts[i] == DiceNum)
            {
                scoreDictionary[Combination.Yacht] = YatchScore;
                break;
            }
        }

        // Large Straight 점수 계산
        if ((counts[1] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1) ||
            (counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 1))
        {
            scoreDictionary[Combination.LargeStraight] = LargeStraightScore;
        }

        // Small Straight 점수 계산
        if ((counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1) ||
            (counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1) ||
            (counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1))
        {
            scoreDictionary[Combination.SmallStraight] = SmallStraightScore;
        }

        // FourOfAKind 점수 계산
        foreach (var key in counts.Keys)
        {
            if (counts[key] >= 4)
            {
                scoreDictionary[Combination.FourOfAKind] = numbersSum;
            }
        }

        // FullHouse 점수 계산
        var threeOfAKind = false;
        var pair = false;
        foreach (var key in counts.Keys)
        {
            if (counts[key] == 3)
            {
                threeOfAKind = true;
            }
            if (counts[key] == 2)
            {
                pair = true;
            }
        }
        if (threeOfAKind && pair)
        {
            scoreDictionary[Combination.FullHouse] = numbersSum;
        }

        // Choice 점수 계산
        scoreDictionary[Combination.Choice] = numbersSum;

        return scoreDictionary;
    }

    private void OnQuitButton(Unit unit)
    {
        if (_pauseMenuParent.gameObject.activeInHierarchy)
        {
            _pauseMenuResumeButton.onClick.Invoke();
            return;
        }

        PauseMenuAsync(destroyCancellationToken).Forget();
    }

    private async UniTask PauseMenuAsync(CancellationToken cancellationToken)
    {
        var selectionBackedup = EventSystem.current.currentSelectedGameObject;

        _pauseMenuParent.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(_pauseMenuResumeButton.gameObject);
        var result = await Utilities.Utilities.OnAnyClickAsync(_pauseMenuResumeButton, _pauseMenuQuitButton, cancellationToken);

        if(result == _pauseMenuQuitButton)
        {
            _quitCancellationTokenSource.Cancel();
        }
        else
        {
            _pauseMenuParent.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(selectionBackedup);
        }
    }

    private struct UserChoice
    {
        public ChoiceType choiceType;
        public Combination combination;
    }

    private enum ChoiceType
    {
        Roll,
        Confirm
    }

    public struct GameParameter
    {

    }

    public struct GameResult
    {

    }
}
