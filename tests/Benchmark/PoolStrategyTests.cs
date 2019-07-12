using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Benchmark
{

    /*
     * Typical results below; conclusion - Array is *only* good for PushPopOnce, but that scenario
     * is already handled by the static; Array is also *teriible* at the TakeEmpty and PutFull
     * scenarios, which are both key scenarios in a busy pool, and even PuspPopCapacity is "bad".
     * 
     * The best overall performer is the linked-list, so: let's
     * use that as our impl
     
| Method |  Job | Runtime |      Categories |         Mean |      Error |     StdDev |       Median | Ratio | RatioSD |
|------- |----- |-------- |---------------- |-------------:|-----------:|-----------:|-------------:|------:|--------:|
|  Queue |  Clr |     Clr |       TakeEmpty |    14.504 ns |  0.0159 ns |  0.0124 ns |    14.500 ns |  4.55 |    0.09 |
|  Stack |  Clr |     Clr |       TakeEmpty |    14.493 ns |  0.0174 ns |  0.0136 ns |    14.490 ns |  4.55 |    0.09 |
|  Array |  Clr |     Clr |       TakeEmpty |   262.634 ns |  1.3934 ns |  1.3034 ns |   261.810 ns | 82.38 |    1.79 |
| Linked |  Clr |     Clr |       TakeEmpty |     3.189 ns |  0.0592 ns |  0.0554 ns |     3.200 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue |  Clr |     Clr |         PutFull |    14.742 ns |  0.0128 ns |  0.0107 ns |    14.740 ns |  9.18 |    0.03 |
|  Stack |  Clr |     Clr |         PutFull |    14.787 ns |  0.0897 ns |  0.0796 ns |    14.750 ns |  9.21 |    0.06 |
|  Array |  Clr |     Clr |         PutFull |   116.343 ns |  0.0954 ns |  0.0796 ns |   116.340 ns | 72.44 |    0.22 |
| Linked |  Clr |     Clr |         PutFull |     1.606 ns |  0.0054 ns |  0.0051 ns |     1.610 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue |  Clr |     Clr |     PushPopOnce |    33.335 ns |  0.1011 ns |  0.0946 ns |    33.280 ns |  1.08 |    0.00 |
|  Queue |  Clr |     Clr |     PushPopOnce |    32.726 ns |  0.1084 ns |  0.0961 ns |    32.695 ns |  1.06 |    0.00 |
|  Array |  Clr |     Clr |     PushPopOnce |    19.873 ns |  0.0817 ns |  0.0764 ns |    19.830 ns |  0.64 |    0.00 |
| Linked |  Clr |     Clr |     PushPopOnce |    30.979 ns |  0.0846 ns |  0.0750 ns |    30.980 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue |  Clr |     Clr | PushPopCapacity |   671.571 ns |  3.3753 ns |  2.9922 ns |   670.200 ns |  1.14 |    0.01 |
|  Queue |  Clr |     Clr | PushPopCapacity |   661.440 ns |  2.6375 ns |  2.4672 ns |   660.000 ns |  1.13 |    0.01 |
|  Array |  Clr |     Clr | PushPopCapacity | 4,285.657 ns |  4.1265 ns |  3.6580 ns | 4,285.100 ns |  7.30 |    0.02 |
| Linked |  Clr |     Clr | PushPopCapacity |   587.062 ns |  2.2624 ns |  1.8892 ns |   586.400 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue | Core |    Core |       TakeEmpty |    17.407 ns |  0.0165 ns |  0.0129 ns |    17.405 ns |  5.43 |    0.01 |
|  Stack | Core |    Core |       TakeEmpty |    17.317 ns |  0.0124 ns |  0.0097 ns |    17.320 ns |  5.41 |    0.01 |
|  Array | Core |    Core |       TakeEmpty |   125.818 ns |  0.7502 ns |  0.7017 ns |   125.340 ns | 39.25 |    0.18 |
| Linked | Core |    Core |       TakeEmpty |     3.203 ns |  0.0090 ns |  0.0075 ns |     3.200 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue | Core |    Core |         PutFull |    17.355 ns |  0.0208 ns |  0.0162 ns |    17.360 ns |  9.59 |    0.02 |
|  Stack | Core |    Core |         PutFull |    17.389 ns |  0.0139 ns |  0.0108 ns |    17.390 ns |  9.61 |    0.02 |
|  Array | Core |    Core |         PutFull |   118.301 ns |  0.1272 ns |  0.0993 ns |   118.250 ns | 65.39 |    0.11 |
| Linked | Core |    Core |         PutFull |     1.809 ns |  0.0030 ns |  0.0027 ns |     1.810 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue | Core |    Core |     PushPopOnce |    43.743 ns |  0.1115 ns |  0.0931 ns |    43.690 ns |  1.44 |    0.01 |
|  Queue | Core |    Core |     PushPopOnce |    41.309 ns |  0.0992 ns |  0.0928 ns |    41.280 ns |  1.36 |    0.01 |
|  Array | Core |    Core |     PushPopOnce |    13.209 ns |  0.0103 ns |  0.0086 ns |    13.210 ns |  0.44 |    0.00 |
| Linked | Core |    Core |     PushPopOnce |    30.301 ns |  0.1166 ns |  0.1034 ns |    30.275 ns |  1.00 |    0.00 |
|        |      |         |                 |              |            |            |              |       |         |
|  Queue | Core |    Core | PushPopCapacity |   846.071 ns |  2.5055 ns |  2.2210 ns |   846.100 ns |  1.17 |    0.00 |
|  Queue | Core |    Core | PushPopCapacity |   825.586 ns |  3.0404 ns |  2.6953 ns |   824.100 ns |  1.14 |    0.00 |
|  Array | Core |    Core | PushPopCapacity | 2,802.560 ns | 32.8024 ns | 30.6834 ns | 2,801.800 ns |  3.88 |    0.05 |
| Linked | Core |    Core | PushPopCapacity |   721.720 ns |  2.2538 ns |  2.1082 ns |   723.000 ns |  1.00 |    0.00 |


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
                var newNode = new Node<T>();
                newNode.Tail = head;
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
