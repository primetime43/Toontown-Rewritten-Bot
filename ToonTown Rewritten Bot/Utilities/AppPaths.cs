using System;
using System.IO;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class AppPaths
    {
        public static string ExeDirectory { get; } = Path.GetDirectoryName(Environment.ProcessPath);

        public static string GameDataDirectory { get; } = CreateDirectory(
            GameProfile.GetDataDirectory(GameProfile.Current, ExeDirectory));

        public static string TemplatesDirectory { get; } = FindTemplatesDirectory();

        private static string CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        private static string FindTemplatesDirectory()
        {
            // Preserve Rewritten's existing development templates. Clash never uses them.
            if (!GameProfile.IsClash)
            {
                for (var dir = new DirectoryInfo(ExeDirectory); dir?.Parent != null; dir = dir.Parent)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "ToonTown Rewritten Bot.csproj")))
                        return CreateDirectory(Path.Combine(dir.FullName, "Templates"));
                }
            }
            return CreateDirectory(Path.Combine(GameDataDirectory, "Templates"));
        }
    }
}
