using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    public class FireAndForgetTests
    {
        static void LockedAdd<T>(List<T> list, T value)
        {
            lock(list) { list.Add(value); }
        }

        [Fact]
        public async Task AsValueTask()
        {
            var list = new List<string>();
            var allDone = PooledValueTaskSource.Create();

            LockedAdd(list, "a");
            await TestAsync();
            LockedAdd(list, "b");
            await allDone.Task;
            LockedAdd(list, "c");

            Assert.Equal("a,d,e,f,b,c", string.Join(',', list));

            async ValueTask TestAsync()
            {
                LockedAdd(list, "d");
                await Task.Yield();
                LockedAdd(list, "e");
                await Task.Yield();
                LockedAdd(list, "f");
                await Task.Yield();
                allDone.TrySetResult(); // this is just so we know all added
            }
        }

        [Fact]
        public async Task AsFireAndForget()
        {
            var list = new List<string>();
            var allDone = PooledValueTaskSource.Create();

            LockedAdd(list, "a");
            await TestAsync();
            LockedAdd(list, "b");
            await allDone.Task;
            LockedAdd(list, "c");

            Assert.Equal("a,d,b,e,f,c", string.Join(',', list));

            async FireAndForget TestAsync()
            {
                LockedAdd(list, "d");
                await Task.Yield();
                LockedAdd(list, "e");
                await Task.Yield();
                LockedAdd(list, "f");
                await Task.Yield();
                allDone.TrySetResult();// this is just so we know all added
            }
        }
    }
}
