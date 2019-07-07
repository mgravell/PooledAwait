Exploration of custom awaitables; typical results:

|    Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------- |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
|      Task | 1.655 us | 0.0491 us | 0.0027 us |  1.00 | 0.0371 |     - |     - |     120 B |
| ValueTask | 1.688 us | 0.1370 us | 0.0075 us |  1.02 | 0.0410 |     - |     - |     128 B |
|  TaskLike | 1.818 us | 0.0930 us | 0.0051 us |  1.10 |      - |     - |     - |         - |

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e.

``` c#
public async Task<int> ViaTask() {...}
public async ValueTask<int> ViaValueTask() {...}
public async TaskLike<int> ViaTaskLike() {...}
```

All of them have the same threading/execution-context/sync-context semantics; there's no cheating.