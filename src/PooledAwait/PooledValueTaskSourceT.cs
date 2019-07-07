using PooledAwait.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// A task-source that automatically recycles when the task is awaited
    /// </summary>
    public readonly struct PooledValueTaskSource<T>
    {
        /// <summary>
        /// Gets the task that corresponds to this instance; it can only be awaited once
        /// </summary>
        public ValueTask<T> Task => new PooledValueTask<T>(_source, _token).AsValueTask();

        /// <summary>
        /// Rents a task-source that will be recycled when the task is awaited
        /// </summary>
        public static PooledValueTaskSource<T> Create()
        {
            var source = PooledState<T>.Create(out var token);
            return new PooledValueTaskSource<T>(source, token);
        }

        private readonly PooledState<T> _source;
        private readonly short _token;

        internal PooledValueTaskSource(PooledState<T> source, short token)
        {
            _source = source;
            _token = token;
        }

        /// <summary>
        /// Test whether the source is valid
        /// </summary>
        public bool IsValid => _source != null && _source.IsValid(_token);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        public bool TrySetResult(T result) => _source != null && _source.TrySetResult(result, _token);

        /// <summary>
        /// Set the result of the operation
        /// </summary>
        public bool TrySetException(Exception error) => _source != null && _source.TrySetException(error, _token);
    }
}
