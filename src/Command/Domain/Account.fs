module Banking.Command.Domain.Account

open Banking.Model.Data
open FCQRS.Common

type BalanceUpdateDetails ={ Account : Account ; Diff : Money}

type Event =
    | BalanceUpdated of BalanceUpdateDetails

type Command =
    | Deposit of OperationDetails

type State = { Account : Account option }


module internal Actor = 
    open FCQRS.Model.Data

    let applyEvent (event: Event<_>) (state: State ) =
        match event.EventDetails, state with
        | BalanceUpdated details, _ -> { Account = Some details.Account }

    let handleCommand (cmd: Command<_>) (state: State) =
        match cmd.CommandDetails, state with
        | Deposit details , _ ->
            let account = 
                match state.Account with
                | Some a -> 
                    { a with Balance =  (details.Money |> ValueLens.Value) + a.Balance }

                | None -> 
                {   AccountName = details.AccountName; 
                    Balance = details.Money |> ValueLens.Value; 
                    Owner = details.UserIdentity
                 }

            BalanceUpdated { Account = account; Diff = details.Money |> ValueLens.Value } |> PersistEvent

    let init (env: _)  (actorApi: IActor) =
        let initialState = {  Account = None; }
        actorApi.InitializeActor env initialState "Accounting"  handleCommand applyEvent


    let factory (env: _)  actorApi entityId =
        (init env actorApi).RefFor DEFAULT_SHARD entityId
