using PooledAwait.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace PooledAwait
{
    public readonly struct PooledValueTaskSource<T>
    {
        private readonly PooledState<T>? _source;
        private readonly short _token;

        internal PooledValueTaskSource(PooledState<T> source, short token)
        {
            _source = source;
            _token = token;
        }

        public bool IsValid => _source != null && _source.IsValid(_token);
        public bool TrySetResult(T result) => _source != null && _source.TrySetResult(result, _token);
        public bool TrySetException(Exception error) => _source != null && _source.TrySetException(error, _token);
    }
}
