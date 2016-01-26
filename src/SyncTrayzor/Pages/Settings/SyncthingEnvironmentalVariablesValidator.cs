using System.Collections.Generic;
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
                IEnumerable<KeyValuePair<string, string>> result;
                return KeyValueStringParser.TryParse(str, out result);
            }).WithMessage(Resources.SettingsView_Validation_SyncthingEnvironmentalVariablesMustHaveFormat);

            RuleFor(x => x.Value).Must(str =>
            {
                IEnumerable<KeyValuePair<string, string>> result;
                KeyValueStringParser.TryParse(str, out result);
                return !result.Any(x => x.Key == "STTRACE");
            }).WithMessage(Resources.SettingsView_Validation_SetSttraceInTab);
        }
    }
}
