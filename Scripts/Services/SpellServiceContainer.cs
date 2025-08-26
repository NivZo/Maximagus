using System;
using Maximagus.Scripts.Managers;
using Scripts.Interfaces.Services;

namespace Maximagus.Scripts.Services
{
    public static class SpellServiceContainer
    {
        private static ISpellSnapshotService _snapshotService;
        private static ISpellStateService _stateService;
        private static IDamageCalculationService _damageService;
        private static CommandValidationService _validationService;
        private static SnapshotExecutionService _snapshotExecutionService;
        private static readonly object _lock = new object();

        public static ISpellSnapshotService SnapshotService
        {
            get
            {
                if (_snapshotService == null)
                {
                    lock (_lock)
                    {
                        if (_snapshotService == null)
                        {
                            var logger = ServiceLocator.GetService<ILogger>();
                            _snapshotService = new SpellSnapshotService(logger);
                        }
                    }
                }
                return _snapshotService;
            }
        }

        public static ISpellStateService StateService
        {
            get
            {
                if (_stateService == null)
                {
                    lock (_lock)
                    {
                        if (_stateService == null)
                        {
                            var logger = ServiceLocator.GetService<ILogger>();
                            _stateService = new SpellStateService(logger);
                        }
                    }
                }
                return _stateService;
            }
        }

        public static IDamageCalculationService DamageService
        {
            get
            {
                if (_damageService == null)
                {
                    lock (_lock)
                    {
                        if (_damageService == null)
                        {
                            var logger = ServiceLocator.GetService<ILogger>();
                            _damageService = new DamageCalculationService(logger);
                        }
                    }
                }
                return _damageService;
            }
        }

        public static CommandValidationService ValidationService
        {
            get
            {
                if (_validationService == null)
                {
                    lock (_lock)
                    {
                        if (_validationService == null)
                        {
                            var logger = ServiceLocator.GetService<ILogger>();
                            _validationService = new CommandValidationService(logger);
                        }
                    }
                }
                return _validationService;
            }
        }

        public static SnapshotExecutionService SnapshotExecutionService
        {
            get
            {
                if (_snapshotExecutionService == null)
                {
                    lock (_lock)
                    {
                        if (_snapshotExecutionService == null)
                        {
                            var logger = ServiceLocator.GetService<ILogger>();
                            _snapshotExecutionService = new SnapshotExecutionService(logger, ValidationService);
                        }
                    }
                }
                return _snapshotExecutionService;
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _snapshotService = null;
                _stateService = null;
                _damageService = null;
                _validationService = null;
                _snapshotExecutionService = null;
            }
        }
    }
}