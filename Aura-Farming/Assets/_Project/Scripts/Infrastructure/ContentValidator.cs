using System;
using System.Collections.Generic;

namespace AuraFarming.Infrastructure
{
    public enum ContentValidationError
    {
        InvalidConfig,
        InvalidSignalCount,
        MissingSignal,
        UndefinedSignalId,
        DuplicateSignalId,
        UndefinedSignalName,
        UndefinedSignalValue,
        InvalidEventCount,
        MissingEvent,
        UndefinedEventId,
        DuplicateEventId,
        UndefinedEventName,
        UndefinedEventDescription,
        UndefinedEventEffect
    }

    public sealed class ContentValidationResult
    {
        public ContentValidationResult(IReadOnlyList<ContentValidationError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            var copy = new ContentValidationError[errors.Count];
            for (var index = 0; index < errors.Count; index++)
            {
                copy[index] = errors[index];
            }

            Errors = Array.AsReadOnly(copy);
        }

        public bool IsValid => Errors.Count == 0;
        public IReadOnlyList<ContentValidationError> Errors { get; }
    }

    public static class ContentValidator
    {
        private const int RequiredSignalCount = 9;
        private const int RequiredEventCount = 6;

        public static ContentValidationResult Validate(MatchConfig config)
        {
            var errors = new List<ContentValidationError>();
            if (config == null)
            {
                errors.Add(ContentValidationError.InvalidConfig);
                return new ContentValidationResult(errors);
            }

            ValidateSignals(config.Signals, errors);
            ValidateEvents(config.Events, errors);
            return new ContentValidationResult(errors);
        }

        private static void ValidateSignals(
            IReadOnlyList<SignalDefinition> signals,
            ICollection<ContentValidationError> errors)
        {
            if (signals.Count != RequiredSignalCount)
            {
                AddOnce(errors, ContentValidationError.InvalidSignalCount);
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < signals.Count; index++)
            {
                var signal = signals[index];
                if (signal == null)
                {
                    AddOnce(errors, ContentValidationError.MissingSignal);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(signal.Id))
                {
                    AddOnce(errors, ContentValidationError.UndefinedSignalId);
                }
                else if (!ids.Add(signal.Id))
                {
                    AddOnce(errors, ContentValidationError.DuplicateSignalId);
                }

                if (string.IsNullOrWhiteSpace(signal.DisplayName))
                {
                    AddOnce(errors, ContentValidationError.UndefinedSignalName);
                }

                if (signal.Value <= 0)
                {
                    AddOnce(errors, ContentValidationError.UndefinedSignalValue);
                }
            }
        }

        private static void ValidateEvents(
            IReadOnlyList<EventDefinition> events,
            ICollection<ContentValidationError> errors)
        {
            if (events.Count != RequiredEventCount)
            {
                AddOnce(errors, ContentValidationError.InvalidEventCount);
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < events.Count; index++)
            {
                var definition = events[index];
                if (definition == null)
                {
                    AddOnce(errors, ContentValidationError.MissingEvent);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    AddOnce(errors, ContentValidationError.UndefinedEventId);
                }
                else if (!ids.Add(definition.Id))
                {
                    AddOnce(errors, ContentValidationError.DuplicateEventId);
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    AddOnce(errors, ContentValidationError.UndefinedEventName);
                }

                if (string.IsNullOrWhiteSpace(definition.Description))
                {
                    AddOnce(errors, ContentValidationError.UndefinedEventDescription);
                }

                if (string.IsNullOrWhiteSpace(definition.EffectKey))
                {
                    AddOnce(errors, ContentValidationError.UndefinedEventEffect);
                }
            }
        }

        private static void AddOnce(
            ICollection<ContentValidationError> errors,
            ContentValidationError error)
        {
            if (!errors.Contains(error))
            {
                errors.Add(error);
            }
        }
    }
}
