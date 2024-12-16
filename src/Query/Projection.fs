module internal Banking.Query.Projection

open FSharp.Data.Sql.Common
open FCQRS.Actor
open Akka.Streams
open Akka.Persistence.Query
open FCQRS.Common
open FCQRS.Model.Query
open Banking.Command.Domain
open SqlProvider
open Microsoft.Extensions.Logging

type CID = FCQRS.Model.Data.CID

let handleEventWrapper (ctx: Sql.dataContext) (offsetValue) (event: obj) =
        let dataEvent =
            match event with
            | :? Event<Account.Event> as  event ->
                AccountProjection.handle ctx event
            | _ -> []
        let offset = ctx.Main.Offsets.Individuals.Banking
        offset.OffsetCount <- offsetValue
        ctx.SubmitUpdates()
        dataEvent



let init env (connectionString: string) (actorApi: IActor) query =
    let ctx = Sql.GetDataContext(connectionString)

    use conn = ctx.CreateConnection()
    conn.Open()
    let cmd = conn.CreateCommand()
    cmd.CommandText <- "PRAGMA journal_mode=WAL;"
    cmd.ExecuteNonQuery() |> ignore

    let offsetCount = ctx.Main.Offsets.Individuals.Banking.OffsetCount
    FCQRS.Query.init actorApi offsetCount (handleEventWrapper ctx) query
    