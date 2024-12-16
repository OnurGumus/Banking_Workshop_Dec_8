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
    let sagaCheck  (o: obj) = []

    let accountShard =  Account.Actor.factory env actorApi
    let transferShard = Transfer.Actor.factory env actorApi

    
    actorApi.InitializeSagStarter sagaCheck

// initialize all shards
    Account.Actor.init env  actorApi |> ignore
    Transfer.Actor.init env  actorApi |> ignore

    // return the actor factories
    { new IActorFactories with
        member _.AccountFactory entityId = accountShard entityId
        member _.TransferFactory entityId = transferShard entityId

        }