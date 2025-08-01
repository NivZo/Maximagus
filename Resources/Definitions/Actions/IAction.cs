
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.Actions
{
    public interface IAction
    {
        void Execute(SpellContext context);
    }
}
