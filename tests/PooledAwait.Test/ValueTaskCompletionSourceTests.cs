﻿using System;
using PooledAwait.Internal;
using Xunit;
using System.Threading.Tasks;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class ValueTaskCompletionSourceTests
    {
        [Fact]
        public void DefaultInstanceBehavior()
        {
            ValueTaskCompletionSource<int> source = default;
            Assert.True(source.IsNull);
            Assert.False(source.IsOptimized);

            Assert.False(source.TrySetResult(42));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DefaultTask(bool shouldFault)
        {
            var source = ValueTaskCompletionSource<int>.Create();
            Counters.Reset();
            source = ValueTaskCompletionSource<int>.Create();
#if DEBUG
            Assert.Equal(1, Counters.TaskAllocated.Value);
#endif
            Assert.True(source.IsOptimized);

            Verify(source, shouldFault);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OptimizedTask(bool shouldFault)
        {
            var source = ValueTaskCompletionSource<int>.CreateOptimized();
            Counters.Reset();
            source = ValueTaskCompletionSource<int>.CreateOptimized();
#if DEBUG
            Assert.Equal(1, Counters.TaskAllocated.Value);
#endif
            Assert.True(source.IsOptimized);

            Verify(source, shouldFault);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FallbackTask(bool shouldFault)
        {
            var source = ValueTaskCompletionSource<int>.CreateFallback();
            Counters.Reset();
            source = ValueTaskCompletionSource<int>.CreateFallback();
#if DEBUG
            Assert.Equal(1, Counters.TaskAllocated.Value);
#endif
            Assert.False(source.IsOptimized);

            Verify(source, shouldFault);
        }

        private void Verify(ValueTaskCompletionSource<int> source, bool shouldFault)
        {
            var task = source.Task;
            Assert.False(task.IsCompleted);
            Assert.False(task.Status == TaskStatus.RanToCompletion);

            if (shouldFault)
            {
                Assert.True(source.TrySetException(new FormatException()));
                Assert.False(source.TrySetResult(42));
                Assert.False(source.TrySetException(new FormatException()));

                for (int i = 0; i < 2; i++) // can check multiple times
                {
                    Assert.True(task.IsFaulted);
                    Assert.True(task.IsCompleted);
                    Assert.False(task.Status == TaskStatus.RanToCompletion);
                    var ex = Assert.Throws<AggregateException>(() => task.Result);
                    Assert.IsType<FormatException>(ex.InnerException);
                }
            }
            else
            {
                Assert.True(source.TrySetResult(42));
                Assert.False(source.TrySetResult(42));
                Assert.False(source.TrySetException(new FormatException()));

                for (int i = 0; i < 2; i++) // can check multiple times
                {
                    Assert.False(task.IsFaulted);
                    Assert.True(task.IsCompleted);
                    Assert.True(task.Status == TaskStatus.RanToCompletion);
                    Assert.Equal(42, task.Result);
                }
            }

            Assert.False(source.IsNull); // still good
        }
    }
}
