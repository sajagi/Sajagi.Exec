namespace Tests

open System.Threading
open FsUnit
open System
open System.IO
open NUnit.Framework
open Sajagi.Exec

module ExecTests =

  let equalIgnoringCase (expected:string) = Is.EqualTo(expected).IgnoreCase

  let whereFuncName = $"{nameof(where)}"

  let notExistingExecutableName = "idontexist"
  let invalidWhereArgument = @"C:\foo.exe"

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