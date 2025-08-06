using System.Collections.Generic;
using Maximagus.Scripts.Spells.Abstractions;

namespace Maximagus.Scripts.Managers
{
    /// <summary>
    /// Interface for managing game resources to ensure a single source of truth
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Gets a spell card resource by its ID
        /// </summary>
        /// <param name="resourceId">The ID of the resource to get</param>
        /// <returns>The SpellCardResource or null if not found</returns>
        SpellCardResource GetSpellCardResource(string resourceId);

        /// <summary>
        /// Gets a random spell card resource
        /// </summary>
        /// <returns>A random SpellCardResource</returns>
        SpellCardResource GetRandomSpellCardResource();

        /// <summary>
        /// Gets all available spell card resources
        /// </summary>
        /// <returns>An IEnumerable of all SpellCardResources</returns>
        IEnumerable<SpellCardResource> GetAllSpellCardResources();
    }
}