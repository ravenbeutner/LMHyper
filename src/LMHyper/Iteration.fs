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

module LMHyper.Iteration 

open FsOmegaLib.GNBA
open FsOmegaLib.Operations

open Util
open Configuration
open HyperLTL

type Result = 
    | SAT 
    | UNSAT 
    | UNKNOWN

let rec private iter (config: Configuration) (univPi : TraceVariable, existsPiList : list<TraceVariable>) (remainingIterations : option<int>) (gnba : GNBA<int,'T * TraceVariable>)  = 
    if remainingIterations.IsSome && remainingIterations.Value <= 0 then 
        UNKNOWN
    else 
        let isEmpty = 
            FsOmegaLib.Operations.AutomataChecks.isEmpty false config.SolverConfig.MainPath config.SolverConfig.AutfiltPath gnba
            |> AutomataOperationResult.defaultWith (fun err -> 
                config.Logger.LogN err.DebugInfo
                raise <| LMHyperException err.Info
            ) 
            
        if isEmpty then 
            UNSAT 
        else 
            let universalProjection = 
                gnba
                |> GNBA.projectToTargetAPs (gnba.APs |> List.filter (fun (_, pi) -> pi = univPi))
                |> GNBA.mapAPs fst


            let projMap = 
                existsPiList 
                |> List.map (fun pi -> 
                    let proj = 
                        gnba
                        |> GNBA.projectToTargetAPs (gnba.APs |> List.filter (fun (_, pi') -> pi = pi'))
                        |> GNBA.mapAPs fst

                    pi, proj
                    )
                |> Map.ofList

            let containedMap = 
                existsPiList
                |> List.map (fun pi -> 
                    let res = 
                        FsOmegaLib.Operations.AutomataChecks.isContained false config.SolverConfig.MainPath config.SolverConfig.AutfiltPath projMap.[pi] universalProjection 
                        |> AutomataOperationResult.defaultWith (fun err -> 
                            config.Logger.LogN err.DebugInfo
                            raise <| LMHyperException err.Info
                        )

                    pi, res
                    )
                |> Map.ofList

            if Map.forall (fun _ b -> b) containedMap then 
                // The formula is satisfiable
                SAT 
            else 
                config.Logger.LogN "Start refinement iteration"

                let conjuncts = 
                    existsPiList 
                    |> List.filter (fun pi -> not containedMap[pi])
                    |> List.map (fun pi -> 
                        projMap[pi]
                        |> GNBA.mapAPs (fun x -> x, univPi)
                        ) 

                let conjunction = 
                    AutomataUtil.constructConjunctionOfGNBAs (gnba :: conjuncts)
                    |> FsOmegaLib.Operations.AutomatonConversions.convertToGNBA false config.SolverConfig.MainPath config.SolverConfig.AutfiltPath Effort.HIGH 
                    |> AutomataOperationResult.defaultWith (fun err -> 
                        config.Logger.LogN err.DebugInfo
                        raise <| LMHyperException err.Info
                    )

                iter config (univPi, existsPiList) (remainingIterations |> Option.map (fun x -> x - 1)) conjunction

let isSat (config: Configuration) (iterations : option<int>) (hyperltl : HyperLTL<'T>) = 
    let blockPrefix = HyperLTL.extractBlocks hyperltl.QuantifierPrefix

    let univPi, existsPiList = 
        match blockPrefix with 
        | [(FORALL, [univPi]); (EXISTS, existsPiList)] -> 
            univPi, existsPiList
        | _ -> 
            raise <| LMHyperException "LMHyper is only applicable to \\forall^1\\exists^* HyperLTL formulas"

    let gnba = 
        FsOmegaLib.Operations.LTLConversion.convertLTLtoGNBA false config.SolverConfig.MainPath config.SolverConfig.Ltl2tgbaPath hyperltl.LTLMatrix 
        |> AutomataOperationResult.defaultWith (fun err -> 
            config.Logger.LogN err.DebugInfo
            raise <| LMHyperException err.Info
        )

    iter config (univPi, existsPiList) iterations gnba
