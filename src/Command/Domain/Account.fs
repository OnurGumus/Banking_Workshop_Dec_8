module Banking.Command.Domain.Account

open Banking.Model.Data
open FCQRS.Common

type BalanceUpdateDetails ={ Account : Account ; Diff : Money}
type AccountMismatch = { TargetAccount : Account; TargetUser : UserIdentity }
type Event =
    | BalanceUpdated of BalanceUpdateDetails
    | OverdraftAttempted of Account * Money
    | NoReservationFound
    | MoneyReserved of Money
    
type Command =
    | Deposit of OperationDetails
    | ReserveMoney of Money
    | ConfirmReservation

type State = {
    Account: Account option
    Resevations: Event<Event> list
} 


module internal Actor =
    open FCQRS.Model.Data
    
    let applyEvent (event: Event<_>) (_: State as state) =
        match event.EventDetails, state with
        | BalanceUpdated ( b:BalanceUpdateDetails), _ ->
            let state  ={ state with Account = Some b.Account }
            if state.Resevations |> List.exists (fun x -> x.CorrelationId = event.CorrelationId) then
                { state 
                    with Resevations = state.Resevations 
                        |> List.filter (fun x -> x.CorrelationId <> event.CorrelationId) }
            else state

        | MoneyReserved _, _ ->
            { state with Resevations = state.Resevations @ [event] }
        
        | OverdraftAttempted _, _
        | _ -> state

    let handleCommand (cmd:Command<_>) (state:State)  =
        let corID = cmd.CorrelationId
        match cmd.CommandDetails, state with

        | ReserveMoney money, _ ->
            let existingEvent =  
                state.Resevations |> List.tryFind (fun x -> x.CorrelationId = corID) 
            let eventAcion : EventAction<Event> =
                match existingEvent with
                | Some x -> x |> PublishEvent
                | None -> 
                    // neeed to check existing balance with existing reservations
                    if  state.Account.Value.Balance < money then
                        (OverdraftAttempted (state.Account.Value, money)) |> PersistEvent
                    else
                        (MoneyReserved money ) |> PersistEvent
            eventAcion

        | ConfirmReservation, _ ->
           let findReservation = state.Resevations |> List.tryFind (fun x -> x.CorrelationId = corID) |> Option.map (fun x -> x.EventDetails) 
           match findReservation with
            
                | Some (MoneyReserved(m)) ->
                    (BalanceUpdated 
                        { 
                            Account = 
                                { state.Account.Value 
                                    with Balance = state.Account.Value.Balance - (m |> ValueLens.Value) }; 

                            Diff = m

                        } ) 
                            |> PersistEvent
                | _ -> (NoReservationFound) |> DeferEvent
           
        | Deposit{ Money = (ResultValue money); UserIdentity = userIdentity; AccountName = accountName }, _ ->

         if state.Account.IsNone then
            let newAccount = { AccountName = accountName; Balance = money; Owner = userIdentity }
            (BalanceUpdated { Account = newAccount; Diff = money } ) |> PersistEvent
            else
                let account = { state.Account.Value with Balance = (state.Account.Value.Balance + money) }
                (BalanceUpdated { 
                    Account = account; 
                    Diff = money }) |> PersistEvent

    let init (env: _)  (actorApi: IActor) =
        let initialState = {  Account = None; Resevations = [] }
        actorApi.InitializeActor env initialState "Accounting"  handleCommand applyEvent

    let factory (env: #_)  actorApi entityId =
        (init env  actorApi).RefFor DEFAULT_SHARD entityId
