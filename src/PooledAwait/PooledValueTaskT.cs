using PooledAwait.Internal;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    [AsyncMethodBuilder(typeof(PooledValueTaskBuilder<>))]
    public readonly struct PooledValueTask<T>
    {
        private readonly PooledState<T>? _source;
        private readonly short _token;
        private readonly T _result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledValueTask(PooledState<T> source, short token)
        {
            _source = source;
            _token = token;
            _result = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledValueTask(T result)
        {
            _source = default;
            _token = default;
            _result = result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<T> AsValueTask() => _source == null ? new ValueTask<T>(_result) : new ValueTask<T>(_source, _token);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator ValueTask<T>(in PooledValueTask<T> task) => task.AsValueTask();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter<T> GetAwaiter() => AsValueTask().GetAwaiter();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => AsValueTask().ConfigureAwait(continueOnCapturedContext);
    }
}
