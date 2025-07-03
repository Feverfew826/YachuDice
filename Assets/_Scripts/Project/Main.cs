using System;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using Feverfew.DiLib;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using YachuDice.Environment.Interface;
using YachuDice.Primitive;
using YachuDice.Utilities;

using static Unity.Netcode.NetworkSceneManager;

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
            await IngameDebugConsoleStarter.OpenIngameDebugConsoleAndDontDestroyAsync(cancellationToken);

            while (true)
            {
                var titleScene = SceneManager.GetActiveScene();
                var titleSceneManager = SceneManager.GetActiveScene().GetComponent<TitleSceneManager>();
                var titleSceneUserInputResult = await titleSceneManager.WaitUserInputAsync(cancellationToken);

                if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.PlayLocalGame)
                {
                    await SceneManager.LoadSceneAsync("GameScene").ToUniTask(cancellationToken: cancellationToken).WithLoadingScreen();
                    var gameManager = SceneManager.GetActiveScene().GetComponent<LocalGameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new LocalGameManager.LocalGameParameter(), cancellationToken);
                    await SceneManager.LoadSceneAsync("TitleScene").ToUniTask(cancellationToken: cancellationToken).WithLoadingScreen();
                }
                else if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.PlayNetworkGameAsHost)
                {
                    using var authenticatedRelayNetworkFacade = AuthenticatedRelayNetworkFacade.GetDisposableInstance();
                    if (authenticatedRelayNetworkFacade == null)
                        continue;

                    var startHostResult = await authenticatedRelayNetworkFacade.StartHostThenShowJoinCodeAsync(cancellationToken);
                    if (startHostResult == false)
                        continue;

                    var networkGameScene = default(Scene);
                    using (LoadingScreen.StartDisposableLoadingScreen())
                    {
                        var loadCompletionSource = new UniTaskCompletionSource<Scene>();
                        var loadEventCompletionSource = new UniTaskCompletionSource();
                        SceneEventDelegate onSceneEventHandler = (Unity.Netcode.SceneEvent sceneEvent) =>
                        {
                            Debug.Log($"[SceneEvent] ClientId: {sceneEvent.ClientId}, SceneEventType: {sceneEvent.SceneEventType}, SceneName: {sceneEvent.SceneName}");

                            if (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.LoadComplete && sceneEvent.SceneName == "NetworkGameScene")
                                loadCompletionSource.TrySetResult(sceneEvent.Scene);

                            if (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.LoadEventCompleted && sceneEvent.SceneName == "NetworkGameScene")
                                loadEventCompletionSource.TrySetResult();
                        };

                        authenticatedRelayNetworkFacade.NetworkManager.SceneManager.OnSceneEvent += onSceneEventHandler;

                        var sceneEventProgressStatus = authenticatedRelayNetworkFacade.NetworkManager.SceneManager.LoadScene("NetworkGameScene", LoadSceneMode.Single);
                        if (sceneEventProgressStatus != Unity.Netcode.SceneEventProgressStatus.Started)
                            continue;

                        networkGameScene = await loadCompletionSource.Task.AttachExternalCancellation(cancellationToken);
                        await loadEventCompletionSource.Task.AttachExternalCancellation(cancellationToken);

                        authenticatedRelayNetworkFacade.NetworkManager.SceneManager.OnSceneEvent -= onSceneEventHandler;
                    }

                    var gameManager = networkGameScene.GetComponent<NetworkGameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new NetworkGameManager.NetworkGameParameter(), authenticatedRelayNetworkFacade, cancellationToken);

                    await SceneManager.LoadSceneAsync("TitleScene").ToUniTask(cancellationToken: cancellationToken).WithLoadingScreen();
                }
                else if (titleSceneUserInputResult.userInput == TitleSceneManager.UserInputType.PlayNetworkGameAsClient)
                {
                    using var authenticatedRelayNetworkFacade = AuthenticatedRelayNetworkFacade.GetDisposableInstance();
                    if (authenticatedRelayNetworkFacade == null)
                        continue;

                    var startHostResult = await authenticatedRelayNetworkFacade.RetrieveJoinCodeThenConnectToHostAsync(cancellationToken);
                    if (startHostResult == false)
                        continue;

                    var networkGameScene = default(Scene);
                    using (LoadingScreen.StartDisposableLoadingScreen())
                    {
                        var loadCompletionSource = new UniTaskCompletionSource<Scene>();
                        var loadEventCompletionSource = new UniTaskCompletionSource();
                        SceneEventDelegate onSceneEventHandler = (Unity.Netcode.SceneEvent sceneEvent) =>
                        {
                            Debug.Log($"[SceneEvent] ClientId: {sceneEvent.ClientId}, SceneEventType: {sceneEvent.SceneEventType}, SceneName: {sceneEvent.SceneName}");

                            if (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.LoadComplete && sceneEvent.SceneName == "NetworkGameScene")
                                loadCompletionSource.TrySetResult(sceneEvent.Scene);

                            if (sceneEvent.SceneEventType == Unity.Netcode.SceneEventType.LoadEventCompleted && sceneEvent.SceneName == "NetworkGameScene")
                                loadEventCompletionSource.TrySetResult();
                        };

                        authenticatedRelayNetworkFacade.NetworkManager.SceneManager.OnSceneEvent += onSceneEventHandler;

                        networkGameScene = await loadCompletionSource.Task.AttachExternalCancellation(cancellationToken);
                        await loadEventCompletionSource.Task.AttachExternalCancellation(cancellationToken);

                        authenticatedRelayNetworkFacade.NetworkManager.SceneManager.OnSceneEvent -= onSceneEventHandler;
                    }

                    var gameManager = networkGameScene.GetComponent<NetworkGameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new NetworkGameManager.NetworkGameParameter(), authenticatedRelayNetworkFacade, cancellationToken);

                    await SceneManager.LoadSceneAsync("TitleScene").ToUniTask(cancellationToken: cancellationToken).WithLoadingScreen();
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
            // 노트북이 구려서 PlayMode 진입 후 버벅이면서 애니메이션이 끊기는게 보기 안좋아서 잠시 기다렸다 시작.
            await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
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
