using System;
using System.Collections.Generic;
using System.Threading;

namespace D365.Framework.Tools.DataMigration.Helper
{
    public static class RetryHelper
    {
        public static void Do(Action<int> action, TimeSpan retryInterval, int maxAttemptCount = 5, bool increaseWithAttempts = true)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep((int)retryInterval.TotalMilliseconds * (increaseWithAttempts ? attempted : 1)); //Mit jedem Versuch länger warten
                    }
                    action(attempted + 1);
                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }

        public static T Do<T>(Func<int, T> action, TimeSpan retryInterval, int maxAttemptCount = 5, bool increaseWithAttempts = true)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep((int)retryInterval.TotalMilliseconds * (increaseWithAttempts ? attempted : 1)); //Mit jedem Versuch länger warten
                    }
                    return action(attempted + 1);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}