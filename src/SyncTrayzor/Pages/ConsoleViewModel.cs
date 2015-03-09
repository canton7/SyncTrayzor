using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SyncTrayzor.Services;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen
    {
        private const int maxLogMessages = 1500;

        private readonly ISyncThingManager syncThingManager;
        private readonly IConfigurationProvider configurationProvider;

        public ObservableQueue<string> LogMessages { get; private set; }

        public ConsoleViewModel(
            ISyncThingManager syncThingManager,
            IConfigurationProvider configurationProvider
            )
        {
            this.syncThingManager = syncThingManager;
            this.configurationProvider = configurationProvider;
            this.LogMessages = new ObservableQueue<string>();

            var configuration = configurationProvider.Load();

            configurationProvider.ConfigurationChanged += (o, e) =>
            {
                configuration = configurationProvider.Load();
            };

            this.syncThingManager.MessageLogged += (o, e) =>
            {
                var message = e.LogMessage;

                // Check if device IDs need to be obfuscated
                if (configuration.ObfuscateDeviceIDs)
                {
                    var rgx = new Regex(@"-[0-9A-Z]{7}-[0-9A-Z]{7}-[0-9A-Z]{7}-[0-9A-Z]{7}-[0-9A-Z]{7}-[0-9A-Z]{7}-[0-9A-Z]{7}");
                    message = rgx.Replace(message, "");
                }

                this.LogMessages.Enqueue(message);
                if (this.LogMessages.Count > maxLogMessages)
                    this.LogMessages.Dequeue();
            };
        }
    }
}
