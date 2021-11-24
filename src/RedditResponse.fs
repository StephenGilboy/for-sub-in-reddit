module RedditApiResponse

type ListingData = {
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

type ListingChild = {
    kind: string
    data: ListingData
}

type ListingResponseData = {
    after: string
    dist: int
    modhash: string
    geo_filter: string
    children: List<ListingChild>
    before: string
}

type ListingResponse = {
    kind: string
    data: ListingResponseData
}
