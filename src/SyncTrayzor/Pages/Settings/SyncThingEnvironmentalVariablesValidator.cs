using System.Collections.Generic;
using FluentValidation;
using SyncTrayzor.Properties;

namespace SyncTrayzor.Pages.Settings
{
    public class SyncThingEnvironmentalVariablesValidator : AbstractValidator<SettingItem<string>>
    {
        public SyncThingEnvironmentalVariablesValidator()
        {
            RuleFor(x => x.Value).Must(str =>
            {
                IEnumerable<KeyValuePair<string, string>> result;
                return KeyValueStringParser.TryParse(str, out result);
            }).WithMessage(Resources.SettingsView_Validation_SyncthingEnvironmentalVariablesMustHaveFormat);
        }
    }
}
