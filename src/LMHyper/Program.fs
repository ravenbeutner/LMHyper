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

module LMHyper.Program

open System.IO

open Util
open Configuration
open Iteration
open CommandLineParser

let mutable raiseExceptions = false

let run args =    
    let sw = System.Diagnostics.Stopwatch()
    let swTotal = System.Diagnostics.Stopwatch()
    swTotal.Start()
    sw.Start()

    let solverConfig = Configuration.getConfig()

    let cmdArgs = 
        match CommandLineParser.parseCommandLineArguments (Array.toList args) with 
        | Result.Ok x -> x 
        | Result.Error e -> raise <| LMHyperException $"{e}"

    raiseExceptions <- cmdArgs.RaiseExceptions

    let logger = 
        {
            Logger.Log = fun x -> 
                if cmdArgs.LogPrintouts then 
                    printf $"{x}"
        }

    let config = 
        {
            Configuration.SolverConfig = solverConfig
            Logger = logger
        }

    let path = 
        cmdArgs.InputFile
        |> Option.defaultWith (fun _ -> raise <| LMHyperException "No input file given")

    let c = 
        try File.ReadAllText path with
        | _ -> raise <| LMHyperException $"Could not open {path}"
    
    let hyperltl = 
        HyperLTL.Parser.parseHyperLTL Util.ParserUtil.escapedStringParser c 
        |> Result.defaultWith (fun e -> raise <| LMHyperException $"Could not parse HyperLTL formula: {e}")

    HyperLTL.HyperLTL.findError hyperltl
    |> Option.iter (fun err -> 
        raise <| LMHyperException $"Error in HyperLTL formula: {err}"
        ) 

    sw.Restart()

    let res = 
        Iteration.isSat config cmdArgs.IterationBound hyperltl 

    config.Logger.LogN $"Performed computation in %i{sw.ElapsedMilliseconds} ms (%.4f{double(sw.ElapsedMilliseconds) / 1000.0} s)"

    match res with 
    | SAT -> printfn "SAT"
    | UNSAT -> printfn "UNSAT"
    | UNKNOWN -> printfn "UNKNOWN"

    config.Logger.LogN $"Total Time: %i{swTotal.ElapsedMilliseconds} ms (%.4f{double(swTotal.ElapsedMilliseconds) / 1000.0} s)"
    

[<EntryPoint>]
let main args =   
    try 
        run args

        0
    with 
    | LMHyperException msg -> 
        printfn "===== ERROR ====="
        printfn $"{msg}"

        if raiseExceptions then 
            reraise()

        1
    | e -> 
        printfn "===== ERROR ====="
        printfn $"{e.Message}"

        if raiseExceptions then 
            reraise()

        1
        