using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace PooledAwait.Internal
{
    // NET45 lacks some useful Task APIs; shim over them
    internal static class TaskUtils
    {
        internal static readonly short InitialTaskSourceVersion = new ManualResetValueTaskSourceCore<Nothing>().Version;

        public static readonly TaskCanceledException SharedTaskCanceledException = new TaskCanceledException();
#if NET45
        public static readonly Task CompletedTask = TaskFactory<Nothing>.FromResult(default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> FromException<T>(Exception exception)
        {
            var source = ValueTaskCompletionSource<T>.Create();
            source.TrySetException(exception);
            return source.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FromException(Exception exception) => FromException<bool>(exception);
#else
        public static readonly Task CompletedTask = Task.CompletedTask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<T> FromException<T>(Exception exception) => Task.FromException<T>(exception);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task FromException(Exception exception) => Task.FromException(exception);
#endif


        internal static class TaskFactory<TResult>
        {
            // draws from AsyncMethodBuilder, but less boxing

            public static readonly Task<TResult> Canceled = CreateCanceled();

            static Task<TResult> CreateCanceled()
            {
                var source = ValueTaskCompletionSource<TResult>.Create();
                source.TrySetCanceled();
                return source.Task;
            }

            private static readonly TaskCache<TResult> _cache = (TaskCache<TResult>)CreateCacheForType();

            private static object CreateCacheForType()
            {
                if (typeof(TResult) == typeof(Nothing)) return new NothingTaskCache();
                if (typeof(TResult) == typeof(int)) return new Int32TaskCache();
                if (typeof(TResult) == typeof(int?)) return new NullableInt32TaskCache();
                if (typeof(TResult) == typeof(bool)) return new BooleanTaskCache();
                if (typeof(TResult) == typeof(bool?)) return new NullableBooleanTaskCache();

                Type underlyingType = Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult);
                if (underlyingType == typeof(uint)
                 || underlyingType == typeof(byte)
                 || underlyingType == typeof(sbyte)
                 || underlyingType == typeof(char)
                 || underlyingType == typeof(decimal)
                 || underlyingType == typeof(long)
                 || underlyingType == typeof(ulong)
                 || underlyingType == typeof(short)
                 || underlyingType == typeof(ushort)
                 || underlyingType == typeof(float)
                 || underlyingType == typeof(double)
                 || underlyingType == typeof(IntPtr)
                 || underlyingType == typeof(UIntPtr)
                    ) return new DefaultEquatableTaskCache<TResult>();

                if (typeof(TResult) == typeof(string)) return new StringTaskCache();
#if !NETSTANDARD1_3
                if (!typeof(TResult).IsValueType) return new ObjectTaskCache<TResult>();
#endif
                return new TaskCache<TResult>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Task<TResult> FromResult(TResult result) => _cache.FromResult(result);
        }

        class TaskCache<TResult>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual Task<TResult> FromResult(TResult value) => Task.FromResult(value);
        }
        class NothingTaskCache : TaskCache<Nothing>
        {
            private static readonly Task<Nothing> s_Instance = Task.FromResult<Nothing>(default);
            internal override Task<Nothing> FromResult(Nothing value) => s_Instance;
        }
        class DefaultEquatableTaskCache<TResult> : TaskCache<TResult>
        {
            private static readonly Task<TResult> s_Default = Task.FromResult<TResult>(default!);
            private static readonly EqualityComparer<TResult> _comparer = EqualityComparer<TResult>.Default;
            internal override Task<TResult> FromResult(TResult value)
                => _comparer.Equals(value, default!) ? s_Default : base.FromResult(value);
        }
        class ObjectTaskCache<TResult> : TaskCache<TResult>
        {
            private static readonly Task<TResult> s_Null = Task.FromResult<TResult>(default!);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override Task<TResult> FromResult(TResult value)
                => value == null ? s_Null : base.FromResult(value);
        }
        sealed class StringTaskCache : ObjectTaskCache<string>
        {
            private static readonly Task<string> s_Empty = Task.FromResult<string>("");
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override Task<string> FromResult(string value)
                => value == "" ? s_Empty : base.FromResult(value);
        }
        sealed class BooleanTaskCache : TaskCache<bool>
        {
            static readonly Task<bool> s_True = Task.FromResult(true), s_False = Task.FromResult(false);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override Task<bool> FromResult(bool value) => value ? s_True : s_False;
        }
        sealed class NullableBooleanTaskCache : TaskCache<bool?>
        {
            static readonly Task<bool?> s_True = Task.FromResult((bool?)true), s_False = Task.FromResult((bool?)false),
                s_Null = Task.FromResult((bool?)null);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override Task<bool?> FromResult(bool? value) =>
                value.HasValue ? (value.GetValueOrDefault() ? s_True : s_False) : s_Null;
        }
        sealed class Int32TaskCache : TaskCache<int>
        {
            const int MIN_INC = -1, MAX_EXC = 11;
            static readonly Task<int>[] s_Known = CreateKnown();
            static Task<int>[] CreateKnown()
            {
                var arr = new Task<int>[MAX_EXC - MIN_INC];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = Task.FromResult(i + MIN_INC);
                return arr;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override Task<int> FromResult(int value)
                => value >= MIN_INC && value < MAX_EXC ? s_Known[value - MIN_INC] : base.FromResult(value);
        }
        sealed class NullableInt32TaskCache : TaskCache<int?>
        {
            const int MIN_INC = -1, MAX_EXC = 11;
            static readonly Task<int?>[] s_Known = CreateKnown();
            static readonly Task<int?> s_Null = Task.FromResult((int?)null);
            static Task<int?>[] CreateKnown()
            {
                var arr = new Task<int?>[MAX_EXC - MIN_INC];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = Task.FromResult((int?)(i + MIN_INC));
                return arr;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override Task<int?> FromResult(int? nullable)
            {
                if (nullable.HasValue)
                {
                    int value = nullable.GetValueOrDefault();
                    return value >= MIN_INC && value < MAX_EXC ? s_Known[value - MIN_INC] : base.FromResult(nullable);
                }
                return s_Null;
            }
        }

    }
}
