module internal Command.Domain.ActorFactories

open FCQRS
open Common
open Akkling.Cluster.Sharding
open Banking.Command.Domain

[<Interface>]
type IActorFactories =
    abstract AccountFactory: string -> IEntityRef<obj>


let factories (env: #_) (actorApi: IActor) =
    let sagaCheck  (o: obj) = []

    let accountShard =  Account.Actor.factory env actorApi

    actorApi.InitializeSagStarter sagaCheck


    Account.Actor.init env  actorApi |> ignore

    { new IActorFactories with
        member _.AccountFactory entityId = accountShard entityId }