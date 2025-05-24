using UnityEngine;

using YachuDice.Environment.Interface;

namespace YachuDice.Environment.AndroidEnvironment
{
    public class AndroidEnvironment : IEnvironment
    {
        public void ExitGame()
        {
            Application.Quit();
        }
    }
}
