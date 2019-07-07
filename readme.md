Exploration of custom awaitables; typical results:

| Method |      Categories |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------- |---------------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
|      T |            Task | 2,018.727 ns |  46.644 ns |  2.5567 ns | 0.0352 |      - |     - |     120 B |
|   void |            Task | 2,069.356 ns | 206.219 ns | 11.3036 ns | 0.0352 |      - |     - |     112 B |
|        |                 |              |            |            |        |        |       |           |
|      T |       ValueTask | 2,082.902 ns | 192.511 ns | 10.5522 ns | 0.0391 |      - |     - |     128 B |
|   void |       ValueTask |     3.797 ns |  16.979 ns |  0.9307 ns | 0.0001 | 0.0000 |     - |       1 B |
|        |                 |              |            |            |        |        |       |           |
|      T | PooledValueTask | 2,407.020 ns | 122.842 ns |  6.7334 ns |      - |      - |     - |         - |
|   void | PooledValueTask |     8.296 ns |   7.092 ns |  0.3887 ns | 0.0001 | 0.0000 |     - |         - |

(I'm not sure that I trust the `void` timings)

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e.

``` c#
public async Task<int> ViaTask() {...}
public async ValueTask<int> ViaValueTask() {...}
public async TaskLike<int> ViaTaskLike() {...}
```

All of them have the same threading/execution-context/sync-context semantics; there's no cheating.