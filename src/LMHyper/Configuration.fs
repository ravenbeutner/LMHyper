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

module LMHyper.Configuration 

open System.IO

open FsOmegaLib.JSON

open Util

type SolverConfiguration = 
    {
        MainPath : string
        Ltl2tgbaPath: string
        AutfiltPath : string
    }

type Logger = 
    {
        Log : string -> unit
    }

    member this.LogN s = this.Log (s + "\n")

type Configuration = 
    {
        SolverConfig : SolverConfiguration
        Logger : Logger
    }

let private parseConfigFile (s : string) =
    match FsOmegaLib.JSON.Parser.parseJsonString s with 
    | Result.Error err -> raise <| LMHyperException $"Could not parse config file: %s{err}"
    | Result.Ok x -> 
        {
            MainPath = "./"
            
            Ltl2tgbaPath = 
                (JSON.tryLookup "ltl2tgba" x)
                |> Option.defaultWith (fun _ -> raise <| LMHyperException "No field 'ltl2tgba' found")
                |> JSON.tryGetString
                |> Option.defaultWith (fun _ -> raise <| LMHyperException "Field 'ltl2tgba' must contain a string")
            
            AutfiltPath = 
                (JSON.tryLookup "autfilt" x)
                |> Option.defaultWith (fun _ -> raise <| LMHyperException "No field 'autfilt' found")
                |> JSON.tryGetString
                |> Option.defaultWith (fun _ -> raise <| LMHyperException "Field 'autfilt' must contain a string")
        }

let getConfig() = 
    // By convention the paths.json file is located in the same directory as the executable
    let configPath = 
        System.IO.Path.Join [|System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); "paths.json"|]
                     
    // Check if the path to the config file exists
    if System.IO.FileInfo(configPath).Exists |> not then 
        raise <| LMHyperException "The paths.json file does not exist in the same directory as the executable"            
    
    // Parse the config File
    let configContent = 
        try
            File.ReadAllText configPath
        with 
            | _ -> 
                raise <| LMHyperException $"Could not open {configPath}"

    let solverConfig = parseConfigFile configContent

    solverConfig