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
    
    // initialize FCQRS acommand api
    let actorApi = FCQRS.Actor.api config loggerFactory
    let actorFactories = Command.Domain.ActorFactories.factories env actorApi
    // get a subscriber send commands
    let accountSubs =  actorApi.CreateCommandSubscription actorFactories.AccountFactory


    let transferSubs = actorApi.CreateCommandSubscription actorFactories.TransferFactory
// just and interface first params should CID for internal tracking
    { new IAPI with
        member _.Withdraw cid = failwith "Not implemented"
        member _.Deposit cid =
            AccountingHandler.deposit (accountSubs  cid)
        member _.Transfer cid = TransferHandler.transfer (transferSubs cid)
        member _.ActorApi = actorApi }
