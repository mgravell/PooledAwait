using PooledAwait.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// A task-source that automatically recycles when the task is awaited
    /// </summary>
    public readonly struct PooledValueTaskSource
    {
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object? obj) => obj is PooledValueTaskSource pvt && _source == pvt._source && _token == pvt._token;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => (_source == null ? 0 : _source.GetHashCode()) ^ _token;
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(PooledValueTaskSource);

        /// <summary>
        /// Gets the task that corresponds to this instance; it can only be awaited once
        /// </summary>
        public ValueTask Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source == null ? default : new ValueTask(_source, _token);
        }

        /// <summary>
        /// Indicates whether this instance is well-defined against a value task instance
        /// </summary>
        public bool HasTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source != null;
        }

        internal PooledValueTask PooledTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new PooledValueTask(_source, _token);
        }

        /// <summary>
        /// Rents a task-source that will be recycled when the task is awaited
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledValueTaskSource Create()
        {
            var source = PooledState<Nothing>.Create(out var token);
            return new PooledValueTaskSource(source, token);
        }

        private readonly PooledState<Nothing> _source;
        private readonly short _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledValueTaskSource(PooledState<Nothing> source, short token)
        {
            _source = source;
            _token = token;
        }

        /// <summary>
        /// Test whether the source is valid
        /// </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source != null && _source.IsValid(_token);
        }

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetResult() => _source != null && _source.TrySetResult(default, _token);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult()
        {
            if (!TrySetResult()) ThrowHelper.ThrowInvalidOperationException();
        }

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetException(Exception error) => _source != null && _source.TrySetException(error, _token);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            if (!TrySetException(exception)) ThrowHelper.ThrowInvalidOperationException();
        }

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetCanceled() => _source != null && _source.TrySetCanceled(_token);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCanceled()
        {
            if (!TrySetCanceled()) ThrowHelper.ThrowInvalidOperationException();
        }

        /// <summary>
        /// Indicates whether this is an invalid default instance
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source == null;
        }
    }
}
