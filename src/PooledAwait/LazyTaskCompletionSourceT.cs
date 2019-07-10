using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// Like a ValueTaskCompletionSource<typeparamref name="T"/>, but the actual task will only become allocated
    /// if the .Task is consumed; this is useful for APIs where the consumer *may* consume a task
    /// </summary>
    public readonly struct LazyTaskCompletionSource<T> : IDisposable
    {
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object obj) => obj is LazyTaskCompletionSource<T> ltcs && _state == ltcs._state && _token == ltcs._token;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => (_state == null ? 0 : _state.GetHashCode()) ^ _token;
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(LazyTaskCompletionSource);

        private readonly LazyTaskState<T> _state;
        private readonly short _token;

        /// <summary>
        /// Gets the task associated with this instance
        /// </summary>
        public Task<T> Task => (Task<T>)(_state?.GetTask(_token) ?? ThrowHelper.ThrowInvalidOperationException<Task<T>>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LazyTaskCompletionSource(LazyTaskState<T> state)
        {
            _state = state;
            _token = state.Version;
        }

        /// <summary>
        /// Create a new instance; this instance should be disposed when it is known to be unwanted
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LazyTaskCompletionSource<T> Create()
            => new LazyTaskCompletionSource<T>(LazyTaskState<T>.Create());

        /// <summary>
        /// Create a new instance; this instance will never by recycled
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LazyTaskCompletionSource<T> CreateConstant(T value)
            => new LazyTaskCompletionSource<T>(LazyTaskState<T>.CreateConstant(value));


        private static LazyTaskCompletionSource<T> _canceled;

        /// <summary>
        /// A global LazyTaskCompletionSource that represents a cancelled operation
        /// </summary>
        public static LazyTaskCompletionSource<T> CanceledTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _canceled.IsValid ? _canceled : _canceled = new LazyTaskCompletionSource<T>(LazyTaskState<T>.CreateCanceled());
        }

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult(T result) => _state != null && _state.TrySetResult(_token, result);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            if (!TrySetResult(result)) ThrowHelper.ThrowInvalidOperationException();
        }

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
            => _state != null && _state.TrySetCanceled(_token, cancellationToken);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCanceled(CancellationToken cancellationToken = default)
        {
            if (!TrySetCanceled(cancellationToken)) ThrowHelper.ThrowInvalidOperationException();
        }

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception exception) => _state != null && _state.TrySetException(_token, exception);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (!TrySetException(exception)) ThrowHelper.ThrowInvalidOperationException();
        }

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
