namespace FSharp.HashCollections

open System
open System.Collections.Generic
open System.Linq

/// Underlying Hash Trie implementation for other collections.
module internal HashTrie =

    let [<Literal>] PartitionSize = 5
    let [<Literal>] PartitionMask = 0b11111
    let [<Literal>] MaxShiftValue = 32 // Partition Size amount of 1 bits

    let inline getIndexNoShift shiftedHash = shiftedHash &&& PartitionMask
    let inline getIndex keyHash shift = getIndexNoShift (keyHash >>> shift)

    let inline tryFind
        ([<InlineIfLambda>] keyExtractor: 'tknode -> 'tk)
        ([<InlineIfLambda>] valueExtractor: 'tknode -> 'tv)
        (eqTemplate: 'teq when 'teq :> IEqualityComparer<'tk>)
        (k: 'tk)
        (hashMap: HashTrieRoot<'tknode>) : 'tv voption =

        let keyHash = eqTemplate.GetHashCode(k)

        let handleHashCollisionList (l : _ list) =
            let rec findInList currentList =
                match currentList with
                | entry :: tail ->
                    let extractedKey = keyExtractor entry
                    if eqTemplate.Equals(extractedKey, k)
                    then ValueSome (valueExtractor entry)
                    else findInList tail
                | [] -> ValueNone
            findInList l

        let rec getRec content remainderHash =
            let index = getIndexNoShift remainderHash
            
            if content.Nodes.BitMap = BitMaskType.MaxValue
            then
                match content.Nodes.Content.[index] with
                | TrieNode content -> getRec content (remainderHash >>> PartitionSize)
                | HashCollisionNode hcn -> handleHashCollisionList hcn
            else
                match CompressedArray.get index content.Nodes with // This checks if the bit was set in the first place.
                | (true, content) ->
                    match content with
                    | TrieNode content -> getRec content (remainderHash >>> PartitionSize)
                    | HashCollisionNode hcn -> handleHashCollisionList hcn
                | _ -> 
                    match CompressedArray.get index content.Entries with
                    | (true, node) -> 
                        if eqTemplate.Equals(keyExtractor node, k)
                        then valueExtractor node |> ValueSome
                        else ValueNone
                    | _ -> ValueNone

        getRec hashMap.RootData keyHash

    let inline buildTrieNodeContent(nodes, entries) = {Nodes = nodes; Entries = entries}
    let inline buildTrieNode(nodes, entries) = TrieNode(buildTrieNodeContent(nodes, entries)) 

    let inline addNodesToResolveConflict oldNodeMap oldValuesMap existingEntry newEntry existingEntryHash currentKeyHash shift = 
        let rec createRequiredDepthNodes (first: bool) shift =
            let existingEntryIndex = getIndex existingEntryHash shift
            let currentEntryIndex = getIndex currentKeyHash shift
            
            if existingEntryIndex <> currentEntryIndex
            then
                let ca =
                    CompressedArray.ofTwoElements
                        existingEntryIndex existingEntry
                        currentEntryIndex newEntry
                (CompressedArray.empty, ca)
            else
                let newShift = shift + PartitionSize
                let subNode = 
                    if newShift >= MaxShiftValue
                    then HashCollisionNode([ existingEntry; newEntry] )
                    else buildTrieNode <| createRequiredDepthNodes false newShift
                if first
                then (CompressedArray.set existingEntryIndex subNode oldNodeMap, CompressedArray.unset currentEntryIndex oldValuesMap) // Not sure about empty here.
                else (CompressedArray.ofSingleElement existingEntryIndex subNode, CompressedArray.empty) // Not sure about empty here.
        createRequiredDepthNodes true shift

    let inline add
        ([<InlineIfLambda>] keyExtractor: 'tknode -> 'tk)
        (eqTemplate: 'teq when 'teq :> IEqualityComparer<'tk>)
        (knode: 'tknode)
        (hashMap: HashTrieRoot<'tknode>) : HashTrieRoot<'tknode> =

        let inline equals x y = eqTemplate.Equals(x, y)
        let inline hash o = eqTemplate.GetHashCode(o)

        let key = keyExtractor knode
        let keyHash = hash key
        
        let handleHashCollisionNode entries = 
            let rec replaceElementIfExists previouslySeen tailList =
                match tailList with
                | entryNode :: tail ->
                    let extractedKey = keyExtractor entryNode
                    if equals extractedKey key
                    then (List.append (knode :: previouslySeen) tail, true)
                    else replaceElementIfExists (entryNode :: previouslySeen) tail
                | [] -> ([], false)

            let (newList, replaced) = replaceElementIfExists [] entries
            if replaced
            then struct (HashCollisionNode(newList), false)
            else struct (HashCollisionNode(knode :: entries), true)

        let rec handleNodeContent ({ Nodes = nodes; Entries = values }: TrieNodeContent<_>) shift : struct (TrieNodeContent<_> * bool)= 
            let index = getIndex keyHash shift
            if nodes.BitMap = BitMaskType.MaxValue
            then
                let nodeAtPos = nodes.Content.[index]
                let (newPosNode, isAdded) = 
                    match nodeAtPos with
                    | TrieNode(nodeContent) -> 
                        let struct (d, isAdded) = handleNodeContent nodeContent (shift + PartitionSize)
                        (TrieNode d, isAdded)
                    | HashCollisionNode entries ->  
                        let struct (r, isAdded) = handleHashCollisionNode entries
                        (r, isAdded)
                struct (buildTrieNodeContent (nodes |> CompressedArray.replaceNoCheck index newPosNode, values), isAdded)
            else
                let indexBit = CompressedArray.getBitMapForIndex index
                if CompressedArray.boundsCheckIfSetForBitMapIndex nodes.BitMap indexBit then // Case where node is already present, just recursively update it and replace.
                    let compressedIndex = CompressedArray.getCompressedIndexForIndexBitmap nodes.BitMap indexBit
                    let nodeAtPos = nodes.Content.[compressedIndex]
                    let (newPosNode, isAdded) = 
                        match nodeAtPos with
                        | TrieNode(nodeContent) -> 
                            let struct (d, isAdded) = handleNodeContent nodeContent (shift + PartitionSize)
                            (TrieNode d, isAdded)
                        | HashCollisionNode entries -> 
                            let struct (r, isAdded) = handleHashCollisionNode entries
                            (r, isAdded)
                    struct (buildTrieNodeContent (nodes |> CompressedArray.replaceNoCheck compressedIndex newPosNode, values), isAdded)
                elif CompressedArray.boundsCheckIfSetForBitMapIndex values.BitMap indexBit then // Where there is a value with that current one.
                    let compressedIndex = CompressedArray.getCompressedIndexForIndexBitmap values.BitMap indexBit
                    let valueAtPos = values.Content.[compressedIndex]
                    let extractedKey = keyExtractor valueAtPos
                    if equals extractedKey key
                    then struct (buildTrieNodeContent (nodes, CompressedArray.replaceNoCheck compressedIndex knode values), false) // Replace existing value
                    else struct (buildTrieNodeContent <| addNodesToResolveConflict nodes values valueAtPos knode (hash extractedKey) keyHash shift, true) // Move value to node list and create nodes as appropriate.
                else
                    // Add new entry
                    let newBitMap = values.BitMap ||| indexBit
                    let compressedIndex = CompressedArray.getCompressedIndexForIndexBitmap values.BitMap indexBit
                    let newContent = ArrayHelpers.copyArrayInsertInMiddle compressedIndex knode values.Content
                    let newCa = { BitMap = newBitMap; Content = newContent }
                    struct (buildTrieNodeContent(nodes, newCa), true)

        let struct (c, isAdded) = handleNodeContent hashMap.RootData 0

        { CurrentCount = if isAdded then hashMap.CurrentCount + 1 else hashMap.CurrentCount
          RootData = c }

    let inline ofSeq
        ([<InlineIfLambda>] keyExtractor: 'tknode -> 'tk)
        (eqTemplate: 'teq when 'teq :> IEqualityComparer<'tk>)
        (nodeList: #seq<'tknode>)
        (hashMap: HashTrieRoot<'tknode>) : HashTrieRoot<'tknode> =

        let inline equals x y = eqTemplate.Equals(x, y)
        let inline hash o = eqTemplate.GetHashCode(o)

        let inline addMutable knode content = 
            let key = keyExtractor knode
            let keyHash = hash key

            let handleHashCollisionNode entries = 
                let rec replaceElementIfExists previouslySeen tailList =
                    match tailList with
                    | entryNode :: tail ->
                        let extractedKey = keyExtractor entryNode
                        if equals extractedKey key
                        then (List.append (knode :: previouslySeen) tail, true)
                        else replaceElementIfExists (entryNode :: previouslySeen) tail
                    | [] -> ([], false)

                let (newList, replaced) = replaceElementIfExists [] entries
                if replaced
                then struct (HashCollisionNode(newList), false)
                else struct (HashCollisionNode(knode :: entries), true)

            let rec handleNodeContent ({ Nodes = nodes; Entries = values } as trieNodeContent: TrieNodeContent<_>) shift : struct (TrieNodeContent<_> * bool)= 
                let index = getIndex keyHash shift
                if nodes.BitMap = BitMaskType.MaxValue
                then
                    let (newPosNode, isAdded) = 
                        match nodes.Content.[index] with
                        | TrieNode(nodeContent) -> 
                            let struct (d, isAdded) = handleNodeContent nodeContent (shift + PartitionSize)
                            (TrieNode d, isAdded)
                        | HashCollisionNode entries -> 
                            let struct (d, isAdded) = handleHashCollisionNode entries
                            (d, isAdded)
                    nodes.Content[index] <- newPosNode
                    struct (trieNodeContent, isAdded)
                else
                    let indexBit = CompressedArray.getBitMapForIndex index
                    if CompressedArray.boundsCheckIfSetForBitMapIndex nodes.BitMap indexBit then // Case where node is already present, just recursively update it and replace.
                        let compressedIndex = CompressedArray.getCompressedIndexForIndexBitmap nodes.BitMap indexBit
                        let nodeAtPos = nodes.Content.[compressedIndex]
                        let (newPosNode, isAdded) = 
                            match nodeAtPos with
                            | TrieNode(nodeContent) -> 
                                let struct (d, isAdded) = handleNodeContent nodeContent (shift + PartitionSize)
                                (TrieNode d, isAdded)
                            | HashCollisionNode entries -> 
                                let struct (r, isAdded) = handleHashCollisionNode entries
                                (r, isAdded)
                        nodes.Content[compressedIndex] <- newPosNode
                        struct (trieNodeContent, isAdded)
                    elif CompressedArray.boundsCheckIfSetForBitMapIndex values.BitMap indexBit then // Where there is a value with that current one.
                        let compressedIndex = CompressedArray.getCompressedIndexForIndexBitmap values.BitMap indexBit
                        let valueAtPos = values.Content.[compressedIndex]
                        let extractedKey = keyExtractor valueAtPos
                        if equals extractedKey key
                        then 
                            values.Content[compressedIndex] <- knode
                            struct (trieNodeContent, false) // Replace existing value
                        else struct (buildTrieNodeContent <| addNodesToResolveConflict nodes values valueAtPos knode (hash extractedKey) keyHash shift, true) // Move value to node list and create nodes as appropriate.
                    else
                        // Add new entry
                        let newBitMap = values.BitMap ||| indexBit
                        let compressedIndex = CompressedArray.getCompressedIndexForIndexBitmap values.BitMap indexBit
                        let newContent = ArrayHelpers.copyArrayInsertInMiddle compressedIndex knode values.Content
                        let newCa = { BitMap = newBitMap; Content = newContent }
                        struct (buildTrieNodeContent(nodes, newCa), true)

            handleNodeContent content 0

        let mutable count = hashMap.CurrentCount
        let mutable rootData = hashMap.RootData
        
        for i in nodeList do
            let struct (newRootData, isAdded) = addMutable i rootData
            if isAdded then count <- count + 1
            rootData <- newRootData

        { CurrentCount = count; RootData = rootData }

    [<Struct>] 
    type internal SubNodeChange<'t> = 
        | RemoveChildNode
        | RemoveChildNodeAndPreserveSingleValue of childValueToPromote: 't
        | NewChildNode of trieNode: HashTrieNode<'t>
        | NoChange

    let inline remove
        ([<InlineIfLambda>] keyExtractor)
        (eqTemplate: 'teq when 'teq :> IEqualityComparer<'tk>)
        (k: 'tk)
        (hashMap: HashTrieRoot< 'tknode>) =

        let inline equals (x: 'tk) (y: 'tk) = eqTemplate.Equals(x, y)
        let inline hash (o: 'tk) = eqTemplate.GetHashCode(o)

        let keyHash = hash k
        let rec traverseNodes first nodes values shift =
            
            let index = getIndex keyHash shift
            match nodes |> CompressedArray.get index with
            | (false, _) ->
                match values |> CompressedArray.get index with
                | (true, entry) -> 
                    if equals (keyExtractor entry) k
                    then 
                        let newValues = CompressedArray.unset index values
                        match CompressedArray.count nodes, CompressedArray.count newValues with
                        | (0, 1) when not first -> struct (RemoveChildNodeAndPreserveSingleValue newValues.Content.[0], true)
                        | (_, _) -> struct (NewChildNode (buildTrieNode (nodes, values |> CompressedArray.unset index)), true)
                    else struct (NoChange, false)
                | _ -> struct (NoChange, false)
            | (true, subNode) ->
                let struct (nodeValueList, newValuesList, didWeRemove) =
                    match subNode with
                    | TrieNode({Nodes = subNodes; Entries = subValues }) ->
                        let subIndex = getIndex keyHash (shift + PartitionSize)
                        let bit = CompressedArray.getBitMapForIndex subIndex
                        if ((bit &&& subNodes.BitMap) <> CompressedArray.Zero) || ((bit &&& subValues.BitMap <> CompressedArray.Zero))
                        then // Case where node is already present, just recursively update it and replace.
                            let (struct (childNodeOpt, didWeRemove)) = traverseNodes false subNodes subValues (shift + PartitionSize)
                            match childNodeOpt with
                            | NewChildNode(childNode) -> struct (nodes |> CompressedArray.set index childNode, values, didWeRemove)
                            | RemoveChildNode -> struct (nodes |> CompressedArray.unset index, values, didWeRemove)
                            | RemoveChildNodeAndPreserveSingleValue(promotedNode) ->
                                struct (nodes |> CompressedArray.unset index, values |> CompressedArray.set index promotedNode, didWeRemove)
                            | NoChange -> struct (nodes, values, didWeRemove)
                        else struct (nodes, values, false)
                    | HashCollisionNode collisions ->
                        // TODO: This could be further optimised but hash collisions should be unlikely.
                        let newList = collisions |> List.filter (fun x -> equals (keyExtractor x) k |> not)
                        let didWeRemove = collisions |> List.exists (fun x -> equals (keyExtractor x) k)
                        match newList with
                        | [ entry ] -> struct (nodes |> CompressedArray.unset index, values |> CompressedArray.set index entry, didWeRemove) // Project parent node to hash collision and unset where this node was.
                        | _ :: _ -> struct (nodes |> CompressedArray.set index (HashCollisionNode(newList)), values, didWeRemove)
                        | [] -> failwithf "This should never happen; hash collision nodes should always have more than one entry"      
                
                // This decides the new parent node minimally required based on child node end state.
                match nodeValueList |> CompressedArray.count, newValuesList |> CompressedArray.count with
                | (0, 1) when not first -> struct (RemoveChildNodeAndPreserveSingleValue newValuesList.Content.[0], didWeRemove)
                | (0, 1) when first -> struct (NewChildNode (buildTrieNode(nodeValueList, newValuesList)), didWeRemove) // Root node should always be TrieNode
                | (0, 0) -> struct (RemoveChildNode, didWeRemove)
                | (_, _) when not didWeRemove -> struct (NoChange, didWeRemove)
                | (_, _) -> struct (NewChildNode (buildTrieNode(nodeValueList, newValuesList)), didWeRemove)
            
        // Need to start the chain. This is harder since we need knowledge of two levels at once (current + sublevel)
        // This is because deletions on a sub-level can mean we create a different node on the current level.
        let changeAndRemovalStatus = traverseNodes true hashMap.RootData.Nodes hashMap.RootData.Entries 0

        match changeAndRemovalStatus with
        | struct (NewChildNode (TrieNode newRootNode), true) -> { CurrentCount = hashMap.CurrentCount - 1; RootData = newRootNode; }
        | struct (NewChildNode _, true) -> failwithf "Only expect trie nodes at root"
        | struct (RemoveChildNode, true) -> { CurrentCount = 0; RootData = { Nodes = CompressedArray.empty; Entries = CompressedArray.empty }; }
        | struct (NoChange, false) -> hashMap
        | struct (_, false) -> hashMap // If no removal then no change required (unlike Add where replace could occur).
        | struct (NoChange, true) -> failwithf "Should never occur"
        | struct (RemoveChildNodeAndPreserveSingleValue _, _) -> failwith "Should never occur at root node"

    let public count hashMap = hashMap.CurrentCount

    let public toSeq (hashMap: HashTrieRoot<_>) =
        let rec yieldNodes node = seq {
            match node with
            | TrieNode({Nodes = nodes; Entries = values }) -> 
                for values in values.Content do yield values
                for node in nodes.Content do yield! yieldNodes node
            | HashCollisionNode entries -> for entry in entries do yield entry
        }

        yieldNodes (TrieNode hashMap.RootData)

    let isEmpty hashMap = hashMap.CurrentCount = 0

    [<GeneralizableValue>]
    let empty< 'tk, 'eq when 'eq : (new : unit -> 'eq)> : HashTrieRoot< ^tk> =
        { CurrentCount = 0; RootData = { Nodes = CompressedArray.empty; Entries = CompressedArray.empty } }

    [<Struct>]
    type private ItemToCheckForEquality<'t> =
        | SingleItem of singleItem: 't
        | ListOfItems of listOfItems: 't list

    /// Given the right key and extra check function (for value checking above key) we can determine whether the given HashTrie is equal
    /// for the specialised implementation above (HashMap or HashSet). This is a O(n) implementation except for HashCollectionNodes.
    let inline equals 
        (eqTemplate: 'teq when 'teq :> IEqualityComparer<_>) 
        equalKeyExtractor 
        ([<InlineIfLambda>] extraCheck) 
        ([<InlineIfLambda>] hashFunction)
        (h1: HashTrieRoot<'n>) 
        (h2: HashTrieRoot<'n>) =
        
        // Used only for HashCollision checks.
        let customEqComparer = {
            new IEqualityComparer<_> with
                override _.Equals(x, y) = eqTemplate.Equals(equalKeyExtractor x, equalKeyExtractor y) && extraCheck x y
                override _.GetHashCode(obj: _) = hashFunction obj
        }    

        // Custom recursive that allows us to be O(n) for the equality check mostly, with the exception of the hash check.
        let rec equalsSpecificSeq node = seq {
            match node with
            | TrieNode({Nodes = nodes; Entries = values }) -> 
                for value in values.Content do yield SingleItem value
                for node in nodes.Content do yield! equalsSpecificSeq node
            | HashCollisionNode entries -> yield ListOfItems entries
        }

        let inline recurse (enumerable1: seq<_>) (enumerable2: seq<_>) = 
            
            use enum1 = enumerable1.GetEnumerator()
            use enum2 = enumerable2.GetEnumerator()

            let rec recurseRec() =
                let enum1V = enum1.MoveNext()
                let _ = enum2.MoveNext()
                if enum1V // Then not at end of list.
                then
                    let currentIsEqual = 
                        match enum1.Current, enum2.Current with
                        | (SingleItem x, SingleItem y) -> customEqComparer.Equals(x, y)
                        | (ListOfItems x, ListOfItems y) -> System.Linq.Enumerable.Except(x, y, customEqComparer) |> Seq.isEmpty
                        | _ -> false
                    if currentIsEqual then recurseRec() else false
                else true

            recurseRec()

        h1.CurrentCount = h2.CurrentCount && recurse (equalsSpecificSeq (TrieNode h1.RootData)) (equalsSpecificSeq (TrieNode h2.RootData))