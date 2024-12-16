module Banking.Query.API

open Microsoft.Extensions.Configuration
open Banking.Model.Data
open FCQRS.Serialization
open SqlProvider
open FSharp.Data.Sql

let queryApi env (config: IConfiguration) actorApi =
    let connString = config.GetSection("config:connection-string").Value
    let query
        (
            ty: System.Type,
            filter,
            orderby,
            orderbydesc,
            thenby,
            thenbydesc,
            take: int option,
            skip,
            (cacheKey: string option)
        ) : Async<obj seq> =
            let ctx = Sql.GetDataContext(connString)
            //gets an augment to apply the fltering and paging. Internal FCQRS stuff here
            let augment db =
                FCQRS.SQLProvider.Query.augmentQuery filter orderby orderbydesc thenby thenbydesc take skip db

            let res =
                    task{
                        if ty = typeof<Account> then
                            // quriesi accounts
                            let q =
                                query {
                                    for c in ctx.Main.Accounts do
                                        select c
                                }
                            // apply the augment and execute the query
                            let! m =   augment <@ q @> |> Seq.executeQueryAsync
                            // desiriilze document o account
                            return m  |> Seq.map (fun x -> x.Document |> decodeFromBytes<Account> :> obj)
                            // example for another type
                            //    else if ty = typeof<AnotherType> then
                            //     // quriesi accounts
                            //     let q =
                            //         query {
                            //             for c in ctx.Main.AnotherType do
                            //                 select c
                            //         }
                            //     // apply the augment and execute the query
                            //     let! m =   augment <@ q @> |> Seq.executeQueryAsync
                            //     // desiriilze document o account
                            //     return m  |> Seq.map (fun x -> x.Document |> decodeFromBytes<AnotherType> :> obj)
                                
                        else
                            return failwith "not implemented"
                    }
             // returns the result as list           
            res |> Async.AwaitTask
    

    // call the projection
    Projection.init env connString actorApi query
