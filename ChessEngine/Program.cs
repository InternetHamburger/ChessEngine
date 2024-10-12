using ChessEngine.src;

namespace ChessEngine
{
    internal class Program
    {
        static void Main()
        {
            UCI uci = new();

            while (true)
            {
                string? input = Console.ReadLine();
                uci.HandleInput(input);
            }
        }
    }
}