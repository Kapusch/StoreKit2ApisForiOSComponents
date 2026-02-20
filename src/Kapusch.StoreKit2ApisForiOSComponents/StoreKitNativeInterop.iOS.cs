using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kapusch.StoreKit2.iOS;

internal sealed class RestorePayloadTransaction
{
  [JsonPropertyName("productId")]
  public string? ProductId { get; init; }

  [JsonPropertyName("originalTransactionId")]
  public string? OriginalTransactionId { get; init; }

  [JsonPropertyName("signedTransactionInfo")]
  public string? SignedTransactionInfo { get; init; }
}

internal static partial class StoreKitNativeInterop
{
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void PurchaseCallback(
    int status,
    IntPtr productId,
    IntPtr originalTransactionId,
    IntPtr signedTransactionInfo,
    IntPtr errorCode,
    IntPtr errorMessage,
    IntPtr context
  );

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void RestoreCallback(
    int status,
    IntPtr payloadJson,
    IntPtr errorCode,
    IntPtr errorMessage,
    IntPtr context
  );

  [DllImport("__Internal", EntryPoint = "kstorekit2_purchase_start")]
  private static extern void PurchaseStart(
    string productId,
    string? appAccountToken,
    PurchaseCallback callback,
    IntPtr context
  );

  [DllImport("__Internal", EntryPoint = "kstorekit2_restore_start")]
  private static extern void RestoreStart(
    string productIdsJson,
    RestoreCallback callback,
    IntPtr context
  );

  private sealed class PurchaseRequestContext
  {
    public PurchaseRequestContext(TaskCompletionSource<StoreKitPurchaseResult> completion)
    {
      Completion = completion;
      Callback = OnPurchaseCompleted;
    }

    public TaskCompletionSource<StoreKitPurchaseResult> Completion { get; }

    public PurchaseCallback Callback { get; }
  }

  private sealed class RestoreRequestContext
  {
    public RestoreRequestContext(TaskCompletionSource<StoreKitRestoreResult> completion)
    {
      Completion = completion;
      Callback = OnRestoreCompleted;
    }

    public TaskCompletionSource<StoreKitRestoreResult> Completion { get; }

    public RestoreCallback Callback { get; }
  }

  public static Task<StoreKitPurchaseResult> PurchaseAsync(
    string productId,
    string? appAccountToken,
    CancellationToken cancellationToken
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(productId);
    cancellationToken.ThrowIfCancellationRequested();

    var completion = new TaskCompletionSource<StoreKitPurchaseResult>(
      TaskCreationOptions.RunContinuationsAsynchronously
    );
    var requestContext = new PurchaseRequestContext(completion);
    var gcHandle = GCHandle.Alloc(requestContext, GCHandleType.Normal);

    try
    {
      PurchaseStart(productId.Trim(), appAccountToken, requestContext.Callback, GCHandle.ToIntPtr(gcHandle));
    }
    catch
    {
      if (gcHandle.IsAllocated)
      {
        gcHandle.Free();
      }

      throw;
    }

    return completion.Task;
  }

  public static Task<StoreKitRestoreResult> RestoreAsync(
    IReadOnlyList<string> productIds,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(productIds);
    cancellationToken.ThrowIfCancellationRequested();

    var sanitized = productIds
      .Where(static id => !string.IsNullOrWhiteSpace(id))
      .Select(static id => id.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToArray();

    var payloadJson = JsonSerializer.Serialize(sanitized, StoreKitJsonContext.Default.StringArray);

    var completion = new TaskCompletionSource<StoreKitRestoreResult>(
      TaskCreationOptions.RunContinuationsAsynchronously
    );
    var requestContext = new RestoreRequestContext(completion);
    var gcHandle = GCHandle.Alloc(requestContext, GCHandleType.Normal);

    try
    {
      RestoreStart(payloadJson, requestContext.Callback, GCHandle.ToIntPtr(gcHandle));
    }
    catch
    {
      if (gcHandle.IsAllocated)
      {
        gcHandle.Free();
      }

      throw;
    }

    return completion.Task;
  }

  private static void OnPurchaseCompleted(
    int status,
    IntPtr productId,
    IntPtr originalTransactionId,
    IntPtr signedTransactionInfo,
    IntPtr errorCode,
    IntPtr errorMessage,
    IntPtr context
  )
  {
    var gcHandle = GCHandle.FromIntPtr(context);
    if (gcHandle.Target is not PurchaseRequestContext requestContext)
    {
      if (gcHandle.IsAllocated)
      {
        gcHandle.Free();
      }

      return;
    }

    try
    {
      var result = new StoreKitPurchaseResult(
        MapOutcome(status),
        PtrToString(productId),
        PtrToString(originalTransactionId),
        PtrToString(signedTransactionInfo),
        PtrToString(errorCode),
        PtrToString(errorMessage)
      );

      requestContext.Completion.TrySetResult(result);
    }
    finally
    {
      if (gcHandle.IsAllocated)
      {
        gcHandle.Free();
      }
    }
  }

  private static void OnRestoreCompleted(
    int status,
    IntPtr payloadJson,
    IntPtr errorCode,
    IntPtr errorMessage,
    IntPtr context
  )
  {
    var gcHandle = GCHandle.FromIntPtr(context);
    if (gcHandle.Target is not RestoreRequestContext requestContext)
    {
      if (gcHandle.IsAllocated)
      {
        gcHandle.Free();
      }

      return;
    }

    try
    {
      var transactions = ParseRestoreTransactions(PtrToString(payloadJson));
      var result = new StoreKitRestoreResult(
        MapOutcome(status),
        transactions,
        PtrToString(errorCode),
        PtrToString(errorMessage)
      );

      requestContext.Completion.TrySetResult(result);
    }
    finally
    {
      if (gcHandle.IsAllocated)
      {
        gcHandle.Free();
      }
    }
  }

  private static IReadOnlyList<StoreKitRestoreTransaction> ParseRestoreTransactions(string? payloadJson)
  {
    if (string.IsNullOrWhiteSpace(payloadJson))
    {
      return [];
    }

    try
    {
      var payload = JsonSerializer.Deserialize(
        payloadJson,
        StoreKitJsonContext.Default.ListRestorePayloadTransaction
      );

      if (payload is null || payload.Count == 0)
      {
        return [];
      }

      return payload
        .Where(static item =>
          !string.IsNullOrWhiteSpace(item.ProductId)
          && !string.IsNullOrWhiteSpace(item.OriginalTransactionId)
          && !string.IsNullOrWhiteSpace(item.SignedTransactionInfo)
        )
        .Select(static item => new StoreKitRestoreTransaction(
          item.ProductId!,
          item.OriginalTransactionId!,
          item.SignedTransactionInfo!
        ))
        .ToArray();
    }
    catch
    {
      return [];
    }
  }

  private static StoreKitInteropOutcome MapOutcome(int status) =>
    status switch
    {
      0 => StoreKitInteropOutcome.Success,
      1 => StoreKitInteropOutcome.UserCancelled,
      2 => StoreKitInteropOutcome.Pending,
      _ => StoreKitInteropOutcome.Failed,
    };

  private static string? PtrToString(IntPtr value) =>
    value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(value);
}
