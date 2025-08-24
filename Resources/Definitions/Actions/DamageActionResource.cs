
using System;
using Godot;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Spell;
using Scripts.Utilities;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class DamageActionResource : ActionResource
    {
        [Export] public DamageType DamageType { get; set; }
        [Export] public int Amount { get; set; }

        public DamageActionResource()
        {
            ResourceLocalToScene = true;
        }

        public override string GetPopUpEffectText(IGameStateData gameState)
        {
            try
            {
                var snapshot = SnapshotLookupHelper.TryGetSnapshotForAction(gameState, ActionId, "DamageActionResource");
                
                if (snapshot != null)
                {
                    var finalDamage = snapshot.ActionResult.FinalDamage;
                    GD.Print($"[DamageActionResource] Using snapshot damage for popup: {finalDamage} (action: {ActionId})");
                    return $"-{finalDamage:F0}";
                }
                
                GD.Print($"[DamageActionResource] Using base damage for popup: {Amount} (action: {ActionId})");
                return $"-{Amount}";
            }
            catch (Exception ex)
            {
                GD.Print($"[DamageActionResource] Error getting popup text from snapshot: {ex.Message}");
                return $"-{Amount}";
            }
        }

        public override Color PopUpEffectColor => DamageType switch
        {
            DamageType.Fire => new Color(1, 0.5f, 0),
            DamageType.Frost => new Color(0, 0.5f, 1),
            DamageType.PerChill => new Color(0, 0.5f, 1),
            _ => new Color(1, 1, 1)
        };

        public override GameCommand CreateExecutionCommand(string cardId)
        {
            return new ExecuteCardActionCommand(this, cardId);
        }
    }
}
