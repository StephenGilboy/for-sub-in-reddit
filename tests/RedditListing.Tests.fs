module RedditListingTests

open System
open System.Net.Http
open System.Web
open Xunit
open Common
open RedditListing
open TestHelpers

[<Fact>]
let ``getListingBySubreddit should return a valid HttpRequestMessage`` () =
    // Arrange
    let grant = getValidGrant
    let sort = ListingSort.New
    let take = "ts_123"
    let pagination =
        {
            Take = take |> After
            Count = 10 |> uint
            Limit = 10 |> uint16
        }
    let wantAuthHeader = $"Bearer %s{grant.AccessToken}"

    // Act
    let gotRequest = getListingBySubreddit grant userAgent baseUri subreddit sort pagination

    // Assert
    match gotRequest with
    | Error msg -> Assert.True(false, $"getListingBySubreddit returned error with valid parameters. %s{msg}")
    | Ok req ->
        let gotAuthHeaders = req.Headers.TryGetValues("Authorization")
        match fst gotAuthHeaders with
        | false -> Assert.True(false, "No Authorization header was set")
        | true -> 
            let gotAuthValue = snd gotAuthHeaders |> Seq.head
            Assert.Equal(wantAuthHeader, gotAuthValue) 
        let gotUserAgentHeaders = req.Headers.TryGetValues("User-Agent")
        match fst gotUserAgentHeaders with
        | false -> Assert.True(false, "No User-Agent header was set")
        | true ->
            let gotUaValue = snd gotUserAgentHeaders |> Seq.head
            Assert.Equal(userAgent |> string, gotUaValue)
        let gotUri = new UriBuilder(req.RequestUri)
        Assert.Equal(baseUri, $"%s{gotUri.Scheme}://%s{gotUri.Host}")
        let gotQuery = HttpUtility.ParseQueryString(gotUri.Query)
        Assert.NotNull(gotQuery.Get("after"))
        Assert.NotNull(gotQuery.Get("count"))
        Assert.NotNull(gotQuery.Get("limit"))
        let after = gotQuery.Get("after") |> After
        let count =  gotQuery.Get("count") |> uint
        let limit = gotQuery.Get("limit") |> uint16
        Assert.Equal(pagination.Take, after)
        Assert.Equal(pagination.Count, count)
        Assert.Equal(pagination.Limit, limit)
