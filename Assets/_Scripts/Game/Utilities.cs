using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Utilities
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