using System.Runtime.CompilerServices;
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

internal sealed class StoreKitOfferMetadataPayload
{
	[JsonPropertyName("productId")]
	public string? ProductId { get; init; }

	[JsonPropertyName("isEligibleForIntroOffer")]
	public bool IsEligibleForIntroOffer { get; init; }

	[JsonPropertyName("introOfferDays")]
	public int? IntroOfferDays { get; init; }
}

internal static unsafe partial class StoreKitNativeInterop
{
	[LibraryImport(
		"__Internal",
		EntryPoint = "kstorekit2_purchase_start",
		StringMarshalling = StringMarshalling.Utf8
	)]
	private static partial void PurchaseStart(
		string productId,
		string? appAccountToken,
		delegate* unmanaged[Cdecl]<
			int,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			void> callback,
		IntPtr context
	);

	[LibraryImport(
		"__Internal",
		EntryPoint = "kstorekit2_restore_start",
		StringMarshalling = StringMarshalling.Utf8
	)]
	private static partial void RestoreStart(
		string productIdsJson,
		delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, IntPtr, void> callback,
		IntPtr context
	);

	[LibraryImport(
		"__Internal",
		EntryPoint = "kstorekit2_offer_metadata_start",
		StringMarshalling = StringMarshalling.Utf8
	)]
	private static partial void OfferMetadataStart(
		string productIdsJson,
		delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void> callback,
		IntPtr context
	);

	[LibraryImport("__Internal", EntryPoint = "kstorekit2_transaction_updates_start")]
	private static partial void TransactionUpdatesStart(
		delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> callback
	);

	private static int _transactionUpdatesStarted;
	private static event Action<StoreKitTransactionUpdate>? TransactionUpdated;

	private sealed class PurchaseRequestContext(
		TaskCompletionSource<StoreKitPurchaseResult> completion
	)
	{
		public TaskCompletionSource<StoreKitPurchaseResult> Completion { get; } = completion;
	}

	private sealed class RestoreRequestContext(
		TaskCompletionSource<StoreKitRestoreResult> completion
	)
	{
		public TaskCompletionSource<StoreKitRestoreResult> Completion { get; } = completion;
	}

	private sealed class OfferMetadataRequestContext(
		TaskCompletionSource<IReadOnlyList<StoreKitOfferMetadata>> completion
	)
	{
		public TaskCompletionSource<IReadOnlyList<StoreKitOfferMetadata>> Completion { get; } = completion;
	}

	private sealed class CallbackSubscription(Action dispose) : IDisposable
	{
		private Action? _dispose = dispose;

		public void Dispose()
		{
			Interlocked.Exchange(ref _dispose, null)?.Invoke();
		}
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
			PurchaseStart(
				productId.Trim(),
				appAccountToken,
				&OnPurchaseCompleted,
				GCHandle.ToIntPtr(gcHandle)
			);
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

		var payloadJson = JsonSerializer.Serialize(
			sanitized,
			StoreKitJsonContext.Default.StringArray
		);

		var completion = new TaskCompletionSource<StoreKitRestoreResult>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		var requestContext = new RestoreRequestContext(completion);
		var gcHandle = GCHandle.Alloc(requestContext, GCHandleType.Normal);

		try
		{
			RestoreStart(payloadJson, &OnRestoreCompleted, GCHandle.ToIntPtr(gcHandle));
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

	public static Task<IReadOnlyList<StoreKitOfferMetadata>> GetOfferMetadataAsync(
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
		if (sanitized.Length == 0)
		{
			return Task.FromResult<IReadOnlyList<StoreKitOfferMetadata>>([]);
		}

		var payloadJson = JsonSerializer.Serialize(
			sanitized,
			StoreKitJsonContext.Default.StringArray
		);
		var completion = new TaskCompletionSource<IReadOnlyList<StoreKitOfferMetadata>>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		var requestContext = new OfferMetadataRequestContext(completion);
		var gcHandle = GCHandle.Alloc(requestContext, GCHandleType.Normal);

		try
		{
			OfferMetadataStart(payloadJson, &OnOfferMetadataCompleted, GCHandle.ToIntPtr(gcHandle));
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

	public static IDisposable SubscribeToTransactionUpdates(Action<StoreKitTransactionUpdate> handler)
	{
		ArgumentNullException.ThrowIfNull(handler);
		TransactionUpdated += handler;
		return new CallbackSubscription(() => TransactionUpdated -= handler);
	}

	public static void EnsureTransactionUpdatesListenerStarted()
	{
		if (Interlocked.CompareExchange(ref _transactionUpdatesStarted, 1, 0) != 0)
		{
			return;
		}

		try
		{
			TransactionUpdatesStart(&OnTransactionUpdated);
		}
		catch
		{
			Interlocked.Exchange(ref _transactionUpdatesStarted, 0);
			throw;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
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

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
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

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnOfferMetadataCompleted(
		IntPtr payloadJson,
		IntPtr errorCode,
		IntPtr errorMessage,
		IntPtr context
	)
	{
		var gcHandle = GCHandle.FromIntPtr(context);
		if (gcHandle.Target is not OfferMetadataRequestContext requestContext)
		{
			if (gcHandle.IsAllocated)
			{
				gcHandle.Free();
			}

			return;
		}

		try
		{
			var errorCodeValue = PtrToString(errorCode);
			var errorMessageValue = PtrToString(errorMessage);
			if (!string.IsNullOrWhiteSpace(errorCodeValue) || !string.IsNullOrWhiteSpace(errorMessageValue))
			{
				requestContext.Completion.TrySetException(
					new InvalidOperationException(
						errorMessageValue ?? errorCodeValue ?? "StoreKit offer metadata query failed."
					)
				);
				return;
			}

			requestContext.Completion.TrySetResult(ParseOfferMetadata(PtrToString(payloadJson)));
		}
		finally
		{
			if (gcHandle.IsAllocated)
			{
				gcHandle.Free();
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnTransactionUpdated(
		IntPtr productId,
		IntPtr originalTransactionId,
		IntPtr transactionId
	)
	{
		var handlers = TransactionUpdated;
		if (handlers is null)
		{
			return;
		}

		var update = new StoreKitTransactionUpdate(
			PtrToString(productId),
			PtrToString(originalTransactionId),
			PtrToString(transactionId)
		);

		foreach (var invocation in handlers.GetInvocationList())
		{
			if (invocation is not Action<StoreKitTransactionUpdate> handler)
			{
				continue;
			}

			try
			{
				handler(update);
			}
			catch
			{
				// Keep the native callback resilient: one managed subscriber must not break the listener.
			}
		}
	}

	private static IReadOnlyList<StoreKitRestoreTransaction> ParseRestoreTransactions(
		string? payloadJson
	)
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

	private static IReadOnlyList<StoreKitOfferMetadata> ParseOfferMetadata(string? payloadJson)
	{
		if (string.IsNullOrWhiteSpace(payloadJson))
		{
			return [];
		}

		try
		{
			var payload = JsonSerializer.Deserialize(
				payloadJson,
				StoreKitJsonContext.Default.ListStoreKitOfferMetadataPayload
			);
			if (payload is null || payload.Count == 0)
			{
				return [];
			}

			return payload
				.Where(static item => !string.IsNullOrWhiteSpace(item.ProductId))
				.Select(static item => new StoreKitOfferMetadata(
					item.ProductId!,
					item.IsEligibleForIntroOffer,
					item.IntroOfferDays
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
