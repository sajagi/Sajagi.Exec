namespace global

open FsUnit
open NUnit.Framework

[<assembly: Parallelizable(ParallelScope.Children)>]
do()

// type InitMsgUtils() =
//     inherit FSharpCustomMessageFormatter()