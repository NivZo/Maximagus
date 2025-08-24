using System.Collections.Generic;
using Maximagus.Scripts.Spells.Abstractions;

namespace Maximagus.Scripts.Managers
{
    /// <summary>
    /// Interface for managing game resources to ensure a single source of truth
    /// </summary>
    public interface IResourceManager
    {
        SpellCardResource GetNextSpellCardResource();
    }
}