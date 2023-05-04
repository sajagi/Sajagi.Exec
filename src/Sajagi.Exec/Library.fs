﻿module Sajagi.Exec

open System
open System.Diagnostics
open System.IO

type Output =
    | Ignore
    | Capture

type StartOptions = { Output: Output; ErrorOutput: Output }

let defaultStartOpts = { Output = Ignore; ErrorOutput = Ignore }

type WaitOptions = { ExpectedExitCode: int option }

let defaultWaitOpts = { ExpectedExitCode = Some 0 }

[<RequireQualifiedAccess>]
module ExecRaw =
    type ExeArgs =
        | Raw of string
        | Args of string list

    type OutputProcessor =
        | NoProcessor
        | Capturing of Async<string>

    type OutputResult =
        | Ignored
        | Captured of string

        member this.StringOutput =
            match this with
            | Captured s -> s
            | _ -> failwith "Output was ignored"

    type RunningProcess =
        { Process: Process
          OutputProcessor: OutputProcessor
          ErrorOutputProcessor: OutputProcessor }

    type FinishedProcess =
        { ErrorOutput: OutputResult
          ExitCode: int
          Output: OutputResult }

    let start (opts: StartOptions) exe args =
        let psi = ProcessStartInfo(exe)

        match args with
        | Raw args -> psi.Arguments <- args
        | Args args -> args |> List.iter (fun a -> psi.ArgumentList.Add(a))

        psi.RedirectStandardOutput <- opts.Output = Capture
        psi.RedirectStandardError <- opts.ErrorOutput = Capture

        let ps = Process.Start(psi)

        let mkProcessor (outputf: unit -> StreamReader) =
            function
            | Ignore -> NoProcessor
            | Capture -> outputf().ReadToEndAsync() |> Async.AwaitTask |> Capturing

        // calling StandardOutput or StandardError getters without setting RedirectStandardOutput or RedirectStandardError will throw
        let outputProcessor = mkProcessor (fun () -> ps.StandardOutput) opts.Output
        let errorOutputProcessor = mkProcessor (fun () -> ps.StandardError) opts.ErrorOutput

        { Process = ps
          OutputProcessor = outputProcessor
          ErrorOutputProcessor = errorOutputProcessor }

    let waitAsync opts (ps: RunningProcess) =
        async {
            do! ps.Process.WaitForExitAsync() |> Async.AwaitTask

            let psExitCode = ps.Process.ExitCode

            match opts.ExpectedExitCode with
            | Some exitCode ->
                if psExitCode <> exitCode then
                    failwith $"Process exited with exit code {psExitCode}"
            | None -> ()

            let getOutput ps =
                async {
                    match ps with
                    | NoProcessor -> return OutputResult.Ignored
                    | Capturing stringifier ->
                        let! result = stringifier
                        return Captured result
                }

            let! output = getOutput ps.OutputProcessor
            let! errorOutput = getOutput ps.ErrorOutputProcessor

            return
                { ErrorOutput = errorOutput
                  ExitCode = psExitCode
                  Output = output }
        }

    let runAsync exe args (startOptions: StartOptions) (waitOptions: WaitOptions) =
        start startOptions exe args |> waitAsync waitOptions


type Executable(exe: string) as this =
    do
        if (File.Exists(exe) = false) then
            raise <| FileNotFoundException($"Executable '{exe}' does not exist")

    let start_ args opts =
        let opts = defaultArg opts defaultStartOpts
        ExecRaw.start opts exe args

    let runAsync_ args startOpts waitOpts =
        let startOpts = defaultArg startOpts defaultStartOpts
        let waitOpts = defaultArg waitOpts defaultWaitOpts
        ExecRaw.runAsync exe args startOpts waitOpts

    /// Starts the executable and returns the running process object.
    member _.start(args: string, ?opts) = start_ (ExecRaw.Raw args) opts

    /// Starts the executable and returns the running process object.
    member _.start(args: string list, ?opts) = start_ (ExecRaw.Args args) opts

    /// Runs the executable and returns the finished process object.
    member _.runAsync(args: string, ?startOpts, ?waitOpts) =
        runAsync_ (ExecRaw.Raw args) startOpts waitOpts

    /// Runs the executable and returns the finished process object.
    member _.runAsync(args: string list, ?startOpts, ?waitOpts) =
        runAsync_ (ExecRaw.Args args) startOpts waitOpts

    /// Runs the executable and returns the finished process object.
    member _.runAsync(args: ExecRaw.ExeArgs, ?startOpts, ?waitOpts) = runAsync_ args startOpts waitOpts

    /// Runs the executable and returns the finished process object.
    member _.run(args: string, ?startOpts, ?waitOpts) =
        this.runAsync (args, ?startOpts = startOpts, ?waitOpts = waitOpts)
        |> Async.RunSynchronously

    /// Runs the executable and returns the finished process object.
    member _.run(args: string list, ?startOpts, ?waitOpts) =
        this.runAsync (args, ?startOpts = startOpts, ?waitOpts = waitOpts)
        |> Async.RunSynchronously

    /// Runs the executable and returns the finished process object.
    member _.run(args: ExecRaw.ExeArgs, ?startOpts, ?waitOpts) =
        this.runAsync (args, ?startOpts = startOpts, ?waitOpts = waitOpts)
        |> Async.RunSynchronously


