module Banking.Server.Environments

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Banking.Application.Command.Accounting

type AppEnv(config: IConfiguration, loggerFactory: ILoggerFactory) =
    let mutable commandApi = Unchecked.defaultof<_>

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

    member this.Reset() = 
            Migrations.reset config
            this.Init()
    member this.Init() = 
        Migrations.init config
        commandApi <- Banking.Command.API.api this
