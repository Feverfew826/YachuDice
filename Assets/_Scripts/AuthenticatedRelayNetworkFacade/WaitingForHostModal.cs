using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YachuDice.Relay
{
    public class WaitingForHostModal : MonoBehaviour
    {
        public static async UniTask OpenWaitingForHostModalAsync(CancellationToken cancellationToken)
        {
            var selectionBackedup = EventSystem.current.currentSelectedGameObject;

            using var modalLoad = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<WaitingForHostModal>("WaitingForHostModal.prefab", cancellationToken: cancellationToken);
            var modal = modalLoad.Instance;
            EventSystem.current.SetSelectedGameObject(modal._disconnectButton.gameObject);

            try
            {
                await modal._disconnectButton.OnClickAsync(cancellationToken);
            }
            finally
            {
                EventSystem.current.SetSelectedGameObject(selectionBackedup);
            }
        }

        [SerializeField] private Button _disconnectButton;
    }
}
