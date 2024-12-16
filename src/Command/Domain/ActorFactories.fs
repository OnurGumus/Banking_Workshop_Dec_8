module internal Command.Domain.ActorFactories

open FCQRS
open Common
open Akkling.Cluster.Sharding
open Banking.Command.Domain

// custom interface for actor factories, string is the entity id
[<Interface>]
type IActorFactories =
    abstract AccountFactory: string -> IEntityRef<obj>
    abstract TransferFactory: string -> IEntityRef<obj>


let factories (env: #_) (actorApi: IActor) =
    // condition for which events should start the saga
 


    let accountShard =  Account.Actor.factory env actorApi
    let transferShard = Transfer.Actor.factory env actorApi
    let tranferSagaShard = TransferSaga.factory env actorApi

    let sagaCheck  (o: obj) =
        match o with
        | :? (Event<Transfer.Event>) as e ->
                match e.EventDetails with
                | Transfer.TransferRequested _ ->
                    [ tranferSagaShard , id |> Some |> PrefixConversion, o]
                | _ -> []
            | _ -> []


    
    actorApi.InitializeSagaStarter sagaCheck

// initialize all shards
    Account.Actor.init env  actorApi |> ignore
    Transfer.Actor.init env  actorApi |> ignore
    TransferSaga.init env  actorApi |> ignore
    
    // return the actor factories
    { new IActorFactories with
        member _.AccountFactory entityId = accountShard entityId
        member _.TransferFactory entityId = transferShard entityId

        }