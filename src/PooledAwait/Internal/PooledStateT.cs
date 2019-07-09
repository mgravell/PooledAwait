using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace PooledAwait.Internal
{
    internal sealed class PooledState<T> : IValueTaskSource<T>, IValueTaskSource
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledState<T> Create(out short token)
        {
            var obj = Pool<PooledState<T>>.TryGet() ?? new PooledState<T>();
            token = obj._source.Version;
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PooledState() => Counters.PooledStateAllocated.Increment();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValid(short token) => _source.Version == token;

        private ManualResetValueTaskSourceCore<T> _source; // needs to be mutable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetResult(short token)
        {
            // we only support getting the result once; doing this recycles the source and advances the token

            lock (SyncLock) // we need to be really paranoid about cross-threading over changing the token
            {
                var status = _source.GetStatus(token); // do this *outside* the try/finally
                try // so that we don't increment the counter if someone asks for the wrong value
                {
                    switch(status)
                    {
                        case ValueTaskSourceStatus.Canceled:
                            ThrowHelper.ThrowTaskCanceledException();
                            break;
                        case ValueTaskSourceStatus.Pending:
                            Monitor.Wait(SyncLock);
                            break;
                    }
                    return _source.GetResult(token);
                }
                finally
                {
                    _source.Reset();
                    if (_source.Version != TaskUtils.InitialTaskSourceVersion)
                    {
                        Pool<PooledState<T>>.TryPut(this);
                        Counters.PooledStateRecycled.Increment();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IValueTaskSource.GetResult(short token) => GetResult(token);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SignalResult(short token)
        {
            lock (SyncLock)
            {
                if (token == _source.Version && _source.GetStatus(token) != ValueTaskSourceStatus.Pending)
                {
                    Monitor.Pulse(SyncLock);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _source.OnCompleted(continuation, state, token, flags);

        private object SyncLock
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception error, short token)
        {
            if (token == _source.Version)
            {

                switch (_source.GetStatus(token))
                {
                    case ValueTaskSourceStatus.Pending:
                        _source.SetException(error);
                        // only need to signal if SetException didn't inline a handler
                        if (token == _source.Version) SignalResult(token);
                        return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result, short token)
        {
            if (token == _source.Version)
            {
                switch (_source.GetStatus(token))
                {
                    case ValueTaskSourceStatus.Pending:
                        _source.SetResult(result);
                        // only need to signal if SetResult didn't inline a handler
                        if (token == _source.Version) SignalResult(token);
                        return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(short token)
            => TrySetException(TaskUtils.SharedTaskCanceledException, token);
    }
}
