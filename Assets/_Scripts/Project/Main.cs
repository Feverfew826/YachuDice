using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;

public static class Main
{
    private static bool _isEditor =
#if UNITY_EDITOR
        true;
#else
        false;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Boot()
    {
        MainAsync(new CancellationToken()).Forget();
    }

    public async static UniTask MainAsync(CancellationToken cancellationToken)
    {
        while(true)
        {
            var titleScene = SceneManager.GetActiveScene();
            var titleSceneManager = titleScene.GetRootGameObjects().First(elmt => elmt.TryGetComponent<TitleSceneManager>(out var _)).GetComponent<TitleSceneManager>();
            var titleSceneUserInputResult = await titleSceneManager.WaitUserInputAsync(cancellationToken);

            if(titleSceneUserInputResult.IsStartGame)
            {
                await SceneManager.LoadSceneAsync("GameScene");
                var gameManager = SceneManager.GetActiveScene().GetRootGameObjects().First(elmt => elmt.TryGetComponent<GameManager>(out var _)).GetComponent<GameManager>();
                var gameResult = await gameManager.PlayGameAsync(new GameManager.GameParameter(), cancellationToken);
                await SceneManager.LoadSceneAsync("TitleScene");
            }
            else if (titleSceneUserInputResult.IsExitGame)
            {
                if(_isEditor)
                    EditorApplication.ExitPlaymode();
                else
                    Application.Quit();

                break;
            }
        }
    }
}
