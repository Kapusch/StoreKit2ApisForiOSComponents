namespace Kapusch.StoreKit2.iOS;

/// <summary>
/// Describes a verified transaction update observed from StoreKit's long-lived <c>Transaction.updates</c> stream.
/// </summary>
public sealed record StoreKitTransactionUpdate(
  string? ProductId,
  string? OriginalTransactionId,
  string? TransactionId
);

/// <summary>
/// StoreKit-backed merchandising metadata for a subscription product.
/// </summary>
public sealed record StoreKitOfferMetadata(
  string ProductId,
  bool IsEligibleForIntroOffer,
  int? IntroOfferDays,
  decimal? CurrentPrice,
  string? CurrentPriceDisplayText
);

/// <summary>
/// Server-signed promotional offer payload used by StoreKit purchase options.
/// </summary>
public sealed record StoreKitPromotionalOfferSignature(
  string OfferId,
  string KeyId,
  string Nonce,
  string Signature,
  long Timestamp
);

public interface IStoreKit2BillingClient
{
  Task<StoreKitPurchaseResult> PurchaseAsync(
    string productId,
    string? appAccountToken = null,
    CancellationToken cancellationToken = default
  );

  Task<StoreKitPurchaseResult> PurchaseWithPromotionalOfferAsync(
    string productId,
    StoreKitPromotionalOfferSignature promotionalOffer,
    string? appAccountToken = null,
    CancellationToken cancellationToken = default
  );

  Task<StoreKitRestoreResult> RestoreAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  );

  Task<StoreKitRestoreResult> GetCurrentEntitlementsAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  );

  Task<IReadOnlyList<StoreKitOfferMetadata>> GetOfferMetadataAsync(
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

  public Task<StoreKitPurchaseResult> PurchaseWithPromotionalOfferAsync(
    string productId,
    StoreKitPromotionalOfferSignature promotionalOffer,
    string? appAccountToken = null,
    CancellationToken cancellationToken = default
  ) =>
    StoreKitNativeInterop.PurchaseWithPromotionalOfferAsync(
      productId,
      promotionalOffer,
      appAccountToken,
      cancellationToken
    );

  public Task<StoreKitRestoreResult> RestoreAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  ) => StoreKitNativeInterop.RestoreAsync(productIds, cancellationToken);

  public Task<StoreKitRestoreResult> GetCurrentEntitlementsAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  ) => StoreKitNativeInterop.GetCurrentEntitlementsAsync(productIds, cancellationToken);

  public Task<IReadOnlyList<StoreKitOfferMetadata>> GetOfferMetadataAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken = default
  ) => StoreKitNativeInterop.GetOfferMetadataAsync(productIds, cancellationToken);

  public void EnsureTransactionUpdatesListenerStarted() =>
    StoreKitNativeInterop.EnsureTransactionUpdatesListenerStarted();

  public IDisposable SubscribeToTransactionUpdates(Action<StoreKitTransactionUpdate> handler) =>
    StoreKitNativeInterop.SubscribeToTransactionUpdates(handler);
}
