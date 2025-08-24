
using System;

/// <summary>
/// Interface for spell processing management using command-based approach.
/// Handles spell execution through the centralized command system.
/// </summary>
public interface ISpellProcessingManager
{
    /// <summary>
    /// Processes a spell using the command-based system.
    /// Creates and executes a SpellCastCommand through the GameCommandProcessor.
    /// </summary>
    /// <param name="onComplete">Callback to execute when spell processing is complete</param>
    void ProcessSpell(Action onComplete);
}