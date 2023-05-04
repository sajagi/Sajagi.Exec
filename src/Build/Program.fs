// ./build pack <version>

open System
open System.IO

open Sajagi.Exec

let parentDir (p:string) = Path.GetDirectoryName(p)
let rootPath = __SOURCE_DIRECTORY__ |> parentDir |> parentDir

Environment.CurrentDirectory <- rootPath

let dotnet = where "dotnet"
let paket = where "paket"

let clean () =
    exec dotnet "clean src/Sajagi.Exec"

let pack version =
    exec dotnet "build --configuration Release src/Sajagi.Exec"
    exec paket $"pack --version %s{version} --template \"src/Sajagi.Exec/paket.template\" \"out/\""

let test () =
    exec dotnet "test src/Sajagi.Exec.Tests/"

[<EntryPoint>]
let main (args: string[]) =
    match args with
    | [| "pack"; "--version"; version |] ->
        clean()
        test()
        pack version
    | [| "test" |] ->
        test()
    | _ -> failwith $"Target %A{args} is not supported"
    0