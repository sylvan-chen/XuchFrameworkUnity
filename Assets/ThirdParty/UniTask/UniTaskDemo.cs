// ReSharper disable Unity.LoadSceneUnexistingScene
// ReSharper disable Unity.UnknownResource
// ReSharper disable UnusedVariable

#pragma warning disable CS0168

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers; // Extension awaiter/methods can be used by this namespace
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThirdParty.UniTaskDemo
{
    public class UniTaskDemo : MonoBehaviour
    {
        // You can return the type as struct UniTask<T>(or UniTask), it is a unity specialized lightweight alternative of Task<T>
        // Zero allocation and fast execution for zero overhead async/await integrate with Unity

        #region Unity AsyncOperation

        public async UniTask<string> UnityAsyncOperation()
        {
            // Direct await
            var asset = await Resources.LoadAsync<TextAsset>("foo");
            var txt = (await UnityWebRequest.Get("https://...").SendWebRequest()).downloadHandler.text;

            // .WithCancellation enables Cancel, GetCancellationTokenOnDestroy synchronizes with the lifetime of GameObject
            // after Unity 2022.2. You can use `destroyCancellationToken` in MonoBehaviour
            var asset2 = await Resources.LoadAsync<TextAsset>("bar").WithCancellation(this.GetCancellationTokenOnDestroy());

            // .ToUniTask accepts progress callback(and all options), Progress.Create is a lightweight alternative of IProgress<T>
            var asset3 = await Resources.LoadAsync<TextAsset>("baz").ToUniTask(Progress.Create<float>(x => Debug.Log(x)));

            // LoadSceneAsync is a SPECIAL CASE, don`t use .ToUniTask or .WithCancellation. It will change the execution order of `Start method` and `code after await`
            // See https://github.com/Cysharp/UniTask/blob/master/README.md#playerloop
            await SceneManager.LoadSceneAsync("scene2");

            // Return async-value.(or you can use `UniTask`(no result), `UniTaskVoid`(fire and forget)).
            return (asset as TextAsset)?.text ?? throw new InvalidOperationException("Asset not found");
        }

        #endregion

        #region Unity Coroutine

        private bool isActive = true;

        public async UniTask UnityCoroutine()
        {
            // Await frame-based operation like a coroutine
            await UniTask.DelayFrame(100);

            // Replacement of yield return new WaitForSeconds/WaitForSecondsRealtime
            await UniTask.Delay(TimeSpan.FromSeconds(10), ignoreTimeScale: false);

            // Yield any playerloop timing(PreUpdate, Update, LateUpdate, etc...)
            await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);

            // Replacement of `yield return null`
            // .Yield may execute in the same frame if you call it before Update, while .NextFrame always execute in next frame (which is closer to yield return null)
            await UniTask.Yield();
            await UniTask.NextFrame();

            // Replacement of `yield return new WaitForEndOfFrame`
            // NOTE: `UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate)` is NOT equivalent to WaitForEndOfFrame
            // PlayerLoopTiming.LastPostLateUpdate cannot represent the real end of frame. That's the reason why it requires a MonoBehaviour as a parameter before Unity 2023.1
            // In Unity 2023.1, it provides UnityEngine.Awaitable.EndOfFrameAsync
#if UNITY_2023_1_OR_NEWER
            await UniTask.WaitForEndOfFrame();
#else
            // requires MonoBehaviour(CoroutineRunner))
            await UniTask.WaitForEndOfFrame(this); // this is MonoBehaviour
#endif

            // Replacement of `yield return new WaitForFixedUpdate`(same as UniTask.Yield(PlayerLoopTiming.FixedUpdate))
            await UniTask.WaitForFixedUpdate();

            // Replacement of yield return WaitUntil
            await UniTask.WaitUntil(() => isActive == false);
            // Special helper of WaitUntil
            await UniTask.WaitUntilValueChanged(this, x => x.isActive);

            // You can await IEnumerator coroutines
            await FooCoroutineEnumerator();

            // You can await a standard task
            await Task.Run(() => 100);
            await Task.Run(() => 100).AsUniTask(); // More recommendation
        }

        private IEnumerator FooCoroutineEnumerator()
        {
            yield return null;
        }

        #endregion

        #region Convert Callback to UniTask

        // You can use `UniTaskCompletionSource` to wrap callback
        public UniTask<bool> WrapByUniTaskCompletionSource()
        {
            var utcs = new UniTaskCompletionSource<bool>();

            // When complete, call utcs.TrySetResult();
            // When failed, call utcs.TrySetException();
            // When cancel, call utcs.TrySetCanceled();
            CallBackFunc(() =>
            {
                // ... do something
                utcs.TrySetResult(true);
            });

            return utcs.Task; // Return UniTask<bool>

            void CallBackFunc(Action callback)
            {
                callback?.Invoke();
            }
        }

        #endregion

        #region Cancellation & Exception Handling

        public Button cancelButton;

        public async UniTask Cancellation()
        {
            // Standard CancellationTokenSource
            var cts = new CancellationTokenSource();
            cancelButton.onClick.AddListener(() => cts.Cancel());

            await UnityWebRequest.Get("http://google.co.jp").SendWebRequest().WithCancellation(cts.Token);

            await UniTask.DelayFrame(1000, cancellationToken: cts.Token);

            // CancellationToken can be created by MonoBehaviour's extension method GetCancellationTokenOnDestroy
            // `GetCancellationTokenOnDestroy` makes the CancellationToken lifecycle be the same as GameObject
            await UniTask.DelayFrame(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            // For propagate Cancellation, all async methods recommend accepting CancellationToken at last argument, and pass CancellationToken from root to end
            await FooAsync(this.GetCancellationTokenOnDestroy());

            // If there is no method catching the exception, it will be thrown to the caller, finally into `UniTaskSchedular.UnobservedTaskException`
            // Cancellation will throw OperationCanceledException, which is ignored by default
            // When you want to handle the exception, you can ignore OperationCanceledException by using the `when` clause
            try
            {
                await FooAsync(this.GetCancellationTokenOnDestroy());
            }
            catch (Exception ex) when (ex is not OperationCanceledException) // when (ex is not OperationCanceledException) at C# 9.0
            {
                // ... handle exception
            }

            // Throw and catch OperationCanceledException is slightly heavy. Consider using `UniTask.SuppressCancellationThrow` to suppress a cancellation exception, it instead returns (bool isCanceled, T result)
            var (isCanceled, _) = await ValueAsync(cts.Token).SuppressCancellationThrow();
            if (isCanceled)
            {
                // ...
            }

            // Some features depend on PlayerLoop will check CancellationToken until specific PlayerLoopTiming
            // If you want to cancel a task immediately, you can pass `cancelImmediately: true`
            // NOTE: `cancelImmediately: true` is not recommended in most cases, it costs higher performance
            await UniTask.Yield(cts.Token, cancelImmediately: true);
        }

        private async UniTask FooAsync(CancellationToken cancellationToken)
        {
            await BarAsync(cancellationToken);
        }

        private async UniTask BarAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);
        }

        private async UniTask<int> ValueAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);
            return 10;
        }

        #endregion

        #region Timeout Handling

        public async UniTask Timeout()
        {
            // Timeout is a variation of Cancellation. You can set timeout by `CancellationTokenSource.CancelAfterSlim(TimeSpan)`
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(5)); // 5sec timeout.
            try
            {
                await UnityWebRequest.Get("http://foo").SendWebRequest().WithCancellation(cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cts.Token)
                {
                    Debug.Log("Timeout");
                }
            }

            // NOTE: Although it is possible to use `.Timeout` or `.TimeoutWithoutException`, but it's NOT recommended
            // `.Timeout` and `.TimeoutWithoutException` works outside the task, it just ignored the task but not really kill the task
            await UnityWebRequest.Get("http://foo").SendWebRequest().ToUniTask().Timeout(TimeSpan.FromSeconds(5)); // NOT RECOMMENDED!
        }

        public async UniTask LinkMultiCancellationTokenSources()
        {
            // If you want to use another CancellationTokenSource to handle timeout, you can use `CancellationTokenSource.CreateLinkedTokenSource` to link two CancellationTokenSources
            var cancelToken = new CancellationTokenSource();
            cancelButton.onClick.AddListener(() =>
            {
                cancelToken.Cancel(); // cancel from button click.
            });

            var timeoutToken = new CancellationTokenSource();
            timeoutToken.CancelAfterSlim(TimeSpan.FromSeconds(5)); // 5sec timeout.

            try
            {
                // Combine token
                var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken.Token, timeoutToken.Token);

                await UnityWebRequest.Get("http://foo").SendWebRequest().WithCancellation(linkedTokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                if (timeoutToken.IsCancellationRequested)
                {
                    Debug.Log("Timeout.");
                }
                else if (cancelToken.IsCancellationRequested)
                {
                    Debug.Log("Cancel clicked.");
                }
            }
        }

        // Optimize for reduce allocation of CancellationTokenSource for timeout per call async method, you can use UniTask's TimeoutController
        private TimeoutController timeoutController = new TimeoutController(); // setup to field for reuse.

        public async UniTask TimeoutController()
        {
            try
            {
                // you can pass timeoutController.Timeout(TimeSpan) to cancellationToken
                await UnityWebRequest.Get("http://foo").SendWebRequest().WithCancellation(timeoutController.Timeout(TimeSpan.FromSeconds(5)));
                timeoutController.Reset(); // call Reset (Stop timeout timer and ready for reuse) when succeeded.
            }
            catch (OperationCanceledException ex)
            {
                if (timeoutController.IsTimeout())
                {
                    Debug.Log("timeout");
                }
            }

            // It also can be linked to other CancellationTokenSource
            var clickCancelSource = new CancellationTokenSource();
            timeoutController = new TimeoutController(clickCancelSource);
        }

        #endregion

        #region Multi-Threading

        public async UniTask MultiThreading()
        {
            // Way 1: Use UniTask.RunOnThreadPool (UniTask.Run if in older versions)

            await UniTask.RunOnThreadPool(() => Debug.Log("Hello from ThreadPool!"));
            // await UniTask.Run(() => Debug.Log("Hello from ThreadPool!")); // UniTask.Run is deprecated.

            // Way 2: Use UniTask.SwitchToMainThread

            // Multithreading, run on ThreadPool under this code
            await UniTask.SwitchToThreadPool();

            /* work on ThreadPool */

            // return to MainThread(same as `ObserveOnMainThread` in UniRx)
            await UniTask.SwitchToMainThread();
        }

        #endregion

        #region Async Stream

        // Stream is a line for persistant producing. Such as IEnumerable<T> in Unity, we can consume data asynchronously with `foreach`
        // Async stream is a way to consume data asynchronously like `async foreach` in C# 8.0. AsyncEnumerable is a pull-based asynchronous stream
        // UniTask provides UniTaskAsyncEnumerable, which can be used like AsyncEnumerable, and can access Unity lifecycle and events asynchronously like a list

        private Button button;
        private Text textComponent;

        public async UniTask AsyncStream_LINQ(CancellationToken token)
        {
            /* 1. Presets */

            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate().WithCancellation(token))
            {
                // Do something in every Update
            }

            await foreach (var _ in UniTaskAsyncEnumerable.Interval(TimeSpan.FromSeconds(3)).WithCancellation(token))
            {
                // Do something in every 3 seconds
            }

            await foreach (var _ in UniTaskAsyncEnumerable.IntervalFrame(180).WithCancellation(token))
            {
                // Do something in every 180 frames
            }

            await foreach (var _ in UniTaskAsyncEnumerable.Timer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10)).WithCancellation(token))
            {
                // Do something after 3 seconds first, and repeat every 10 seconds
            }

            await foreach (var _ in UniTaskAsyncEnumerable.TimerFrame(180, 600).WithCancellation(token))
            {
                // Do something after 180 frames first and repeat every 600 frames
            }

            await foreach (var _ in UniTaskAsyncEnumerable.EveryValueChanged(this, x => x.isActive).WithCancellation(token))
            {
                // Do something when target value(`this.isActive`) changed
                Debug.Log($"Active changed: {this.isActive}");
            }

            /* 2. Events for UGUI */

            // Single await - Use `GetAsync***EventHandler`, every await will hang up until the next triggered event
            using (var handler = button.GetAsyncClickEventHandler(token))
            {
                await handler.OnClickAsync();
                await handler.OnClickAsync();
                await handler.OnClickAsync();
                Debug.Log("Three times clicked");
            }

            // Continuous listening - Use `***AsAsyncEnumerable` with `foreach` 
            await button.OnClickAsAsyncEnumerable().Take(3).LastAsync(token);
            Debug.Log("Three times clicked");

            await button.OnClickAsAsyncEnumerable().Take(3).ForEachAsync(_ => { Debug.Log("Every clicked"); }, cancellationToken: token);
            Debug.Log("Three times clicked, complete.");

            /* 3. Events for MonoBehaviour */

            // Use `GetAsync***Trigger` to get trigger

            // Single await
            var trigger = this.GetAsyncCollisionEnterTrigger();
            await trigger.OnCollisionEnterAsync(token);
            await trigger.OnCollisionEnterAsync(token);
            await trigger.OnCollisionEnterAsync(token);

            // Continuous listening
            await foreach (var collision in this.GetAsyncCollisionEnterTrigger().Take(3).WithCancellation(token))
            {
                Debug.Log($"Collision: {collision.gameObject.name}");
            }

            /* 4. Reactive Property */

            var rp = new AsyncReactiveProperty<int>(99);

            // AsyncReactiveProperty itself is IUniTaskAsyncEnumerable, you can query by LINQ
            rp.ForEachAsync(x => { Debug.Log(x); }, token).Forget();

            rp.Value = 10; // push 10 to all subscribers
            rp.Value = 11; // push 11 to all subscribers

            // WithoutCurrent ignore initial value
            // `BindTo` bind stream value to unity components.
            rp.WithoutCurrent().BindTo(this.textComponent, token);

            // Single wait until the next value set
            await rp.WaitAsync(token);

            // Also exists ToReadOnlyAsyncReactiveProperty to ensure subscribers can't edit the value
            var rp2 = new AsyncReactiveProperty<int>(99);
            var readOnlyRp2 = rp.CombineLatest(rp2, (x, y) => (x, y)).ToReadOnlyAsyncReactiveProperty(CancellationToken.None);

            /* 5. Backpressure process */

            // Signal miss - if you await 3 seconds at the first signal, the continuous signals in the next 3 seconds will be ignored
            // Cannot get click event during 3 seconds complete
            await button.OnClickAsAsyncEnumerable().ForEachAwaitAsync(
                async x => { await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: token); },
                cancellationToken: token);

            // Queue - if you don't want to lose any signal, use `.Queue`. The continuous signals will be queued until last complete
            await button.OnClickAsAsyncEnumerable().Queue().ForEachAwaitAsync(
                async x => { await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: token); },
                cancellationToken: token);

            // Fire and Forget - also not lose any signal, use `.FireAndForget`. The continuous signals will be triggered instant independently
            button.OnClickAsAsyncEnumerable().Subscribe(async x => { await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: token); });

            /* 6. Custom UniTaskEnumerable */
            await MyEveryUpdate().ForEachAsync(frameCount => Debug.Log($"Frame: {frameCount}"), cancellationToken: token);

            IUniTaskAsyncEnumerable<int> MyEveryUpdate()
            {
                // writer(IAsyncWriter<T>) has `YieldAsync(value)` method.
                return UniTaskAsyncEnumerable.Create<int>(async (writer, ct) =>
                {
                    var frameCount = 0;
                    await UniTask.Yield();
                    while (!ct.IsCancellationRequested)
                    {
                        await writer.YieldAsync(frameCount++); // instead of `yield return`
                        await UniTask.Yield();
                    }
                });
            }
        }

        public async UniTask AsyncStream_WhenAll_Any_Each()
        {
            var task1 = GetTextAsync(UnityWebRequest.Get("http://google.com"));
            var task2 = GetTextAsync(UnityWebRequest.Get("http://bing.com"));
            var task3 = GetTextAsync(UnityWebRequest.Get("http://yahoo.com"));

            // concurrent async-wait and get results easily by tuple syntax
            var (google, bing, yahoo) = await UniTask.WhenAll(task1, task2, task3);

            // shorthand of WhenAll, tuple can await directly
            var (google2, bing2, yahoo2) = await (task1, task2, task3);

            // WhenAny returns the first completed task, `winArgumentIndex` is the index of the first completed task
            var (winArgumentIndex, google3, bing3, yahoo3) = await UniTask.WhenAny(task1, task2, task3);

            // WhenEach returns a UniTaskAsyncEnumerable that can be consumed with foreach
            await UniTask.WhenEach(task1, task2, task3).ForEachAsync(x => Debug.Log(x), this.GetCancellationTokenOnDestroy());

            async UniTask<string> GetTextAsync(UnityWebRequest req)
            {
                var op = await req.SendWebRequest();
                return op.downloadHandler.text;
            }
        }

        #endregion
    }
}