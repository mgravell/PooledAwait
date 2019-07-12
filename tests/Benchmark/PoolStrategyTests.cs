using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Benchmark
{

    /*
     * Typical results below; conclusion - Array is *only* good for PushPopOnce, but that scenario
     * is already handled by the static; By the time we're up to PushPopQuarter (quarter-capacity),
     * array is "bad" again. Array is also *teriible* at the TakeEmpty and PutFull
     * scenarios, which are both key scenarios in a busy pool, and even PuspPopCapacity is "bad".
     * 
     * The best overall performer is the linked-list, so: let's
     * use that as our impl
     
| Method |  Job | Runtime |      Categories |         Mean |     Error |    StdDev |       Median |  Ratio | RatioSD |
|------- |----- |-------- |---------------- |-------------:|----------:|----------:|-------------:|-------:|--------:|
|  Queue |  Clr |     Clr |       TakeEmpty |    14.500 ns | 0.0176 ns | 0.0147 ns |    14.500 ns |   5.65 |    0.01 |
|  Stack |  Clr |     Clr |       TakeEmpty |    14.495 ns | 0.0105 ns | 0.0088 ns |    14.490 ns |   5.65 |    0.01 |
|  Array |  Clr |     Clr |       TakeEmpty |   262.012 ns | 0.1567 ns | 0.1308 ns |   261.980 ns | 102.07 |    0.19 |
| Linked |  Clr |     Clr |       TakeEmpty |     2.567 ns | 0.0053 ns | 0.0047 ns |     2.570 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue |  Clr |     Clr |         PutFull |    15.618 ns | 0.6589 ns | 1.4323 ns |    14.785 ns |  14.38 |    1.52 |
|  Stack |  Clr |     Clr |         PutFull |    14.758 ns | 0.0146 ns | 0.0114 ns |    14.750 ns |  12.68 |    0.10 |
|  Array |  Clr |     Clr |         PutFull |   117.158 ns | 0.0593 ns | 0.0495 ns |   117.160 ns | 100.67 |    0.76 |
| Linked |  Clr |     Clr |         PutFull |     1.164 ns | 0.0095 ns | 0.0084 ns |     1.160 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue |  Clr |     Clr |     PushPopOnce |    33.328 ns | 0.0968 ns | 0.0906 ns |    33.280 ns |   1.08 |    0.01 |
|  Queue |  Clr |     Clr |     PushPopOnce |    32.716 ns | 0.1537 ns | 0.1438 ns |    32.690 ns |   1.06 |    0.01 |
|  Array |  Clr |     Clr |     PushPopOnce |    19.831 ns | 0.0235 ns | 0.0183 ns |    19.825 ns |   0.64 |    0.00 |
| Linked |  Clr |     Clr |     PushPopOnce |    30.806 ns | 0.1262 ns | 0.1181 ns |    30.800 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue |  Clr |     Clr |  PushPopQuarter |   837.967 ns | 0.3326 ns | 0.2597 ns |   838.075 ns |   1.14 |    0.00 |
|  Queue |  Clr |     Clr |  PushPopQuarter |   825.743 ns | 0.5666 ns | 0.5022 ns |   825.775 ns |   1.12 |    0.00 |
|  Array |  Clr |     Clr |  PushPopQuarter | 1,477.112 ns | 0.9879 ns | 0.8249 ns | 1,476.800 ns |   2.00 |    0.00 |
| Linked |  Clr |     Clr |  PushPopQuarter |   737.677 ns | 0.7767 ns | 0.6486 ns |   737.400 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue |  Clr |     Clr | PushPopCapacity |   671.785 ns | 2.2005 ns | 1.8375 ns |   670.600 ns |   1.15 |    0.00 |
|  Queue |  Clr |     Clr | PushPopCapacity |   660.843 ns | 2.1805 ns | 1.9330 ns |   659.700 ns |   1.13 |    0.00 |
|  Array |  Clr |     Clr | PushPopCapacity | 4,313.338 ns | 2.9127 ns | 2.4323 ns | 4,312.400 ns |   7.36 |    0.02 |
| Linked |  Clr |     Clr | PushPopCapacity |   586.429 ns | 1.8915 ns | 1.6767 ns |   585.600 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue | Core |    Core |       TakeEmpty |    17.463 ns | 0.1816 ns | 0.1517 ns |    17.430 ns |   5.12 |    0.04 |
|  Stack | Core |    Core |       TakeEmpty |    17.535 ns | 0.0319 ns | 0.0267 ns |    17.540 ns |   5.14 |    0.01 |
|  Array | Core |    Core |       TakeEmpty |   125.329 ns | 0.0545 ns | 0.0510 ns |   125.310 ns |  36.72 |    0.05 |
| Linked | Core |    Core |       TakeEmpty |     3.413 ns | 0.0048 ns | 0.0043 ns |     3.415 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue | Core |    Core |         PutFull |    17.350 ns | 0.0095 ns | 0.0074 ns |    17.350 ns |  15.05 |    0.06 |
|  Stack | Core |    Core |         PutFull |    17.368 ns | 0.0230 ns | 0.0192 ns |    17.370 ns |  15.07 |    0.05 |
|  Array | Core |    Core |         PutFull |   117.256 ns | 0.3167 ns | 0.2963 ns |   117.210 ns | 101.73 |    0.53 |
| Linked | Core |    Core |         PutFull |     1.153 ns | 0.0049 ns | 0.0046 ns |     1.150 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue | Core |    Core |     PushPopOnce |    42.199 ns | 0.1160 ns | 0.1085 ns |    42.120 ns |   1.38 |    0.01 |
|  Queue | Core |    Core |     PushPopOnce |    41.179 ns | 0.1683 ns | 0.1492 ns |    41.100 ns |   1.35 |    0.01 |
|  Array | Core |    Core |     PushPopOnce |    12.993 ns | 0.0128 ns | 0.0114 ns |    12.990 ns |   0.43 |    0.00 |
| Linked | Core |    Core |     PushPopOnce |    30.513 ns | 0.2241 ns | 0.2096 ns |    30.470 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue | Core |    Core |  PushPopQuarter | 1,061.417 ns | 0.7060 ns | 0.5512 ns | 1,061.300 ns |   1.43 |    0.00 |
|  Queue | Core |    Core |  PushPopQuarter | 1,035.825 ns | 1.5696 ns | 1.3914 ns | 1,035.550 ns |   1.40 |    0.00 |
|  Array | Core |    Core |  PushPopQuarter |   960.818 ns | 2.8905 ns | 2.5623 ns |   961.350 ns |   1.29 |    0.00 |
| Linked | Core |    Core |  PushPopQuarter |   742.104 ns | 1.2823 ns | 1.1367 ns |   741.775 ns |   1.00 |    0.00 |
|        |      |         |                 |              |           |           |              |        |         |
|  Queue | Core |    Core | PushPopCapacity |   847.520 ns | 2.0409 ns | 1.9091 ns |   847.200 ns |   1.43 |    0.00 |
|  Queue | Core |    Core | PushPopCapacity |   824.738 ns | 2.1207 ns | 1.7708 ns |   823.800 ns |   1.39 |    0.00 |
|  Array | Core |    Core | PushPopCapacity | 2,794.871 ns | 1.2471 ns | 1.1055 ns | 2,794.600 ns |   4.72 |    0.01 |
| Linked | Core |    Core | PushPopCapacity |   592.257 ns | 1.7664 ns | 1.5658 ns |   591.800 ns |   1.00 |    0.00 |


    */
    [CoreJob, ClrJob]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class PoolStrategyTests
    {
        internal const int Capacity = 20;
        private readonly Queue<object> _queue = new Queue<object>(Capacity);
        private readonly Stack<object> _stack = new Stack<object>(Capacity);
        private readonly object[] _array = new object[Capacity];
        private Node<object>? _spares = Node<object>.Create(Capacity), _head;
        static readonly object s_TestObject = new object();

        const int OpsPerInvoke = 10000;

        [IterationCleanup]
        public void Reset()
        {
            while (Util<object>.TryGet(_queue) != null) { }
            while (Util<object>.TryGet(_stack) != null) { }
            while (Util<object>.TryGet(_array) != null) { }
            while (Node<object>.TryGet(ref _head, ref _spares) != null) { }
        }

        [BenchmarkCategory("TakeEmpty")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Queue")]
        public void TakeEmpty_Queue()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryGet(_queue);
            }
        }

        [BenchmarkCategory("TakeEmpty")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Stack")]
        public void TakeEmpty_Stack()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryGet(_stack);
            }
        }

        [BenchmarkCategory("TakeEmpty")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Array")]
        public void TakeEmpty_Array()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryGet(_array);
            }
        }

        [BenchmarkCategory("TakeEmpty")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Linked", Baseline = true)]
        public void TakeEmpty_Linked()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Node<object>.TryGet(ref _head, ref _spares);
            }
        }

        [BenchmarkCategory("PutFull")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Queue")]
        public void PutFull_Queue()
        {
            while (Util<object>.TryPut(_queue, s_TestObject)) { }
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryPut(_queue, s_TestObject);
            }
        }

        [BenchmarkCategory("PutFull")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Stack")]
        public void PutFull_Stack()
        {
            while (Util<object>.TryPut(_stack, s_TestObject)) { }
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryPut(_stack, s_TestObject);
            }
        }

        [BenchmarkCategory("PutFull")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Array")]
        public void PutFull_Array()
        {
            while (Util<object>.TryPut(_array, s_TestObject)) { }
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryPut(_array, s_TestObject);
            }
        }

        [BenchmarkCategory("PutFull")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Linked", Baseline = true)]
        public void PutFull_Linked()
        {
            while (Node<object>.TryPut(ref _head, ref _spares, s_TestObject)) { }
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Node<object>.TryPut(ref _head, ref _spares, s_TestObject);
            }
        }


        [BenchmarkCategory("PushPopOnce")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Queue")]
        public void PushPopOnce_Queue()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryPut(_queue, s_TestObject);
                Util<object>.TryGet(_queue);
            }
        }

        [BenchmarkCategory("PushPopOnce")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Queue")]
        public void PushPopOnce_Stack()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryPut(_stack, s_TestObject);
                Util<object>.TryGet(_stack);
            }
        }

        [BenchmarkCategory("PushPopOnce")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Array")]
        public void PushPopOnce_Array()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Util<object>.TryPut(_array, s_TestObject);
                Util<object>.TryGet(_array);
            }
        }

        [BenchmarkCategory("PushPopOnce")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Linked", Baseline = true)]
        public void PushPopOnce_Linked()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                Node<object>.TryPut(ref _head, ref _spares, s_TestObject);
                Node<object>.TryGet(ref _head, ref _spares);
            }
        }
        const int QuarterCapacity = Capacity / 4;
        [BenchmarkCategory("PushPopQuarter")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / QuarterCapacity, Description = "Queue")]
        public void PushPopQuarter_Queue()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                for (int j = 0; j < QuarterCapacity; j++) Util<object>.TryPut(_queue, s_TestObject);
                for (int j = 0; j < QuarterCapacity; j++) Util<object>.TryGet(_queue);
            }
        }

        [BenchmarkCategory("PushPopQuarter")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / QuarterCapacity, Description = "Queue")]
        public void PushPopQuarter_Stack()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                for (int j = 0; j < QuarterCapacity; j++) Util<object>.TryPut(_stack, s_TestObject);
                for (int j = 0; j < QuarterCapacity; j++) Util<object>.TryGet(_stack);
            }
        }

        [BenchmarkCategory("PushPopQuarter")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / QuarterCapacity, Description = "Array")]
        public void PushPopQuarter_Array()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                for (int j = 0; j < QuarterCapacity; j++) Util<object>.TryPut(_array, s_TestObject);
                for (int j = 0; j < QuarterCapacity; j++) Util<object>.TryGet(_array);
            }
        }

        [BenchmarkCategory("PushPopQuarter")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / QuarterCapacity, Description = "Linked", Baseline = true)]
        public void PushPopQuarter_Linked()
        {
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                for (int j = 0; j < QuarterCapacity; j++) Node<object>.TryPut(ref _head, ref _spares, s_TestObject);
                for (int j = 0; j < QuarterCapacity; j++) Node<object>.TryGet(ref _head, ref _spares);
            }
        }

        [BenchmarkCategory("PushPopCapacity")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / Capacity, Description = "Queue")]
        public void PushPopCapacity_Queue()
        {
            for (int i = 0; i < OpsPerInvoke / Capacity; i++)
            {
                for (int j = 0; j < Capacity; j++) Util<object>.TryPut(_queue, s_TestObject);
                for (int j = 0; j < Capacity; j++) Util<object>.TryGet(_queue);
            }
        }

        [BenchmarkCategory("PushPopCapacity")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / Capacity, Description = "Queue")]
        public void PushPopCapacity_Stack()
        {
            for (int i = 0; i < OpsPerInvoke / Capacity; i++)
            {
                for (int j = 0; j < Capacity; j++) Util<object>.TryPut(_stack, s_TestObject);
                for (int j = 0; j < Capacity; j++) Util<object>.TryGet(_stack);
            }
        }

        [BenchmarkCategory("PushPopCapacity")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / Capacity, Description = "Array")]
        public void PushPopCapacity_Array()
        {
            for (int i = 0; i < OpsPerInvoke / Capacity; i++)
            {
                for (int j = 0; j < Capacity; j++) Util<object>.TryPut(_array, s_TestObject);
                for (int j = 0; j < Capacity; j++) Util<object>.TryGet(_array);
            }
        }

        [BenchmarkCategory("PushPopCapacity")]
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / Capacity, Description = "Linked", Baseline = true)]
        public void PushPopCapacity_Linked()
        {
            for (int i = 0; i < OpsPerInvoke / Capacity; i++)
            {
                for (int j = 0; j < Capacity; j++) Node<object>.TryPut(ref _head, ref _spares, s_TestObject);
                for (int j = 0; j < Capacity; j++) Node<object>.TryGet(ref _head, ref _spares);
            }
        }


    }
    sealed class Node<T> where T : class
    {
        public T? Value;
        public Node<T>? Tail;

        public static T? TryGet(ref Node<T>? head, ref Node<T>? spares)
        {
            Node<T>? taken = PopNode(ref head);
            if (taken == null) return null;
            var value = taken.Value;
            taken.Value = null;
            PushNode(ref spares, taken);
            return value;
        }

        public static bool TryPut(ref Node<T>? head, ref Node<T>? spares, T value)
        {
            Node<T>? taken = PopNode(ref spares);
            if (taken == null) return false;
            taken.Value = value;
            PushNode(ref head, taken);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Node<T>? PopNode(ref Node<T>? field)
        {
            Node<T>? head = Volatile.Read(ref field);
            while (true)
            {
                if (head == null) return null;
                var newHead = head.Tail;

                var swap = Interlocked.CompareExchange(ref field, newHead, head);
                if ((object?)swap == (object?)head) return head; // success
                head = swap; // failure; retry
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PushNode(ref Node<T>? field, Node<T> node)
        {
            Node<T>? head = Volatile.Read(ref field);
            while (true)
            {
                node.Tail = head;
                var swap = Interlocked.CompareExchange(ref field, node, head);
                if ((object?)swap == (object?)head) return; // success
                head = swap; // failure; retry
            }
        }

        public static Node<T>? Create(int count)
        {
            Node<T>? head = null;
            for (int i = 0; i < count; i++)
            {
                var newNode = new Node<T>
                {
                    Tail = head
                };
                head = newNode;
            }
            return head;
        }
    }

    static class Util<T> where T : class
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? TryGet(Queue<T> queue)
        {
            lock (queue)
            {
                return queue.Count == 0 ? null : queue.Dequeue();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? TryGet(Stack<T> stack)
        {
            lock (stack)
            {
                return stack.Count == 0 ? null : stack.Pop();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPut(Queue<T> queue, T value)
        {
            lock (queue)
            {
                if (queue.Count < PoolStrategyTests.Capacity)
                {
                    queue.Enqueue(value);
                    return true;
                }
                return false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPut(Stack<T> stack, T value)
        {
            lock (stack)
            {
                if (stack.Count < PoolStrategyTests.Capacity)
                {
                    stack.Push(value);
                    return true;
                }
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? TryGet(T?[] pool)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                var tmp = Interlocked.Exchange(ref pool[i], null);
                if (tmp != null) return tmp;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPut(T?[] pool, T value)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (Interlocked.CompareExchange(ref pool[i], value, null) == null)
                    return true;
            }
            return false;
        }
    }
}
