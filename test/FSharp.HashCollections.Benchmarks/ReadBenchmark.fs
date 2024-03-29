namespace FSharp.HashCollections.Benchmarks

open BenchmarkDotNet.Attributes

module private ReadBenchmarkConstants =
    let [<Literal>] OperationsPerInvokeInt = 100000
open ReadBenchmarkConstants

open FSharp.HashCollections
open FSharpx.Collections.Experimental

type ReadBenchmarks() =

    let mutable hashMapData = HashMap.empty
    let mutable imToolsMap = ImTools.ImHashMap.Empty
    let mutable fsharpMapData = Map.empty
    let mutable fsharpXHashMap = FSharpx.Collections.PersistentHashMap.empty
    let mutable systemImmutableMap = System.Collections.Immutable.ImmutableDictionary.Empty
    let mutable fsharpDataAdaptiveMap = FSharp.Data.Adaptive.HashMap.Empty
    let mutable fsharpXChampMap: FSharpx.Collections.Experimental.ChampHashMap<_, _> = FSharpx.Collections.Experimental.ChampHashMap<int32, int32>()
    let mutable languageExtMap = LanguageExt.HashMap.Empty

    let mutable keyToLookup = Array.zeroCreate OperationsPerInvokeInt
    let mutable dummyBufferVOption = Array.zeroCreate OperationsPerInvokeInt
    let mutable dummyBufferOption = Array.zeroCreate OperationsPerInvokeInt
    let mutable dummyBufferNoOption = Array.zeroCreate OperationsPerInvokeInt
    let randomGen = System.Random()

    [<Params(10, 100, 1000, 100_000, 500_000, 750_000, 1_000_000, 5_000_000, 10_000_000)>]
    member val public CollectionSize = 0 with get, set

    member this.SetupKeyToLookup() =
        for i = 0 to keyToLookup.Length - 1 do
            keyToLookup.[i] <- randomGen.Next(0, this.CollectionSize)

    [<GlobalSetup(Target = "GetHashMap")>]
    member this.SetupHashMapData() =
        hashMapData <- FSharp.HashCollections.HashMap.empty
        for i = 0 to this.CollectionSize - 1 do
            hashMapData <- hashMapData |> HashMap.add i i
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetImToolsHashMap")>]
    member this.SetupImToolsHashMapData() =
        imToolsMap <- ImTools.ImHashMap.Empty
        for i = 0 to this.CollectionSize - 1 do
            imToolsMap <- imToolsMap.AddOrUpdate(i, i)
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetFSharpMap")>]
    member this.SetupFSharpMapData() =
        fsharpMapData <- Map.empty
        for i = 0 to this.CollectionSize - 1 do
            fsharpMapData <- fsharpMapData |> Map.add i i
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetFSharpXHashMap")>]
    member this.SetupFSharpXMapData() =
        fsharpXHashMap <- FSharpx.Collections.PersistentHashMap.empty
        for i = 0 to this.CollectionSize - 1 do
            fsharpXHashMap <- fsharpXHashMap.Add(i, i)
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetSystemCollectionsImmutableMap")>]
    member this.SetupSystemCollectionsImmutableMapData() =
        systemImmutableMap <- System.Collections.Immutable.ImmutableDictionary.Empty
        for i = 0 to this.CollectionSize - 1 do
            systemImmutableMap <- systemImmutableMap.Add(i, i)
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetFSharpDataAdaptiveMap")>]
    member this.SetupFSharpDataAdaptiveMapData() =
        fsharpDataAdaptiveMap <- FSharp.Data.Adaptive.HashMap.Empty
        for i = 0 to this.CollectionSize - 1 do
            fsharpDataAdaptiveMap <- fsharpDataAdaptiveMap.Add(i, i)
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetFSharpxChampMap")>]
    member this.SetupFSharpXChampMap() =
        fsharpXChampMap <- FSharpx.Collections.Experimental.ChampHashMap<int, int>()
        for i = 0 to this.CollectionSize - 1 do
            fsharpXChampMap <- ChampHashMap.add fsharpXChampMap i i
        this.SetupKeyToLookup()

    [<GlobalSetup(Target = "GetLangExtMap")>]
    member this.SetupLangExtMap() =
        languageExtMap <- LanguageExt.HashMap.Empty
        for i = 0 to this.CollectionSize - 1 do
            languageExtMap <- languageExtMap.Add(i, i)
        this.SetupKeyToLookup()

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetHashMap() =
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferVOption.[i] <- hashMapData |> FSharp.HashCollections.HashMap.tryFind k
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetImToolsHashMap() =
        let mutable i = 0
        for k in keyToLookup do
            match imToolsMap.TryFind(k) with
            | (true, x) -> dummyBufferNoOption.[i] <- x
            | _ -> ()
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetFSharpMap() =
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferOption.[i] <- fsharpMapData |> Map.tryFind k
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetFSharpXHashMap() =
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferNoOption.[i] <- fsharpXHashMap |> FSharpx.Collections.PersistentHashMap.find k
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetSystemCollectionsImmutableMap() =
        let mutable i = 0
        for k in keyToLookup do
            match systemImmutableMap.TryGetValue(k) with
            | (true, x) -> dummyBufferNoOption.[i] <- x
            | _ -> ()
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetFSharpDataAdaptiveMap() =
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferVOption.[i] <- fsharpDataAdaptiveMap |> FSharp.Data.Adaptive.HashMap.tryFindV k
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetFSharpxChampMap() =
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferOption.[i] <- FSharpx.Collections.Experimental.ChampHashMap.tryGetValue fsharpXChampMap k 
            i <- i + 1

    [<Benchmark(OperationsPerInvoke = OperationsPerInvokeInt)>]
    member _.GetLangExtMap() =
        let mutable i = 0
        for k in keyToLookup do
            dummyBufferNoOption.[i] <- languageExtMap.[k]
            i <- i + 1