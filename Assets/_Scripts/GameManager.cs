using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UniRx;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

using Utilities;

public class GameManager : MonoBehaviour
{
    private const int DiceNum = 5;
    private const int TurnNum = 12;

    private const int YatchScore = 50;
    private const int LargeStraightScore = 30;
    private const int SmallStraightScore = 15;

    [SerializeField] private int _rollNum = 3;

    [SerializeField] private string[] _playerNames;
    [SerializeField] private RectTransform _playerParent;
    [SerializeField] private PlayerScoreUi _playerPrefab;

    [SerializeField] private Dice[] _dices;
    [SerializeField] private Image[] _keepImages;
    [SerializeField] private Button[] _keepButtons;
    [SerializeField] private Button[] _fixButtons;

    [SerializeField] private Button _rollButton;

    [SerializeField] private float _rollDuration;
    [SerializeField] private float _force;
    [SerializeField] private float _torque;
    [SerializeField] private float _recallDuration;

    private List<PlayerScoreUi> _players = new List<PlayerScoreUi>();

    private ReactiveCollection<bool> _keepFlags = new ReactiveCollection<bool>();
    private List<Category> _allCategories = new List<Category>();
    private List<Vector3> _diceInitialPositions = new List<Vector3>();

    private void Start()
    {
        Assert.AreEqual(_dices.Length, DiceNum);
        Assert.AreEqual(_keepImages.Length, DiceNum);
        Assert.AreEqual(_keepButtons.Length, DiceNum);
        Assert.AreEqual(_fixButtons.Length, System.Enum.GetValues(typeof(Category)).Length);

        foreach (var playerName in _playerNames)
        {
            var newPlayer = Instantiate(_playerPrefab, _playerParent);
            newPlayer.SetName(playerName);
            _players.Add(newPlayer);
        }

        foreach (var dice in _dices)
        {
            _diceInitialPositions.Add(dice.transform.position);
            _keepFlags.Add(false);
        }

        foreach (Category category in System.Enum.GetValues(typeof(Category)))
            _allCategories.Add(category);

        for (var i = 0; i < DiceNum; i++)
        {
            var indexCapture = i;
            _keepButtons[indexCapture].OnClickAsObservable().Subscribe(_ => _keepFlags[indexCapture] = _keepFlags[indexCapture] == false).AddTo(this);
            _keepFlags.ObserveReplace().Subscribe(data => _keepImages[data.Index].gameObject.SetActive(data.NewValue)).AddTo(this);
        }

        PlayGameAsync(destroyCancellationToken).Forget();
    }

