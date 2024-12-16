module Banking.Command.Domain.Account

open Banking.Model.Data
open FCQRS.Common

type BalanceUpdateDetails ={ Account : Account ; Diff : Money}
// we define events and commands state
type Event =
    | BalanceUpdated of BalanceUpdateDetails

type Command =
    | Deposit of OperationDetails

type State = { Account : Account option }


module internal Actor = 
    open FCQRS.Model.Data

    // all persiste events come here and we decide to new state
    let applyEvent (event: Event<_>) (state: State ) =
        match event.EventDetails, state with
        | BalanceUpdated details, _ -> 
            { Account = Some details.Account }

    // all commands come here and we decide to persist event, your business logic is here
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

    // initialize shard for avoiding first time hit performance issue
    let init (env: _)  (actorApi: IActor) =
        // initial state
        let initialState = {  Account = None; }
        // initialize actor by giving shard type name in this "Accounting"
        actorApi.InitializeActor env initialState "Accounting"  handleCommand applyEvent


    // creates a cluster sharder  actor given entity id and default shart can be changed
    let factory (env: _)  actorApi entityId =
        (init env actorApi).RefFor DEFAULT_SHARD entityId
