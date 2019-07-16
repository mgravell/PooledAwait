using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Benchmark
{

    /*
    Typical results below; conclusion - Array is *only* good for PushPopOnce, but that scenario
    is already handled by the static; LinkedList was tempting before the ABA problem surfaced,
    but with the fixes for that, the performance deteriorates significantly, such that it becomes
    undesirable; we might as well pick and choose between a queue and a stack.


    | Method |  Job | Runtime |      Categories |        Mean |     Error |    StdDev | Ratio | RatioSD |
    |------- |----- |-------- |---------------- |------------:|----------:|----------:|------:|--------:|
    |  Queue |  Clr |     Clr |       TakeEmpty |    14.28 ns |  0.007 ns |  0.006 ns |  0.43 |    0.00 |
    |  Stack |  Clr |     Clr |       TakeEmpty |    14.49 ns |  0.007 ns |  0.005 ns |  0.44 |    0.00 |
    |  Array |  Clr |     Clr |       TakeEmpty |   261.74 ns |  0.209 ns |  0.185 ns |  7.96 |    0.02 |
    | Linked |  Clr |     Clr |       TakeEmpty |    32.88 ns |  0.110 ns |  0.098 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue |  Clr |     Clr |         PutFull |    14.74 ns |  0.008 ns |  0.006 ns |  0.46 |    0.00 |
    |  Stack |  Clr |     Clr |         PutFull |    14.79 ns |  0.094 ns |  0.084 ns |  0.46 |    0.00 |
    |  Array |  Clr |     Clr |         PutFull |   117.10 ns |  0.089 ns |  0.079 ns |  3.62 |    0.01 |
    | Linked |  Clr |     Clr |         PutFull |    32.38 ns |  0.113 ns |  0.100 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue |  Clr |     Clr |     PushPopOnce |    33.31 ns |  0.098 ns |  0.092 ns |  0.27 |    0.00 |
    |  Stack |  Clr |     Clr |     PushPopOnce |    32.77 ns |  0.196 ns |  0.174 ns |  0.27 |    0.00 |
    |  Array |  Clr |     Clr |     PushPopOnce |    19.88 ns |  0.099 ns |  0.093 ns |  0.16 |    0.00 |
    | Linked |  Clr |     Clr |     PushPopOnce |   122.73 ns |  0.143 ns |  0.126 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue |  Clr |     Clr |  PushPopQuarter |   838.78 ns |  1.416 ns |  1.182 ns |  0.27 |    0.00 |
    |  Stack |  Clr |     Clr |  PushPopQuarter |   824.98 ns |  4.554 ns |  3.803 ns |  0.27 |    0.00 |
    |  Array |  Clr |     Clr |  PushPopQuarter | 1,484.93 ns |  1.713 ns |  1.518 ns |  0.48 |    0.00 |
    | Linked |  Clr |     Clr |  PushPopQuarter | 3,068.15 ns |  2.910 ns |  2.722 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue |  Clr |     Clr | PushPopCapacity |   672.43 ns |  1.988 ns |  1.859 ns |  0.27 |    0.00 |
    |  Stack |  Clr |     Clr | PushPopCapacity |   661.65 ns |  2.289 ns |  2.141 ns |  0.27 |    0.00 |
    |  Array |  Clr |     Clr | PushPopCapacity | 4,289.29 ns | 10.743 ns | 10.049 ns |  1.75 |    0.01 |
    | Linked |  Clr |     Clr | PushPopCapacity | 2,456.54 ns |  4.447 ns |  3.713 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue | Core |    Core |       TakeEmpty |    17.31 ns |  0.008 ns |  0.006 ns |  0.50 |    0.00 |
    |  Stack | Core |    Core |       TakeEmpty |    17.41 ns |  0.026 ns |  0.021 ns |  0.50 |    0.00 |
    |  Array | Core |    Core |       TakeEmpty |   125.34 ns |  0.053 ns |  0.041 ns |  3.60 |    0.01 |
    | Linked | Core |    Core |       TakeEmpty |    34.80 ns |  0.104 ns |  0.093 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue | Core |    Core |         PutFull |    17.41 ns |  0.083 ns |  0.078 ns |  0.58 |    0.00 |
    |  Stack | Core |    Core |         PutFull |    17.42 ns |  0.099 ns |  0.088 ns |  0.59 |    0.00 |
    |  Array | Core |    Core |         PutFull |   117.21 ns |  0.291 ns |  0.243 ns |  3.94 |    0.02 |
    | Linked | Core |    Core |         PutFull |    29.77 ns |  0.106 ns |  0.094 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue | Core |    Core |     PushPopOnce |    42.42 ns |  0.133 ns |  0.118 ns |  0.38 |    0.00 |
    |  Stack | Core |    Core |     PushPopOnce |    41.86 ns |  0.105 ns |  0.093 ns |  0.38 |    0.00 |
    |  Array | Core |    Core |     PushPopOnce |    13.22 ns |  0.009 ns |  0.008 ns |  0.12 |    0.00 |
    | Linked | Core |    Core |     PushPopOnce |   110.80 ns |  0.705 ns |  0.660 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue | Core |    Core |  PushPopQuarter | 1,073.27 ns |  1.278 ns |  1.133 ns |  0.38 |    0.00 |
    |  Stack | Core |    Core |  PushPopQuarter | 1,054.11 ns |  2.613 ns |  2.444 ns |  0.37 |    0.00 |
    |  Array | Core |    Core |  PushPopQuarter |   961.63 ns |  2.607 ns |  2.438 ns |  0.34 |    0.00 |
    | Linked | Core |    Core |  PushPopQuarter | 2,833.23 ns |  4.159 ns |  3.890 ns |  1.00 |    0.00 |
    |        |      |         |                 |             |           |           |       |         |
    |  Queue | Core |    Core | PushPopCapacity |   857.20 ns |  2.797 ns |  2.479 ns |  0.38 |    0.00 |
    |  Stack | Core |    Core | PushPopCapacity |   824.37 ns |  2.185 ns |  1.937 ns |  0.36 |    0.00 |
    |  Array | Core |    Core | PushPopCapacity | 2,791.16 ns |  6.472 ns |  6.054 ns |  1.23 |    0.01 |
    | Linked | Core |    Core | PushPopCapacity | 2,260.40 ns |  6.156 ns |  5.758 ns |  1.00 |    0.00 |


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
        
        static readonly object s_TestObject = new object();

        const int OpsPerInvoke = 10000;

        [IterationCleanup]
        public void Reset()
        {
            while (Util<object>.TryGet(_queue) != null) { }
            while (Util<object>.TryGet(_stack) != null) { }
            while (Util<object>.TryGet(_array) != null) { }
            while (SimplifiedPool<object>.TryGet() != null) { }
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
                SimplifiedPool<object>.TryGet();
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
            while (SimplifiedPool<object>.TryPut(s_TestObject)) { }
            for (int i = 0; i < OpsPerInvoke; i++)
            {
                SimplifiedPool<object>.TryPut(s_TestObject);
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
        [Benchmark(OperationsPerInvoke = OpsPerInvoke, Description = "Stack")]
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
                SimplifiedPool<object>.TryPut(s_TestObject);
                SimplifiedPool<object>.TryGet();
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
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / QuarterCapacity, Description = "Stack")]
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
                for (int j = 0; j < QuarterCapacity; j++) SimplifiedPool<object>.TryPut(s_TestObject);
                for (int j = 0; j < QuarterCapacity; j++) SimplifiedPool<object>.TryGet();
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
        [Benchmark(OperationsPerInvoke = OpsPerInvoke / Capacity, Description = "Stack")]
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
                for (int j = 0; j < Capacity; j++) SimplifiedPool<object>.TryPut(s_TestObject);
                for (int j = 0; j < Capacity; j++) SimplifiedPool<object>.TryGet();
            }
        }


    }

    static class SimplifiedPool<T> where T : class
    {
        private static Node? _spares = Create(PoolStrategyTests.Capacity), _head;

        public static T? TryGet()
        {
            Node? taken = PopNode(ref _head);
            if (taken == null) return null;
            var value = taken.Value;
            taken.Value = null;
            PushNode(ref _spares, taken);
            return value;
        }

        public static bool TryPut(T value)
        {
            Node? taken = PopNode(ref _spares);
            if (taken == null) return false;
            taken.Value = value;
            PushNode(ref _head, taken);
            return true;
        }

        private static readonly Node s_BusySentinel = new Node();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Node? TakeBySpinning(ref Node? field)
        {
            SpinWait spinner = default;
            Node? taken;
            do
            {
                spinner.SpinOnce();
                taken = Interlocked.Exchange(ref field, s_BusySentinel);
            } while (ReferenceEquals(taken, s_BusySentinel));
            return taken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Node? PopNode(ref Node? field)
        {
            var head = Interlocked.Exchange(ref field, s_BusySentinel);
            if (ReferenceEquals(head, s_BusySentinel))
            {
                // it was already busy (the exchange was a no-op)
                head = TakeBySpinning(ref field);
            }

            // so we detached and marked it busy; nobody else
            // should be messing, so we can just swap it back in
            Interlocked.Exchange(ref field, head?.Tail);
            if (head != null) head.Tail = null;
            return head;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PushNode(ref Node? field, Node node)
        {
            var head = Interlocked.Exchange(ref field, s_BusySentinel);
            if (ReferenceEquals(head, s_BusySentinel))
            {
                // it was already busy (the exchange was a no-op)
                head = TakeBySpinning(ref field);
            }

            // so we detached and marked it busy; nobody else
            // should be messing, so we can just swap it back in
            node.Tail = head;
            Interlocked.Exchange(ref field, node);
        }

        private static Node? Create(int count)
        {
            Node? head = null;
            for (int i = 0; i < count; i++)
            {
                var newNode = new Node
                {
                    Tail = head
                };
                head = newNode;
            }
            return head;
        }

        sealed class Node
        {
            public T? Value;
            public Node? Tail;
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
