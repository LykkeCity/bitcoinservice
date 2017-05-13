using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;

namespace LkeServices.Helpers
{
    public class Retry
    {
        public static async Task<T> Try<T>(Func<Task<T>> action, Func<Exception, bool> exceptionFilter, int tryCount, ILog logger)
        {
            int @try = 0;
            while (true)
            {
                try
                {
                    return await action();                    
                }
                catch (Exception ex)
                {                    
                    @try++;
                    if (!exceptionFilter(ex) || @try >= tryCount)
                        throw;
                    await logger.WriteErrorAsync("Retry", "Try", null, ex);
                }
            }
        }

        public static async Task Try(Func<Task> action, Func<Exception, bool> exceptionFilter, int tryCount, ILog logger)
        {
            int @try = 0;
            while (true)
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    @try++;
                    if (!exceptionFilter(ex) || @try >= tryCount)
                        throw;
                    await logger.WriteErrorAsync("Retry", "Try", null, ex);
                }
            }
        }
    }
}
