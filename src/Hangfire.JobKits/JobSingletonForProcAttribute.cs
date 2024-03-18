using System;
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.JobKits.Worker;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.JobKits
{
    [PublicAPI]

    public class JobSingletonForProcAttribute : JobFilterAttribute, IServerFilter
    {
        public bool IsSingleton { get; set; } = true;

        public JobSingletonForProcAttribute(bool isSingleton = true)
        {
            IsSingleton = IsSingleton;
        }

        /// <summary>
        /// ����������
        /// </summary>
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        /// <summary>
        /// check Job is processing
        /// </summary>
        /// <param name="actionName">full action name</param>
        /// <returns></returns>
        /// <remarks>2024.03.14 �վ㬰Server����ɧP�_</remarks>
        private static bool IsRunning(string currentId, IMonitoringApi api, string actionName)
        {
            var processingCount = Convert.ToInt32(api.ProcessingCount());
            var processingJobs = api.ProcessingJobs(0, processingCount);

            return processingJobs.Any(job => job.Key != currentId && job.Value?.Job?.Method?.GetFullActionName() == actionName);
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            var currentJob = filterContext.BackgroundJob.Job;
            string actionName = currentJob.Method.GetFullActionName();

            var currentId = filterContext.BackgroundJob.Id;

            if (IsSingleton && IsRunning(currentId, filterContext.Storage.GetMonitoringApi(), actionName))
            {
                filterContext.Canceled = true;
                string exMsg = $"�u�@:[{actionName}]\r\n�Ѽ�:[{string.Join(",", currentJob.Args?.ToArray())}]\r\n���ư���";
                Logger.Warn(exMsg);
                throw new Exception(exMsg);
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
        }
    }
}