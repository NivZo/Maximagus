using System.Collections.Generic;
using Maximagus.Scripts.Spells.Abstractions;

namespace Maximagus.Scripts.Managers
{

    public interface IResourceManager
    {
        SpellCardResource GetNextSpellCardResource();
    }
}