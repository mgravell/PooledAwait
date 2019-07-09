# PooledAwait

Low-allocation utilies for writing `async` methods, and related tools

### Contents

- [`PooledValueTask` / `PooledValueTask<T>`](#pooledvaluetask--pooledvaluetaskt)
- [`FireAndForget`](#fireandforget)
- [`ValueTaskCompletionSource<T>`](#valuetaskcompletionsourcet)
- [`PooledValueTaskSource / PooledValueTaskSource<T>`](#pooledvaluetasksource--pooledvaluetasksourcet)
- [`Pool`](#pool)

---

## `PooledValueTask` / `PooledValueTask<T>`

These are the main tools of the library; their purpose is to remove the boxing of the async state-machine and builder that happens when a method
marked `async` performs an `await` on an awaitable target that *is not yet complete*, i.e.

``` c#
async ValueTask<int> SomeMethod()
{
	await Task.Yield(); // *is not yet complete*
	return 42
}
```

If you've ever looked in a profiler and seen things like `System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.AsyncStateMachineBox`1`
or `YourLib.<<SomeMethod>g__Inner|8_0>d`, then that's what I'm talking about. You can avoid this by simply using a different return type:

- `PooledValueTask<T>` instead of `ValueTask<T>`
- `PooledValueTask` instead of `ValueTask`
- `PooledTask<T>` instead of `Task<T>` (this still has to allocate a `Task<T>`)
- `PooledTask` instead of `Task` (this still has to allocate a `Task` if the operation faults or doesn't complete synchronously)

For `private` / `internal` methods, you can probably just *change the return type directly*:

``` c#
private async PooledValueTask<int> SomeMethod()
{
	await Task.Yield(); // *is not yet complete*
	return 42
}
```

For methods on your `public` API surface, you can use a "local function" to achieve the same thing without changing the exposed return type:

``` c#
private ValueTask<int> SomeMethod() // not marked async
{
	return Impl();
	async PooledValueTask<int>() Impl()
	{
		await Task.Yield(); // *is not yet complete*
		return 42
	}
}
```

(all of the `Pooled*` types have `implicit` conversion operators to their more well-recognized brethren).

And that's it! That's all you have to do. The "catch" (there's always a catch) is that the following **no longer works** for
the `ValueTask[<T>]` versions (it stays working for the `Task[<T>]` versions):

``` c#
var pending = SomeIncompleteMethodAsync(); // note no "await" here

var x = await pending;
var y = await pending; // await the **same result**
```

In reality, **this almost never happens**. Usually you `await` something *once*, *almost always* right away. So... yeah.

---

## `FireAndForget`

Ever find yourself needing a fire-and-forget API? This adds one. All you do is declare the return type as `FireAndForget`:

``` c#
FireAndForget SomeMethod(...) {
   // .. things before the first incomplete await happen on the calling thread
   await SomeIncompleteMethod();
   // .. other bits continue running in the background
}
```

As soon as the method uses `await` against an incomplete operation, the calling
task regains control as though it were complete; the rest of the operation continues in the background. The caller can simply `await`
the fire-and-forget method with confidence that it only runs synchronously to the first incomplete operation. If you're not in an `async`
method, you can use "discard" to tell the compiler not to tell you to `await` it:

``` c#
_ = SomeFireAndForgetMethodAsync();
```

You won't get unobserved-task-exception problems. If you want to see any exceptions that happen, there is an event `FireAndForget.Exception`
that you can subscribe to. Otherwise, they just evaporate.

---

## `ValueTaskCompletionSource<T>`

Do you make use of `TaskCompletionSource<T>`? Do you hate that this adds another allocation *on top of* the `Task<T>` that you actually wanted?
`ValueTaskCompletionSource<T>` is your friend. It uses smoke and magic to work like `TaskCompletionSource<T>`, but without the extra
allocation (unless it discovers that the magic isn't working for your system). Usage:

``` c#
var source = ValueTaskCompletionSource<int>.Create();
// ...
source.TrySetResult(42); // etc
```

The main difference here is that you now have a `struct` instead of a `class`. If you want to test whether an instance is a *real* value
(as opposed to the `default`), check `.HasTask`.

---

## `PooledValueTaskSource` / `PooledValueTaskSource<T>`

These again work like `TaskCompletionSource<T>`, but a: for `ValueType[<T>]`, and b: with the same zero-allocation features that
`PooledValueTask` / `PooledValueTask<T>` exhibit. Once again, the "catch" is that you can only await their `.Task` *once*. Usage:

``` c#
var source = PooledValueTaskSource<int>.Create();
// ...
source.TrySetResult(42); // etc
```

---

## `Pool`

Ever need a light-weight basic pool of objects? That's this. Nothing fancy. The first API is a simple get/put:

``` c#
var obj = Pool.TryRent<SomeType>() ?? new SomeType();
// ...
Pool.Return(obj);
```

Note that it leaves creation to you (hence the `?? new SomeType()`), and it is the caller's responsibility to not retain and access
a reference object that you have notionally returned to the pool.

Considerations:

- you may wish to use `try`/`finally` to put things back into the pool even if you leave through failure
- if the object might **unnecessarily** keep large graphs of sub-objects "reachable" (in terms of GC), you should ensure that any references are wiped before putting an object into the pool
- if the object implements `IResettable`, the pool will automatically call the `Reset()` method for you before storing items in the pool

A second API is exposed for use with value-types; there are a lot of scenarios in which you have some state that you need to expose
to an API that takes `object` - especially with callbacks like `WaitCallback`, `SendOrPostCallback`, `Action<object>`, etc. The data
will only be unboxed once at the receiver - so: rather than use a *regular* box, we can *rent* a box. Also, if you have multiple items of
state that you need to convey - consider a value-tuple.

``` c#
int id = ...
string name = ...
var obj = Pool.Box((id, name));
// ... probably pass obj to a callback-API
```

then later:

``` c#
(var id, var name) = Pool.UnboxAndReturn<(int, string)>(obj);
// use id/name as usual
```

It is the caller's responsibility to only access the state once.


