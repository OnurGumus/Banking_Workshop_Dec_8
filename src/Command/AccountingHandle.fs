module internal AccountingHandler

open Banking.Command
open Banking.Model.Command.Accounting
open Banking.Model.Data
open FCQRS.Common
open FCQRS.Model.Aether
open FCQRS.Model.Aether.Operators
open FCQRS.Model.Data
open Domain.Account

let deposit createSubs : Deposit =
    fun operationDetails ->
        let actorId  = 
            "Account_" +  
                (operationDetails.AccountName ^. (Lens.toValidated AccountName.Value_ >-> ShortString.Value_  ))
        async{
            let! subscr = createSubs actorId (Deposit operationDetails) (fun (e:Event) -> match e with | BalanceUpdated _ -> true)
            
            match subscr with
            | {EventDetails = BalanceUpdated _;   Version  = v } ->  
                return  v |> ValueLens.TryCreate |> Result.mapError (fun e -> [e.ToString()])
        }