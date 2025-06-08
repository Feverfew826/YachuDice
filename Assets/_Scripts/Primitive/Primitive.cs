using System;

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace YachuDice.Primitive
{
    public static class LoadingScreen
    {
        private static GameObject _loadingScreenInstance;

        private static int _counter;
        public static IDisposable StartDisposableLoadingScreen()
        {
            if (_loadingScreenInstance == null)
            {
                var prefab = Resources.Load<GameObject>("LoadingScreen");
                _loadingScreenInstance = GameObject.Instantiate(prefab);
                GameObject.DontDestroyOnLoad(_loadingScreenInstance);
            }

            return new LoadingScreenDisposable();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
        public static async UniTask<T> WithLoadingScreen<T>(this UniTask<T> task)
        {
            using (StartDisposableLoadingScreen())
            {
                return await task;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:명명 스타일", Justification = "<보류 중>")]
        public static async UniTask WithLoadingScreen(this UniTask task)
        {
            using (StartDisposableLoadingScreen())
            {
                await task;
            }
        }

        private class LoadingScreenDisposable : IDisposable
        {
            public LoadingScreenDisposable()
            {
                _counter++;
                _loadingScreenInstance.SetActive(true);
            }

            public void Dispose()
            {
                _counter--;
                if (_counter == 0)
                    _loadingScreenInstance.SetActive(false);
            }
        }
    }
}