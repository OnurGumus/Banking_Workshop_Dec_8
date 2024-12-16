module AccountProjection

open FSharp.Data.Sql.Common

open Banking.Model.Data
open FCQRS.Serialization
open FCQRS.Model
open Akka.Persistence.Query
open FCQRS.Model.Query
open Banking.Command.Domain
open SqlProvider
open FCQRS.Model.Data
open Banking.Application.Event

// gets db context (connection ) and and event, updates db and returns new event
// that event is a DTO style event, not the event we persisted. It's defined Applicatioin Projec
let handle (ctx: Sql.dataContext)(e:FCQRS.Common.Event<Account.Event>) =
    let eventDetails = e.EventDetails
    let cid = e.CorrelationId

    match eventDetails with
    | Account.BalanceUpdated { Account = account } ->
        let owner = account.Owner |> ValueLens.Value |> ValueLens.Value
        let accountName = account.AccountName |> ValueLens.Value |> ValueLens.Value
        let balance = account.Balance |> ValueLens.Value
        let serialize = encodeToBytes account
        let existingRow =
            query {
                for c in (ctx.Main.Accounts) do
                    where (c.UserIdentity = owner && c.AccountName = accountName)
                    take 1
                    select c
            }
            |> Seq.tryHead

        match existingRow with
        | Some row ->
            row.Balance <- balance
            row.Document <- serialize
            row.UpdatedAt <- System.DateTime.UtcNow
            row.Version <- e.Version

        | None ->
            let row =
                ctx.Main.Accounts.``Create(Balance, CreatedAt, Document, UpdatedAt, Version)`` (
                    balance,
                    System.DateTime.UtcNow,
                    encodeToBytes account,
                    System.DateTime.UtcNow,
                    e.Version
                )
            row.AccountName <- accountName
            row.UserIdentity <- owner
        [{
                Type = AccountEvent(BalanedUpdated account)
                CID = cid
            }]
    | _ -> []
            
