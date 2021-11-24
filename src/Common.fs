module Common
open System

type OAuthGrant = {
    AccessToken: string
    TokenType: string
    Expires: DateTime
    Scope: string
}

let isOAuthAccessTokenExpired oauth =
    DateTime.UtcNow > oauth.Expires