using System.Collections.Generic;
using System.Collections.Immutable;
using Maximagus.Resources.Definitions.Actions;
using Scripts.State;

namespace Scripts.Interfaces.Services
{
    public interface ISpellSnapshotService
    {
        EncounterStateSnapshot PreCalculateActionWithSnapshot(
            ActionResource action,
            EncounterState currentEncounterState);

        ImmutableArray<EncounterStateSnapshot> PreCalculateSpellWithSnapshots(
            IGameStateData initialGameState,
            IEnumerable<CardState> playedCards);

        IGameStateData ApplyEncounterSnapshot(
            IGameStateData gameState,
            EncounterStateSnapshot snapshot);
    }
}