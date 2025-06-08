
namespace YachuDice.Environment.Interface
{
    public interface IEnvironment
    {
        bool IsMobilePlatform { get; }
        void ExitGame();
    }
}
