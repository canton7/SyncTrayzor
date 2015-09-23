using FluentValidation;
using SyncTrayzor.Properties;
using System.Linq;

namespace SyncTrayzor.Pages.Settings
{
    public class SyncThingApiKeyValidator : AbstractValidator<SettingItem<string>>
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

        public SyncThingApiKeyValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage(Resources.SettingsView_Validation_NotShouldBeEmpty);
            RuleFor(x => x.Value).Must(x => x.All(c => apiKeyChars.Contains(c))).WithMessage(Resources.SettingsView_Validation_ApiKeyInvalidChars);
        }
    }
}
