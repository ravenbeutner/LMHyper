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

module LMHyper.CommandLineParser

type CommandLineArguments = 
    {
        InputFile : option<string>
        IterationBound : option<int>

        LogPrintouts : bool // If set to true, we log intermediate steps to the console
        RaiseExceptions : bool // If set to true, we raise exceptions
    }

    static member Default = 
        {
            InputFile = None
            IterationBound = None

            LogPrintouts = false
            RaiseExceptions = false
        }

let rec private splitByPredicate (f : 'T -> bool) (xs : list<'T>) = 
    match xs with 
        | [] -> [], []
        | x::xs -> 
            if f x then 
                [], x::xs 
            else 
                let r1, r2 = splitByPredicate f xs 
                x::r1, r2

let parseCommandLineArguments (args : list<string>) =
    let rec parseArgumentsRec (args : list<string>) (opt : CommandLineArguments) = 

        match args with 
        | [] -> Result.Ok opt
        | x :: xs -> 
            match x with 
            | "--log" -> 
                parseArgumentsRec xs { opt with LogPrintouts = true }
            | "--iter" -> 
                match xs with 
                | [] -> Result.Error ("Option '-iter' must be followed by a number")
                | y :: ys -> 
                    try 
                        let i = System.Int32.Parse y
                        parseArgumentsRec ys { opt with IterationBound = Some i }
                    with
                    | _ -> Result.Error ("Could not parse iteration count")

            | s when s <> "" && s.Trim().StartsWith "-" -> 
                Result.Error ("Option " + s + " is not supported" )

            | x -> 
                // When no option is given, we assume that this is the input 
                if opt.InputFile.IsSome then 
                    Result.Error "Input files cannot be given more than once"
                else 
                    parseArgumentsRec xs {opt with InputFile = Some x}

    parseArgumentsRec args CommandLineArguments.Default
                                