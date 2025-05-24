using UnityEngine;

using YachuDice.Environment.Interface;

namespace YachuDice.Environment.WindowsEnvironment
{
    public class WindowsEnvironment : IEnvironment
    {
        public void ExitGame()
        {
            Application.Quit();
        }
    }
}
