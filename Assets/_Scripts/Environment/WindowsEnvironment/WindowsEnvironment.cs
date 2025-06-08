using UnityEngine;

using YachuDice.Environment.Interface;

namespace YachuDice.Environment.WindowsEnvironment
{
    public class WindowsEnvironment : IEnvironment
    {
        public bool IsMobilePlatform => false;

        public void ExitGame()
        {
            Application.Quit();
        }
    }
}
