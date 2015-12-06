using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Pages.Settings;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen
    {
        private const int maxLogMessages = 1500;

        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly Buffer<string> logMessagesBuffer;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;

        public Queue<string> LogMessages { get;  }
        public bool LogPaused { get; set; }

        public ConsoleViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            IConfigurationProvider configurationProvider,
            Func<SettingsViewModel> settingsViewModelFactory)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;
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

                if (!this.LogPaused)
                    this.NotifyOfPropertyChange(() => this.LogMessages);
            };

            this.syncThingManager.MessageLogged += (o, e) =>
            {
                this.logMessagesBuffer.Add(e.LogMessage);
            };

            this.Bind(s => s.LogPaused, (o, e) =>
            {
                if (!e.NewValue)
                    this.NotifyOfPropertyChange(() => this.LogMessages);
            });
        }

        public void ClearLog()
        {
            this.LogMessages.Clear();
            this.NotifyOfPropertyChange(() => this.LogMessages);
        }

        public void ShowSettings()
        {
            var vm = this.settingsViewModelFactory();
            vm.SelectLoggingTab();
            this.windowManager.ShowDialog(vm);
        }
    }
}
