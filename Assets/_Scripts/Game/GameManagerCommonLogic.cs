using System.Collections.Generic;
using System.Linq;

public static class GameManagerCommonLogic
{
    public static (Combination combination, int score) ProcessUserChoiceConfirm(GameElementContainer gameElementContainer, PlayerScoreBoard playerScoreBoard, Combination confirmedCombination)
    {
        foreach (var combination in Constants.AllCombinations)
            playerScoreBoard.ResetText(combination);

        var scores = CalculateCombinationScores(gameElementContainer.GetCurrentDiceValues());

        playerScoreBoard.SetConfirmedScore(confirmedCombination, scores[confirmedCombination]);

        return (confirmedCombination, scores[confirmedCombination]);
    }

    public static Dictionary<Combination, int> CalculateCombinationScores(List<int> numbers)
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
