Exploration of custom awaitables; typical results:

|          Method | Categories |         Mean |      Error |     StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------- |----------- |-------------:|-----------:|-----------:|-------:|-------:|------:|----------:|
|            Task |        int | 2,060.952 ns | 150.538 ns |  8.2515 ns | 0.0352 |      - |     - |     120 B |
|       ValueTask |        int | 2,094.100 ns | 193.854 ns | 10.6258 ns | 0.0391 |      - |     - |     128 B |
| PooledValueTask |        int | 2,410.402 ns | 350.542 ns | 19.2144 ns |      - |      - |     - |         - |
|      PooledTask |        int | 2,314.172 ns |  98.018 ns |  5.3727 ns | 0.0273 |      - |     - |      96 B |
|                 |            |              |            |            |        |        |       |           |
|            Task |       void | 2,056.287 ns |  87.165 ns |  4.7778 ns | 0.0352 |      - |     - |     112 B |
|       ValueTask |       void |     3.865 ns |   9.092 ns |  0.4984 ns | 0.0001 | 0.0000 |     - |       1 B | **
| PooledValueTask |       void |    10.496 ns |  11.156 ns |  0.6115 ns | 0.0001 | 0.0000 |     - |         - | **
|      PooledTask |       void | 2,299.826 ns | 121.711 ns |  6.6714 ns | 0.0273 |      - |     - |      96 B |

(I'm not sure that I trust the benchmarks marked `**`)

The 3 tests do the exact same thing; the only thing that changes is the return type, i.e. whether it is `async Task<int>`, `async ValueTask<int>`, `async PooledTask<int>` or `async PooledValueTask<int>`.

All of them have the same threading/execution-context/sync-context semantics; there's no cheating going on.