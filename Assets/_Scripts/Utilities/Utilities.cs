using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine.Assertions;
using UnityEngine.UI;

namespace YachuDice.Utilities
{
    public static class Utilities
    {
        public static async UniTask<int> OnAnyClickAsync(this Button[] buttons, CancellationToken cancellationToken)
        {
            Assert.IsTrue(buttons.All(button => button != null));

            using var whenAnyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var whenAnyCancellationToken = whenAnyCancellationTokenSource.Token;

            var tasks = new List<UniTask>(buttons.Length);
            foreach (var button in buttons)
                tasks.Add(button.OnClickAsync(whenAnyCancellationToken));

            var result = await UniTask.WhenAny(tasks);
            whenAnyCancellationTokenSource.Cancel();

            return result;
        }

        public static async UniTask<Button> OnAnyClickAsync(Button button0, Button button1, CancellationToken cancellationToken)
        {
            using var whenAnyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var whenAnyCancellationToken = whenAnyCancellationTokenSource.Token;

            var result = await UniTask.WhenAny(button0.OnClickAsync(whenAnyCancellationToken), button1.OnClickAsync(whenAnyCancellationToken));
            whenAnyCancellationTokenSource.Cancel();

            if (result == 0)
                return button0;
            else
                return button1;
        }

        public static async UniTask<Button> OnAnyClickAsync(Button button0, Button button1, Button button2, CancellationToken cancellationToken)
        {
            using var whenAnyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var whenAnyCancellationToken = whenAnyCancellationTokenSource.Token;

            var result = await UniTask.WhenAny(
                button0.OnClickAsync(whenAnyCancellationToken),
                button1.OnClickAsync(whenAnyCancellationToken),
                button2.OnClickAsync(whenAnyCancellationToken)
                );
            whenAnyCancellationTokenSource.Cancel();

            if (result == 0)
                return button0;
            else if (result == 1)
                return button1;
            else
                return button2;
        }

        public static async UniTask<Button> OnAnyClickAsync(Button button0, Button button1, Button button2, Button button3, CancellationToken cancellationToken)
        {
            using var whenAnyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var whenAnyCancellationToken = whenAnyCancellationTokenSource.Token;

            var result = await UniTask.WhenAny(
                button0.OnClickAsync(whenAnyCancellationToken),
                button1.OnClickAsync(whenAnyCancellationToken),
                button2.OnClickAsync(whenAnyCancellationToken),
                button3.OnClickAsync(whenAnyCancellationToken)
                );
            whenAnyCancellationTokenSource.Cancel();

            if (result == 0)
                return button0;
            else if (result == 1)
                return button1;
            else if (result == 2)
                return button2;
            else
                return button3;
        }

        public static async UniTask<(int winArgumentIndex, T0 result0, T1 result1)> WhenAnyWithLoserCancellationAsync<T0, T1>(UniTask<T0> task0, UniTask<T1> task1)
        {
            using var whenAnyCancellationTokenSource = new CancellationTokenSource();

            var whenAnyCancellableTask0 = task0.AttachExternalCancellation(whenAnyCancellationTokenSource.Token);
            var whenAnyCancellableTask1 = task1.AttachExternalCancellation(whenAnyCancellationTokenSource.Token);

            var result = await UniTask.WhenAny(whenAnyCancellableTask0, whenAnyCancellableTask1);
            whenAnyCancellationTokenSource.Cancel();

            return result;
        }

        public static async UniTask<int> WhenAnyWithLoserCancellationAsync(params UniTask[] tasks)
        {
            using var whenAnyCancellationTokenSource = new CancellationTokenSource();

            var whenAnyCancellableTasks = new UniTask[tasks.Length];
            for (var i = 0; i < tasks.Length; i++)
                whenAnyCancellableTasks[i] = tasks[i].AttachExternalCancellation(whenAnyCancellationTokenSource.Token);

            var result = await UniTask.WhenAny(whenAnyCancellableTasks);
            whenAnyCancellationTokenSource.Cancel();

            return result;
        }

        // 참고: https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/Consume.cs
        public static void Consume<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var _ in source)
            {
            }
        }
    }
}