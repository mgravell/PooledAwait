[Documentation](https://mgravell.github.io/PooledAwait/)

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

Based on an operation that uses `Task.Yield()` to ensure that the operations are incomplete; ".NET" means the inbuilt out-of-the box implementation; "Pooled" means the implementation from this library.

In particular, notice:

- zero allocations for `PooledValueTask[<T>]` vs `ValueTask[<T>]` (on .NET Core; *significantly reduced* on .NET Framework)
- *reduced* allocations for `PooledTask[<T>]` vs `Task[<T>]`
- no performance degredation; just lower allocations

``` txt
| Method |  Job | Runtime |   Categories |     Mean |     Error |    StdDev |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------- |----- |-------- |------------- |---------:|----------:|----------:|-------:|-------:|-------:|----------:|
|   .NET |  Clr |     Clr |      Task<T> | 2.159 us | 0.0427 us | 0.0474 us | 0.0508 | 0.0039 |      - |     344 B |
| Pooled |  Clr |     Clr |      Task<T> | 2.037 us | 0.0246 us | 0.0230 us | 0.0273 | 0.0039 |      - |     182 B |
|   .NET | Core |    Core |      Task<T> | 1.397 us | 0.0024 us | 0.0022 us | 0.0176 |      - |      - |     120 B |
| Pooled | Core |    Core |      Task<T> | 1.349 us | 0.0058 us | 0.0054 us | 0.0098 |      - |      - |      72 B |
|        |      |         |              |          |           |           |        |        |        |           |
|   .NET |  Clr |     Clr |         Task | 2.065 us | 0.0200 us | 0.0167 us | 0.0508 | 0.0039 |      - |     336 B |
| Pooled |  Clr |     Clr |         Task | 1.979 us | 0.0179 us | 0.0167 us | 0.0273 | 0.0039 |      - |     182 B |
|   .NET | Core |    Core |         Task | 1.390 us | 0.0159 us | 0.0149 us | 0.0176 |      - |      - |     112 B |
| Pooled | Core |    Core |         Task | 1.361 us | 0.0055 us | 0.0051 us | 0.0098 |      - |      - |      72 B |
|        |      |         |              |          |           |           |        |        |        |           |
|   .NET |  Clr |     Clr | ValueTask<T> | 2.087 us | 0.0403 us | 0.0431 us | 0.0547 | 0.0078 | 0.0039 |     352 B |
| Pooled |  Clr |     Clr | ValueTask<T> | 1.924 us | 0.0248 us | 0.0220 us | 0.0137 | 0.0020 |      - |     100 B |
|   .NET | Core |    Core | ValueTask<T> | 1.405 us | 0.0078 us | 0.0073 us | 0.0195 |      - |      - |     128 B |
| Pooled | Core |    Core | ValueTask<T> | 1.374 us | 0.0116 us | 0.0109 us |      - |      - |      - |         - |
|        |      |         |              |          |           |           |        |        |        |           |
|   .NET |  Clr |     Clr |    ValueTask | 2.056 us | 0.0206 us | 0.0183 us | 0.0508 | 0.0039 |      - |     344 B |
| Pooled |  Clr |     Clr |    ValueTask | 1.948 us | 0.0388 us | 0.0416 us | 0.0137 | 0.0020 |      - |     100 B |
|   .NET | Core |    Core |    ValueTask | 1.408 us | 0.0140 us | 0.0117 us | 0.0176 |      - |      - |     120 B |
| Pooled | Core |    Core |    ValueTask | 1.366 us | 0.0039 us | 0.0034 us |      - |      - |      - |         - |
```

Note that *most* of the remaining allocations are actually the work-queue internals of `Task.Yield()` (i.e. how
`ThreadPool.QueueUserWorkItem` works) - we've removed virtually all of the unnecessary overheads that came from the
`async` machinery. Most real-world scenarios aren't using `Task.Yield()` - they are waiting on external data, etc - so
they won't see these. Plus they are effectively zero on .NET Core 3.

The tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is
`async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.
All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.

