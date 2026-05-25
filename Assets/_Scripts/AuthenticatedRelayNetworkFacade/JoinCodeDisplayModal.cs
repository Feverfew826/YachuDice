using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UniRx;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YachuDice.Relay
{
    public class JoinCodeDisplayModal : MonoBehaviour
    {
        public static async UniTask<bool> OpenJoinCodeDisplayModalAsync(string joinCode, NetworkManager networkManager, int minPlayers, int maxPlayers, CancellationToken cancellationToken)
        {
            var selectionBackedup = EventSystem.current.currentSelectedGameObject;

            using var joinCodeDisplayModalLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<JoinCodeDisplayModal>("JoinCodeDisplayModal.prefab", cancellationToken: cancellationToken);
            var joinCodeDisplayModal = joinCodeDisplayModalLoads.Instance;
            EventSystem.current.SetSelectedGameObject(joinCodeDisplayModal._startButton.gameObject);

            joinCodeDisplayModal._joinCodeText.text = joinCode;

            using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            PollConnectedCountAsync(joinCodeDisplayModal, networkManager, minPlayers, maxPlayers, pollCts.Token).Forget();

            var startButtonTask = joinCodeDisplayModal._startButton.OnClickAsync(cancellationToken);
            var quitButtonTask = joinCodeDisplayModal._quitButton.OnClickAsync(cancellationToken);
            var result = await Utilities.Utilities.WhenAnyWithLoserCancellationAsync(startButtonTask, quitButtonTask);

            pollCts.Cancel();

            EventSystem.current.SetSelectedGameObject(selectionBackedup);

            return result == 0;
        }

        private static async UniTask PollConnectedCountAsync(JoinCodeDisplayModal modal, NetworkManager networkManager, int minPlayers, int maxPlayers, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                var totalPlayers = networkManager != null ? networkManager.ConnectedClients.Count : 0;
                if (modal._connectedCountText != null)
                    modal._connectedCountText.text = $"{totalPlayers} / {maxPlayers}";
                modal._startButton.interactable = totalPlayers >= minPlayers;
                await UniTask.NextFrame(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        [SerializeField] private TextMeshProUGUI _joinCodeText;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _copyButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private TextMeshProUGUI _connectedCountText;

        private void Start()
        {
            _copyButton.OnClickAsObservable().Subscribe(_ => GUIUtility.systemCopyBuffer = _joinCodeText.text).AddTo(this);
        }
    }
}
