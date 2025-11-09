using System;
using System.Threading;

namespace  D365.Framework.Tools.DataMigration.Helper
{
    public class SemaphoreUsage : IDisposable
    {
        private SemaphoreSlim Semaphore
        {
            get;
        }

        public SemaphoreUsage(SemaphoreSlim semaphore)
        {
            Semaphore = semaphore;
            Semaphore.Wait();
        }

        public void Dispose()
        {
            Semaphore.Release();
        }
    }
}