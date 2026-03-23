using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Models.Localization
{
    public class ZipLoadSource : ArchiveLoadSource
    {
        public ZipLoadSource(string zipPath, string internalPath)
            : base(zipPath, internalPath)
        {
        }
    }
}




