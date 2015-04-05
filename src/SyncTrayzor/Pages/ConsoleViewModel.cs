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
using SyncTrayzor.Services.Config;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen
    {
        private const int maxLogMessages = 1500;

        private readonly ISyncThingManager syncThingManager;
        private readonly Buffer<string> logMessagesBuffer;

        public Queue<string> LogMessages { get; private set; }

        public ConsoleViewModel(
            ISyncThingManager syncThingManager,
            IConfigurationProvider configurationProvider)
        {
            this.syncThingManager = syncThingManager;
            this.LogMessages = new Queue<string>();

            // Display log messages 100ms after the previous message, or every 500ms if they're arriving thick and fast
            this.logMessagesBuffer = new Buffer<string>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
            this.logMessagesBuffer.Delivered += (o, e) =>
            {
                foreach (var message in e.Items)
                {
                    this.LogMessages.Enqueue(message);
                    if (this.LogMessages.Count > maxLogMessages)
                        this.LogMessages.Dequeue();
                }

                this.NotifyOfPropertyChange(() => this.LogMessages);
            };

            this.syncThingManager.MessageLogged += (o, e) =>
            {
                this.logMessagesBuffer.Add(e.LogMessage);
            };
        }
    }
}
