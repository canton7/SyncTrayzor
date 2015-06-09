using FluentValidation;
using SyncTrayzor.Properties.Strings;
using SyncTrayzor.Services.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
