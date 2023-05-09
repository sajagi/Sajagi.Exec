// Adapted 1:1 from https://github.com/dotnet/corert/blob/master/src/System.Private.CoreLib/shared/System/PasteArguments.cs
// (MIT Licensed)

module private PasteArguments

open System
open System.Text

let private containsNoWhitespaceOrQuotes (s: string) =
    s |> Seq.exists (fun c -> Char.IsWhiteSpace(c) || c = '\"') |> not

let appendArgument (stringBuilder:StringBuilder) (argument:string) =
    let appendChar (c:char) = stringBuilder.Append(c) |> ignore
    let appendCharMulti (c:char) (n:int) = stringBuilder.Append(c, n) |> ignore
    let appendString (s:string) = stringBuilder.Append(s) |> ignore
    if (stringBuilder.Length <> 0) then appendChar ' '

    // Parsing rules for non-argv[0] arguments:
    //   - Backslash is a normal character except followed by a quote.
    //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
    //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
    //   - Parsing stops at first whitespace outside of quoted region.
    //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
    if (argument.Length > 0 && containsNoWhitespaceOrQuotes argument) then
        // Simple case - no quoting or changes needed.
        appendString argument
    else

    appendChar '"'

    let mutable idx = 0;
    while (idx < argument.Length) do
        let c = argument[idx]
        idx <- idx + 1

        match c with
        | '\\' ->
            let mutable numBackSlash = 1
            while (idx < argument.Length && argument[idx] = '\\') do
                idx <- idx + 1;
                numBackSlash <- numBackSlash + 1;

            if (idx = argument.Length) then
                // We'll emit an end quote after this so must double the number of backslashes.
                appendCharMulti '\\' (numBackSlash * 2)

            else if (argument[idx] = '"') then

                // Backslashes will be followed by a quote. Must double the number of backslashes.
                appendCharMulti '\\' (numBackSlash * 2 + 1)
                appendChar '"'
                idx <- idx + 1

            else
                // Backslash will not be followed by a quote, so emit as normal characters.
                appendCharMulti '\\' numBackSlash

        | '"' ->
            // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
            // by another quote (which parses differently pre-2008 vs. post-2008.)
            appendChar '\\'
            appendChar '"'

        | _ ->
            appendChar c

    appendChar '"'