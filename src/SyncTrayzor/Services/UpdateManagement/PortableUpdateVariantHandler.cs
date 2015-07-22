using System;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class PortableUpdateVariantHandler : IUpdateVariantHandler
    {
        public string VariantName => "portable";

        public bool CanAutoInstall => false;

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
