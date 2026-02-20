namespace Kapusch.StoreKit2.iOS;

public enum StoreKitInteropOutcome
{
  Success = 0,
  UserCancelled = 1,
  Pending = 2,
  Failed = 3,
}

public sealed record StoreKitPurchaseResult(
  StoreKitInteropOutcome Outcome,
  string? ProductId,
  string? OriginalTransactionId,
  string? SignedTransactionInfo,
  string? ErrorCode,
  string? ErrorMessage
);
