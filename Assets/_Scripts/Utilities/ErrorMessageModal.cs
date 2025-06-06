using System.Threading;

using Cysharp.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YachuDice.Utilities
{
    public class ErrorMessageModal : MonoBehaviour
    {
        public static async UniTask OpenErrorMessageModalAsync(string errorMessage, CancellationToken cancellationToken)
        {
            var selectionBackedup = EventSystem.current.currentSelectedGameObject;

            using var errorMessageModalLoads = await AddressableWrapper.AddressableWrapper.DisposableInstantiateAsync<ErrorMessageModal>("ErrorMessageModal.prefab", cancellationToken: cancellationToken);
            var errorMessageModal = errorMessageModalLoads.Instance;
            EventSystem.current.SetSelectedGameObject(errorMessageModal.gameObject);

            errorMessageModal._joinCodeText.text = ToHumanReadableMessage(errorMessage);

            await errorMessageModal._quitButton.OnClickAsync(cancellationToken);

            EventSystem.current.SetSelectedGameObject(selectionBackedup);
        }

        public static string ToHumanReadableMessage(string errorMessage)
        {
            return errorMessage switch
            {
                "UnityServices_InitializeFailed" => "Failed to initializing service. Please check network status.",
                "Authentication_PreconditionFailed_UnityServiceInitialization" => "Service is not initialized. Please retry from first step.",
                "Authentication_SignInAnonymouslyFailed" => "Failed to retrieve player information. Please check network status.",
                "Relay_PreconditionFailed_Authentication" => "Player information is not exist. Please retry from first step.",
                "Relay_CreateAllocationFailed" => "Failed to create network environment. Please check network status.",
                "Relay_PreconditionFailed_Allocation" => "Network environment is not created. Please retry from first step.",
                "Relay_CreateJoinCodeFailed" => "Failed to create join code. Please check network status.",
                "Relay_JoinCodeInputEmpty" => "Please enter the join code from the host.",
                "Relay_JoinAllocationAsync" => "Failed to join to host. Please check join code is correct or check network status.",
                _ => errorMessage
            };
        }

        [SerializeField] private TextMeshProUGUI _joinCodeText;
        [SerializeField] private Button _quitButton;
    }
}
