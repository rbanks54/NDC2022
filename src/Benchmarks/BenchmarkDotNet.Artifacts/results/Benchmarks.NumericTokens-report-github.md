``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22621
11th Gen Intel Core i7-1195G7 2.90GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.300
  [Host]     : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT
  DefaultJob : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT


```
|       Method |         input |     Mean |    Error |   StdDev |  Gen 0 | Allocated |
|------------- |-------------- |---------:|---------:|---------:|-------:|----------:|
| ValidNumbers | 57473.3458382 | 21.51 ns | 0.425 ns | 0.397 ns | 0.0038 |      24 B |
