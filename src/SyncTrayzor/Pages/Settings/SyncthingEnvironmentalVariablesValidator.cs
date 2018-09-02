using System.Linq;
using FluentValidation;
using SyncTrayzor.Properties;

namespace SyncTrayzor.Pages.Settings
{
    public class SyncthingEnvironmentalVariablesValidator : AbstractValidator<SettingItem<string>>
    {
        public SyncthingEnvironmentalVariablesValidator()
        {
            RuleFor(x => x.Value).Must(str =>
            {
                return KeyValueStringParser.TryParse(str, out var result);
            }).WithMessage(Resources.SettingsView_Validation_SyncthingEnvironmentalVariablesMustHaveFormat);
        }
    }
}
