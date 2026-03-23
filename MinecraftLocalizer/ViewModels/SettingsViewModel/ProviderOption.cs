namespace MinecraftLocalizer.ViewModels
{
    public sealed class ProviderOption
    {
        public required string Id { get; init; }
        public required string Label { get; init; }

        public override string ToString() => Label;
    }
}
