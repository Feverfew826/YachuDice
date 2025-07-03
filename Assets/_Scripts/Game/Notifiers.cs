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

public class EmotionNotifier
{
    public static async UniTask ShowEmotionNotifierAsync(EmotionButtonPanel.Emotion emotion, int xOffset, CancellationToken cancellationToken)
    {
        using var animatorLoad = await AddressableWrapper.DisposableInstantiateAsync<Animator>($"EmotionNotifier_{emotion}.prefab");
        var animator = animatorLoad.Instance;

        // Hard-coded!! But it's fine for now.
        var childRectTransform = (RectTransform)animator.transform.GetChild(0);
        childRectTransform.anchoredPosition = childRectTransform.anchoredPosition + (Vector2.right * xOffset);

        animator.Update(0f);
        var length = animator.GetCurrentAnimatorStateInfo(0).length;
        await UniTask.Delay(System.TimeSpan.FromSeconds(length), cancellationToken: cancellationToken);
    }
}