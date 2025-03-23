using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    public struct UserInput
    {
        public bool IsStartGame;
        public bool IsExitGame;

        public string[] playerNames;
    }

    public async UniTask<UserInput> WaitUserInputAsync(CancellationToken cancellationToken)
    {
        using var whenAnyCancellationTokenSource = new CancellationTokenSource();
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, whenAnyCancellationTokenSource.Token);

        var startButtonTask = _startButton.OnClickAsync(linkedCancellationTokenSource.Token);
        var quitButtonTask = _quitButton.OnClickAsync(linkedCancellationTokenSource.Token);

        var result = await UniTask.WhenAny(startButtonTask, quitButtonTask);
        whenAnyCancellationTokenSource.Cancel();

        const int Start = 0;
        const int Quit = 1;
        if (result == Start)
            return new UserInput { IsStartGame = true };
        else if (result == Quit)
            return new UserInput { IsExitGame = true };
        else
            throw new InvalidOperationException();
    }
}
