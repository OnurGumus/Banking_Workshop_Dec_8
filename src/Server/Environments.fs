module Banking.Server.Environments

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Banking.Application.Command.Accounting
open FCQRS.Model.Query
open Banking.Application.Event

type AppEnv(config: IConfiguration, loggerFactory: ILoggerFactory) =
    let mutable commandApi = Unchecked.defaultof<_>
    let mutable queryApi = Unchecked.defaultof<_>

    interface ILoggerFactory with
        member this.AddProvider(provider: ILoggerProvider) : unit = loggerFactory.AddProvider(provider)
        member this.Dispose() : unit = loggerFactory.Dispose()

        member this.CreateLogger(categoryName: string) : ILogger =
            loggerFactory.CreateLogger(categoryName)

    interface IConfiguration with
        member _.Item
            with get (key: string) = config.[key]
            and set key v = config.[key] <- v

        member _.GetChildren() = config.GetChildren()
        member _.GetReloadToken() = config.GetReloadToken()
        member _.GetSection key = config.GetSection(key)

    interface IAccounting with
        member _.Deposit cid = 
            commandApi.Deposit cid
        member _.Withdraw cid = 
            commandApi.Withdraw cid
        member _.Transfer cid =
            commandApi.Transfer cid
    
    interface IQuery<DataEvent> with
            member _.Query<'t>(?filter, ?orderby, ?orderbydesc, ?thenby, ?thenbydesc, ?take, ?skip, ?cacheKey) =
                async {
                    let! res =
                        queryApi.Query(
                            ty = typeof<'t>,
                            ?filter = filter,
                            ?orderby = orderby,
                            ?orderbydesc = orderbydesc,
                            ?thenby = thenby,
                            ?thenbydesc = thenbydesc,
                            ?take = take,
                            ?skip = skip,
                            ?cacheKey = cacheKey
    
                        )
                    return res |> Seq.cast<'t> |> List.ofSeq
                }
            member _.Subscribe(cb, cancellationToken) = 
                let ks = queryApi.Subscribe(cb)
                cancellationToken.Register(fun _ ->ks.Shutdown()) |> ignore
                
            member _.Subscribe(filter, take, cb, cancellationToken) = 
                let ks, res = queryApi.Subscribe(filter, take, cb)
                cancellationToken.Register(fun _ ->ks.Shutdown()) |> ignore
                res
        
    member this.Reset() = 
            Migrations.reset config
            this.Init()
    member this.Init() = 
        Migrations.init config
        commandApi <- Banking.Command.API.api this
        queryApi <- Banking.Query.API.queryApi  this config commandApi.ActorApi
