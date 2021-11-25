module Common
open System

let redditBaseUri = "https://oauth.reddit.com"

type UserAgent = UserAgent of string

type OAuthGrant = {
    AccessToken: string
    TokenType: string
    Expires: DateTime
    Scope: string
}

type AccessTokenStatus =
    | Expired
    | Valid

let getAccessTokenStatus oauth =
    if DateTime.UtcNow > oauth.Expires then
        Expired
    else
        Valid

type Thing =
    | Comment
    | Account
    | Link
    | Message
    | Subreddit
    | Award
    | Unknown

type Fullname = Fullname of string

type Take =
    | After of string
    | Before of string

type Pagination = {
    Take: Take
    Count: uint
    Limit: uint16
}

let getThingPrefix thing =
    match thing with
    | Comment -> "t1_"
    | Account -> "t2_"
    | Link -> "t3_"
    | Message -> "t4_"
    | Subreddit -> "t5_"
    | Award -> "t6_"
    | Unknown -> ""

let getThingByPrefix prefix =
    match prefix with
    | "t1" -> Comment
    | "t2" -> Account
    | "t3" -> Link
    | "t4" -> Message
    | "t5" -> Subreddit
    | "t6" -> Award
    | _ -> Unknown

let getThingFullname thing id =
    let prefix = getThingPrefix thing
    Fullname $"%s{prefix}%s{id}"