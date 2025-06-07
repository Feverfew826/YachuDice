using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UniRx;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YachuDice.Relay
{
    public class JoinCodeDisplayModal : MonoBehaviour
    {
        public static async UniTask<bool> OpenJoinCodeDisplayModalAsync(string joinCode, Func<bool> predicate, CancellationToken cancellationToken)
        {
            var selectionBackedup = EventSystem.current.currentSelectedGameObject;

            using var joinCodeDisplayModalLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<JoinCodeDisplayModal>("JoinCodeDisplayModal.prefab", cancellationToken: cancellationToken);
            var joinCodeDisplayModal = joinCodeDisplayModalLoads.Instance;
            EventSystem.current.SetSelectedGameObject(joinCodeDisplayModal.gameObject);

            joinCodeDisplayModal._joinCodeText.text = joinCode;

            var waitTask = UniTask.WaitUntil(predicate, cancellationToken: cancellationToken);
            var quitButtonTask = joinCodeDisplayModal._quitButton.OnClickAsync(cancellationToken);
            var result = await Utilities.Utilities.WhenAnyWithLoserCancellationAsync(waitTask, quitButtonTask);

            EventSystem.current.SetSelectedGameObject(selectionBackedup);

            if (result == 0)
                return true;
            else
                return false;
        }

        [SerializeField] private TextMeshProUGUI _joinCodeText;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _copyButton;

        private void Start()
        {
            _copyButton.OnClickAsObservable().Subscribe(_ => GUIUtility.systemCopyBuffer = _joinCodeText.text).AddTo(this);
        }
    }
}
