module Banking.Server.Program
open System
open System.IO
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Hocon.Extensions.Configuration
open Banking.Application.Command.Accounting
open FCQRS.Model.Data
open FCQRS.Model.Aether.Operators
open FCQRS.Model.Aether
open Banking.Model.Data
open Banking.Model.Command.Accounting

//let tempFile = "/workspaces/Banking/src/Server/Database/Banking.db"
let tempFile = Path.GetTempFileName()
let connString = $"Data Source={tempFile}"
let wd = __SOURCE_DIRECTORY__
let configBuilder =
    ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddHoconFile(Path.Combine(wd, "config.hocon"))
        .AddInMemoryCollection(
            dict
                [| "config:connection-string", connString
                   "config:akka:persistence:journal:sql:connection-string", connString
                   "config:akka:persistence:snapshot-store:sql:connection-string", connString
                   "config:akka:persistence:query:journal:sql:connection-string", connString |]
        )
        

let config = configBuilder.Build()

let lf = LoggerFactory.Create(fun builder -> builder.AddConsole().AddDebug() |> ignore)

let env = new Banking.Server.Environments.AppEnv(config,lf)

env.Reset()
open FCQRS.Model.Aether.Operators
open FCQRS.Model.Query
open System.Threading

let acc = env :> IAccounting


let cid:CID  = Guid.NewGuid().ToString() |> ValueLens.CreateAsResult |> Result.value
let money :Money =  ValueLens.Create  10
let deposit : Deposit = acc.Deposit cid 
let userIdentity: UserIdentity = "my user" |> ValueLens.CreateAsResult |> Result.value 
let accountName: AccountName =  "123"  |> ValueLens.CreateAsResult |> Result.value
let postiveMoney : PositiveMoney = money |> ValueLens.TryCreate |> Result.value
let operationDetails = { UserIdentity = userIdentity; AccountName = accountName ; Money = postiveMoney} 
let depositResult = deposit  operationDetails |> Async.RunSynchronously
printfn "Deposit: %A" depositResult

//////----------------- Above Desit to Account 123
/// 

let query = env :> IQuery<_>
let cidTransfer:CID  = Guid.NewGuid().ToString() |> ValueLens.CreateAsResult |> Result.value

/// We subscrbie to CID Transfer to finish but not wait yet.
let transferSubscription = query.Subscribe((fun e -> e.Type.IsTransferEvent && e.CID = cidTransfer),1,ignore, CancellationToken.None) 

let transfer : Transfer = acc.Transfer cidTransfer


let toAccountName: AccountName =  "456"  |> ValueLens.CreateAsResult |> Result.value
let toUserIdentity: UserIdentity = "my user" |> ValueLens.CreateAsResult |> Result.value
let toMoney :Money =  ValueLens.Create  5
let toPostiveMoney : PositiveMoney = toMoney |> ValueLens.TryCreate |> Result.value
let toOperationDetails = { UserIdentity = toUserIdentity; AccountName = toAccountName ; Money = toPostiveMoney}

deposit  toOperationDetails |> Async.RunSynchronously |> ignore


/// Above code despists 5 to account 456
/// 

let transferDetails = { OperationDetails = operationDetails; DestinationAccountName= toAccountName}
let transferResult = transfer  transferDetails  |> Async.RunSynchronously



transferSubscription |> Async.RunSynchronously |> ignore

printfn "Press any key to exit"


query.Query<Account>() |> Async.RunSynchronously |> Seq.iter (fun x -> printfn "Account: %A" x)
Console.ReadLine() |> ignore
