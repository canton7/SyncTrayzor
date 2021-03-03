using NLog;
using System;
using System.Management;

namespace SyncTrayzor.Services
{
    public class GraphicsCardDetector
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool? _isIntelXe;
        public bool IsIntelXe
        {
            get
            {
                if (this._isIntelXe == null)
                    this._isIntelXe = GetIsIntelXe();
                return this._isIntelXe.Value;
            }
        }

        private static bool GetIsIntelXe()
        { 
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentBitsPerPixel"] != null && obj["CurrentHorizontalResolution"] != null)
                {
                    string name = obj["Name"]?.ToString();
                    if (name.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        name.IndexOf(" Xe ", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        logger.Info($"Graphics card: {name}");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
