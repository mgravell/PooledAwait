using System;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    [Collection("Sequential")]
    public class CompletedTaskIdentityTests
    {
        [Fact]
        public void NoType()
        {
            using var x = LazyTaskCompletionSource.Create();
            Assert.True(x.TrySetResult());
            Assert.Same(x.Task, Task.CompletedTask);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AllBooleans(bool result)
        {
            using var x = LazyTaskCompletionSource<bool>.Create();
            using var y = LazyTaskCompletionSource<bool>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.Same(x.Task, y.Task);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void AllNullableBooleans(bool? result)
        {
            using var x = LazyTaskCompletionSource<bool?>.Create();
            using var y = LazyTaskCompletionSource<bool?>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.Same(x.Task, y.Task);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        public void CommonIntegers(int result)
        {
            using var x = LazyTaskCompletionSource<int>.Create();
            using var y = LazyTaskCompletionSource<int>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.Same(x.Task, y.Task);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(11)]
        public void UnommonIntegers(int result)
        {
            using var x = LazyTaskCompletionSource<int>.Create();
            using var y = LazyTaskCompletionSource<int>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.NotSame(x.Task, y.Task);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        public void CommonNullableIntegers(int? result)
        {
            using var x = LazyTaskCompletionSource<int?>.Create();
            using var y = LazyTaskCompletionSource<int?>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.Same(x.Task, y.Task);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(11)]
        public void UnommonNullableIntegers(int? result)
        {
            using var x = LazyTaskCompletionSource<int?>.Create();
            using var y = LazyTaskCompletionSource<int?>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.NotSame(x.Task, y.Task);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void CommonStrings(string result)
        {
            using var x = LazyTaskCompletionSource<string>.Create();
            using var y = LazyTaskCompletionSource<string>.Create();
            Assert.True(x.TrySetResult(result));
            Assert.True(y.TrySetResult(result));
            Assert.Same(x.Task, y.Task);
        }

        [Fact]
        public void Single() => TestStructDefaultIdentity<float>();
        [Fact]
        public void Double() => TestStructDefaultIdentity<double>();
        [Fact]
        public void Boolean() => TestStructDefaultIdentity<bool>();
        [Fact]
        public void Char() => TestStructDefaultIdentity<char>();
        [Fact]
        public void SByte() => TestStructDefaultIdentity<sbyte>();
        [Fact]
        public void Int16() => TestStructDefaultIdentity<short>();
        [Fact]
        public void Int32() => TestStructDefaultIdentity<int>();
        [Fact]
        public void Int64() => TestStructDefaultIdentity<long>();
        [Fact]
        public void Byte() => TestStructDefaultIdentity<byte>();
        [Fact]
        public void UInt16() => TestStructDefaultIdentity<ushort>();
        [Fact]
        public void UInt32() => TestStructDefaultIdentity<uint>();
        [Fact]
        public void UInt64() => TestStructDefaultIdentity<ulong>();
        [Fact]
        public void IntPtr() => TestStructDefaultIdentity<IntPtr>();
        [Fact]
        public void UIntPtr() => TestStructDefaultIdentity<UIntPtr>();

        private static void TestStructDefaultIdentity<T>() where T : struct
        {
            using (var x = LazyTaskCompletionSource<T>.Create())
            using (var y = LazyTaskCompletionSource<T>.Create())
            {
                Assert.True(x.TrySetResult(default));
                Assert.True(y.TrySetResult(default));
                Assert.Same(x.Task, y.Task);
            }

            using var nx = LazyTaskCompletionSource<T?>.Create();
            using var ny = LazyTaskCompletionSource<T?>.Create();
            Assert.True(nx.TrySetResult(default));
            Assert.True(ny.TrySetResult(default));
            Assert.Same(nx.Task, ny.Task);
        }
    }
}
