using System.Collections.Generic;

using TMPro;

using UniRx;

using UnityEngine;

public class PlayerScoreBoard : MonoBehaviour
{
    private const int BonusScoreThreshold = 63;
    private const int BonusScore = 35;
    [SerializeField] private Cell[] _cells;
    [SerializeField] private Cell _totalScoreCell;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _bonusSumText;

    private ReactiveDictionary<Combination, int> _scoreDictionary = new ReactiveDictionary<Combination, int>();

    public void SetName(string name)
    {
        _nameText.text = name;
    }

    public int CalcTotalScore()
    {
        var sum = 0;

        foreach (var key in _scoreDictionary.Keys)
            sum += _scoreDictionary[key];

        var bonusSum = CalcBonusSum();

        if (bonusSum >= BonusScoreThreshold)
            sum += BonusScore;

        return sum;
    }

    private int CalcBonusSum()
    {
        var bonusSums = 0;
        if (_scoreDictionary.ContainsKey(Combination.Aces))
            bonusSums += _scoreDictionary[Combination.Aces];
        if (_scoreDictionary.ContainsKey(Combination.Deuces))
            bonusSums += _scoreDictionary[Combination.Deuces];
        if (_scoreDictionary.ContainsKey(Combination.Threes))
            bonusSums += _scoreDictionary[Combination.Threes];
        if (_scoreDictionary.ContainsKey(Combination.Fours))
            bonusSums += _scoreDictionary[Combination.Fours];
        if (_scoreDictionary.ContainsKey(Combination.Fives))
            bonusSums += _scoreDictionary[Combination.Fives];
        if (_scoreDictionary.ContainsKey(Combination.Sixes))
            bonusSums += _scoreDictionary[Combination.Sixes];

        return bonusSums;
    }

    public bool HasConfirmedScore(Combination category)
    {
        return _scoreDictionary.ContainsKey(category);
    }

    private void Start()
    {
        _totalScoreCell.SetScore(0, Color.black);
        _scoreDictionary.ObserveAdd().Subscribe(_ =>
        {
            _bonusSumText.text = $"{CalcBonusSum().ToString()} / {BonusScoreThreshold}";
            _totalScoreCell.SetScore(CalcTotalScore(), Color.black);
        }).AddTo(this);
    }

    public void Highlight(bool on)
    {
        foreach (var cell in _cells)
            cell.Highlight(on);

        _totalScoreCell.Highlight(on);
    }

    public void SetPreviewScores(Dictionary<Combination, int> scoreDictionary)
    {
        foreach (var key in scoreDictionary.Keys)
        {
            if (_scoreDictionary.ContainsKey(key) == false)
                SetScore(key, scoreDictionary[key], Color.gray);
        }
    }

    public void SetConfirmedScore(Combination category, int score)
    {
        _scoreDictionary.Add(category, score);
        SetScore(category, score, Color.black);
    }

    public void ResetText(Combination category)
    {
        if (_scoreDictionary.ContainsKey(category) == false)
        {
            var index = (int)category;

            _cells[index].ResetText();
        }
    }

    private void SetScore(Combination category, int score, Color color)
    {
        var index = (int)category;

        _cells[index].SetScore(score, color);
    }
}
