using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// Like a ValueTaskCompletionSource, but the actual task will only become allocated
    /// if the .Task is consumed; this is useful for APIs where the consumer *may* consume a task
    /// </summary>
    public readonly struct LazyTaskCompletionSource : IDisposable
    {

        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object obj) => obj is LazyTaskCompletionSource ltcs && _state == ltcs._state && _token == ltcs._token;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => (_state == null ? 0 : _state.GetHashCode()) ^ _token;
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(LazyTaskCompletionSource);

        private readonly LazyTaskState<Nothing> _state;
        private readonly short _token;

        /// <summary>
        /// Gets the task associated with this instance
        /// </summary>
        public Task Task => _state?.GetTask(_token) ?? ThrowHelper.ThrowInvalidOperationException<Task>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LazyTaskCompletionSource(LazyTaskState<Nothing> state)
        {
            _state = state;
            _token = state.Version;
        }

        /// <summary>
        /// Create a new instance; this instance should be disposed when it is known to be unwanted
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LazyTaskCompletionSource Create()
            => new LazyTaskCompletionSource(LazyTaskState<Nothing>.Create());

        /// <summary>
        /// Attempt to set the outcome for this operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult() => _state != null && _state.TrySetResult(_token, default);

        /// <summary>
        /// Attempt to set the outcome for this operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default) => _state != null && _state.TrySetCanceled(_token, cancellationToken);

        /// <summary>
        /// Attempt to set the outcome for this operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception) => _state != null && _state.TrySetException(_token, exception);

        /// <summary>
        /// Release all resources associated with this operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _state?.Recycle(_token);

        internal bool IsValid => _state != null && _state.IsValid(_token);
        internal bool HasSource => _state != null && _state.HasSource;
        internal bool HasTask => _state != null && _state.HasTask;
    }
}
