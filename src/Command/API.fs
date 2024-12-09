module Banking.Command.API

open FCQRS.Model.Data
open FCQRS.Actor
open Banking.Model.Command.Accounting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open FCQRS.Common

[<Interface>]
type IAPI =
    abstract Withdraw: CID -> Withdraw
    abstract Deposit: CID -> Deposit
    abstract Transfer: CID -> Transfer
    abstract ActorApi: IActor

let api (env: _) =
    let config = env :> IConfiguration
    let loggerFactory = env :> ILoggerFactory
    
    let actorApi = FCQRS.Actor.api config loggerFactory
    let actorFactories = Command.Domain.ActorFactories.factories env actorApi
    let accountSubs =  actorApi.CreateCommandSubscription actorFactories.AccountFactory

    { new IAPI with
        member _.Withdraw cid = failwith "Not implemented"
        member _.Deposit cid =
            AccountingHandler.deposit (accountSubs  cid)
        member _.Transfer cid = failwith "Not implemented"
        member _.ActorApi = actorApi }
