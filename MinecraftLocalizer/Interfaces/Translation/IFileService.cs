using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Interfaces.Translation
{
    /// <summary>
    /// Interface for a service that works with files.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Loads tree nodes from a file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>Collection of tree nodes.</returns>
        Task<IEnumerable<TreeNodeItem>> LoadFileNodesAsync(string filePath);
    }
}


