using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace PooledAwait
{
    /// <summary>
    /// A ValueTask with a custom source and builder
    /// </summary>
    [AsyncMethodBuilder(typeof(MethodBuilders.PooledValueTaskMethodBuilder))]
    public readonly struct PooledValueTask
    {
        /// <summary><see cref="Object.Equals(Object)"/></summary>
        public override bool Equals(object? obj) => obj is PooledValueTask pvt && _source == pvt._source && _token == pvt._token;
        /// <summary><see cref="Object.GetHashCode"/></summary>
        public override int GetHashCode() => (_source == null ? 0 : _source.GetHashCode()) ^ _token;
        /// <summary><see cref="Object.ToString"/></summary>
        public override string ToString() => nameof(PooledValueTask);

        private readonly IValueTaskSource _source;
        private readonly short _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PooledValueTask(IValueTaskSource source, short token)
        {
            _source = source;
            _token = token;
        }

        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask AsValueTask() => _source == null ? default : new ValueTask(_source, _token);

        /// <summary>
        /// Gets the instance as a value-task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ValueTask(in PooledValueTask task) => task.AsValueTask();

        /// <summary>
        /// Gets the awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter GetAwaiter() => AsValueTask().GetAwaiter();

        /// <summary>
        /// Gets the configured awaiter for the task
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
            => AsValueTask().ConfigureAwait(continueOnCapturedContext);

        /// <summary>
        /// Rents a task-source that will be recycled when the task is awaited
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PooledValueTaskSource CreateSource() => PooledValueTaskSource.Create();

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
