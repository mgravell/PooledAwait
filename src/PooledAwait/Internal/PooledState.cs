using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        private PooledState() => AllocCounters.PooledStateAllocated.Increment();

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
                _source.GetResult(token);
            }
            finally
            {
                _source.Reset();
                Pool<PooledState>.TryPut(this);
                AllocCounters.PooledStateRecycled.Increment();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _source.OnCompleted(continuation, state, token, flags);

        public bool TrySetException(Exception error, short token)
        {
            if (token == _source.Version)
            {
                try { _source.SetException(error); } catch (InvalidOperationException) { return false; }
                return true;
            }
            return false;
        }

        public bool TrySetResult(short token)
        {
            if (token == _source.Version)
            {
                try { _source.SetResult(true); } catch (InvalidOperationException) { return false; }
                return true;
            }
            return false;
        }
    }
}
