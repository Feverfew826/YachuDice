using System.Threading;

using Cysharp.Threading.Tasks;

using UniRx;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using YachuDice.AddressableWrapper;

public class QualitySettingsModal : MonoBehaviour
{
    [SerializeField] private Button _veryLowButton;
    [SerializeField] private Button _lowButton;
    [SerializeField] private Button _mediumButton;
    [SerializeField] private Button _highButton;
    [SerializeField] private Button _veryHighButton;
    [SerializeField] private Button _ultraButton;
    [SerializeField] private Button _exitButton;

    public static async UniTask OpenQualitySettingsModalAsync(CancellationToken cancellationToken)
    {
        var selectionBackedup = EventSystem.current.currentSelectedGameObject;

        using var qualitySettingsModalLoads = await AddressableWrapper.DisposableInstantiateAsync<QualitySettingsModal>("QualitySettings.prefab", cancellationToken: cancellationToken);
        var qualitySettingsModal = qualitySettingsModalLoads.Instance;
        EventSystem.current.SetSelectedGameObject(qualitySettingsModal.gameObject);

        qualitySettingsModal._veryLowButton.OnClickAsObservable().Subscribe(_ => QualitySettings.SetQualityLevel(0)).AddTo(qualitySettingsModal);
        qualitySettingsModal._lowButton.OnClickAsObservable().Subscribe(_ => QualitySettings.SetQualityLevel(1)).AddTo(qualitySettingsModal);
        qualitySettingsModal._mediumButton.OnClickAsObservable().Subscribe(_ => QualitySettings.SetQualityLevel(2)).AddTo(qualitySettingsModal);
        qualitySettingsModal._highButton.OnClickAsObservable().Subscribe(_ => QualitySettings.SetQualityLevel(3)).AddTo(qualitySettingsModal);
        qualitySettingsModal._veryHighButton.OnClickAsObservable().Subscribe(_ => QualitySettings.SetQualityLevel(4)).AddTo(qualitySettingsModal);
        qualitySettingsModal._ultraButton.OnClickAsObservable().Subscribe(_ => QualitySettings.SetQualityLevel(5)).AddTo(qualitySettingsModal);

        await qualitySettingsModal.WaitAsync(cancellationToken);
        EventSystem.current.SetSelectedGameObject(selectionBackedup);
    }

    public async UniTask WaitAsync(CancellationToken cancellationToken)
    {
        await _exitButton.OnClickAsync();
    }
}
