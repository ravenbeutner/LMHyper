(*    
    Copyright (C) 2024 Raven Beutner

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*)

module LMHyper.AutomataUtil 

open System.Collections.Generic

open FsOmegaLib.SAT
open FsOmegaLib.AutomatonSkeleton
open FsOmegaLib.GNBA

open Util

let internal constructConjunctionOfGNBAs (autList : list<GNBA<int, 'L>>) = 
    let autList = GNBA.bringToSameAPs autList

    let sumOfAccSets = 
        (0, autList)
        ||> List.mapFold (fun s x -> 
            s, s + x.NumberOfAcceptingSets
            )
        |> fst

    let initStates = 
        autList
        |> List.map (fun x -> x.InitialStates |> seq)
        |> Util.cartesianProduct
        |> set

    let queue = new Queue<_>(initStates)

    let allStates = new HashSet<_>(initStates)
    let newEdgesDict = new Dictionary<_,_>()
    let newAccSetsDict = new Dictionary<_,_>()

    while queue.Count <> 0 do 
        let n = queue.Dequeue()
        
        let edges = 
            n 
            |> List.mapi (fun i state -> autList.[i].Edges.[state] |> seq)
            |> Util.cartesianProduct
            |> Seq.choose (fun x -> 
                let guards, sucs = List.unzip x

                let combinedGuard = DNF.constructConjunction guards

                if DNF.isSat combinedGuard then  
                    if allStates.Contains sucs |> not then 
                        allStates.Add sucs |> ignore
                        queue.Enqueue sucs
                        
                    Some (combinedGuard, sucs)
                else
                    None
            )
            |> Seq.toList

        // Ensure disjoint acceptance Sets
        let accSets = 
            n 
            |> List.mapi (fun i state -> autList.[i].AcceptanceSets.[state])
            |> List.mapi (fun i accSet -> 
                accSet |> Set.map (fun y -> y + sumOfAccSets.[i])
                )
            |> Set.unionMany
        
        newEdgesDict.Add(n, edges)
        newAccSetsDict.Add(n, accSets)

    {
        GNBA.Skeleton =
            {
                AutomatonSkeleton.States = set allStates;
                APs = autList.[0].APs
                Edges = Util.dictToMap newEdgesDict
            }
        InitialStates = 
            initStates
        AcceptanceSets = 
            Util.dictToMap newAccSetsDict
        NumberOfAcceptingSets = 
            autList 
            |> List.map (fun x -> x.NumberOfAcceptingSets) 
            |> List.sum
    }
    |> GNBA.convertStatesToInt