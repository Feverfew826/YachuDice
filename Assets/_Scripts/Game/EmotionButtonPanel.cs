using UniRx;

using UnityEngine;
using UnityEngine.UI;

public class EmotionButtonPanel : MonoBehaviour
{
    [SerializeField] private Button _yeahButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private Button _oneButton;
    [SerializeField] private Button _twoButton;
    [SerializeField] private Button _threeButton;
    [SerializeField] private Button _fourButton;
    [SerializeField] private Button _fiveButton;
    [SerializeField] private Button _sixButton;

    public enum Emotion
    {
        Yeah,
        No,
        One,
        Two,
        Three,
        Four,
        Five,
        Six
    }

    private ReactiveCommand<Emotion> _onEmotionNotifierRequested = new();
    public IReactiveCommand<Emotion> OnEmotionNotifierRequested => _onEmotionNotifierRequested;

    private void Awake()
    {
        _yeahButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.Yeah)).AddTo(this);
        _noButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.No)).AddTo(this);
        _oneButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.One)).AddTo(this);
        _twoButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.Two)).AddTo(this);
        _threeButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.Three)).AddTo(this);
        _fourButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.Four)).AddTo(this);
        _fiveButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.Five)).AddTo(this);
        _sixButton.OnClickAsObservable().Subscribe(_ => _onEmotionNotifierRequested.Execute(Emotion.Six)).AddTo(this);
    }
}
