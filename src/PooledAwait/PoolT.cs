﻿using PooledAwait.Internal;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PooledAwait
{
    /// <summary>
    /// A general-purpose pool of object references; it is the caller's responsibility
    /// to ensure that overlapped usage does not occur
    /// </summary>
    internal static class Pool<T> where T : class
    {
        internal static readonly int Size = CalculateSize();
        private static Node? _liveNodes;
        private static Node? _spareNodes = Node.Create(Size);

        [ThreadStatic]
        private static T? ts_local;

        static int CalculateSize()
        {
            const int DefaultSize = 16;
            int size = DefaultSize;

            var type = typeof(T);
            if (type == typeof(object)) ThrowHelper.ThrowInvalidOperationException("Pool<Object> is not supported; please use a more specific type");

#if !NETSTANDARD1_3
            const int MinSize = 0, MaxSize = 256;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pool.ItemBox<>))
            {   // if doing a boxed T, tell us about the T - not the box
                type = type.GetGenericArguments()[0];
            }

            var attrib = (PoolSizeAttribute)Attribute.GetCustomAttribute(type, typeof(PoolSizeAttribute), true);
            if (attrib != null)
            {
                if (attrib.Size < MinSize) size = MinSize;
                else if (attrib.Size > MaxSize) size = MaxSize;
                else size = attrib.Size;
            }
#endif
            return size;
        }

        internal static int Count(out int empty, out int withValues)
        {
            empty = Node.Length(ref _spareNodes);
            withValues = Node.Length(ref _liveNodes);
            return empty + withValues;
        }

        /// <summary>
        /// Gets an instance from the pool if possible
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? TryGet()
        {
            var tmp = ts_local;
            ts_local = null;
            return tmp ?? FromPool();

            static T? FromPool()
            {
                var taken = Node.Pop(ref _liveNodes);
                if (taken == null) return null;
#if DEBUG
                taken.DebugMarkInactive();
#endif

                var value = taken.Value;
                taken.Value = null;
                Node.Push(ref _spareNodes, taken);
                return value;
            }
        }

        /// <summary>
        /// Puts an instance back into the pool
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TryPut(T value)
        {
            if (value != null)
            {
                if (ts_local == null)
                {
                    ts_local = value;
                    return;
                }
                ToPool(value);
            }
            static void ToPool(T value)
            {
                var taken = Node.Pop(ref _spareNodes);
                if (taken == null) return;
#if DEBUG
                taken.DebugMarkActive();
#endif
                taken.Value = value;
                Node.Push(ref _liveNodes, taken);
            }
        }

        private sealed class Node
        {
#if DEBUG
            [Conditional("DEBUG")]
            internal void DebugMarkActive()
            {

                int actual = Interlocked.CompareExchange(ref ActiveCount, 1, 0);
                if (actual != 0) throw new InvalidOperationException($"failed to mark active; expected 0, got {actual}");
            }
            [Conditional("DEBUG")]
            internal void DebugMarkInactive()
            {

                int actual = Interlocked.CompareExchange(ref ActiveCount, 0, 1);
                if (actual != 1) throw new InvalidOperationException($"failed to mark inactive; expected 1, got {actual}");
            }
            int ActiveCount;
#endif
            public T? Value;
            public volatile Node? Tail;

            private static readonly Node s_BusySentinel = new Node();


            [MethodImpl(MethodImplOptions.NoInlining)]
            static void SpinUntilFree(ref Node? field)
            {
                SpinWait spinner = default;
                do
                {
                    spinner.SpinOnce();
                } while (ReferenceEquals(Volatile.Read(ref field), s_BusySentinel));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static Node? Pop(ref Node? field)
            {
                while (true)
                {
                    var head = Interlocked.Exchange(ref field, s_BusySentinel);
                    if (ReferenceEquals(head, s_BusySentinel))
                    {
                        // it was already busy (the exchange was a no-op)
                        SpinUntilFree(ref field);
                    }
                    else
                    {
                        // so we detached and marked it busy; nobody else
                        // should be messing, so we can just swap it back in
                        Interlocked.Exchange(ref field, head?.Tail);
                        if (head != null) head.Tail = null;
                        return head;
                    }
                }
            }
            static readonly object sLock = new object();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void Push(ref Node? field, Node node)
            {
                Debug.Assert(node != null);
                Debug.Assert(node.Tail == null);
                while (true)
                {
                    var head = Interlocked.Exchange(ref field, s_BusySentinel);
                    if (ReferenceEquals(head, s_BusySentinel))
                    {
                        // it was already busy (the exchange was a no-op)
                        SpinUntilFree(ref field);
                    }
                    else
                    {
                        // so we detached and marked it busy; nobody else
                        // should be messing, so we can just swap it back in
                        node.Tail = head;
                        Interlocked.Exchange(ref field, node);
                        return;
                    }
                }
            }

            internal static Node? Create(int count)
            {
                Node? head = null;
                for (int i = 0; i < count; i++)
                {
                    var newNode = new Node();
                    newNode.Tail = head;
                    head = newNode;
                }
                return head;
            }

            internal static int Length(ref Node? field)
            {
                int count = 0;
                var node = Volatile.Read(ref field);
                while (node != null)
                {
                    count++;
                    node = node.Tail;
                }
                return count;
            }
        }
    }

}
