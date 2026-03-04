using System;
using System.IO;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class AppPaths
    {
        public static string ExeDirectory { get; } = Path.GetDirectoryName(Environment.ProcessPath);
    }
}
