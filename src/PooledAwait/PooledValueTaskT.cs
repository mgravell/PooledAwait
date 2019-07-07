using PooledAwait.Internal;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PooledAwait
{
    /// <summary>
    /// A ValueTask<typeparamref name="T"/> with a custom source and builder
    /// </summary>
    [AsyncMethodBuilder(typeof(TaskBuilders.PooledValueTaskBuilder<>))]
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

        /// <summary>
        /// Creates a value-task with a fixed value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PooledValueTask(T result)
        {
            _source = default;
            _token = default;
            _result = result;
        }

        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<T> AsValueTask() => _source == null ? new ValueTask<T>(_result) : new ValueTask<T>(_source, _token);

        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ValueTask<T>(in PooledValueTask<T> task) => task.AsValueTask();

        /// <summary>
        /// Gets the awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter<T> GetAwaiter() => AsValueTask().GetAwaiter();

        /// <summary>
        /// Gets the configured awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
            => AsValueTask().ConfigureAwait(continueOnCapturedContext);

        /// <summary>
        /// Rents a task-source that will be recycled when the task is awaited
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PooledValueTaskSource<T> CreateSource() => PooledValueTaskSource<T>.Create();
    }
}
