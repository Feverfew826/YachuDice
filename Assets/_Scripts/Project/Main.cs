using System;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using Feverfew.DiLib;

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

                if (titleSceneUserInputResult.IsStartGame)
                {
                    await SceneManager.LoadSceneAsync("GameScene");
                    var gameManager = SceneManager.GetActiveScene().GetComponent<GameManager>();
                    var gameResult = await gameManager.PlayGameAsync(new GameManager.GameParameter(), cancellationToken);
                    await SceneManager.LoadSceneAsync("TitleScene");
                }
                else if (titleSceneUserInputResult.IsExitGame)
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
            if (rootGameObjects.TryGetComponent<GameManager>(out var gameManager))
            {
                await gameManager.PlayGameAsync(new GameManager.GameParameter(), Application.exitCancellationToken);

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
