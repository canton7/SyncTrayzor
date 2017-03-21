using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using SyncTrayzor.Localization;
using SyncTrayzor.Properties;

namespace SyncTrayzor.Pages.Settings
{
    public class SyncthingCommandLineFlagsValidator : AbstractValidator<SettingItem<string>>
    {
        // This dups stuff from SyncthingProcessRunner
        private static readonly string[] forbiddenArgs = new[] { "-no-browser", "-no-restart", "-gui-apikey", "-gui-address", "-home" };

        public SyncthingCommandLineFlagsValidator()
        {
            RuleFor(x => x.Value).Must(str =>
            {
                return KeyValueStringParser.TryParse(str, out var result, mustHaveValue: false);
            }).WithMessage(Resources.SettingsView_Validation_SyncthingCommandLineFlagsMustHaveFormat);

            RuleFor(x => x.Value).SetValidator(new IndividualFlagsValidator());
        }

        private class IndividualFlagsValidator : AbstractValidator<string>
        {
            public IndividualFlagsValidator()
            {
                Custom(str =>
                {
                    KeyValueStringParser.TryParse(str, out var result, mustHaveValue: false);

                    if (!result.All(flag => flag.Key.StartsWith("-")))
                        return new ValidationFailure(null, Resources.SettingsView_Validation_SyncthingCommandLineFlagsMustBeginWithHyphen);

                    var firstFailure = result.Select(flag => flag.Key).FirstOrDefault(key => forbiddenArgs.Contains(key));
                    if (firstFailure != null)
                        return new ValidationFailure(null, Localizer.F(Resources.SettingsView_Validation_SyncthingCommandLineFlagIsNotAllowed, firstFailure));
                    return null;
                });
            }
        }
    }
}
