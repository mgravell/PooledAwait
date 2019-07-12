using PooledAwait.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;
using Xunit.Abstractions;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class BasicTests
    {
        private readonly ITestOutputHelper Log;
        public BasicTests(ITestOutputHelper log) => Log = log;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CompletedTaskIdentity(bool isAsync)
        {
            Task pending = Impl();
            if (isAsync) Assert.NotSame(Task.CompletedTask, pending);
            else Assert.Same(Task.CompletedTask, pending);
            await pending;

            Log?.WriteLine(Counters.Summary());

            async PooledTask Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocTaskAsync(bool isAsync)
        {
            Task pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            for (int i = 0; i < 5; i++) await Impl();
            Counters.Reset();
            await Impl();
#if DEBUG
            Log?.WriteLine(Counters.Summary());
            Assert.Equal(isAsync ? 1 : 0, Counters.TaskAllocated.Value);
            Assert.Equal(isAsync ? 1 : 0, Counters.TotalAllocations);
            Assert.Equal(0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);
#endif

            async PooledTask Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocValueTaskAsync(bool isAsync)
        {
            ValueTask pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            for (int i = 0; i < 5; i++) await Impl();
            Counters.Reset();
            await Impl();
#if DEBUG
            Log?.WriteLine(Counters.Summary());
            Assert.Equal(isAsync ? 1 : 0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);
            Assert.Equal(0, Counters.StateMachineBoxAllocated.Value);
            Assert.Equal(0, Counters.TotalAllocations);
#endif

            async PooledValueTask Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocTaskTAsync(bool isAsync)
        {
            Task<int> pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            for (int i = 0; i < 5; i++) await Impl();
            Counters.Reset();
            Assert.Equal(42, await Impl());
#if DEBUG
            Log?.WriteLine(Counters.Summary());
            Assert.Equal(isAsync ? 1 : 0, Counters.TotalAllocations);
            Assert.Equal(isAsync ? 1 : 0, Counters.TaskAllocated.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);
#endif

            async PooledTask<int> Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                return 42;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ZeroAllocValueTaskTAsync(bool isAsync)
        {
            ValueTask<int> pending = Impl();
            Assert.Equal(!isAsync, pending.IsCompleted);
            await pending;
            for (int i = 0; i < 5; i++) await Impl();
            Counters.Reset();
            Assert.Equal(42, await Impl());
#if DEBUG
            Log?.WriteLine(Counters.Summary());
            Assert.Equal(0, Counters.TotalAllocations);
            Assert.Equal(isAsync ? 1 : 0, Counters.PooledStateRecycled.Value);
            Assert.Equal(isAsync ? 2 : 0, Counters.StateMachineBoxRecycled.Value);
#endif

            async PooledValueTask<int> Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                return 42;
            }
        }

        class TestException : Exception { }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task ValueTaskTOutcomesViaPooledBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async PooledValueTask<int> Impl()
            {

                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
                return 42;
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task ValueTaskTOutcomesViaDefaultBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async ValueTask<int> Impl()
            {

                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
                return 42;
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task ValueTaskTOutcomesViaSource(ValueTaskSourceStatus status, bool isAsync)
        {
            var source = PooledValueTaskSource<int>.Create();
            void Set()
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Succeeded: source.TrySetResult(42); break;
                    case ValueTaskSourceStatus.Faulted: source.TrySetException(new TestException()); break;
                    case ValueTaskSourceStatus.Canceled: source.TrySetCanceled(); break;
                }
            }
            async FireAndForget SetAsync()
            {
                await Task.Delay(50);
                Set();
            }
            if (isAsync) await SetAsync();
            else Set();
            await TestOutcome(status, isAsync, source.Task);
        }

        static async Task TestOutcome(ValueTaskSourceStatus status, bool isAsync, ValueTask<int> pending)
        {
            Assert.Equal(!isAsync, pending.IsCompleted);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Succeeded, pending.IsCompletedSuccessfully);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Canceled, pending.IsCanceled);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Faulted, pending.IsFaulted);

            try
            {
                Assert.Equal(42, await pending);
                Assert.Equal(ValueTaskSourceStatus.Succeeded, status);
            }
            catch (OperationCanceledException)
            {
                Assert.Equal(ValueTaskSourceStatus.Canceled, status);
            }
            catch (TestException)
            {
                Assert.Equal(ValueTaskSourceStatus.Faulted, status);
            }
        }


        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task ValueTaskOutcomesViaPooledBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async PooledValueTask Impl()
            {

                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task ValueTaskOutcomesViaDefaultBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async ValueTask Impl()
            {

                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task ValueTaskOutcomesViaSource(ValueTaskSourceStatus status, bool isAsync)
        {
            var source = PooledValueTaskSource.Create();
            void Set()
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Succeeded: source.TrySetResult(); break;
                    case ValueTaskSourceStatus.Faulted: source.TrySetException(new TestException()); break;
                    case ValueTaskSourceStatus.Canceled: source.TrySetCanceled(); break;
                }
            }
            async FireAndForget SetAsync()
            {
                await Task.Delay(50);
                Set();
            }
            if (isAsync) await SetAsync();
            else Set();
            await TestOutcome(status, isAsync, source.Task);
        }

        static async Task TestOutcome(ValueTaskSourceStatus status, bool isAsync, ValueTask pending)
        {
            Assert.Equal(!isAsync, pending.IsCompleted);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Succeeded, pending.IsCompletedSuccessfully);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Canceled, pending.IsCanceled);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Faulted, pending.IsFaulted);

            try
            {
                await pending;
                Assert.Equal(ValueTaskSourceStatus.Succeeded, status);
            }
            catch (OperationCanceledException)
            {
                Assert.Equal(ValueTaskSourceStatus.Canceled, status);
            }
            catch (TestException)
            {
                Assert.Equal(ValueTaskSourceStatus.Faulted, status);
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskTOutcomesViaPooledBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async PooledTask<int> Impl()
            {

                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
                return 42;
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskTOutcomesViaDefaultBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async Task<int> Impl()
            {

                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
                return 42;
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskTOutcomesViaCustomSource(ValueTaskSourceStatus status, bool isAsync)
        {
            var source = ValueTaskCompletionSource<int>.Create();
            void Set()
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Succeeded: source.TrySetResult(42); break;
                    case ValueTaskSourceStatus.Faulted: source.TrySetException(new TestException()); break;
                    case ValueTaskSourceStatus.Canceled: source.TrySetCanceled(); break;
                }
            }
            async FireAndForget SetAsync()
            {
                await Task.Delay(50);
                Set();
            }
            if (isAsync) await SetAsync();
            else Set();
            await TestOutcome(status, isAsync, source.Task);
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskTOutcomesViaDefaultSource(ValueTaskSourceStatus status, bool isAsync)
        {
            var source = new TaskCompletionSource<int>();
            void Set()
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Succeeded: source.TrySetResult(42); break;
                    case ValueTaskSourceStatus.Faulted: source.TrySetException(new TestException()); break;
                    case ValueTaskSourceStatus.Canceled: source.TrySetCanceled(); break;
                }
            }
            async FireAndForget SetAsync()
            {
                await Task.Delay(50);
                Set();
            }
            if (isAsync) await SetAsync();
            else Set();
            await TestOutcome(status, isAsync, source.Task);
        }

        async Task TestOutcome(ValueTaskSourceStatus status, bool isAsync, Task<int> pending)
        {
            Log?.WriteLine($"status: {pending.Status}");
            Assert.Equal(!isAsync, pending.IsCompleted);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Succeeded, pending.Status == TaskStatus.RanToCompletion);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Canceled, pending.IsCanceled);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Faulted, pending.IsFaulted);

            try
            {
                Assert.Equal(42, await pending);
                Assert.Equal(ValueTaskSourceStatus.Succeeded, status);
            }
            catch (OperationCanceledException)
            {
                Assert.Equal(ValueTaskSourceStatus.Canceled, status);
            }
            catch (TestException)
            {
                Assert.Equal(ValueTaskSourceStatus.Faulted, status);
            }
        }


        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskOutcomesViaPooledBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async PooledTask Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskOutcomesViaDefaultBuilder(ValueTaskSourceStatus status, bool isAsync)
        {
            using var source = new CancellationTokenSource();
            if (status == ValueTaskSourceStatus.Canceled) source.Cancel();

            await TestOutcome(status, isAsync, Impl());

            async Task Impl()
            {
                if (isAsync)
                {
                    await Task.Delay(50);
                    await Task.Yield();
                }
                source.Token.ThrowIfCancellationRequested();
                if (status == ValueTaskSourceStatus.Faulted) throw new TestException();
            }
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskOutcomesViaCustomSource(ValueTaskSourceStatus status, bool isAsync)
        {
            var source = ValueTaskCompletionSource<Nothing>.Create();
            void Set()
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Succeeded: source.TrySetResult(default); break;
                    case ValueTaskSourceStatus.Faulted: source.TrySetException(new TestException()); break;
                    case ValueTaskSourceStatus.Canceled: source.TrySetCanceled(); break;
                }
            }
            async FireAndForget SetAsync()
            {
                await Task.Delay(50);
                Set();
            }
            if (isAsync) await SetAsync();
            else Set();
            await TestOutcome(status, isAsync, source.Task);
        }

        [Theory]
        [InlineData(ValueTaskSourceStatus.Canceled, true)]
        [InlineData(ValueTaskSourceStatus.Canceled, false)]
        [InlineData(ValueTaskSourceStatus.Faulted, true)]
        [InlineData(ValueTaskSourceStatus.Faulted, false)]
        [InlineData(ValueTaskSourceStatus.Succeeded, true)]
        [InlineData(ValueTaskSourceStatus.Succeeded, false)]
        public async Task TaskOutcomesViaDefaultSource(ValueTaskSourceStatus status, bool isAsync)
        {
            var source = new TaskCompletionSource<Nothing>();
            void Set()
            {
                switch (status)
                {
                    case ValueTaskSourceStatus.Succeeded: source.TrySetResult(default); break;
                    case ValueTaskSourceStatus.Faulted: source.TrySetException(new TestException()); break;
                    case ValueTaskSourceStatus.Canceled: source.TrySetCanceled(); break;
                }
            }
            async FireAndForget SetAsync()
            {
                await Task.Delay(50);
                Set();
            }
            if (isAsync) await SetAsync();
            else Set();
            await TestOutcome(status, isAsync, source.Task);
        }

        async Task TestOutcome(ValueTaskSourceStatus status, bool isAsync, Task pending)
        {
            Log?.WriteLine($"status: {pending.Status}");
            Assert.Equal(!isAsync, pending.IsCompleted);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Succeeded, pending.Status == TaskStatus.RanToCompletion);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Canceled, pending.IsCanceled);
            Assert.Equal(!isAsync && status == ValueTaskSourceStatus.Faulted, pending.IsFaulted);

            try
            {
                await pending;
                Assert.Equal(ValueTaskSourceStatus.Succeeded, status);
            }
            catch (OperationCanceledException)
            {
                Assert.Equal(ValueTaskSourceStatus.Canceled, status);
            }
            catch (TestException)
            {
                Assert.Equal(ValueTaskSourceStatus.Faulted, status);
            }
        }
    }
}
