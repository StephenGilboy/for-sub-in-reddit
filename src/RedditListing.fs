(**
---
title: Reddit Listings
category: Api
cateogryindex: 1
index: 1
---
# Reddit Listings Api: Functions for getting Subreddit listings (posts)

These are all the funtions that act on the Reddit [Listings](https://www.reddit.com/dev/api/#section_listings) Api endpoints.

*)

module RedditListing

open System
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Web
open Common

/// Contains the proerties returned by the api to easily deserialize from JSON.
/// NOTE: This is not a complete list of the properties returned. Will ad more as needed
type ListingDataResponse = {
    author: string
    clicked: bool
    downs: uint32
    id: string
    num_comments: uint32
    saved: bool
    subreddit: string
    title: string
    ups: uint32
    upvote_ratio: double
    url: string
}

/// Listing children returned by the api
type ListingChildResponse = {
    kind: string
    data: ListingDataResponse
}

/// Listing response data containing pagination and other data along with the listings
type ListingResponseData = {
    after: string
    dist: int
    modhash: string
    geo_filter: string
    children: List<ListingChildResponse>
    before: string
}

/// The top level response object from the listings api
type ListingResponse = {
    kind: string
    data: ListingResponseData
}

/// The ways Reddit sorts their listings
type ListingSort = 
    | Controversial
    | Hot
    | New
    | Random
    | Rising
    | Top

/// Get the string equivelent of the listing sort
let getSortString sort =
    match sort with
    | Controversial -> "controversial"
    | Hot -> "hot"
    | New -> "new"
    | Random -> "random"
    | Rising -> "rising"
    | Top -> "top"

/// The Listing 
/// NOTE: These are not all the properties a listing can have. Will add more as needed.
type Listing = {
    Id: string
    Type: Thing
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

(*** hide ***)
/// Creates the HttpRequestMessage 
let getListingHttpRequestMessage (grant: OAuthGrant, userAgent: UserAgent, uri: string) =
    match getAccessTokenStatus grant with
    | Expired -> Error "Auth token exipred"
    | Valid -> 
        let request = new HttpRequestMessage(HttpMethod.Get, uri)
        request.Headers.Add("Authorization", $"Bearer %s{grant.AccessToken}")
        match request.Headers.TryAddWithoutValidation("User-Agent", userAgent |> string) with
        | false -> Error $"Unable to add User-Agent %s{userAgent |> string}"
        | true -> Ok request

/// Gets listings by their fullnames (id)
let getListingByFullnameHttpRequestMethod (grant: OAuthGrant, userAgent: UserAgent, baseUri: string, fullnames: List<Fullname>) =
    match getAccessTokenStatus grant with
    | Expired-> Error "Auth token expired"
    | Valid -> 
        if fullnames.Length = 0 then
            Error "Unable to get listings by fullname when there are no fullnames in the list"
        else
            let names = fullnames |> List.reduce (fun acc elm -> Fullname $"{acc},{elm}")
            let uriBuilder = new UriBuilder($"%s{baseUri}/by_id/%s{string names}")
            let uri = uriBuilder.ToString()
            getListingHttpRequestMessage (grant, userAgent, uri)

/// Gets listings from given subreddit by the sort
let getListingBySubreddit grant userAgent baseUri subreddit sort pagination =
    match getAccessTokenStatus grant with
    | Expired -> Error "Auth token exipred"
    | Valid ->
        let sortStr = getSortString sort
        let uriBuilder = new UriBuilder($"%s{baseUri}/r/%s{subreddit}/%s{sortStr}")

        // Set Query String parameters
        let query = HttpUtility.ParseQueryString(uriBuilder.Query)     
        match pagination.Take with
        | After str -> query["after"] <- str
        | Before str -> query["before"] <- str
        query["count"] <- pagination.Count.ToString()
        query["limit"] <- pagination.Limit.ToString()
        uriBuilder.Query <- query.ToString()
        let uri = uriBuilder.ToString()
        getListingHttpRequestMessage (grant, userAgent, uri)
        
        
/// Deserializes listing response and returns Listings
let getListings (content: string) =
    let json: ListingResponse = JsonSerializer.Deserialize<ListingResponse>(content)
    json.data.children
    |> List.map (fun c ->
        let thing = getThingByPrefix c.kind
        {
            Id = c.data.id
            Type = thing
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