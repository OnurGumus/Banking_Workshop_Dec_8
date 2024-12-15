module Banking.Command.Domain.Transfer

open Banking.Model.Data
open FCQRS.Common
open FCQRS
type TransferEventDetails = { From: AccountName; To: AccountName; Amount: PositiveMoney }


type Command = 
    | Transfer of TransferDetails

type Event =
    | TransferRequested of TransferEventDetails

type State = {
    TransferDetails: TransferEventDetails option
}

module internal Actor  =
    open Actor

    let applyEvent (event:Event<_>) (state:State) =
        // match (Event (TransferRequested details))
        match event.EventDetails , state with 
        | (TransferRequested details), state ->
            { state with TransferDetails = Some details }

    let handleCommand (cmd:Command<_>) (state:State) = 
        match cmd.CommandDetails, state with
        | Transfer transferDetails, state ->
            let details =  
                    {   From = transferDetails.OperationDetails.AccountName; 
                        To = transferDetails.DestinationAccountName; 
                        Amount = transferDetails.OperationDetails.Money }
            
            (TransferRequested details) |> PersistEvent
        

    let init (env:_) (actorApi : IActor) =
        let initialState = { TransferDetails = None }

        actorApi.InitializeActor env initialState "Transfer" handleCommand applyEvent


    let factory (env:_) actorApi entityId =
        (init env actorApi).RefFor DEFAULT_SHARD entityId