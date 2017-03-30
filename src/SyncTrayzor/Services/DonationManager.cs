using System;
using System.ComponentModel;
using Stylet;
using SyncTrayzor.Services.Config;

namespace SyncTrayzor.Services
{
    // Implementing INPC is hacky, but convenient
    public interface IDonationManager : INotifyPropertyChanged, IDisposable
    {
        bool HaveDonated { get; }

        void Donate();
    }

    public class DonationManager : PropertyChangedBase, IDonationManager
    {
        // Not in the app.config, in case some sysadmin wants to change it
        private const string donateUrl = "https://synctrayzor.antonymale.co.uk/donate";

        private readonly IConfigurationProvider configurationProvider;
        private readonly IProcessStartProvider processStartProvider;

        public bool HaveDonated { get; private set; }

        public DonationManager(IConfigurationProvider configurationProvider, IProcessStartProvider processStartProvider)
        {
            this.configurationProvider = configurationProvider;
            this.processStartProvider = processStartProvider;

            this.HaveDonated = this.configurationProvider.Load().HaveDonated;
            this.configurationProvider.ConfigurationChanged += this.ConfigurationChanged;
        }

        private void ConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            this.HaveDonated = e.NewConfiguration.HaveDonated;
        }

        public void Donate()
        {
            this.processStartProvider.StartDetached(donateUrl);
            this.configurationProvider.AtomicLoadAndSave(x => x.HaveDonated = true);
        }

        public void Dispose()
        {
            this.configurationProvider.ConfigurationChanged -= this.ConfigurationChanged;
        }
    }
}
