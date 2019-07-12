using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PooledAwait.Test
{
    public class ComplexAsyncTests
    {
        [Fact]
        public async Task HorriblyThreaded_Task()
        {
            var tasks = new Task<int>[100];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Impl(i);

            for(int i = 0; i < tasks.Length; i++)
            {
                Assert.Equal(i, await tasks[i]);
            }

            async static PooledTask<int> Impl(int j)
            {
                for (int i = 0; i < 50; i++)
                {
                    await Task.Yield();
                }
                return j;
            }
        }

        [Fact]
        public async Task HorriblyThreaded_ValueTask()
        {
            var tasks = new ValueTask<int>[100];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Impl(i);

            for (int i = 0; i < tasks.Length; i++)
            {
                Assert.Equal(i, await tasks[i]);
            }

            async static PooledValueTask<int> Impl(int j)
            {
                for (int i = 0; i < 50; i++)
                {
                    await Task.Yield();
                }
                return j;
            }
        }
    }
}
