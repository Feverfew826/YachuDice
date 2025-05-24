using UnityEditor;

using YachuDice.Environment.Interface;

namespace YachuDice.Environment.EditorEnvironment
{
    public class EditorEnvironment : IEnvironment
    {
        public void ExitGame()
        {
            EditorApplication.ExitPlaymode();
        }
    }

}
