module internal Banking.Query.Projection

open FSharp.Data.Sql.Common
open Akka.Persistence.Query
open FCQRS.Common
open Banking.Command.Domain
open SqlProvider

// offsetValue (last procced event numer sequentually increasing) and the event itself as Event<_>
let handleEventWrapper (ctx: Sql.dataContext) (offsetValue) (event: obj) =
        let dataEvent =
            match event with
            | :? Event<Account.Event> as  event ->
                AccountProjection.handle ctx event
            | _ -> []
        // select the offset and update it
        let offset = ctx.Main.Offsets.Individuals.Banking
        offset.OffsetCount <- offsetValue
        // commit the transaction 
        ctx.SubmitUpdates()
        // return the event subscribers get notifie
        dataEvent



let init env (connectionString: string) (actorApi: IActor) query =
    let ctx = Sql.GetDataContext(connectionString)
    /// Only if you want WAL support for SQLite perfornace
    use conn = ctx.CreateConnection()
    conn.Open()
    let cmd = conn.CreateCommand()
    cmd.CommandText <- "PRAGMA journal_mode=WAL;"
    cmd.ExecuteNonQuery() |> ignore
    // end wall

    // this is function executed only once when the is the app is started
    // get the offset and send ti Query API
    let offsetCount = ctx.Main.Offsets.Individuals.Banking.OffsetCount

    FCQRS.Query.init actorApi offsetCount (handleEventWrapper ctx) query
    