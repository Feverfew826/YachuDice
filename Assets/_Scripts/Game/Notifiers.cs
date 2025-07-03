using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

using YachuDice.AddressableWrapper;

public class CombinationNotifier
{
    public static async UniTask ShowCombinationNotifierAsync(Combination combination, CancellationToken cancellationToken)
    {
        using var animatorLoad = await AddressableWrapper.DisposableInstantiateAsync<Animator>($"CombinationNotifier_{combination}.prefab");
        var animator = animatorLoad.Instance;

        animator.Update(0f);
        var length = animator.GetCurrentAnimatorStateInfo(0).length;
        await UniTask.Delay(System.TimeSpan.FromSeconds(length), cancellationToken: cancellationToken);
    }

}
public class GamePhaseNotifier
{
    public enum GamePhase
    {
        GameStart,
        MyTurn,
        OpponentsTurn,
        Player1sTurn,
        Player2sTurn,
        YouWin,
        YouLose,
        Player1Win,
        Player2Win,
    }

    public static async UniTask ShowGamePhaseNotifierAsync(GamePhase gamePhase, CancellationToken cancellationToken)
    {
        using var animatorLoad = await AddressableWrapper.DisposableInstantiateAsync<Animator>($"GamePhaseNotifier_{gamePhase}.prefab");
        var animator = animatorLoad.Instance;

        animator.Update(0f);
        var length = animator.GetCurrentAnimatorStateInfo(0).length;
        await UniTask.Delay(System.TimeSpan.FromSeconds(length), cancellationToken: cancellationToken);
    }
}
