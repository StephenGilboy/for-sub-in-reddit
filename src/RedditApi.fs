module RedditApi

open System
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Web
open Common
open RedditApiResponse

type Kind =
    | Comment
    | Account
    | Link
    | Message
    | Subreddit
    | Award
    | Unknown

type Sort = 
    | Controversial
    | Hot
    | New
    | Random
    | Rising
    | Top

type Take =
    | After of string
    | Before of string

type Pagination = {
    Take: Take
    Count: uint
    Limit: uint16
}

type Listing = {
    Id: string
    Type: Kind
    Subreddit: string
    Title: string
    Author: string
    Url: string
    UpVotes: uint32
    DownVotes: uint32
    Clicked: bool
    Saved: bool
    UpvoteRatio: double
    NumberOfComments: uint32
}

let getTypePrefix kind =
    match kind with
    | Comment -> "t1_"
    | Account -> "t2_"
    | Link -> "t3_"
    | Message -> "t4_"
    | Subreddit -> "t5_"
    | Award -> "t6_"
    | Unknown -> ""

let getKind prefix =
    match prefix with
    | "t1" -> Comment
    | "t2" -> Account
    | "t3" -> Link
    | "t4" -> Message
    | "t5" -> Subreddit
    | "t6" -> Award
    | _ -> Unknown

let getSortString sort =
    match sort with
    | Controversial -> "controversial"
    | Hot -> "hot"
    | New -> "new"
    | Random -> "random"
    | Rising -> "rising"
    | Top -> "top"

let getFullname listing =
    let prefix = getTypePrefix listing.Type
    $"%s{prefix}%s{listing.Id}"


let getListingRequest grant userAgent subreddit sort pagination =
    if isOAuthAccessTokenExpired grant then
        None
    else
    let sortStr = getSortString sort
    let uri = $"https://oauth.reddit.com/r/%s{subreddit}/%s{sortStr}"
    let uriBuilder = new UriBuilder(uri)

    // Set Query String parameters
    let query = HttpUtility.ParseQueryString(uriBuilder.Query)     
    match pagination.Take with
    | After str -> query["after"] <- str
    | Before str -> query["before"] <- str
    query["count"] <- pagination.Count.ToString()
    query["limit"] <- pagination.Limit.ToString()
    uriBuilder.Query <- query.ToString()
    
    // Create Request
    let request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString())
    request.Headers.Add("Authorization", $"Bearer %s{grant.AccessToken}")
    request.Headers.UserAgent.ParseAdd(userAgent)
    Some request

let sendRequest (request: HttpRequestMessage, client: HttpClient) =
    task {
        let! response = client.SendAsync(request) |> Async.AwaitTask
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | false ->
            return Error $"HTTP: %s{response.ReasonPhrase} Message: %s{content} SENT: %s{request.RequestUri.AbsoluteUri}"
        | true -> return Ok content
    }

let getListings (content: string) =
    let json: ListingResponse = JsonSerializer.Deserialize<ListingResponse>(content)
    json.data.children
    |> List.map (fun c ->
        let kind = getKind c.kind
        {
            Id = c.data.id
            Type = kind
            Subreddit = c.data.subreddit
            Title = c.data.title
            Author = c.data.author
            Url = c.data.url
            UpVotes = c.data.ups
            DownVotes = c.data.downs
            Clicked = c.data.clicked
            UpvoteRatio = c.data.upvote_ratio
            NumberOfComments = c.data.num_comments
            Saved = c.data.saved
        }
    )
