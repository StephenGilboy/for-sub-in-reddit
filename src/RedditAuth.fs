module RedditAuth

open System
open System.Collections.Generic
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open Common

type RedditConfig = {
    UserAgent: string
    ClientId: string
    ClientSecret: string
    Username: string
    Password: string
}

[<JsonFSharpConverter>]
type RedditAuthResponse = {
    access_token: string
    token_type: string
    expires_in: int64
    scope: string
}

let getUserAgent = 
    match Environment.GetEnvironmentVariable "REDDIT_USER_AGENT" with
    | null -> Error "User Agent is not defined"
    | userAgent -> Ok userAgent

let getRedditAppId =
    match Environment.GetEnvironmentVariable "REDDIT_APP_ID" with
    | null -> Error "Reddit App ID is not defined"
    | appId -> Ok appId

let getRedditSecret =
    match Environment.GetEnvironmentVariable "REDDIT_SECRET" with
    | null -> Error "Reddit Secret is not defined"
    | secret -> Ok secret

let getRedditUser =
    match Environment.GetEnvironmentVariable "REDDIT_USER" with
    | null -> Error "Reddit username is not defined"
    | user -> Ok user

let getRedditPassword = 
    match Environment.GetEnvironmentVariable "REDDIT_PASS" with
    | null -> Error "Reddit password is not defined"
    | pass -> Ok pass

let getRedditConfig = 
    match getUserAgent with
    | Error msg -> Error msg
    | Ok userAgent ->
        match getRedditAppId with
        | Error msg -> Error msg
        | Ok redditAppId ->
            match getRedditSecret with
            | Error msg -> Error msg
            | Ok redditSecret ->
                match getRedditUser with 
                | Error msg -> Error msg
                | Ok redditUser -> 
                    match getRedditPassword with
                    | Error msg -> Error msg
                    | Ok redditPass -> Ok {
                            UserAgent = userAgent
                            ClientId = redditAppId
                            ClientSecret = redditSecret
                            Username = redditUser
                            Password = redditPass
                        }

let getBase64AuthString creds =
    let authStr = $"%s{creds.ClientId}:%s{creds.ClientSecret}"
    Text.ASCIIEncoding.ASCII.GetBytes authStr |> Convert.ToBase64String

let getRedditAuthResponseAsync (content : HttpContent) =
    task {
        let! body = content.ReadAsStringAsync() |> Async.AwaitTask
        try 
            let authResponse = JsonSerializer.Deserialize<RedditAuthResponse>(body)
            return Ok authResponse
        with 
            | :? ArgumentNullException -> return Error "Unable to deserialize the response the response body was null"
            | :? JsonException -> return Error $"Unable to deserialize the response. Recieved: %s{body}"
            | _ as ex -> return Error ex.Message 
    }

let getOAuthToken redditResponse =
    {
        AccessToken = redditResponse.access_token
        TokenType = redditResponse.token_type
        Expires = DateTime.UtcNow.AddSeconds((float)redditResponse.expires_in)
        Scope = redditResponse.scope
    }

let getRedditAppOnlyUserlessGrant (creds : RedditConfig, client: HttpClient) =
    task {
        let body =
            ["grant_type", "client_credentials"; "username", creds.Username; "password", creds.Password ]
            |> List.map (fun (x, y) -> new KeyValuePair<string,string>(x,y))
        let requestContent = new FormUrlEncodedContent(body)
        let base64AuthString = getBase64AuthString creds
        
        let request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token")
        request.Headers.Add("Authorization", $"Basic %s{base64AuthString}")
        request.Headers.UserAgent.ParseAdd(creds.UserAgent)
        request.Content <- requestContent

        let! response = client.SendAsync(request) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | false ->
            let! clientContent = request.Content.ReadAsStringAsync() |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Error $"HTTP: %s{response.ReasonPhrase} Message: %s{content} SENT: %s{clientContent}"
        | true ->
            let! authResponse = getRedditAuthResponseAsync response.Content |> Async.AwaitTask
            match authResponse with
            | Error msg -> return Error msg
            | Ok auth -> return getOAuthToken auth |> Ok
    }
