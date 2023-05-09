namespace Tests

open FsUnit
open System
open System.IO
open NUnit.Framework
open Sajagi.Exec

type DisposableFile (?extension, ?content) =
    let filename =
        let n = Path.GetRandomFileName()
        match extension with None -> n | Some e -> $"{n}.{e}"

    let fullPath = Path.Combine(Path.GetTempPath(), filename)
    do
        match content with
        | None -> File.WriteAllBytes(fullPath, Array.empty)
        | Some c -> File.WriteAllText(fullPath, c)

    member _.FullPath with get() = fullPath

    interface IDisposable with
        member this.Dispose() =
            if File.Exists(fullPath) then File.Delete(fullPath)

module ExecTests =
  let equalIgnoringCase (expected:string) = Is.EqualTo(expected).IgnoreCase

  let whereFuncName = $"{nameof(where)}"

  let notExistingExecutableName = "idontexist"
  let invalidWhereArgument = @"C:\foo.exe"

  module ExecTests =
      [<Test>]
      let ``Can exec .cmd file on Windows`` () =
          if Environment.OSVersion.Platform <> PlatformID.Win32NT then Assert.Ignore("This test can be only run on Windows")
          use cmdFile = new DisposableFile(extension="cmd", content="@echo off\r\necho hello world")
          execWithOutput cmdFile.FullPath ""
          |> should equal "hello world\r\n"

  module WhereTests =
      [<Test>]
      let ``where 'where' finds where`` () =
        where "where"
        |> should equalIgnoringCase (Path.Combine(Environment.SystemDirectory, "where.exe"))

      [<Test>]
      let ``where <not existing executable> throws exception`` () =
          (fun () -> where notExistingExecutableName |> ignore)
          |> should (throwWithMessage $"Could not find executable {notExistingExecutableName}") typeof<Exception>

      [<Test>]
      let ``where with invalid argument throws InvalidOperationException`` () =
          (fun () -> where invalidWhereArgument |> ignore)
          |> should (throwWithMessage $"ERROR: Invalid pattern is specified in \"path:pattern\".") typeof<InvalidOperationException>