using Cysharp.Threading.Tasks;
using Unity.Jobs;

namespace XuchFramework.Extensions
{
    public static class JobExtensions
    {
        public static UniTask CompleteAsync(this JobHandle handle)
        {
            var tcs = new UniTaskCompletionSource();

            JobHandle.ScheduleBatchedJobs(); // Notify the job system to start processing jobs sooner

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