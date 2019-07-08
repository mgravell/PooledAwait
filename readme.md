# What is this?

You know how `async` methods that `await` something **incomplete** end up creating a few objects, right? There's
the boxed state machine, an `Action` that moves it forward, a `Task[<T>]`, etc - right?

Well... what about if there just **wasn't**?

And what if all you had to do was change your `async ValueTask<int>` method to `async PooledValueTask<int>`?

And I hear you; you're saying "but I can't change the public API!". But what if a `PooledValueTask<int>` really *was*
a `ValueTask<int>`? So you can just cheat:

``` c#
public ValueTask<int> DoTheThing() // the outer method is not async
{
	return ReallyDoTheThing();
	async PooledValueTask<int> ReallyDoTheThing()
	{
		... await ...

		... return ...
	}
}
```

And how about if maybe just maybe in the future it could be ([if this happens](https://github.com/dotnet/csharplang/issues/1407)) just:

``` c#
[SomeKindOfAttribute] // <=== this is the only change
public async ValueTask<int> DoTheThing()
{
	// no changes here at all
}
```

Would that be awesome? Because that's what this is!

# How does that work?

The `PooledValueTask[<T>]` etc exist mostly to define a custom **builder**. The builder in this library uses aggressive pooling of classes
that replace the *boxed* approach used by default; we recycle them when the state machine completes.

It also makes use of the `IValueTaskSource[<T>]` API to allow incomplete operations to be represented without a `Task[<T>]`, but with a custom backer.
And we pool that too, recycling it when the task is *awaited*. The only downside: you can't `await` the same result *twice* now, because
once you've awaited it the first time, **it has gone**. A cycling token is used to make sure you can't accidentally read the incorrect
values after the result has been awaited.

We can *even* do this for `Task[<T>]`, except here we can only avoid the boxed state machine; hence `PooledTask[<T>]` exists too. No custom backing in this case, though, since a `Task[<T>]` will
need to be allocated (except for `Task.CompletedTask`, which we special-case).

# Test results

Based on an operation that uses `Task.Yield()` to ensure that the operations are incomplete. The thing to note is the zero
allocations for `PooledValueTask<int>` and `PooledValueTask`.

Disclosure: There's something wrong with 2 results; I haven't got to why yet.

|          Method | Categories |         Mean |      Error |     StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------- |----------- |-------------:|-----------:|-----------:|------:|-------:|-------:|------:|----------:|
|            Task |        int | 1,709.080 ns | 202.236 ns | 11.0852 ns |  1.00 | 0.0176 |      - |     - |     120 B |
|       ValueTask |        int | 1,710.595 ns |  42.522 ns |  2.3308 ns |  1.00 | 0.0195 |      - |     - |     128 B |
| PooledValueTask |        int | 1,611.769 ns |  41.162 ns |  2.2562 ns |  0.94 |      - |      - |     - |         - |
|      PooledTask |        int | 1,634.652 ns | 110.288 ns |  6.0452 ns |  0.96 | 0.0098 |      - |     - |      72 B |
|                 |            |              |            |            |       |        |        |       |           |
|            Task |       void | 1,708.674 ns | 277.749 ns | 15.2244 ns | 1.000 | 0.0176 |      - |     - |     112 B |
|       ValueTask |       void |     5.827 ns |  21.813 ns |  1.1957 ns | 0.003 | 0.0002 | 0.0001 |     - |       2 B |
| PooledValueTask |       void |     6.042 ns |   8.510 ns |  0.4665 ns | 0.004 | 0.0000 | 0.0000 |     - |       1 B |
|      PooledTask |       void | 1,652.051 ns | 105.278 ns |  5.7706 ns | 0.967 | 0.0098 |      - |     - |      72 B |

(I'm not sure that I trust the benchmarks marked †)

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is `async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.

All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.

