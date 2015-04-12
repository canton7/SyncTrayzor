using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class PortableUpdateVariantHandler : IUpdateVariantHandler
    {
        public string VariantName { get { return "portable"; } }

        public bool CanAutoInstall { get { return false; } }

        public Task<bool> TryHandleUpdateAvailableAsync(VersionCheckResults checkResult)
        {
            return Task.FromResult(true);
        }

        public void AutoInstall()
        {
            throw new InvalidOperationException();
        }
    }
}
