using System;

namespace NVM.Utils
{
    public sealed class ConsoleSpinner
    {
        public static ConsoleSpinner Instance => _lazy.Value;
        private static Lazy<ConsoleSpinner> _lazy = new Lazy<ConsoleSpinner>(() => new ConsoleSpinner());

        private readonly int _consoleX;
        private readonly int _consoleY;
        private readonly char[] _frames = {'|', '/', '-', '\\'};
        private int _current;

        private ConsoleSpinner()
        {
            _current = 0;
            _consoleX = Console.CursorLeft;
            _consoleY = Console.CursorTop;
        }

        public void Update()
        {
            Console.Write(_frames[_current]);
            Console.SetCursorPosition(_consoleX, _consoleY);
            if (++_current >= _frames.Length)
            {
                _current = 0;
            }
        }

        public static void Reset()
        {
            _lazy = new Lazy<ConsoleSpinner>(() => new ConsoleSpinner());
        }
    }
}
