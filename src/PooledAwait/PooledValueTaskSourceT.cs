using PooledAwait.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// A task-source that automatically recycles when the task is awaited
    /// </summary>
    public readonly struct PooledValueTaskSource<T>
    {
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object? obj) => obj is PooledValueTaskSource<T> pvt && _token == pvt._token &&
            (_source != null ? _source == pvt._source : (pvt._source == null && EqualityComparer<T>.Default.Equals(_value, pvt._value)));
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => (_source == null ? EqualityComparer<T>.Default.GetHashCode(_value) : _source.GetHashCode()) ^ _token;
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(PooledValueTaskSource);

        /// <summary>
        /// Gets the task that corresponds to this instance; it can only be awaited once
        /// </summary>
        public ValueTask<T> Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source == null ? new ValueTask<T>(_value) : new ValueTask<T>(_source, _token);
        }

        /// <summary>
        /// Indicates whether this instance is well-defined against a value task instance
        /// </summary>
        public bool HasTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source != null;
        }

        internal PooledValueTask<T> PooledTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source == null ? new PooledValueTask<T>(_value) : new PooledValueTask<T>(_source, _token);
        }

        /// <summary>
        /// Rents a task-source that will be recycled when the task is awaited
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledValueTaskSource<T> Create()
        {
            var source = PooledState<T>.Create(out var token);
            return new PooledValueTaskSource<T>(source, token);
        }

        private readonly PooledState<T>? _source;
        private readonly short _token;
        private readonly T _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledValueTaskSource(PooledState<T> source, short token)
        {
            _source = source;
            _token = token;
            _value = default!;
        }

        /// <summary>
        /// Create a new PooledValueTaskSource that will yield a constant value without ever renting/recycling any background state
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledValueTaskSource(T value)
        {
            _source = null;
            _token = default;
            _value = value!;
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
        public bool TrySetResult(T result) => _source != null && _source.TrySetResult(result, _token);

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
