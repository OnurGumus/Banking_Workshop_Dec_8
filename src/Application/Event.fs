module Banking.Application.Event
open Banking.Model.Data

type AccountEvent =
    | BalanedUpdated of Account

// A DU for all DTO Style events where IQeury.Subscirbie API can wait it until it happens
// only used for the read side, not command side.
type DataEvent = 
        | AccountEvent of AccountEvent