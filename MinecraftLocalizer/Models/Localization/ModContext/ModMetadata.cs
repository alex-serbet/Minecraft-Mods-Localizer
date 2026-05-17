namespace MinecraftLocalizer.Models.Localization.ModContext
{
    /// <summary>
    /// Metadata extracted from a mod's manifest (mods.toml / fabric.mod.json / quilt.mod.json).
    /// Used to build the mod-context block that is injected into translation prompts so the LLM
    /// keeps terminology consistent across batches.
    /// </summary>
    public sealed class ModMetadata
    {
        public string? ModId { get; set; }
        public string? Version { get; set; }

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(ModId) &&
            string.IsNullOrWhiteSpace(Version);
    }
}
