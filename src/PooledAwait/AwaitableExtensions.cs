using System.Runtime.CompilerServices;

namespace PooledAwait
{
    /// <summary>
    /// Provides async/await-related extension methods
    /// </summary>
    public static class AwaitableExtensions
    {
        /// <summary>Controls whether a yield operation should respect captured context</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredYieldAwaitable ConfigureAwait(this YieldAwaitable _, bool continueOnCapturedContext)
            => new ConfiguredYieldAwaitable(continueOnCapturedContext);
    }
}
