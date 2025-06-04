using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;

namespace YachuDice.Relay
{
    public class JoinCodeDisplayModal : MonoBehaviour
    {
        public static async UniTask OpenJoinCodeDisplayModalAsync(string joinCode, Func<bool> predicate, CancellationToken cancellationToken)
        {
            var selectionBackedup = EventSystem.current.currentSelectedGameObject;

            using var joinCodeDisplayModalLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<JoinCodeDisplayModal>("JoinCodeDisplayModal.prefab", cancellationToken: cancellationToken);
            var joinCodeDisplayModal = joinCodeDisplayModalLoads.Instance;
            EventSystem.current.SetSelectedGameObject(joinCodeDisplayModal.gameObject);

            joinCodeDisplayModal._joinCodeText.text = joinCode;

            await UniTask.WaitUntil(predicate, cancellationToken: cancellationToken);

            EventSystem.current.SetSelectedGameObject(selectionBackedup);
        }

        [SerializeField] private TextMeshProUGUI _joinCodeText;
    }
}