// todo: allow overrides and use environment variables to determine
let private defaultWherePath =
    Path.Combine(Environment.SystemDirectory, "where.exe")

let private execAsync_ exe (args: ExecRaw.ExeArgs) =
    Executable(exe).runAsync args |> Async.Ignore

/// Runs an executable and checks the exit code.
let execAsync exe args = execAsync_ exe (ExecRaw.Raw args)

/// Runs an executable and checks the exit code.
let execArgsAsync exe args = execAsync_ exe (ExecRaw.Args args)

/// Runs an executable and checks the exit code.
let exec exe args =
    execAsync exe args |> Async.RunSynchronously

/// Runs an executable and checks the exit code.
let execArgs exe args =
    execArgsAsync exe args |> Async.RunSynchronously

let private runAsync_ exe (args: ExecRaw.ExeArgs) =
    let waitOpts = { defaultWaitOpts with ExpectedExitCode = None }
    Executable(exe).runAsync (args, waitOpts = waitOpts)

/// Runs an executable and returns the finished process object. Does not check the exit code.
let runAsync exe args = runAsync_ exe (ExecRaw.Raw args)

/// Runs an executable and returns the finished process object. Does not check the exit code.
let runArgsAsync exe args = runAsync_ exe (ExecRaw.Args args)

/// Runs an executable and returns the finished process object. Does not check the exit code.
let run exe args =
    runAsync exe args |> Async.RunSynchronously

/// Runs an executable and returns the finished process object. Does not check the exit code.
let runArgs exe args =
    runArgsAsync exe args |> Async.RunSynchronously

let private execAsyncWithOutput_ exe (args: ExecRaw.ExeArgs) =
    let exe = Executable(exe)

    async {
        let startOpts = { defaultStartOpts with Output = Capture }
        let! res = exe.runAsync (args, startOpts = startOpts)

        return res.Output.StringOutput
    }

/// Runs an executable and returns the output. Checks the exit code.
let execAsyncWithOutput exe args =
    execAsyncWithOutput_ exe (ExecRaw.Raw args)

/// Runs an executable and returns the output. Checks the exit code.
let execArgsAsyncWithOutput exe args =
    execAsyncWithOutput_ exe (ExecRaw.Args args)

/// Runs an executable and returns the output. Checks the exit code.
let execWithOutput exe args =
    execAsyncWithOutput exe args |> Async.RunSynchronously

/// Runs an executable and returns the output. Checks the exit code.
let execArgsWithOutput exe args =
    execArgsAsyncWithOutput exe args |> Async.RunSynchronously

/// Tries to find the path to an executable using the where command.
let tryWhereAsync (exe: string) : Async<string option> =
    async {
        let whereExe = Executable(defaultWherePath)

        let startOpts = { defaultStartOpts with Output = Capture; ErrorOutput = Capture }
        let waitOpts = { defaultWaitOpts with ExpectedExitCode = None }

        let! ps = whereExe.runAsync ([ exe ], startOpts = startOpts, waitOpts = waitOpts)

        return
            match ps.ExitCode with
            | 0 ->
                ps.Output.StringOutput.Split(Environment.NewLine, StringSplitOptions.TrimEntries ||| StringSplitOptions.RemoveEmptyEntries)
                |> Array.head
                |> Some

            // exit code for not found
            | 1 -> None

            // other reason
            | _ -> invalidOp (ps.ErrorOutput.StringOutput.Trim())
    }

/// Tries to find the path to an executable using the where command.
let tryWhere exe : string option =
    tryWhereAsync exe |> Async.RunSynchronously

/// Finds the path to an executable using the where command.
let whereAsync exe : Async<string> =
    async {
        let! res = tryWhereAsync exe

        return
            res
            |> Option.defaultWith (fun () -> failwith $"Could not find executable {exe}")
    }

/// Finds the path to an executable using the where command.
let where exe : string =
    whereAsync exe |> Async.RunSynchronously