``` ini

BenchmarkDotNet=v0.13.1, OS=arch 
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.403
  [Host]     : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT


```
|                             Method | CollectionSize |       Mean |   Error |  StdDev |
|----------------------------------- |--------------- |-----------:|--------:|--------:|
|                         **AddHashMap** |           **1000** |   **237.0 ns** | **1.28 ns** | **1.14 ns** |
|                     AddToFSharpMap |           1000 |   205.3 ns | 0.83 ns | 0.77 ns |
|             AddToFSharpAdaptiveMap |           1000 |   195.7 ns | 1.33 ns | 1.18 ns |
|                    AddToFSharpXMap |           1000 |   498.8 ns | 3.67 ns | 3.43 ns |
| AddToSystemCollectionsImmutableMap |           1000 |   514.7 ns | 4.02 ns | 3.76 ns |
|               AddToFsharpxChampMap |           1000 |   439.0 ns | 1.85 ns | 1.73 ns |
|                    AddToLangExtMap |           1000 |   225.1 ns | 0.80 ns | 0.75 ns |
|                         **AddHashMap** |         **100000** |   **396.6 ns** | **2.01 ns** | **1.88 ns** |
|                     AddToFSharpMap |         100000 |   350.2 ns | 2.50 ns | 2.09 ns |
|             AddToFSharpAdaptiveMap |         100000 |   316.6 ns | 2.87 ns | 2.69 ns |
|                    AddToFSharpXMap |         100000 |   682.4 ns | 2.86 ns | 2.54 ns |
| AddToSystemCollectionsImmutableMap |         100000 |   826.1 ns | 6.99 ns | 6.54 ns |
|               AddToFsharpxChampMap |         100000 |   485.8 ns | 2.15 ns | 2.01 ns |
|                    AddToLangExtMap |         100000 |   275.3 ns | 2.32 ns | 2.17 ns |
|                         **AddHashMap** |         **500000** |   **448.0 ns** | **2.63 ns** | **2.46 ns** |
|                     AddToFSharpMap |         500000 |   394.4 ns | 2.87 ns | 2.54 ns |
|             AddToFSharpAdaptiveMap |         500000 |   374.1 ns | 2.14 ns | 1.89 ns |
|                    AddToFSharpXMap |         500000 |   804.0 ns | 4.96 ns | 4.64 ns |
| AddToSystemCollectionsImmutableMap |         500000 | 1,017.6 ns | 7.87 ns | 7.36 ns |
|               AddToFsharpxChampMap |         500000 |   570.5 ns | 3.26 ns | 3.05 ns |
|                    AddToLangExtMap |         500000 |   338.7 ns | 1.29 ns | 1.14 ns |
|                         **AddHashMap** |         **750000** |   **442.1 ns** | **3.79 ns** | **3.36 ns** |
|                     AddToFSharpMap |         750000 |   416.3 ns | 2.17 ns | 2.03 ns |
|             AddToFSharpAdaptiveMap |         750000 |   384.8 ns | 3.10 ns | 2.90 ns |
|                    AddToFSharpXMap |         750000 |   835.7 ns | 5.64 ns | 5.28 ns |
| AddToSystemCollectionsImmutableMap |         750000 | 1,014.7 ns | 5.51 ns | 4.89 ns |
|               AddToFsharpxChampMap |         750000 |   692.3 ns | 7.20 ns | 6.73 ns |
|                    AddToLangExtMap |         750000 |   389.8 ns | 1.49 ns | 1.39 ns |
|                         **AddHashMap** |        **1000000** |   **448.9 ns** | **3.85 ns** | **3.60 ns** |
|                     AddToFSharpMap |        1000000 |   428.1 ns | 3.28 ns | 3.07 ns |
|             AddToFSharpAdaptiveMap |        1000000 |   400.4 ns | 2.01 ns | 1.78 ns |
|                    AddToFSharpXMap |        1000000 |   806.3 ns | 5.94 ns | 5.55 ns |
| AddToSystemCollectionsImmutableMap |        1000000 |   999.5 ns | 9.54 ns | 8.92 ns |
|               AddToFsharpxChampMap |        1000000 |   701.0 ns | 4.81 ns | 4.50 ns |
|                    AddToLangExtMap |        1000000 |   386.2 ns | 2.36 ns | 2.20 ns |
|                         **AddHashMap** |        **5000000** |   **511.2 ns** | **3.40 ns** | **3.01 ns** |
|                     AddToFSharpMap |        5000000 |   496.0 ns | 4.30 ns | 4.02 ns |
|             AddToFSharpAdaptiveMap |        5000000 |   475.6 ns | 4.03 ns | 3.77 ns |
|                    AddToFSharpXMap |        5000000 |   857.6 ns | 4.54 ns | 4.25 ns |
| AddToSystemCollectionsImmutableMap |        5000000 | 1,175.7 ns | 7.71 ns | 6.84 ns |
|               AddToFsharpxChampMap |        5000000 |   596.2 ns | 3.32 ns | 3.10 ns |
|                    AddToLangExtMap |        5000000 |   355.0 ns | 2.84 ns | 2.52 ns |
|                         **AddHashMap** |       **10000000** |   **507.1 ns** | **2.33 ns** | **2.06 ns** |
|                     AddToFSharpMap |       10000000 |   525.0 ns | 8.57 ns | 8.02 ns |
|             AddToFSharpAdaptiveMap |       10000000 |   500.4 ns | 4.31 ns | 4.03 ns |
|                    AddToFSharpXMap |       10000000 |   898.4 ns | 5.62 ns | 5.25 ns |
| AddToSystemCollectionsImmutableMap |       10000000 | 1,333.1 ns | 6.82 ns | 6.04 ns |
|               AddToFsharpxChampMap |       10000000 |   656.9 ns | 2.94 ns | 2.75 ns |
|                    AddToLangExtMap |       10000000 |   359.5 ns | 1.58 ns | 1.40 ns |
