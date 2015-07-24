using FluentValidation;
using SyncTrayzor.Properties.Strings;
using SyncTrayzor.Services.Config;

namespace SyncTrayzor.Pages.Settings
{
    public class SyncThingEnvironmentalVariablesValidator : AbstractValidator<SettingItem<string>>
    {
        public SyncThingEnvironmentalVariablesValidator()
        {
            RuleFor(x => x.Value).Must(str =>
            {
                EnvironmentalVariableCollection result;
                return EnvironmentalVariablesParser.TryParse(str, out result);
            }).WithMessage(Resources.SettingsView_Validation_SyncthingEnvironmentalVariablesMustHaveFormat);
        }
    }
}
