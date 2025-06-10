using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _scoreText;

    public void Highlight(bool On)
    {
        if (On)
            _background.color = Color.yellow;
        else
            _background.color = Color.white;
    }

    public void SetScore(int score, Color color)
    {
        _scoreText.text = score.ToString();
        _scoreText.color = color;
    }

    public void ResetText()
    {
        _scoreText.text = "";
    }
}
