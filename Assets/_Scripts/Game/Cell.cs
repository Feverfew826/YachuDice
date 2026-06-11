using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private GameObject _lastRegisteredMarker;
    [SerializeField] private float _stampDuration = 0.3f;
    [SerializeField] private AnimationCurve _stampScaleCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(0.6f, 0.9f), new Keyframe(1f, 1f));

    private void Awake()
    {
        SetLastRegisteredMarker(false);
    }

    public void SetLastRegisteredMarker(bool on)
    {
        _lastRegisteredMarker.SetActive(on);
    }

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

    public void SetConfirmedScore(int score, Color color)
    {
        SetScore(score, color);
        PlayStampAnimationAsync(destroyCancellationToken).Forget();
    }

    public void ResetText()
    {
        _scoreText.text = "";
    }

    private async UniTaskVoid PlayStampAnimationAsync(CancellationToken cancellationToken)
    {
        var rectTransform = _scoreText.rectTransform;
        var elapsed = 0f;

        while (elapsed < _stampDuration)
        {
            var normalizedTime = elapsed / _stampDuration;
            var scale = _stampScaleCurve.Evaluate(normalizedTime);
            rectTransform.localScale = new Vector3(scale, scale, 1f);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            elapsed += Time.deltaTime;
        }

        var finalScale = _stampScaleCurve.Evaluate(1f);
        rectTransform.localScale = new Vector3(finalScale, finalScale, 1f);
    }
}
