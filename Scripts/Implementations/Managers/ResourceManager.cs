using Godot;
using System.Collections.Generic;
using Maximagus.Scripts.Spells.Abstractions;

namespace Maximagus.Scripts.Managers
{
    /// <summary>
    /// Manages loading and caching of game resources to ensure a single source of truth
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        // Remove static instance since we'll use ServiceLocator instead

        private readonly Dictionary<string, SpellCardResource> _spellCardResources = new();
        private readonly ILogger _logger;

        public ResourceManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            PreloadResources();
        }

        /// <summary>
        /// Preloads all spell card resources for efficient access
        /// </summary>
        private void PreloadResources()
        {
            // Load all spell resources in the Resources/Spells directory
            var spellPaths = new[]
            {
                "res://Resources/Spells/FrostShards.tres",
                "res://Resources/Spells/Firebolt.tres",
                "res://Resources/Spells/AmplifyFire.tres",
                "res://Resources/Spells/Shatter.tres"
            };

            foreach (var path in spellPaths)
            {
                try
                {
                    var resource = ResourceLoader.Load<SpellCardResource>(path);
                    if (resource != null)
                    {
                        _spellCardResources[resource.CardId] = resource;
                        GD.Print($"[ResourceManager] Preloaded spell resource: {resource.CardName} (ID: {resource.CardId})");
                    }
                    else
                    {
                        _logger?.LogError($"Failed to load spell resource: {path}");
                    }
                }
                catch (System.Exception ex)
                {
                    _logger?.LogError($"Error loading resource: {path}", ex);
                }
            }

            GD.Print($"[ResourceManager] Preloaded {_spellCardResources.Count} spell resources");
        }

        /// <summary>
        /// Gets a spell card resource by its ID
        /// </summary>
        /// <param name="resourceId">The ID of the resource to get</param>
        /// <returns>The SpellCardResource or null if not found</returns>
        public SpellCardResource GetSpellCardResource(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                _logger?.LogWarning("Attempted to get spell card resource with null or empty ID");
                return null;
            }

            if (_spellCardResources.TryGetValue(resourceId, out var resource))
            {
                return resource;
            }

            _logger?.LogWarning($"Spell card resource not found: {resourceId}");
            return null;
        }

        /// <summary>
        /// Gets a random spell card resource
        /// </summary>
        /// <returns>A random SpellCardResource</returns>
        public SpellCardResource GetRandomSpellCardResource()
        {
            if (_spellCardResources.Count == 0)
            {
                _logger?.LogError("No spell card resources available");
                return null;
            }

            var keys = new System.Collections.Generic.List<string>(_spellCardResources.Keys);
            var randomIndex = new System.Random().Next(keys.Count);
            var randomKey = keys[randomIndex];
            
            return _spellCardResources[randomKey];
        }

        /// <summary>
        /// Gets all available spell card resources
        /// </summary>
        /// <returns>An IEnumerable of all SpellCardResources</returns>
        public IEnumerable<SpellCardResource> GetAllSpellCardResources()
        {
            return _spellCardResources.Values;
        }
    }
}