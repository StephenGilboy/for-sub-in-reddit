module TestHelpers

open System
open Common

let getValidGrant =
    {
        AccessToken = "abcd1234"
        Scope = "a"
        TokenType = "Bearer"
        Expires = DateTime.UtcNow.AddHours(1)
    }

let getExpiredGrant =
    {
        AccessToken = "abcd1234"
        Scope = "a"
        TokenType = "Bearer"
        Expires = DateTime.UtcNow.AddHours(-1)
    }


let userAgent = UserAgent("bot:Test:v1.0 (by for-sub-in-reddit)")

let baseUri = "https://oauth.reddit.com"

let subreddit = "fsharp"
