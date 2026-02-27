namespace Kapusch.StoreKit2.iOS;

/// <summary>
/// Describes a verified transaction update observed from StoreKit's long-lived <c>Transaction.updates</c> stream.
/// </summary>
public sealed record StoreKitTransactionUpdate(
  string? ProductId,
  string? OriginalTransactionId,
  string? TransactionId
);

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

  /// <summary>
  /// Ensures the native <c>Transaction.updates</c> listener is started exactly once for the process.
  /// </summary>
  void EnsureTransactionUpdatesListenerStarted();

  /// <summary>
  /// Subscribes to verified transaction updates emitted by the long-lived StoreKit listener.
  /// </summary>
  IDisposable SubscribeToTransactionUpdates(Action<StoreKitTransactionUpdate> handler);
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

  public void EnsureTransactionUpdatesListenerStarted() =>
    StoreKitNativeInterop.EnsureTransactionUpdatesListenerStarted();

  public IDisposable SubscribeToTransactionUpdates(Action<StoreKitTransactionUpdate> handler) =>
    StoreKitNativeInterop.SubscribeToTransactionUpdates(handler);
}
