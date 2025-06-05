using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YachuDice.Relay
{
    public class JoinCodeInputModal : MonoBehaviour
    {
        public static async UniTask<string> OpenJoinCodeInputModalAsync(CancellationToken cancellationToken)
        {
            var selectionBackedup = EventSystem.current.currentSelectedGameObject;

            using var joinCodeInputModalLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<JoinCodeInputModal>("JoinCodeInputModal.prefab", cancellationToken: cancellationToken);
            var joinCodeInputModal = joinCodeInputModalLoads.Instance;
            EventSystem.current.SetSelectedGameObject(joinCodeInputModal.gameObject);

            var joinCode = await joinCodeInputModal.WaitSubmitAsync(cancellationToken);
            EventSystem.current.SetSelectedGameObject(selectionBackedup);

            return joinCode;
        }

        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _submitButton;

        public async UniTask<string> WaitSubmitAsync(CancellationToken cancellationToken)
        {
            await _submitButton.OnClickAsync();

            return _inputField.text;
        }
    }
}
