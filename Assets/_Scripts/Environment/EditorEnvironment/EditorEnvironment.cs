using UnityEditor;

using YachuDice.Environment.Interface;

namespace YachuDice.Environment.EditorEnvironment
{
    public class EditorEnvironment : IEnvironment
    {
        public bool IsMobilePlatform => false;

        public void ExitGame()
        {
            EditorApplication.ExitPlaymode();
        }
    }

}
