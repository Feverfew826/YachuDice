using System;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using Feverfew.DiLib;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using YachuDice.Environment.Interface;

public static class Main
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Boot()
    {
        SupportEventSystemAsync(Application.exitCancellationToken).Forget();

        if (SceneManager.GetActiveScene().buildIndex == 0)
            MainAsync(Application.exitCancellationToken).Forget();
        else
            PlaySceneAsync(Application.exitCancellationToken).Forget();
    }

    public static async UniTask SupportEventSystemAsync(CancellationToken cancellationToken)
    {
        try
        {
            var lastEventSystem = default(EventSystem);
            var lastSelectedGameObject = default(GameObject);

            while (true)
            {
                var eventSystem = EventSystem.current;

                if (eventSystem != null && eventSystem == lastEventSystem)
                {
                    var selectedGameObject = eventSystem.currentSelectedGameObject;
                    if (selectedGameObject == null)
                        eventSystem.SetSelectedGameObject(lastSelectedGameObject);
                    else
                        lastSelectedGameObject = selectedGameObject;
                }
                else if (eventSystem != null)
                {
                    lastEventSystem = eventSystem;
                }

                await UniTask.NextFrame(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested == false)
                throw;
        }
    }

    public static async UniTask MainAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var titleScene = SceneManager.GetActiveScene();
                var titleSceneManager = SceneManager.GetActiveScene().GetComponent<TitleSceneManager>();
                var titleSceneUserInputResult = await titleSceneManager.WaitUserInputAsync(cancellationToken);

                if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.PlayLocalGame)
                {
                    await SceneManager.LoadSceneAsync("GameScene");
                    var gameManager = SceneManager.GetActiveScene().GetComponent<LocalGameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new LocalGameManager.LocalGameParameter(), cancellationToken);
                    await SceneManager.LoadSceneAsync("TitleScene");
                }
                else if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.PlayNetworkGameAsHost)
                {
                    using var authenticatedRelayNetworkFacade = AuthenticatedRelayNetworkFacade.GetDisposableInstance();
                    if (authenticatedRelayNetworkFacade == null)
                        continue;

                    var startHostResult = await authenticatedRelayNetworkFacade.StartHostThenShowJoinCodeAsync(cancellationToken);
                    if (startHostResult == false)
                        continue;

                    await SceneManager.LoadSceneAsync("NetworkGameScene");

                    var gameManager = SceneManager.GetActiveScene().GetComponent<NetworkGameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new NetworkGameManager.NetworkGameParameter(), authenticatedRelayNetworkFacade, cancellationToken);

                    await SceneManager.LoadSceneAsync("TitleScene");
                }
                else if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.PlayNetworkGameAsClient)
                {
                    using var authenticatedRelayNetworkFacade = AuthenticatedRelayNetworkFacade.GetDisposableInstance();
                    if (authenticatedRelayNetworkFacade == null)
                        continue;

                    var startHostResult = await authenticatedRelayNetworkFacade.RetrieveJoinCodeThenConnectToHostAsync(cancellationToken);
                    if (startHostResult == false)
                        continue;

                    await SceneManager.LoadSceneAsync("NetworkGameScene");

                    var gameManager = SceneManager.GetActiveScene().GetComponent<NetworkGameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new NetworkGameManager.NetworkGameParameter(), authenticatedRelayNetworkFacade, cancellationToken);
                    NetworkManager.Singleton.Shutdown();

                    await SceneManager.LoadSceneAsync("TitleScene");
                }
                else if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.Exit)
                {
                    Containers.ProjectContext.Get<IEnvironment>().ExitGame();

                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested == false)
                throw;
        }
    }

    public static async UniTask PlaySceneAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            if (rootGameObjects.TryGetComponent<NetworkGameManager>(out var networkGameManager))
            {
                using var authenticatedRelayNetworkFacade = AuthenticatedRelayNetworkFacade.GetDisposableInstance();
                if (authenticatedRelayNetworkFacade == null)
                    Containers.ProjectContext.Get<IEnvironment>().ExitGame();

                var autoConnectResult = await authenticatedRelayNetworkFacade.AutoConnectIfNotStartedNetworkAsync(cancellationToken);
                if (autoConnectResult == false)
                    Containers.ProjectContext.Get<IEnvironment>().ExitGame();

                await networkGameManager.PlayGameAsync(new NetworkGameManager.NetworkGameParameter(), authenticatedRelayNetworkFacade, Application.exitCancellationToken);

                Containers.ProjectContext.Get<IEnvironment>().ExitGame();
            }
            else if (rootGameObjects.TryGetComponent<LocalGameManager>(out var gameManager))
            {
                await gameManager.PlayGameAsync(new LocalGameManager.LocalGameParameter(), Application.exitCancellationToken);

                Containers.ProjectContext.Get<IEnvironment>().ExitGame();
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested == false)
                throw;
        }
    }

    public static T GetComponent<T>(this Scene scene)
    {
        scene.GetRootGameObjects().TryGetComponent<T>(out var component);
        return component;
    }

    public static bool TryGetComponent<T>(this Scene scene, out T component)
    {
        return scene.GetRootGameObjects().TryGetComponent<T>(out component);
    }

    public static bool TryGetComponent<T>(this GameObject[] gameObjects, out T component)
    {
        var foundedComponent = default(T);
        gameObjects.FirstOrDefault(elmt => elmt.TryGetComponent(out foundedComponent));
        component = foundedComponent;
        return component != null;
    }
}
