using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Interfaces.Translation
{
    /// <summary>
    /// Interface for a service that works with ZIP archives.
    /// </summary>
    public interface IZipService
    {
        /// <summary>
        /// Loads tree nodes from a ZIP archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <returns>Collection of tree nodes.</returns>
        Task<IEnumerable<TreeNodeItem>> LoadZipNodesAsync(string archivePath);
    }
}


