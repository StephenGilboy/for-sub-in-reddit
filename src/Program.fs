open System
open System.Net.Http
open Common
open RedditAuth

let httpClient = new HttpClient()

let run = 
    async {
        match getRedditConfig with
        | Error msg -> return Error msg
        | Ok creds ->
            let! result = getRedditAppOnlyUserlessGrant (creds, httpClient) |> Async.AwaitTask
            match result with
            | Error msg -> return Error msg
            | Ok auth -> return Ok auth
    }


[<EntryPoint>]
let main argsv : int =
    let creds = run |> Async.RunSynchronously
    match creds with
    | Ok auth -> printfn "Authenticated" |> ignore
    | Error msg -> printfn "Error\n%s" msg
    0
