using System.Collections;
using System.Collections.Generic;

using UniRx;

using UnityEngine;
using UnityEngine.UI;

public class PlayerScoreUi : MonoBehaviour
{
    private const int BonusScoreThreshold = 63;
    private const int BonusScore = 35;
    [SerializeField] private Cell[] _cells;
    [SerializeField] private Cell _totalScoreCell;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _bonusSumText;

    private ReactiveDictionary<Category, int> _scoreDictionary = new ReactiveDictionary<Category, int>();

    public void SetName(string name)
    {
        _nameText.text = name;
    }

    private int CalcTotalScore()
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
        if (_scoreDictionary.ContainsKey(Category.Aces))
            bonusSums += _scoreDictionary[Category.Aces];
        if (_scoreDictionary.ContainsKey(Category.Deuces))
            bonusSums += _scoreDictionary[Category.Deuces];
        if (_scoreDictionary.ContainsKey(Category.Threes))
            bonusSums += _scoreDictionary[Category.Threes];
        if (_scoreDictionary.ContainsKey(Category.Fours))
            bonusSums += _scoreDictionary[Category.Fours];
        if (_scoreDictionary.ContainsKey(Category.Fives))
            bonusSums += _scoreDictionary[Category.Fives];
        if (_scoreDictionary.ContainsKey(Category.Sixes))
            bonusSums += _scoreDictionary[Category.Sixes];

        return bonusSums;
    }

    public bool HasScore(Category category)
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

    public void Turn(bool on)
    {
        foreach (var cell in _cells)
            cell.Turn(on);

        _totalScoreCell.Turn(on);
    }

    public void SetPreviewScores(Dictionary<Category, int> scoreDictionary)
    {
        foreach(var key in scoreDictionary.Keys)
        {
            if(_scoreDictionary.ContainsKey(key) == false)
                SetScore(key, scoreDictionary[key], Color.gray);
        }
    }

    public void SetFixScore(Category category, int score)
    {
        _scoreDictionary.Add(category, score);
        SetScore(category, score, Color.black);
    }

    public void ResetText(Category category)
    {
        if (_scoreDictionary.ContainsKey(category) == false)
        {
            var index = (int)category;

            _cells[index].ResetText();
        }
    }

    private void SetScore(Category category, int score, Color color)
    {
        var index = (int)category;

        _cells[index].SetScore(score, color);
    }
}
