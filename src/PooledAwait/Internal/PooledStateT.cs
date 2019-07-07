using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace PooledAwait.Internal
{
    internal sealed class PooledState<T> : IValueTaskSource<T>
    {
        public static PooledState<T> Create(out short token)
        {
            var obj = Pool<PooledState<T>>.TryGet() ?? new PooledState<T>();
            token = obj._source.Version;
            return obj;
        }

        private PooledState() => AllocCounters.PooledStateAllocated.Increment();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValid(short token) => _source.Version == token;

        public PooledValueTask<T> PooledValueTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new PooledValueTask<T>(this, _source.Version);
        }

        private ManualResetValueTaskSourceCore<T> _source; // needs to be mutable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T IValueTaskSource<T>.GetResult(short token)
        {
            // we only support getting the result once; doing this recycles
            // the source and advances the token
            try
            {
                return _source.GetResult(token);
            }
            finally
            {
                _source.Reset();
                Pool<PooledState<T>>.TryPut(this);
                AllocCounters.PooledStateRecycled.Increment();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => _source.GetStatus(token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
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

        public bool TrySetResult(T result, short token)
        {
            if (token == _source.Version)
            {
                try { _source.SetResult(result); } catch (InvalidOperationException) { return false; }
                return true;
            }
            return false;
        }
    }
}
