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
