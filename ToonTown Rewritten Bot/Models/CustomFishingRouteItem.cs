using System.IO;

namespace ToonTown_Rewritten_Bot.Models
{
    /// <summary>Keeps the displayed route name separate from its stable file identity.</summary>
    public record CustomFishingRouteItem(string FilePath, string Name)
    {
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public string DisplayName { get; init; } = Name;
        public override string ToString() => DisplayName;
    }
}
