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

``` txt
|          Method | Categories | ConfigureAwait |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |----------- |--------------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|            Task |        int |          False | 1.729 us | 0.2741 us | 0.0150 us | 0.0176 |     - |     - |     120 B |
|       ValueTask |        int |          False | 1.706 us | 0.1328 us | 0.0073 us | 0.0195 |     - |     - |     128 B |
| PooledValueTask |        int |          False | 1.608 us | 0.2610 us | 0.0143 us |      - |     - |     - |         - |
|      PooledTask |        int |          False | 1.618 us | 0.0688 us | 0.0038 us | 0.0098 |     - |     - |      72 B |
|            Task |        int |           True | 1.725 us | 0.3130 us | 0.0172 us | 0.0176 |     - |     - |     120 B |
|       ValueTask |        int |           True | 1.689 us | 0.2841 us | 0.0156 us | 0.0195 |     - |     - |     128 B |
| PooledValueTask |        int |           True | 1.648 us | 0.2475 us | 0.0136 us |      - |     - |     - |         - |
|      PooledTask |        int |           True | 1.607 us | 0.1426 us | 0.0078 us | 0.0098 |     - |     - |      72 B |
|                 |            |                |          |           |           |        |       |       |           |
|            Task |       void |          False | 1.666 us | 0.0237 us | 0.0013 us | 0.0176 |     - |     - |     112 B |
|       ValueTask |       void |          False | 1.695 us | 0.3073 us | 0.0168 us | 0.0176 |     - |     - |     120 B |
| PooledValueTask |       void |          False | 1.648 us | 0.1708 us | 0.0094 us |      - |     - |     - |         - |
|      PooledTask |       void |          False | 1.654 us | 0.2729 us | 0.0150 us | 0.0098 |     - |     - |      72 B |
|            Task |       void |           True | 1.645 us | 1.7159 us | 0.0941 us | 0.0176 |     - |     - |     112 B |
|       ValueTask |       void |           True | 1.678 us | 0.0929 us | 0.0051 us | 0.0176 |     - |     - |     120 B |
| PooledValueTask |       void |           True | 1.666 us | 0.2093 us | 0.0115 us |      - |     - |     - |         - |
|      PooledTask |       void |           True | 1.620 us | 0.0583 us | 0.0032 us | 0.0098 |     - |     - |      72 B |
```

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is `async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.

All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.

