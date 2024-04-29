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

module LMHyper.Util 

open System.Collections.Generic

exception LMHyperException of string

let rec cartesianProduct (LL: list<seq<'a>>) =
    match LL with
    | [] -> Seq.singleton []
    | L :: Ls ->
        seq {
            for x in L do
                for xs in cartesianProduct Ls -> x :: xs
        }

let dictToMap (d : Dictionary<'A, 'B>) = 
    d 
    |> Seq.map (fun x -> x.Key, x.Value)
    |> Map.ofSeq

module ParserUtil = 
    open FParsec
    
    let escapedStringParser : Parser<string, unit> =
        let escapedCharParser : Parser<string, unit> =  
            anyOf "\"\\/bfnrt"
            |>> fun x -> 
                match x with
                | 'b' -> "\b"
                | 'f' -> "\u000C"
                | 'n' -> "\n"
                | 'r' -> "\r"
                | 't' -> "\t"
                | c   -> string c

        let doubleQuotes = 
            between
                (pchar '"')
                (pchar '"')
                (stringsSepBy (manySatisfy (fun c -> c <> '"' && c <> '\\')) (pstring "\\" >>. escapedCharParser))

        let singleQuotes = 
            between
                (pchar ''')
                (pchar ''')
                (stringsSepBy (manySatisfy (fun c -> c <> ''' && c <> '\\')) (pstring "\\" >>. escapedCharParser))

        doubleQuotes <|> singleQuotes