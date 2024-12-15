module Banking.Server.Program
open Environments
open Banking.Model.Data
open Banking.Application.Command
open Banking.Application.Command
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Banking.Application.Command.Accounting
open System
open FCQRS.Model.Data
open System.IO
open Hocon.Extensions.Configuration
open FCQRS.Model.Query
open Banking.Application.Event
open System.Threading

let tempFile = Path.GetTempFileName()
let connString = $"Data Source={tempFile}"

let configBuilder =
    ConfigurationBuilder()
        .AddEnvironmentVariables()
#if DEBUG
        .AddHoconFile(Path.Combine( __SOURCE_DIRECTORY__ , "config.hocon"))
#else
        .AddHoconFile("config.hocon")
#endif
        .AddInMemoryCollection(
                dict
                        [|      "config:connection-string", connString
                                "config:akka:persistence:journal:sql:connection-string", connString
                                "config:akka:persistence:snapshot-store:sql:connection-string", connString
                                "config:akka:persistence:query:journal:sql:connection-string", connString |]
        )
      
        
let config = configBuilder.Build()
let lf = LoggerFactory.Create(fun builder -> builder.AddConsole().AddDebug() |> ignore)


let appEnv :AppEnv = new AppEnv(config,lf)

appEnv.Reset()

let accounting = appEnv :> IAccounting
let query = appEnv :> IQuery<DataEvent>

let cid: CID = Guid.NewGuid() |> string |> ValueLens.CreateAsResult |> Result.value

let money: Money = ValueLens.Create 10

let userIdentity: UserIdentity = "my user" |> ValueLens.CreateAsResult |> Result.value

let accountName : AccountName =  "Account10" |> ValueLens.CreateAsResult |> Result.value

let positiveMoney :PositiveMoney = money |> ValueLens.TryCreate |> Result.value

let operationDetails = { UserIdentity = userIdentity; AccountName = accountName ; Money = positiveMoney} 

let transferDetails: TransferDetails =  { OperationDetails = operationDetails; DestinationAccountName = accountName }
let result = accounting.Transfer cid transferDetails |> Async.RunSynchronously

printfn "!!%A" result

Console.ReadLine() |> ignore

// let deposit = accounting.Deposit

// let readSideSubs = query.Subscribe((fun e -> e.CID = cid), 1, ignore, CancellationToken.None) |> Async.StartImmediateAsTask
// let accountingDeposit = deposit cid operationDetails |> Async.RunSynchronously

// printfn "%A" accountingDeposit

// readSideSubs.Wait()
// let accounts = query.Query<Account>(filter = Or(Greater("Balance", 9), 
//         Equal("AccountName","foo")), take = 1, skip = 0, orderby = "Balance") |> Async.RunSynchronously
// printfn "Accounts %A" accounts

// Console.ReadLine() |> ignore

// printfn "%s" connString