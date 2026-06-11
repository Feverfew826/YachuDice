using System;
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
        Player3sTurn,
        Player4sTurn,
        YouWin,
        YouLose,
        Player1Win,
        Player2Win,
        Player3Win,
        Player4Win,
    }

    public static async UniTask ShowGamePhaseNotifierAsync(GamePhase gamePhase, CancellationToken cancellationToken)
    {
        try
        {
            using var animatorLoad = await AddressableWrapper.DisposableInstantiateAsync<Animator>($"GamePhaseNotifier_{gamePhase}.prefab");
            var animator = animatorLoad.Instance;

            animator.Update(0f);
            var length = animator.GetCurrentAnimatorStateInfo(0).length;
            await UniTask.Delay(System.TimeSpan.FromSeconds(length), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            // 자산 미준비(Player3/4 phase prefab 등)인 경우 게임 흐름은 계속 가도록 함.
            Debug.LogWarning($"[GamePhaseNotifier] Failed to show {gamePhase}: {e.Message}");
        }
    }
}

public class GameRuleNotifier
{
    public enum GameRule
    {
        Bonus,
    }

    public static async UniTask ShowGameRuleNotifierAsync(GameRule gameRule, CancellationToken cancellationToken)
    {
        try
        {
            using var animatorLoad = await AddressableWrapper.DisposableInstantiateAsync<Animator>($"GameRuleNotifier_{gameRule}.prefab");
            var animator = animatorLoad.Instance;

            animator.Update(0f);
            var length = animator.GetCurrentAnimatorStateInfo(0).length;
            await UniTask.Delay(System.TimeSpan.FromSeconds(length), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            // 자산 미준비인 경우 게임 흐름은 계속 가도록 함.
            Debug.LogWarning($"[GameRuleNotifier] Failed to show {gameRule}: {e.Message}");
        }
    }
}

public class EmotionNotifier
{
    public enum Character
    {
        Girl,
        Boy,
        Dog,
        Cat,
    }

    public static async UniTask ShowEmotionNotifierAsync(EmotionButtonPanel.Emotion emotion, Character character, int xOffset, CancellationToken cancellationToken)
    {
        try
        {
            using var animatorLoad = await AddressableWrapper.DisposableInstantiateAsync<Animator>($"EmotionNotifier_{character}_{emotion}.prefab");
            var animator = animatorLoad.Instance;

            // Hard-coded!! But it's fine for now.
            var childRectTransform = (RectTransform)animator.transform.GetChild(0);
            childRectTransform.anchoredPosition = childRectTransform.anchoredPosition + (Vector2.right * xOffset);

            animator.Update(0f);
            var length = animator.GetCurrentAnimatorStateInfo(0).length;
            await UniTask.Delay(System.TimeSpan.FromSeconds(length), cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            // 자산 미준비(Dog/Cat prefab 등)인 경우 게임 흐름은 계속 가도록 함.
            Debug.LogWarning($"[EmotionNotifier] Failed to show {character}_{emotion}: {e.Message}");
        }
    }
}