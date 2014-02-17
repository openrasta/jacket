using System;

namespace jacket.Reporting
{
    public class ConsoleColorizer
    {
        public static IDisposable Colorize(ConsoleColor? foreground = null, ConsoleColor? background = null)
        {
            var oldForeground = Console.ForegroundColor;
            var oldBackground = Console.BackgroundColor;
            if (foreground != null)
                Console.ForegroundColor = foreground.Value;
            if (background != null)
                Console.BackgroundColor = background.Value;
            return new ActionOnDispose(() =>
                                       {

                                           if (foreground != null)
                                               Console.ForegroundColor = oldForeground;
                                           if (background != null)
                                               Console.BackgroundColor = oldBackground;
                                       });

        }

        class ActionOnDispose : IDisposable
        {
            readonly Action _action;

            public ActionOnDispose(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}