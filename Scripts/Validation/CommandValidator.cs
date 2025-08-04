using System;
using System.Collections.Generic;
using Scripts.Commands;
using Scripts.State;

namespace Scripts.Validation
{
    /// <summary>
    /// Validates commands before execution to ensure they meet business rules
    /// </summary>
    public class CommandValidator
    {
        private readonly List<ICommandValidationRule> _validationRules;

        public CommandValidator()
        {
            _validationRules = new List<ICommandValidationRule>();
            InitializeDefaultRules();
        }

        /// <summary>
        /// Validates a command against the current game state
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <param name="currentState">The current game state</param>
        /// <returns>Validation result with success status and any error messages</returns>
        public CommandValidationResult ValidateCommand(IGameCommand command, IGameStateData currentState)
        {
            if (command == null)
                return CommandValidationResult.Failure("Command cannot be null");

            if (currentState == null)
                return CommandValidationResult.Failure("Current state cannot be null");

            // First check if the command itself can execute
            if (!command.CanExecute(currentState))
                return CommandValidationResult.Failure($"Command {command.GetDescription()} cannot execute in current state");

            // Run through all validation rules
            var errors = new List<string>();
            var warnings = new List<string>();

            foreach (var rule in _validationRules)
            {
                var result = rule.Validate(command, currentState);
                if (!result.IsValid)
                {
                    if (result.IsCritical)
                        errors.AddRange(result.Messages);
                    else
                        warnings.AddRange(result.Messages);
                }
            }

            if (errors.Count > 0)
                return CommandValidationResult.Failure(errors, warnings);

            if (warnings.Count > 0)
                return CommandValidationResult.SuccessWithWarnings(warnings);

            return CommandValidationResult.Success();
        }

        /// <summary>
        /// Adds a custom validation rule
        /// </summary>
        /// <param name="rule">The validation rule to add</param>
        public void AddValidationRule(ICommandValidationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _validationRules.Add(rule);
        }

        /// <summary>
        /// Removes a validation rule
        /// </summary>
        /// <param name="rule">The validation rule to remove</param>
        public void RemoveValidationRule(ICommandValidationRule rule)
        {
            _validationRules.Remove(rule);
        }

        /// <summary>
        /// Clears all validation rules
        /// </summary>
        public void ClearValidationRules()
        {
            _validationRules.Clear();
        }

        private void InitializeDefaultRules()
        {
            _validationRules.Add(new GamePhaseValidationRule());
            _validationRules.Add(new PlayerStateValidationRule());
            _validationRules.Add(new HandStateValidationRule());
        }
    }

    /// <summary>
    /// Interface for command validation rules
    /// </summary>
    public interface ICommandValidationRule
    {
        /// <summary>
        /// Validates a command against the current state
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <param name="currentState">The current game state</param>
        /// <returns>Validation result</returns>
        ValidationRuleResult Validate(IGameCommand command, IGameStateData currentState);
    }

    /// <summary>
    /// Result of a validation rule check
    /// </summary>
    public class ValidationRuleResult
    {
        public bool IsValid { get; }
        public bool IsCritical { get; }
        public IReadOnlyList<string> Messages { get; }

        private ValidationRuleResult(bool isValid, bool isCritical, IReadOnlyList<string> messages)
        {
            IsValid = isValid;
            IsCritical = isCritical;
            Messages = messages ?? new List<string>();
        }

        public static ValidationRuleResult Valid() => new ValidationRuleResult(true, false, null);
        
        public static ValidationRuleResult Invalid(string message, bool isCritical = true) => 
            new ValidationRuleResult(false, isCritical, new List<string> { message });
        
        public static ValidationRuleResult Invalid(IReadOnlyList<string> messages, bool isCritical = true) => 
            new ValidationRuleResult(false, isCritical, messages);
    }

    /// <summary>
    /// Result of command validation
    /// </summary>
    public class CommandValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> Warnings { get; }

        private CommandValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public static CommandValidationResult Success() => 
            new CommandValidationResult(true, null, null);

        public static CommandValidationResult SuccessWithWarnings(IReadOnlyList<string> warnings) => 
            new CommandValidationResult(true, null, warnings);

        public static CommandValidationResult Failure(string error) => 
            new CommandValidationResult(false, new List<string> { error }, null);

        public static CommandValidationResult Failure(IReadOnlyList<string> errors, IReadOnlyList<string> warnings = null) => 
            new CommandValidationResult(false, errors, warnings);
    }
}