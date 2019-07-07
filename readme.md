Exploration of custom awaitables; typical results:

|          Method | Categories |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------- |----------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
|            Task |        int | 2,040.413 ns |  77.341 ns |  4.2393 ns | 0.0352 |      - |     - |     120 B |
|       ValueTask |        int | 2,070.715 ns | 231.437 ns | 12.6858 ns | 0.0391 |      - |     - |     128 B |
| PooledValueTask |        int | 2,332.105 ns |  46.500 ns |  2.5488 ns |      - |      - |     - |         - |
|      PooledTask |        int | 2,340.138 ns |  30.751 ns |  1.6856 ns | 0.0273 |      - |     - |      96 B |
|                 |            |              |            |            |        |        |       |           |
|            Task |       void | 2,066.650 ns |  56.378 ns |  3.0903 ns | 0.0352 |      - |     - |     112 B |
|       ValueTask |       void |     3.000 ns |   9.942 ns |  0.5449 ns | 0.0001 | 0.0000 |     - |       1 B | **
| PooledValueTask |       void |     8.894 ns |  11.079 ns |  0.6073 ns | 0.0001 | 0.0000 |     - |       1 B | **
|      PooledTask |       void | 2,263.041 ns | 621.058 ns | 34.0423 ns | 0.0273 |      - |     - |      96 B |

(I'm not sure that I trust the benchmarks marked `**`)

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e.

``` c#
public async Task<int> ViaTask() {...}
public async ValueTask<int> ViaValueTask() {...}
public async TaskLike<int> ViaTaskLike() {...}
```

All of them have the same threading/execution-context/sync-context semantics; there's no cheating.