using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine.Assertions;

public static class GameManagerTemp
{
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
            gameElementContainer.UpdateKeepButtons(canKeep);

            var userChoice = await gameElementContainer.WaitUserChoiceRollOrConfirmAsync(cancellationToken);

            // 입력 처리 동안 UI 요소 비활성화
            gameElementContainer.MakeUIElementsUninteractable();

            // 사용자 입력 처리(돌리거나, 멈추거나)
            if (userChoice.choiceType == ChoiceType.Roll)
            {
                await ProcessUserChoiceRollAsync(gameElementContainer, playerScoreBoard, cancellationToken);

                rollCount++;
            }
            else if (userChoice.choiceType == ChoiceType.Confirm)
            {
                ProcessUserChoiceConfirm(gameElementContainer, playerScoreBoard, userChoice.combination);

                break;
            }
            else
            {
                Assert.IsTrue(false, "Unexpected user choice.");
            }
        }

        playerScoreBoard.Highlight(false);
    }



    private static async UniTask ProcessUserChoiceRollAsync(GameElementContainer gameElementContainer, PlayerScoreBoard playerScoreBoard, CancellationToken cancellationToken)
    {
        var rollResult = await gameElementContainer.RollDicesAsync(cancellationToken);

        playerScoreBoard.SetPreviewScores(CalculateCombinationScores(rollResult));
    }

    private static void ProcessUserChoiceConfirm(GameElementContainer gameElementContainer, PlayerScoreBoard playerScoreBoard, Combination confirmedCombination)
    {
        foreach (var combination in Constants.AllCombinations)
            playerScoreBoard.ResetText(combination);

        var scores = CalculateCombinationScores(gameElementContainer.GetCurrentDiceValues());

        playerScoreBoard.SetConfirmedScore(confirmedCombination, scores[confirmedCombination]);
    }

    private static Dictionary<Combination, int> CalculateCombinationScores(List<int> numbers)
    {
        var scoreDictionary = new Dictionary<Combination, int>();
        foreach (var jokbo in Constants.AllCombinations)
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
            if (counts[i] == Constants.DiceNum)
            {
                scoreDictionary[Combination.Yacht] = Constants.YatchScore;
                break;
            }
        }

        // Large Straight 점수 계산
        if ((counts[1] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1) ||
            (counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 1))
        {
            scoreDictionary[Combination.LargeStraight] = Constants.LargeStraightScore;
        }

        // Small Straight 점수 계산
        if ((counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1) ||
            (counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1) ||
            (counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1))
        {
            scoreDictionary[Combination.SmallStraight] = Constants.SmallStraightScore;
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
}
