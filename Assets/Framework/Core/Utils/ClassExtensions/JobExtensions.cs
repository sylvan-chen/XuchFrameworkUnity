using Cysharp.Threading.Tasks;
using Unity.Jobs;

namespace XuchFramework.Extensions
{
    public static class JobExtensions
    {
        /// <summary>
        /// 异步等待 Job 完成
        /// </summary>
        public static UniTask CompleteAsync(this JobHandle handle)
        {
            var tcs = new UniTaskCompletionSource();

            JobHandle.ScheduleBatchedJobs(); // 通知 JobSystem 尽快启动已排队任务

            UniTask.Void(async () =>
            {
                while (!handle.IsCompleted)
                {
                    await UniTask.Yield();
                }

                handle.Complete();
                tcs.TrySetResult();
            });

            return tcs.Task;
        }
    }
}