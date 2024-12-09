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

let configBuilder =  ConfigurationBuilder()
let config = configBuilder.Build()
let lf = LoggerFactory.Create(fun builder -> builder.AddConsole().AddDebug() |> ignore)


let appEnv :AppEnv = new AppEnv(config,lf)

appEnv.Reset()

let accounting = appEnv :> IAccounting

let cid: CID = Guid.NewGuid() |> string |> ValueLens.CreateAsResult |> Result.value

let money: Money = ValueLens.Create 10

let userIdentity: UserIdentity = "my user" |> ValueLens.CreateAsResult |> Result.value

let accountName : AccountName =  "Account10" |> ValueLens.CreateAsResult |> Result.value

let positiveMoney :PositiveMoney = money |> ValueLens.TryCreate |> Result.value

let operationDetails = { UserIdentity = userIdentity; AccountName = accountName ; Money = positiveMoney} 

let deposit = accounting.Deposit

let accountingDeposit = deposit cid operationDetails |> Async.RunSynchronously

printfn "%A" accountingDeposit