    private async UniTask PlayGameAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < TurnNum; i++)
        {
            foreach (var player in _players)
            {
                await PlayTrunAsync(player, cancellationToken);
            }
        }
    }

    private async UniTask PlayTrunAsync(PlayerScoreUi player, CancellationToken cancellationToken)
    {
        player.Turn(true);

        for (var i = 0; i < DiceNum; i++)
            _keepFlags[i] = false;

        var rollCount = 0;
        while (true)
        {
            // UI 초기화
            var hasRolled = rollCount > 0;
            var canRollMore = rollCount < _rollNum;

            _rollButton.gameObject.SetActive(canRollMore);

            UpdateFixButtons(player, hasRolled);

            var canKeep = hasRolled && canRollMore;
            foreach (var button in _keepButtons)
                button.gameObject.SetActive(canKeep);

            var shouldBreakTurn = await ProcessUserChoiceAsync(player, cancellationToken);

            if (shouldBreakTurn)
                break;

            rollCount++;
        }

        player.Turn(false);
    }

    private async System.Threading.Tasks.Task<bool> ProcessUserChoiceAsync(PlayerScoreUi player, CancellationToken cancellationToken)
    {
        // 사용자 입력 수신
        var userInput = await WaitUserInputAsync(cancellationToken);

        MakeUIElementsUninteractable();

        // 사용자 입력 처리(돌리거나, 멈추거나)
        if (userInput.inputType == InputType.Roll)
        {
            await RollDiceAsync(cancellationToken);

            player.SetPreviewScores(CalcScores());

            return false;
        }
        else if (userInput.inputType == InputType.Fix)
        {
            foreach (var category in _allCategories)
                player.ResetText(category);

            var scores = CalcScores();

            player.SetFixScore(userInput.category, scores[userInput.category]);

            return true;
        }

        Assert.IsTrue(false, "Unexpected user input.");
        return false;
    }

    private void MakeUIElementsUninteractable()
    {
        _rollButton.gameObject.SetActive(false);

        foreach (var button in _fixButtons)
            button.interactable = false;

        foreach (var button in _keepButtons)
            button.gameObject.SetActive(false);
    }

    private void UpdateFixButtons(PlayerScoreUi player, bool hasRolled)
    {
        var canFix = hasRolled;
        if (canFix)
        {
            foreach (var category in _allCategories)
            {
                var index = (int)category;
                _fixButtons[index].interactable = player.HasScore(category) == false;
            }
        }
        else
        {
            foreach (var button in _fixButtons)
                button.interactable = false;
        }
    }

    private async UniTask<UserInput> WaitUserInputAsync(CancellationToken cancellationToken)
    {
        using var whenAnyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var whenAnyCancellationToken = whenAnyCancellationTokenSource.Token;

        var rollButtonTask = _rollButton.OnClickAsync(whenAnyCancellationToken);
        var fixButtonTask = _fixButtons.OnAnyClickAsync(whenAnyCancellationToken);

        var whenAnyResult = await UniTask.WhenAny(fixButtonTask, rollButtonTask);
        whenAnyCancellationTokenSource.Cancel();

        if (whenAnyResult.hasResultLeft)
        {
            Assert.IsTrue(typeof(Category).IsEnumDefined(whenAnyResult.result));
            return new UserInput { inputType = InputType.Fix, category = (Category)whenAnyResult.result };
        }
        else
        {
            return new UserInput { inputType = InputType.Roll };
        }
    }

    private Dictionary<Category, int> CalcScores()
    {
        var numbers = new List<int>();

        foreach (var dice in _dices)
            numbers.Add(dice.GetResult());

        return CalcScore(numbers);
    }

    private async UniTask RollDiceAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<UniTask>();
        for (var i = 0; i < DiceNum; i++)
        {
            if (_keepFlags[i] == false)
                tasks.Add(_dices[i].RollAsync(_force, _torque, _rollDuration, cancellationToken));
        }

        await UniTask.WhenAll(tasks);

        foreach (var dice in _dices)
            dice.Stop();

        await RecallDicesAsync(cancellationToken);
    }

    private async UniTask RecallDicesAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<UniTask>();
        for (var i = 0; i < DiceNum; i++)
        {
            var moveTask = MoveAsync(_dices[i].transform, _diceInitialPositions[i], _recallDuration, cancellationToken);
            var rotateTask = _dices[i].RotateToNumberAsync(_recallDuration, cancellationToken);
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

    private Dictionary<Category, int> CalcScore(List<int> numbers)
    {
        var scoreDictionary = new Dictionary<Category, int>();
        foreach (var jokbo in _allCategories)
            scoreDictionary.Add(jokbo, 0);

        var counts = new Dictionary<int, int>();
        for (var i = 1; i <= 6; i++)
            counts.Add(i, numbers.Count(elmt => elmt == i));

        var numbersSum = numbers.Sum();

        // 주사위 눈 별 점수 계산
        scoreDictionary[Category.Aces] = counts[1] * 1;
        scoreDictionary[Category.Deuces] = counts[2] * 2;
        scoreDictionary[Category.Threes] = counts[3] * 3;
        scoreDictionary[Category.Fours] = counts[4] * 4;
        scoreDictionary[Category.Fives] = counts[5] * 5;
        scoreDictionary[Category.Sixes] = counts[6] * 6;

        // Yacht 점수 계산
        for (var i = 1; i <= 6; i++)
        {
            if (counts[i] == DiceNum)
            {
                scoreDictionary[Category.Yacht] = YatchScore;
                break;
            }
        }

        // Large Straight 점수 계산
        if ((counts[1] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1) ||
            (counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 1))
        {
            scoreDictionary[Category.LargeStraight] = LargeStraightScore;
        }

        // Small Straight 점수 계산
        if ((counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1) ||
            (counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1) ||
            (counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1))
        {
            scoreDictionary[Category.SmallStraight] = SmallStraightScore;
        }

        // FourOfAKind 점수 계산
        foreach (var key in counts.Keys)
        {
            if (counts[key] >= 4)
            {
                scoreDictionary[Category.FourOfAKind] = numbersSum;
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
            scoreDictionary[Category.FullHouse] = numbersSum;
        }

        // Choice 점수 계산
        scoreDictionary[Category.Choice] = numbersSum;

        return scoreDictionary;
    }

    private struct UserInput
    {
        public InputType inputType;
        public Category category;
    }

    private enum InputType
    {
        Roll,
        Fix
    }
}
