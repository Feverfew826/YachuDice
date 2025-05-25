using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using YachuDice.AddressableWrapper;

public class QualitySettings : MonoBehaviour
{
    [SerializeField] private Button _saveButton;

    public static async UniTask OpenQualitySettingsAsync(CancellationToken cancellationToken)
    {
        var selectionBackedup = EventSystem.current.currentSelectedGameObject;

        using var qualitySettingsLoads = await AddressableWrapper.DisposableInstantiateAsync<QualitySettings>("QualitySettings.prefab", cancellationToken: cancellationToken);
        var qualitySettings = qualitySettingsLoads.Instance;
        EventSystem.current.SetSelectedGameObject(qualitySettings.gameObject);

        await qualitySettings.WaitAsync(cancellationToken);
        EventSystem.current.SetSelectedGameObject(selectionBackedup);
    }

    public async UniTask WaitAsync(CancellationToken cancellationToken)
    {
        await _saveButton.OnClickAsync();
    }
}
