using PooledAwait.Internal;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    [AsyncMethodBuilder(typeof(TaskBuilders.PooledValueTaskBuilder))]
    public readonly struct PooledValueTask
    {
        private readonly PooledState? _source;
        private readonly short _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledValueTask(PooledState source, short token)
        {
            _source = source;
            _token = token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask AsValueTask() => _source == null ? default : new ValueTask(_source, _token);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static implicit operator ValueTask(in PooledValueTask task) => task.AsValueTask();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter GetAwaiter() => AsValueTask().GetAwaiter();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
            => AsValueTask().ConfigureAwait(continueOnCapturedContext);
    }
}
