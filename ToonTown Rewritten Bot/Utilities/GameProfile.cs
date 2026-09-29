using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal enum GameKind { Rewritten, CorporateClash }

    internal static class GameProfile
    {
        private static readonly string SelectionPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath), "game_selection.json");

        // Fixed for this process: services and caches must all use the same profile.
        public static GameKind Current { get; } = ReadSelection(SelectionPath);
        public static bool IsClash => Current == GameKind.CorporateClash;
        public static string DisplayName => GetDisplayName(Current);
        public static string WindowNotFoundMessage => $"{DisplayName} window not found. Start the game and log in before continuing.";

        public static string GetDisplayName(GameKind game) => game == GameKind.CorporateClash
            ? "Corporate Clash" : "Toontown Rewritten";

        internal static GameKind ReadSelection(string path)
        {
            if (!File.Exists(path)) return GameKind.Rewritten;
            var value = JsonSerializer.Deserialize<string>(File.ReadAllText(path));
            return value switch
            {
                nameof(GameKind.Rewritten) => GameKind.Rewritten,
                nameof(GameKind.CorporateClash) => GameKind.CorporateClash,
                _ => throw new InvalidDataException("Unknown game profile. Choose Rewritten or Corporate Clash.")
            };
        }

        public static void SaveSelection(GameKind game)
        {
            if (!Enum.IsDefined(game)) throw new ArgumentOutOfRangeException(nameof(game));
            File.WriteAllText(SelectionPath, JsonSerializer.Serialize(game.ToString()));
        }

        internal static string GetDataDirectory(GameKind game, string exeDirectory) =>
            game == GameKind.CorporateClash ? Path.Combine(exeDirectory, "Profiles", "CorporateClash") : exeDirectory;

        internal static string[] GetWindowTitles(GameKind game) => game == GameKind.CorporateClash
            ? new[] { "Toontown: Corporate Clash", "Corporate Clash", "Toontown Corporate Clash" }
            : new[] { "Toontown Rewritten" };

        public static IntPtr FindWindow() => FindWindow(Current, title => FindWindowNative(null, title));

        internal static IntPtr FindWindow(GameKind game, Func<string, IntPtr> findByTitle)
        {
            foreach (string title in GetWindowTitles(game))
            {
                var handle = findByTitle(title);
                if (handle != IntPtr.Zero) return handle;
            }
            return IntPtr.Zero;
        }

        internal static bool SupportsFishingLocation(GameKind game, string location) =>
            game == GameKind.Rewritten || location == FishingLocationNames.FishAnywhere ||
            location == FishingLocationNames.CustomFishingAction;

        [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowNative(string className, string windowName);
    }
}
