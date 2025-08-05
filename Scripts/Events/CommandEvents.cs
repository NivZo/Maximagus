using Godot.Collections;

namespace Maximagus.Scripts.Events
{
    // LEGACY COMMAND EVENTS - ALL REMOVED
    // These events were part of the old event-based command pattern
    // Now replaced by pure command system (GameCommandProcessor + IGameCommand)
    
    // REMOVED: StartGameRequestedEvent - replaced by StartGameCommand
    // REMOVED: PlayCardsRequestedEvent - replaced by PlayHandCommand  
    // REMOVED: DiscardCardsRequestedEvent - handled by HandManager directly
    // REMOVED: CastSpellRequestedEvent - integrated into PlayHandCommand
    
    // This file is kept for reference but contains no active event classes
    // TODO: Remove this file entirely once cleanup is verified
}