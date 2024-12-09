module rec Banking.Model.Data

open FCQRS.Model
open FCQRS.Model.Data
open FCQRS.Model.Aether

type UserIdentity =
    private UserIdentity of ShortString
        static member Value_ : Lens<UserIdentity,ShortString> = 
            (fun (UserIdentity u) -> u),
            (fun (g: ShortString) _ ->   g |> UserIdentity)

        override this.ToString() =
            (ValueLens.Value this).ToString()

type AccountName =
    private AccountName of ShortString
        static member Value_ : Lens<AccountName,ShortString> = 
            (fun (AccountName u) -> u), 
            (fun (g: ShortString) _ ->   g |> AccountName)

        override this.ToString() = 
            (ValueLens.Value this).ToString()