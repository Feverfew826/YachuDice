using Feverfew.DiLib;

using UnityEngine;

using YachuDice.Environment.Interface;

namespace YachuDice.Environment
{
    public class EnvironmentInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InjectDependency()
        {
#if UNITY_EDITOR
            Containers.ProjectContext.Set<IEnvironment>(new EditorEnvironment.EditorEnvironment());
#elif UNITY_ANDROID
            Containers.ProjectContext.Set<IEnvironment>(new AndroidEnvironment.AndroidEnvironment());
#endif
        }
    }
}