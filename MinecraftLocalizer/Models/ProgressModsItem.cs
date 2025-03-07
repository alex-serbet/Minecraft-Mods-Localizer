namespace MinecraftLocalizer.Models
{
    public class ProgressModsItem(int progress, int processed, int total, string? filePath = null)
    {
        public int Progress { get; set; } = progress;
        public string? FilePath { get; set; } = filePath;
        public int Processed { get; set; } = processed;
        public int Total { get; set; } = total;
        public double Percentage => Total > 0 ? (double)Processed / Total * 100 : 0;

        public ProgressModsItem(int progress, string? filePath)
            : this(progress, 0, 0, filePath) { }
    }
}
