﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class ContextTests
    {
        private readonly ITestOutputHelper Log;

        public ContextTests(ITestOutputHelper log) => Log = log;

        [Fact]
        public async Task SyncContextRespected_Task()
        {
            using (var ctx = MySyncContext.Create(Log))
            {
                Assert.Equal(0, ctx.PostCount);
                Assert.Equal(0, ctx.SendCount);
                await Impl();
                async Task Impl()
                {
                    await Task.Yield();
                    Assert.Same(ctx, SynchronizationContext.Current);
                }
                Assert.True(ctx.PostCount >= 0 && ctx.PostCount <= 2);
                Assert.Equal(0, ctx.SendCount);
            }
            await Task.Yield();
        }

        [Fact]
        public async Task SyncContextRespected_ValueTask()
        {
            using (var ctx = MySyncContext.Create(Log))
            {
                Assert.Equal(0, ctx.PostCount);
                Assert.Equal(0, ctx.SendCount);
                await Impl();
                async ValueTask Impl()
                {
                    await Task.Yield();
                    Assert.Same(ctx, SynchronizationContext.Current);
                }
                Assert.True(ctx.PostCount >= 0 && ctx.PostCount <= 2);
                Assert.Equal(0, ctx.SendCount);
            }
            await Task.Yield();
        }

        [Fact]
        public async Task SyncContextRespected_PooledTask()
        {
            using (var ctx = MySyncContext.Create(Log))
            {
                Assert.Equal(0, ctx.PostCount);
                Assert.Equal(0, ctx.SendCount);
                await Impl();
                async PooledTask Impl()
                {
                    await Task.Yield();
                    Assert.Same(ctx, SynchronizationContext.Current);
                }
                Assert.True(ctx.PostCount >= 0 && ctx.PostCount <= 2);
                Assert.Equal(0, ctx.SendCount);
            }
            await Task.Yield();
        }

        [Fact]
        public async Task SyncContextRespected_PooledValueTask()
        {
            using (var ctx = MySyncContext.Create(Log))
            {
                Assert.Equal(0, ctx.PostCount);
                Assert.Equal(0, ctx.SendCount);
                await Impl();
                async PooledValueTask Impl()
                {
                    await Task.Yield();
                    Assert.Same(ctx, SynchronizationContext.Current);
                }
                Assert.True(ctx.PostCount >= 0 && ctx.PostCount <= 2);
                Assert.Equal(0, ctx.SendCount);
            }
            await Task.Yield();
        }

        [Fact]
        public async Task TaskSchedulerRespected_Task()
        {
            using (var outer = MyTaskScheduler.Create(Log))
            {
                var ctx = outer;
                Assert.Equal(0, outer.Enqueued);
                Assert.Equal(0, outer.Dequeued);

                var winner = await Task.Factory.StartNew(() => Task.WaitAny(Impl(), Task.Delay(2000)), default, default, ctx);
                Assert.Equal(0, winner);
                Assert.True(outer.Enqueued >= 0 && outer.Enqueued <= 2);
                Assert.Equal(outer.Enqueued, outer.Dequeued);

                async Task Impl()
                {
                    Log?.WriteLine("before yield");
                    await Task.Yield();
                    Log?.WriteLine("after yield");
                    Assert.Same(ctx, TaskScheduler.Current);
                    Log?.WriteLine("after test");
                }
            }
            await Task.Yield();
        }

        sealed class MySyncContext : SynchronizationContext, IDisposable
        {
            int _postCount, _sendCount;
            private readonly ITestOutputHelper _log;

            public int PostCount => Volatile.Read(ref _postCount);
            public int SendCount => Volatile.Read(ref _sendCount);
            public void Reset()
            {
                Volatile.Write(ref _postCount, 0);
                Volatile.Write(ref _sendCount, 0);
            }

            class Box : IThreadPoolWorkItem, IResettable
            {
                private SynchronizationContext? _context;
                private SendOrPostCallback? _callback;
                private object? _state;

                private Box() { }
                public static Box Create(SynchronizationContext context, SendOrPostCallback callback, object state)
                {
                    var box = Pool.TryRent<Box>() ?? new Box();
                    box._context = context;
                    box._callback = callback;
                    box._state = state;
                    return box;
                }

                public void Execute()
                {
                    var old = Current;
                    SetSynchronizationContext(_context);
                    try
                    {
                        _callback?.Invoke(_state);
                    }
                    finally
                    {
                        SetSynchronizationContext(old);
                        Pool.Return(this);
                    }
                }

                public void Reset()
                {
                    _context = null;
                    _callback = null;
                    _state = null;
                }
            }
            public override void Post(SendOrPostCallback d, object state)
            {
                if (d == null) return;
                Interlocked.Increment(ref _postCount);
                Log(d, state);
                ThreadPool.UnsafeQueueUserWorkItem(Box.Create(this, d, state), true);
            }

            private void Log(SendOrPostCallback d, object state, [CallerMemberName] string? caller = null)
            {
                _log?.WriteLine($"[{caller}]: {d.Method.Name} on {d?.Target?.GetType().FullName} with {state?.GetType().FullName}");
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (d == null) return;
                Interlocked.Increment(ref _sendCount);
                Log(d, state);
                d(state);
            }
            private readonly SynchronizationContext _old;
            private MySyncContext(ITestOutputHelper log)
            {
                _log = log;
                _old = Current;
            }
            public void Dispose()
            {
                SetSynchronizationContext(_old);
            }

            public static MySyncContext Create(ITestOutputHelper log)
            {
                var ctx = new MySyncContext(log);
                SetSynchronizationContext(ctx);
                return ctx;
            }
        }

        sealed class MyTaskScheduler : TaskScheduler, IDisposable
        {
            private readonly ITestOutputHelper _log;
            readonly Thread _worker;
            readonly Queue<Task> _queue = new Queue<Task>();

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                lock (_queue) { return _queue.ToArray(); }
            }
            public static MyTaskScheduler Create(ITestOutputHelper log) => new MyTaskScheduler(log);
            private MyTaskScheduler(ITestOutputHelper log)
            {
                _log = log;
                _worker = new Thread(obj => ((MyTaskScheduler)obj).Run());
                _worker.Start(this);
            }
            private void Run()
            {
                _log?.WriteLine(nameof(Run));
                while (true)
                {
                    Task next;
                    lock (_queue)
                    {
                        if (_queue.Count == 0)
                        {
                            if (_disposed) break;
                            Monitor.Wait(_queue, 1000);
                            continue;
                        }
                        next = _queue.Dequeue();
                    }
                    _dequeued++;
                    _log?.WriteLine($"{nameof(TryExecuteTask)}: {next}");
                    var result = TryExecuteTask(next);
                    _log?.WriteLine($"result: {result}");
                }
                _log?.WriteLine("(exit worker)");
            }
            protected override void QueueTask(Task task)
            {
                lock (_queue)
                {
                    _log?.WriteLine(nameof(QueueTask));
                    _queue.Enqueue(task);
                    if (_queue.Count == 1) Monitor.Pulse(_queue);
                    _enqueued++;
                }
            }
            protected override bool TryDequeue(Task task)
            {
                lock (_queue)
                {
                    _log?.WriteLine(nameof(TryDequeue));
                    if (_queue.Count != 0 && _queue.Peek() == task)
                    {
                        _queue.Dequeue();
                        return true;
                    }
                }
                return false;
            }
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                _log?.WriteLine($"{nameof(TryExecuteTaskInline)}: {task}");
                var result = TryExecuteTask(task);
                _log?.WriteLine($"result: {result}");
                return result;
            }
            private volatile bool _disposed = false;
            public void Dispose()
            {
                _disposed = true;
                lock (_queue) { Monitor.PulseAll(_queue); }
                _log?.WriteLine(nameof(Dispose));
            }
            private volatile int _enqueued, _dequeued;
            public int Enqueued => _enqueued;
            public int Dequeued => _dequeued;
        }
    }
}