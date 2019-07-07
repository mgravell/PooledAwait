# What is this?

You know how `async` methods that `await` something **incomplete** end up creating a few objects, right? There's
the boxed state machine, an `Action` that moves it forward, a `Task[<T>]`, etc - right?

Well... what about if there just **wasn't**?

And what if all you had to do was change your `async ValueTask<int>` method to `async PooledValueTask<int>`?

And I here you; you're saying "but I can't change the public API!". But what if a `PooledValueTask<int>` really *was*
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

We can *even* do this for `Task[<T>]`, except here we can only avoid the boxed state machine (and maybe in the future we can lose the
`TaskCompletionSource[<T>]`); hence `PooledTask[<T>]` exists too. No custom backing in this case, though, since a `Task[<T>]` will
need to be allocated (except for `Task.CompletedTask`, which we special-case).

# Test results

Based on an operation that uses `Task.Yield()` to ensure that the operations are incomplete. The thing to note is the zero
allocations for `PooledValueTask<int>` and `PooledValueTask`.

Disclosure: There's something wrong with 2 results; I haven't got to why yet.

|          Method | Categories |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------- |----------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
|            Task |        int | 2,060.952 ns | 150.538 ns |  8.2515 ns | 0.0352 |      - |     - |     120 B |
|       ValueTask |        int | 2,094.100 ns | 193.854 ns | 10.6258 ns | 0.0391 |      - |     - |     128 B |
| PooledValueTask |        int | 2,410.402 ns | 350.542 ns | 19.2144 ns |      - |      - |     - |         - |
|      PooledTask |        int | 2,314.172 ns |  98.018 ns |  5.3727 ns | 0.0273 |      - |     - |      96 B |
|                 |            |              |            |            |        |        |       |           |
|            Task |       void | 2,056.287 ns |  87.165 ns |  4.7778 ns | 0.0352 |      - |     - |     112 B |
|       ValueTask |     † void |     3.865 ns |   9.092 ns |  0.4984 ns | 0.0001 | 0.0000 |     - |       1 B |
| PooledValueTask |     † void |    10.496 ns |  11.156 ns |  0.6115 ns | 0.0001 | 0.0000 |     - |         - |
|      PooledTask |       void | 2,299.826 ns | 121.711 ns |  6.6714 ns | 0.0273 |      - |     - |      96 B |

(I'm not sure that I trust the benchmarks marked †)

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is `async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.

All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.

