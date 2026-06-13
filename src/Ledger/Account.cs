namespace Ledger;

// BEWARE: production should access accounts **only** via `AccountRepository`. This constructor is `public` only
// to support unit testing.
public record Account(string Id, Money Balance);
