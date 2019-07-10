using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PooledAwait.Internal
{
    internal sealed class LazyTaskState<T>
    {
        private short _version;
        private T _result;
        private Exception? _exception;
        private Task? _task;
        private bool _isComplete;
        private ValueTaskCompletionSource<T> _source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckTokenInsideLock(short token)
        {
            if (token != _version) ThrowHelper.ThrowInvalidOperationException();
        }

        public Task GetTask(short token)
        {
            lock (this)
            {
                CheckTokenInsideLock(token);
                if (_task != null) { }
                else if (_exception is OperationCanceledException) _task = TaskUtils.TaskFactory<T>.Canceled;
                else if (_exception != null) _task = TaskUtils.FromException<T>(_exception);
                else if (_isComplete) _task = typeof(T) == typeof(Nothing) ? TaskUtils.CompletedTask : TaskUtils.TaskFactory<T>.FromResult(_result);
                else
                {
                    _source = ValueTaskCompletionSource<T>.Create();
                    _task = _source.Task;
                }
                return _task;
            }
        }

        internal bool IsValid(short token) => Volatile.Read(ref _version) == token;
        internal bool HasSource
        {
            get
            {
                lock (this) { return !_source.IsNull; }
            }
        }
        internal bool HasTask => Volatile.Read(ref _task) != null;

        public bool TrySetResult(short token, T result)
        {
            lock (this)
            {
                if (_isComplete) return false;
                if (token != _version) return false;
                _isComplete = true;
                if (!_source.IsNull) return _source.TrySetResult(result);
                _result = result;
                return true;
            }
        }

        public bool TrySetException(short token, Exception exception)
        {
            lock (this)
            {
                if (_isComplete) return false;
                if (token != _version) return false;
                _isComplete = true;
                if (!_source.IsNull) return _source.TrySetException(exception);
                _exception = exception;
                return true;
            }
        }

        public bool TrySetCanceled(short token, CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                if (_isComplete) return false;
                if (token != _version) return false;
                _isComplete = true;
                if (!_source.IsNull) return _source.TrySetCanceled(cancellationToken);
                _task = TaskUtils.TaskFactory<T>.Canceled;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LazyTaskState<T> Create() => Pool<LazyTaskState<T>>.TryGet() ?? new LazyTaskState<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LazyTaskState()
        {
            Counters.LazyStateAllocated.Increment();
            _result = default!;
            _version = InitialVersion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LazyTaskState<T> CreateConstant(T value)
        {
            var obj = new LazyTaskState<T>();
            obj._version = Constant;
            obj.TrySetResult(Constant, value);
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LazyTaskState<T> CreateCanceled()
        {
            var obj = new LazyTaskState<T>();
            obj._version = Constant;
            obj.TrySetCanceled(Constant);
            return obj;
        }

        const short InitialVersion = 0, Constant = InitialVersion - 1;

        internal short Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _version;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Recycle(short token)
        {
            if (token == Constant) return; // never recycle constant values; this is by design

            if (Volatile.Read(ref _version) != token) return; // wrong version; all bets are off!

            if (!Volatile.Read(ref _isComplete)) // if incomplete, try to cancel
            {
                if (!TrySetCanceled(token)) return; // if that didn't work... give up - don't recycle
            }

            bool haveLock = false;
            try
            {
                // only if uncontested; we're not waiting in a dispose
                Monitor.TryEnter(this, ref haveLock);
                if (haveLock)
                {
                    if (token == _version)
                    {
                        _result = default!;
                        _exception = default;
                        _task = default;
                        _isComplete = false;
                        _source = default;

                        switch (++_version)
                        {
                            case InitialVersion: // don't wrap all the way around when recycling; could lead to conflicts
                            case Constant: // don't allow things to *become* constants
                                break;
                            default:
                                Pool<LazyTaskState<T>>.TryPut(this);
                                break;
                        }
                    }
                }
            }
            finally
            {
                if (haveLock) Monitor.Exit(this);
            }
        }
    }
}
