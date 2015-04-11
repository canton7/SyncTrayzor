using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateVariantHandler
    {
        string VariantName { get; }
        bool CanAutoInstall { get; }

        Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult);
        void AutoInstall();
    }
}
