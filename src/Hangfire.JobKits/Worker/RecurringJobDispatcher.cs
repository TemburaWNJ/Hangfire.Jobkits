using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.States;

namespace Hangfire.JobKits.Worker
{
    internal sealed class RecurringJobDispatcher : IDashboardDispatcher
    {
        public StandbyMap Map { get; }

        private JobKitOptions Options { get; }

        public RecurringJobDispatcher(StandbyMap map, JobKitOptions options)
        {
            Map = map;
            Options = options;
        }

        public async Task Dispatch(DashboardContext context)
        {
            if (!"POST".Equals(context.Request.Method, StringComparison.InvariantCultureIgnoreCase))
            {
                context.Response.StatusCode = 405;
                return;
            }

            try
            {
                var key = context.Request.GetQuery(StandbyKey.IdField);

                var standbyJob = Map.JobCollection[key];

                var cron = (await context.Request.GetFormValuesAsync("recurring_cron")).LastOrDefault();
                var timeZone = Options.RecurringTimeZone ?? TimeZoneInfo.Local;
                var jobReccuringId = (await context.Request.GetFormValuesAsync("job_reccuring_id")).LastOrDefault();

                var parameters = await StandbyHelper.CreateParameters(context, standbyJob.Method);
                var queueString = string.Empty;

                if (standbyJob.UseQueue)
                {
                    queueString = (await context.Request.GetFormValuesAsync("enqueued_state")).LastOrDefault();
                }

                string jobId = string.IsNullOrEmpty(jobReccuringId) ? standbyJob.RecurringJobId : jobReccuringId;

                if (!string.IsNullOrEmpty(queueString))
                {
                    context.GetRecurringJobManager()
                        .AddOrUpdate(jobId, new Job(standbyJob.Method, parameters), cron, timeZone, queueString);
                }
                else
                {
                    context.GetRecurringJobManager().AddOrUpdate(jobId, new Job(standbyJob.Method, parameters), cron, timeZone);
                }

                context.Response.StatusCode = 200;
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(e.Message);
            }
        }
    }
}