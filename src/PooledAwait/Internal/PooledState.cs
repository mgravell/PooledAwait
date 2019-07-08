using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace PooledAwait.Internal
{
    internal sealed class PooledState : IValueTaskSource
    {
        public static PooledState Create(out short token)
        {
            var obj = Pool<PooledState>.TryGet() ?? new PooledState();
            token = obj._source.Version;
            return obj;
        }

        private PooledState() => Counters.PooledStateAllocated.Increment();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValid(short token) => _source.Version == token;

        public PooledValueTask PooledValueTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new PooledValueTask(this, _source.Version);
        }

        private ManualResetValueTaskSourceCore<bool> _source; // needs to be mutable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IValueTaskSource.GetResult(short token)
        {
            // we only support getting the result once; doing this recycles
            // the source and advances the token
            try
            {
                if (_source.GetStatus(token) == ValueTaskSourceStatus.Pending) WaitForResult(token);
                _source.GetResult(token);
            }
            finally
            {
                _source.Reset();
                Pool<PooledState>.TryPut(this);
                Counters.PooledStateRecycled.Increment();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WaitForResult(short token)
        {
            lock (SyncLock)
            {
                if (token == _source.Version && _source.GetStatus(token) == ValueTaskSourceStatus.Pending)
                {
                    Monitor.Wait(SyncLock);
                }
            }
        }

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
        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _source.OnCompleted(continuation, state, token, flags);

        private object SyncLock
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }

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

        public bool TrySetResult(short token)
        {
            if (token == _source.Version)
            {
                switch (_source.GetStatus(token))
                {
                    case ValueTaskSourceStatus.Pending:
                        _source.SetResult(true);
                        // only need to signal if SetResult didn't inline a handler
                        if (token == _source.Version) SignalResult(token);
                        return true;
                }
            }
            return false;
        }
    }
}
