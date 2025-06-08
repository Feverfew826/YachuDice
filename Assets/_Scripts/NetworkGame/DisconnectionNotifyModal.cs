using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using YachuDice.AddressableWrapper;

public class DisconnectionNotifyModal : MonoBehaviour
{
    public static async UniTask OpenDisconnectionNotifyModalAsync(bool isClientDisconnected, CancellationToken cancellationToken)
    {
        var selectionBackedup = EventSystem.current.currentSelectedGameObject;

        using var disconnectionNotifyModalLoads = await AddressableWrapper.DisposableInstantiateAsync<DisconnectionNotifyModal>("DisconnectionNotifyModal.prefab", cancellationToken: cancellationToken);
        var disconnectionNotifyModal = disconnectionNotifyModalLoads.Instance;
        EventSystem.current.SetSelectedGameObject(disconnectionNotifyModal.gameObject);
        disconnectionNotifyModal._messageText.text = isClientDisconnected ? "The connection between client disconnected." : "The connection between host disconnected.";

        await disconnectionNotifyModal._quitButton.OnClickAsync(cancellationToken);

        EventSystem.current.SetSelectedGameObject(selectionBackedup);
    }

    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _quitButton;
}
