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
    // fulfills the Deposit command, given actor id as string, command, and event to wait for filtering
        let actorId  = 
            "Account_" +  
                (operationDetails.AccountName ^. (Lens.toValidated AccountName.Value_ >-> ShortString.Value_  ))
        let actorId:ActorId = actorId |> ValueLens.CreateAsResult |> Result.value
        async{
            let! subscr = createSubs actorId (Deposit operationDetails) (fun (e:Event) -> e.IsBalanceUpdated)
            
            // convert the type which satisifies Deposit command
            match subscr with
            | {
                  EventDetails = BalanceUpdated _
                  Version = v
              } -> 
                return Ok v
            | {
                  EventDetails =   _
                  Version = _
              } -> return   Error [sprintf "Deposit failed for account %s" <| actorId.ToString()]
        }
        