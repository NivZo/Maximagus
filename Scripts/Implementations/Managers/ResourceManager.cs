using Godot;
using System.Collections.Generic;
using Maximagus.Scripts.Spells.Abstractions;
using System.Linq;

namespace Maximagus.Scripts.Managers
{

    public class ResourceManager : IResourceManager
    {
        // Remove static instance since we'll use ServiceLocator instead

        private SpellCardResource[] _spellCardResources;
        private readonly ILogger _logger;
        private int _currentIndex = 0;

        public ResourceManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            PreloadResources();
        }

        private void PreloadResources()
        {
            // Load all spell resources in the Resources/Spells directory
            var spellPaths = new[]
            {
                "res://Resources/Spells/FrostShards.tres",
                "res://Resources/Spells/FrostShards.tres",
                "res://Resources/Spells/FrostShards.tres",
                "res://Resources/Spells/FrostShards.tres",
                "res://Resources/Spells/FrostShards.tres",
                "res://Resources/Spells/Shatter.tres",
                "res://Resources/Spells/Shatter.tres",
                "res://Resources/Spells/Firebolt.tres",
                "res://Resources/Spells/Firebolt.tres",
                "res://Resources/Spells/Firebolt.tres",
                "res://Resources/Spells/Firebolt.tres",
                "res://Resources/Spells/Firebolt.tres",
                "res://Resources/Spells/AmplifyFire.tres",
                "res://Resources/Spells/AmplifyFire.tres",
            };

            _spellCardResources = spellPaths.Shuffle().Select(path => ResourceLoader.Load<SpellCardResource>(path, cacheMode: ResourceLoader.CacheMode.IgnoreDeep)).ToArray();

            _logger.LogInfo($"[ResourceManager] Preloaded {_spellCardResources.Length} spell resources");
        }
        public SpellCardResource GetNextSpellCardResource()
        {
            if (_spellCardResources.Length == 0)
            {
                _logger?.LogError("No spell card resources available");
                return null;
            }

            var resource = _spellCardResources[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _spellCardResources.Length;
            return resource;
        }
    }
}