using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PooledAwait.Internal
{
    internal static class PendingTaskFactory<T>
    {
        private static readonly IFactory s_factory = ChooseFactory();

        public static object Create() => s_factory.Create();

        internal static bool TrySetException(object state, Exception exception)
            => s_factory.TrySetException(state, exception);

        internal static bool TrySetResult(object state, T result)
            => s_factory.TrySetResult(state, result);

        internal static bool IsOptimized => s_factory is ReflectionFactory;

        private static PendingTaskFactory<T>.IFactory ChooseFactory()
        {
            return TryReflection() ?? new TaskCompletionSourceFactory();

            static IFactory? TryReflection()
            {
                try
                {
                    // perform feature tests of our voodoo
                    var factory = new ReflectionFactory();
                    var task = (Task<T>)factory.Create();
                    if (task == null) return null;
                    if (task.IsCompleted) return null;

                    if (!factory.TrySetResult(task, default!)) return null;
                    if (!task.IsCompleted) return null;
                    if (!task.IsCompletedSuccessfully) return null;

                    task = (Task<T>)factory.Create();
                    if (!factory.TrySetException(task, new InvalidOperationException())) return null;
                    if (!task.IsCompleted) return null;
                    if (!task.IsFaulted) return null;
                    try
                    {
                        _ = task.Result;
                        return null;
                    } catch(AggregateException ex) when (ex.InnerException is InvalidOperationException) {}
                    if (!(task.Exception.InnerException is InvalidOperationException)) return null;
                    return factory;
                }
                catch { return null; }
            }
        }

        private interface IFactory
        {
            object Create();
            bool TrySetException(object state, Exception exception);
            bool TrySetResult(object state, T result);
        }
        private sealed class TaskCompletionSourceFactory : IFactory
        {
            public object Create() => new TaskCompletionSource<T>();

            public bool TrySetException(object state, Exception exception)
                => ((TaskCompletionSource<T>)state).TrySetException(exception);

            public bool TrySetResult(object state, T result)
                => ((TaskCompletionSource<T>)state).TrySetResult(result);
        }
        private sealed class ReflectionFactory : IFactory
        {
            static readonly Func<Task<T>, Exception, bool> s_TrySetException
                = (Func<Task<T>, Exception, bool>)Delegate.CreateDelegate(
                    typeof(Func<Task<T>, Exception, bool>),
                    typeof(Task<T>).GetMethod("TrySetException", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(Exception) }, null));

            static readonly Func<Task<T>, T, bool> s_TrySetResult
                = (Func<Task<T>, T, bool>)Delegate.CreateDelegate(
                    typeof(Func<Task<T>, T, bool>),
                    typeof(Task<T>).GetMethod("TrySetResult", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(T) }, null));

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SpinUntilCompleted(Task<T> task)
            {
                // Spin wait until the completion is finalized by another thread.
                var sw = new SpinWait();
                while (!task.IsCompleted)
                    sw.SpinOnce();
            }
            public object Create() => FormatterServices.GetUninitializedObject(typeof(Task<T>));

            public bool TrySetException(object state, Exception exception)
            {
                var task = (Task<T>)state;
                var result = s_TrySetException(task, exception);
                if (!result && !task.IsCompleted) SpinUntilCompleted(task);
                return result;
            }

            public bool TrySetResult(object state, T value)
            {
                var task = (Task<T>)state;
                var result = s_TrySetResult(task, value);
                if (!result && !task.IsCompleted) SpinUntilCompleted(task);
                return result;
            }
        }
    }
}
