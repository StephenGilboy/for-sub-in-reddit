(**
---
title: Reddit Authentication
category: api
categoryindex: 0
index: 0
---

# Reddit Authentication: OAuth
Handles authenticating against Reddit's [OAuth Service](https://github.com/reddit-archive/reddit/wiki/OAuth2).
*)

module RedditAuth

open System
open System.Collections.Generic
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open Common

/// Required values to perform the authentication
type RedditConfig = {
    UserAgent: UserAgent
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

/// Retreives `REDDIT_USER_AGENT` from then Environment to set the UserAgent
let getUserAgent = 
    match Environment.GetEnvironmentVariable "REDDIT_USER_AGENT" with
    | null -> Error "User Agent is not defined"
    | userAgent -> UserAgent userAgent |> Ok

/// Retreives `REDDIT_APP_ID` from the Environment to set the ClientId
let getRedditAppId =
    match Environment.GetEnvironmentVariable "REDDIT_APP_ID" with
    | null -> Error "Reddit App ID is not defined"
    | appId -> Ok appId

/// Retreives `REDDIT_SECRET` from the Environment to set the ClientSecret
let getRedditSecret =
    match Environment.GetEnvironmentVariable "REDDIT_SECRET" with
    | null -> Error "Reddit Secret is not defined"
    | secret -> Ok secret

/// Retreives `REDDIT_USER` from the Environment to set the Username (Actual user's Reddit username)
let getRedditUser =
    match Environment.GetEnvironmentVariable "REDDIT_USER" with
    | null -> Error "Reddit username is not defined"
    | user -> Ok user

/// Retreives `REDDIT_PASS` from the Environment to set the Password (Actual user's Reddit password)
let getRedditPassword = 
    match Environment.GetEnvironmentVariable "REDDIT_PASS" with
    | null -> Error "Reddit password is not defined"
    | pass -> Ok pass

/// Gathers all the required values from the environment, checks for errors, returns a RedditConfig
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

(*** hide ***)
/// Takes the ClientID and CLientSecret to create a Base64String
let getBase64AuthString creds =
    let authStr = $"%s{creds.ClientId}:%s{creds.ClientSecret}"
    Text.ASCIIEncoding.ASCII.GetBytes authStr |> Convert.ToBase64String

/// Deserializes the auth response
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

/// Creates OAuthToken from RedditResponse
let createOAuthToken redditResponse =
    {
        AccessToken = redditResponse.access_token
        TokenType = redditResponse.token_type
        Expires = DateTime.UtcNow.AddSeconds((float)redditResponse.expires_in)
        Scope = redditResponse.scope
    }

/// Retreives an OAuth Grant from Reddit for a App Only Userless grant type
/// Not to be used to authenticate mobile or web users. Only scripts/bots
let getRedditAppOnlyUserlessGrant (creds : RedditConfig, client: HttpClient) =
    task {
        let body =
            ["grant_type", "client_credentials"; "username", creds.Username; "password", creds.Password ]
            |> List.map (fun (x, y) -> new KeyValuePair<string,string>(x,y))
        let requestContent = new FormUrlEncodedContent(body)
        let base64AuthString = getBase64AuthString creds
        
        let request = new HttpRequestMessage(HttpMethod.Post, "https://www.reddit.com/api/v1/access_token")
        request.Headers.Add("Authorization", $"Basic %s{base64AuthString}")
        request.Headers.UserAgent.ParseAdd(string creds.UserAgent)
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
            | Ok auth -> return createOAuthToken auth |> Ok
    }
