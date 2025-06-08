using System;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using YachuDice.Environment.Interface;
using YachuDice.Utilities;

public class TitleSceneManager : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _networkPlayAsHostButton;
    [SerializeField] private Button _networkPlayAsClientButton;
    [SerializeField] private Button _quitButton;

    public enum UserInputType
    {
        PlayLocalGame,
        PlayNetworkGameAsHost,
        PlayNetworkGameAsClient,
        Exit,
    }

    public struct UserInput
    {
        public UserInputType userInput;

        public string[] playerNames;
    }

    private void Start()
    {
        if (Feverfew.DiLib.Containers.ProjectContext.Get<IEnvironment>().IsMobilePlatform)
            _startButton.gameObject.SetActive(false);
    }

    public async UniTask<UserInput> WaitUserInputAsync(CancellationToken cancellationToken)
    {
        var userSelectedButton = await Utilities.OnAnyClickAsync(_startButton, _networkPlayAsHostButton, _networkPlayAsClientButton, _quitButton, cancellationToken);

        if (userSelectedButton == _startButton)
            return new UserInput { userInput = UserInputType.PlayLocalGame };
        else if (userSelectedButton == _networkPlayAsHostButton)
            return new UserInput { userInput = UserInputType.PlayNetworkGameAsHost };
        else if (userSelectedButton == _networkPlayAsClientButton)
            return new UserInput { userInput = UserInputType.PlayNetworkGameAsClient };
        else if (userSelectedButton == _quitButton)
            return new UserInput { userInput = UserInputType.Exit };
        else
            throw new InvalidOperationException();
    }
}
