using Stylet;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Pages.Settings;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen, IDisposable
    {
        private const int maxLogMessages = 1500;

        private readonly IWindowManager windowManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly Buffer<string> logMessagesBuffer;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;

        public Queue<string> LogMessages { get;  }
        public bool LogPaused { get; set; }

        public ConsoleViewModel(
            IWindowManager windowManager,
            ISyncthingManager syncthingManager,
            IConfigurationProvider configurationProvider,
            Func<SettingsViewModel> settingsViewModelFactory)
        {
            this.windowManager = windowManager;
            this.syncthingManager = syncthingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.LogMessages = new Queue<string>();

            // Display log messages 100ms after the previous message, or every 500ms if they're arriving thick and fast
            this.logMessagesBuffer = new Buffer<string>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500));
            this.logMessagesBuffer.Delivered += this.LogMessageDelivered;

            this.syncthingManager.MessageLogged += this.SyncthingMessageLogged;
        }

        private void LogMessageDelivered(object sender, BufferDeliveredEventArgs<string> e)
        {
            foreach (var message in e.Items)
            {
                this.LogMessages.Enqueue(message);
                if (this.LogMessages.Count > maxLogMessages)
                    this.LogMessages.Dequeue();
            }

            if (!this.LogPaused)
                this.NotifyOfPropertyChange(nameof(this.LogMessages));
        }

        private void SyncthingMessageLogged(object sender, MessageLoggedEventArgs e)
        {
            this.logMessagesBuffer.Add(e.LogMessage);
        }

        public void ClearLog()
        {
            this.LogMessages.Clear();
            this.NotifyOfPropertyChange(nameof(this.LogMessages));
        }

        public void ShowSettings()
        {
            var vm = this.settingsViewModelFactory();
            vm.SelectLoggingTab();
            this.windowManager.ShowDialog(vm);
        }

        public void PauseLog()
        {
            this.LogPaused = true;
        }

        public void ResumeLog()
        {
            this.LogPaused = false;
            this.NotifyOfPropertyChange(nameof(this.LogMessages));
        }

        public void Dispose()
        {
            this.syncthingManager.MessageLogged -= this.SyncthingMessageLogged;
        }
    }
}
