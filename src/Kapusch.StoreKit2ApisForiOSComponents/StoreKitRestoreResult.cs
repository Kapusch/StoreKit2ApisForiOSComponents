namespace Kapusch.StoreKit2.iOS;

public sealed record StoreKitRestoreTransaction(
  string ProductId,
  string OriginalTransactionId,
  string SignedTransactionInfo
);

public sealed record StoreKitRestoreResult(
  StoreKitInteropOutcome Outcome,
  IReadOnlyList<StoreKitRestoreTransaction> Transactions,
  string? ErrorCode,
  string? ErrorMessage
);
