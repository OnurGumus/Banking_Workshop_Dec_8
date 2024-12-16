module internal SqlProvider

open FSharp.Data.Sql
open FSharp.Data.Sql.Common

// binary dlls for sqlite sqlite provider integration
[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + @"/libs"

[<Literal>]
let schemaLocation = __SOURCE_DIRECTORY__ + @"/../Server/Database/Schema.sqlite"
#if DEBUG

/// connection string for compile time only
[<Literal>]
let connectionString =
      @"Data Source=" + __SOURCE_DIRECTORY__ + @"/../Server/Database/Banking.db;"

#else

[<Literal>]
let connectionString = @"Data Source=" + @"Database/Banking.db;"

#endif

// this is the only thing matters here
type Sql =
    SqlDataProvider<
        DatabaseProviderTypes.SQLITE,
        SQLiteLibrary=SQLiteLibrary.MicrosoftDataSqlite,
        ConnectionString=connectionString,
        ResolutionPath= resolutionPath,
        ContextSchemaPath=schemaLocation,
        CaseSensitivityChange=CaseSensitivityChange.ORIGINAL
     >