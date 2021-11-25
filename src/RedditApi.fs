module RedditApi

open System
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Web
open Common


let sendRedditApiRequest (request: HttpRequestMessage, client: HttpClient) =
    task {
        let! response = client.SendAsync(request) |> Async.AwaitTask
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | false ->
            return Error $"HTTP: %s{response.ReasonPhrase} Message: %s{content} SENT: %s{request.RequestUri.AbsoluteUri}"
        | true -> return Ok content
    }
