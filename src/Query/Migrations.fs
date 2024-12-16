module Migrations

open FluentMigrator
open System
open Microsoft.Extensions.DependencyInjection
open FluentMigrator.Runner
open Microsoft.Extensions.Configuration
open System.Collections.Generic

// Zero is for reseting the databas
[<MigrationAttribute(0L)>]
type Zero() =
    inherit Migration()

    override this.Up() = ()

    override this.Down() = ()

    // reset akka stuff if db is reset
[<MigrationAttribute(1L)>]
type One() =
    inherit Migration()

    override this.Up() = ()

    override this.Down() =
        try
            if this.Schema.Table("snapshot").Exists() then
                // clean up akka stuff
                this.Execute.Sql("DELETE FROM snapshot")
                this.Execute.Sql("DELETE FROM JOURNAL")
                this.Execute.Sql("DELETE FROM SQLITE_SEQUENCE")
                this.Execute.Sql("DELETE FROM TAGS")
        with _ ->
            ()

    // Creates offset table which is used for event sourciing and booking the last processed event id
[<MigrationAttribute(2L)>]
type AddOffsetsTable() =
    inherit AutoReversingMigration()

    override this.Up() =
        this.Create
            .Table("Offsets")
            .WithColumn("OffsetName")
            .AsString()
            .PrimaryKey()
            .WithColumn("OffsetCount")
            .AsInt64()
            .NotNullable()
            .WithDefaultValue(0)
        |> ignore

        let dict: IDictionary<string, obj> = Dictionary()
        dict.Add("OffsetName", "Banking")
        dict.Add("OffsetCount", 0L)

        this.Insert.IntoTable("Offsets").Row(dict) |> ignore

    // Creates the accounts table
    // with 2 PK, UserIDentity and AccountName
    // Also has a document field for serialziing entire account
[<MigrationAttribute(2024_12_04_2102L)>]
type AddAcountsTable() =
    inherit AutoReversingMigration()

    override this.Up() =
        this.Create
            .Table("Accounts")
            .WithColumn("UserIdentity")
            .AsString()
            .PrimaryKey()
            .WithColumn("AccountName")
            .AsString()
            .PrimaryKey()
            .WithColumn("Balance")
            .AsDecimal()
            .NotNullable()
            .WithColumn("Document")
            .AsBinary()
            .NotNullable()
            .WithColumn("Version")
            .AsInt64()
            .NotNullable()
            .WithColumn("CreatedAt")
            .AsDateTime()
            .NotNullable()
            .Indexed()
            .WithColumn("UpdatedAt")
            .AsDateTime()
            .NotNullable()
            .Indexed()
        |> ignore

// boiler plate code for fluent migrator


let updateDatabase (serviceProvider: IServiceProvider) =
    let runner = serviceProvider.GetRequiredService<IMigrationRunner>()
    runner.MigrateUp()

let resetDatabase (serviceProvider: IServiceProvider) =
    let runner = serviceProvider.GetRequiredService<IMigrationRunner>()

    if runner.HasMigrationsToApplyRollback() then
        runner.RollbackToVersion(0L)


let createServices (config: IConfiguration) =
    let connString =
        config.GetSection("config:connection-string").Value

    ServiceCollection()
        .AddFluentMigratorCore()
        .ConfigureRunner(fun rb ->
            rb
                .AddSQLite()
                .WithGlobalConnectionString(connString)
                .ScanIn(typeof<Zero>.Assembly)
                .For.Migrations()
            |> ignore)
        .AddLogging(fun lb -> lb.AddFluentMigratorConsole() |> ignore)
        .BuildServiceProvider(false)
// initialize the db
let init (env: _) =
    let config = env :> IConfiguration
    use serviceProvider = createServices config
    use scope = serviceProvider.CreateScope()
    updateDatabase scope.ServiceProvider
    
// clears the db remove all tables
let reset (env: _) =
    let config = env :> IConfiguration
    use serviceProvider = createServices config
    use scope = serviceProvider.CreateScope()
    resetDatabase scope.ServiceProvider
    init env