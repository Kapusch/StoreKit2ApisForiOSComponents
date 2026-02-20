namespace Kapusch.StoreKit2.iOS;

public interface IStoreKit2BillingClient
{
  Task<StoreKitPurchaseResult> PurchaseAsync(
    string productId,
    string? appAccountToken = null,
    CancellationToken cancellationToken = default
  );

  Task<StoreKitRestoreResult> RestoreAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  );
}

public sealed class StoreKit2BillingClient : IStoreKit2BillingClient
{
  public Task<StoreKitPurchaseResult> PurchaseAsync(
    string productId,
    string? appAccountToken = null,
    CancellationToken cancellationToken = default
  ) => StoreKitNativeInterop.PurchaseAsync(productId, appAccountToken, cancellationToken);

  public Task<StoreKitRestoreResult> RestoreAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  ) => StoreKitNativeInterop.RestoreAsync(productIds, cancellationToken);
}
