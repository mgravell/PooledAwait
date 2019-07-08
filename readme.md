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

Based on an operation that uses `Task.Yield()` to ensure that the operations are incomplete; legend:

- `BCL` means the inbuilt out-of-the box implementation
- `Pooled` means the implementation from this library
- `/T` means the generic version with a result, i.e. `Task<T>` instead of `Task`

- zero allocations for `PooledValueTask[<T>]` vs `ValueTask[<T>]`
- *reduced* allocations for `PooledTask[<T>]` vs `Task[<T>]`

``` txt
|   Method | Categories |     Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------- |----------- |---------:|----------:|----------:|-------:|------:|------:|----------:|
|   .NET/T |       Task | 1.769 us | 0.3167 us | 0.0174 us | 0.0176 |     - |     - |     120 B |
|     .NET |       Task | 1.723 us | 0.1026 us | 0.0056 us | 0.0176 |     - |     - |     112 B |
| Pooled/T |       Task | 1.626 us | 0.2665 us | 0.0146 us | 0.0098 |     - |     - |      72 B |
|   Pooled |       Task | 1.625 us | 0.1137 us | 0.0062 us | 0.0098 |     - |     - |      72 B |
|          |            |          |           |           |        |       |       |           |
|   .NET/T |  ValueTask | 1.730 us | 0.0278 us | 0.0015 us | 0.0195 |     - |     - |     128 B |
|     .NET |  ValueTask | 1.681 us | 0.0502 us | 0.0028 us | 0.0176 |     - |     - |     120 B |
| Pooled/T |  ValueTask | 1.628 us | 0.1242 us | 0.0068 us |      - |     - |     - |         - |
|   Pooled |  ValueTask | 1.643 us | 0.0885 us | 0.0049 us |      - |     - |     - |         - |
```

The tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is `async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.
All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.

