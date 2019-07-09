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
| Method |  Job | Runtime |   Categories |     Mean |     Error |    StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------- |----- |-------- |------------- |---------:|----------:|----------:|-------:|-------:|------:|----------:|
|   .NET |  Clr |     Clr |      Task<T> | 2.377 us | 0.0570 us | 0.0560 us | 0.0508 | 0.0039 |     - |     344 B |
| Pooled |  Clr |     Clr |      Task<T> | 2.343 us | 0.0388 us | 0.0344 us | 0.0273 | 0.0039 |     - |     182 B |
|   .NET | Core |    Core |      Task<T> | 1.726 us | 0.0217 us | 0.0203 us | 0.0176 |      - |     - |     120 B |
| Pooled | Core |    Core |      Task<T> | 1.644 us | 0.0137 us | 0.0128 us | 0.0098 |      - |     - |      72 B |
|        |      |         |              |          |           |           |        |        |       |           |
|   .NET |  Clr |     Clr |         Task | 2.386 us | 0.0464 us | 0.0749 us | 0.0508 | 0.0039 |     - |     336 B |
| Pooled |  Clr |     Clr |         Task | 2.426 us | 0.0547 us | 0.1028 us | 0.0273 | 0.0039 |     - |     182 B |
|   .NET | Core |    Core |         Task | 1.718 us | 0.0146 us | 0.0137 us | 0.0176 |      - |     - |     112 B |
| Pooled | Core |    Core |         Task | 1.614 us | 0.0072 us | 0.0064 us | 0.0098 |      - |     - |      72 B |
|        |      |         |              |          |           |           |        |        |       |           |
|   .NET |  Clr |     Clr | ValueTask<T> | 2.358 us | 0.0468 us | 0.0460 us | 0.0508 | 0.0039 |     - |     352 B |
| Pooled |  Clr |     Clr | ValueTask<T> | 2.303 us | 0.0477 us | 0.0422 us | 0.0117 | 0.0039 |     - |     101 B |
|   .NET | Core |    Core | ValueTask<T> | 1.736 us | 0.0215 us | 0.0201 us | 0.0195 |      - |     - |     128 B |
| Pooled | Core |    Core | ValueTask<T> | 1.610 us | 0.0101 us | 0.0094 us |      - |      - |     - |         - |
|        |      |         |              |          |           |           |        |        |       |           |
|   .NET |  Clr |     Clr |    ValueTask | 2.347 us | 0.0449 us | 0.0461 us | 0.0508 | 0.0039 |     - |     344 B |
| Pooled |  Clr |     Clr |    ValueTask | 2.333 us | 0.0452 us | 0.0465 us | 0.0117 | 0.0039 |     - |     100 B |
|   .NET | Core |    Core |    ValueTask | 1.655 us | 0.0092 us | 0.0086 us | 0.0176 |      - |     - |     120 B |
| Pooled | Core |    Core |    ValueTask | 1.638 us | 0.0100 us | 0.0093 us |      - |      - |     - |         - |
```

The tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is `async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.
All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.

