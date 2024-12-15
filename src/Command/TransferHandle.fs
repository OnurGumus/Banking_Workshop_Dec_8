module internal TransferHandle

open Banking.Command
open Banking.Model.Command.Accounting
open Banking.Model.Data
open FCQRS.Common
open FCQRS.Model.Aether
open FCQRS.Model.Aether.Operators
open FCQRS.Model.Data
open Domain.Transfer
open System
let deposit createSubs : Transfer =
    fun transferDetail ->
        let actorId = "Transfer_" + Guid.NewGuid().ToString()
        async {
            let! subscr = createSubs actorId (Transfer transferDetail) (fun (e: Event) -> match e with | TransferRequested _ -> true)

            match subscr with 
            | {EventDetails = TransferRequested _; Version = v} -> 
                return v |> ValueLens.TryCreate |> Result.mapError (fun e -> [e.ToString()])
        }
     