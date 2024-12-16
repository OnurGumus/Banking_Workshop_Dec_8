module Banking.Server.Environments

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Banking.Application.Command.Accounting
open FCQRS.Model.Query
open Banking.Application.Event

// a gloabl environment for the application
type AppEnv(config: IConfiguration, loggerFactory: ILoggerFactory) =
    // field to hold the command api
    let mutable commandApi = Unchecked.defaultof<_>
    // field to hold the query api
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
    // bridge IQuery to Query project
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
                //not muched used
                cancellationToken.Register(fun _ -> queryApi.Subscribe(cb).Shutdown()) 
                
            member _.Subscribe(filter, take, cb, cancellationToken) =
                /// allows us to wait till read side finishes you pass tilter, take = 1 typically, callback ignore and cancellation token is none
                    let ks, wait = queryApi.Subscribe(filter, take, cb)
                    let disp = cancellationToken.Register(fun _ ->ks.Shutdown())
                    async{  
                        do! wait
                        return disp
                     }
      // resets environment for tesing  
    member this.Reset() = 
            Migrations.reset config
            this.Init()
     /// initializes the environment, you must call this at the begining of your app       
    member this.Init() = 
        Migrations.init config
        commandApi <- Banking.Command.API.api this
        queryApi <- Banking.Query.API.queryApi  this config commandApi.ActorApi
