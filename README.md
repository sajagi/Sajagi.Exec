# Sajagi.Exec

## Usage

```fsharp
module Examples

// ---------------------------------------------------------------------------------------------
// High-level API
open Sajagi.Exec

let dotnetPath = where "dotnet"

// exit code is checked by default
exec dotnetPath "build --configuration Release --output \"out\" \"src/Awesome.csproj\""
execArgs dotnetPath ["build"; "--configuration"; "Release"; "--output"; "out"; "src/Awesome.csproj"]

execAsync dotnetPath "run" |> Async.RunSynchronously
execArgsAsync dotnetPath ["run"] |> Async.RunSynchronously

// capture output
let output = execWithOutput @"C:\Windows\System32\cmd.exe" "/c echo Hello!"


// ---------------------------------------------------------------------------------------------
// Mid-level API
let dotnet = Executable(dotnetPath)

// handle exit code errors, capture error output
let result = dotnet.run("run", startOpts = { defaultStartOpts with ErrorOutput = Capture }, waitOpts = { defaultWaitOpts with ExpectedExitCode = None })
if result.ExitCode <> 0 then failwith $"dotnet run failed with exit code %d{result.ExitCode} and error: {result.ErrorOutput.StringOutput}"


// ---------------------------------------------------------------------------------------------
// Low-level API

let running = ExecRaw.start defaultStartOpts dotnetPath (ExecRaw.ExeArgs.Raw "run")
let waitResult = running |> ExecRaw.waitAsync defaultWaitOpts |> Async.RunSynchronously

printfn "Ok!"
```

## Build

```
./build test
./build pack --version <version>
```