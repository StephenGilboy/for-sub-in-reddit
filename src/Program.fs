open System
open System.Net.Http
open Common
open RedditAuth
open RedditListing
open RedditApi

let httpClient = new HttpClient()

let printListing listing = 
    printfn "Title: %s\nAuthor: %s\nLink: %s" listing.Title listing.Author listing.Url

let getOAuthGrant config =
    task {
        match config with
        | Error msg -> return Error msg
        | Ok c -> 
            let! result = getRedditAppOnlyUserlessGrant (c, httpClient) |> Async.AwaitTask
            match result with
            | Error msg -> return Error msg
            | Ok auth -> return Ok (auth, c.UserAgent)
    }

let getNewsRequest (input: Result<OAuthGrant * UserAgent, string>) = 
    match input with 
    | Error msg -> Error msg
    | Ok p ->
        let sort = ListingSort.New
        let pagination = {
            Take = After ""
            Count = (uint)0
            Limit = (uint16)10
        }
        let grant = fst p
        let userAgent = snd p
        match getListingBySubreddit grant userAgent "https://oauth.reddit.com" "news" sort pagination with
        | Error msg -> Error msg
        | Ok req -> Ok req

let getRedditListings (request: Result<HttpRequestMessage, string>) =
    task {
        match request with
        | Error msg -> return Error msg
        | Ok req -> 
            let! content = sendRedditApiRequest (req, httpClient) |> Async.AwaitTask
            match content with 
            | Error msg -> return Error msg
            | Ok s -> 
                let listings = getListings s
                return Ok listings
    }

let showListings listings =
    match listings with 
    | Error msg -> Error msg
    | Ok l -> 
        for listing in l do
            printListing listing
        Ok l

let run = 
    async {
            let! grantAndUserAgent = 
                getRedditConfig
                |> getOAuthGrant
                |> Async.AwaitTask
            let! listings = 
                getNewsRequest grantAndUserAgent
                |> getRedditListings
                |> Async.AwaitTask
            return listings
    }


[<EntryPoint>]
let main argsv : int =
    let listings = run |> Async.RunSynchronously
    match showListings listings with
    | Error msg -> printfn "Error\n%s" msg
    | _ -> printfn "Done"
    0
