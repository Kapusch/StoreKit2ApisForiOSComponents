import Foundation
import StoreKit

public typealias KapuschStoreKit2PurchaseCallback = @convention(c) (
  Int32,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafeMutableRawPointer
) -> Void

public typealias KapuschStoreKit2RestoreCallback = @convention(c) (
  Int32,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafeMutableRawPointer
) -> Void

public typealias KapuschStoreKit2OfferMetadataCallback = @convention(c) (
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafeMutableRawPointer
) -> Void

public typealias KapuschStoreKit2TransactionUpdateCallback = @convention(c) (
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?,
  UnsafePointer<CChar>?
) -> Void

private enum NativeStatus: Int32 {
  case success = 0
  case cancelled = 1
  case pending = 2
  case failed = 3
}

private struct RestoreTransactionPayload: Codable {
  let productId: String
  let originalTransactionId: String
  let signedTransactionInfo: String
}

private struct OfferMetadataPayload: Codable {
  let productId: String
  let isEligibleForIntroOffer: Bool
  let introOfferDays: Int?
  let currentPrice: Decimal
  let currentPriceDisplayText: String
}

private final class PurchaseCallbackContext: @unchecked Sendable {
  let callback: KapuschStoreKit2PurchaseCallback
  let context: UnsafeMutableRawPointer

  init(callback: @escaping KapuschStoreKit2PurchaseCallback, context: UnsafeMutableRawPointer) {
    self.callback = callback
    self.context = context
  }
}

private final class RestoreCallbackContext: @unchecked Sendable {
  let callback: KapuschStoreKit2RestoreCallback
  let context: UnsafeMutableRawPointer

  init(callback: @escaping KapuschStoreKit2RestoreCallback, context: UnsafeMutableRawPointer) {
    self.callback = callback
    self.context = context
  }
}

private final class OfferMetadataCallbackContext: @unchecked Sendable {
  let callback: KapuschStoreKit2OfferMetadataCallback
  let context: UnsafeMutableRawPointer

  init(callback: @escaping KapuschStoreKit2OfferMetadataCallback, context: UnsafeMutableRawPointer) {
    self.callback = callback
    self.context = context
  }
}

@MainActor
private var transactionUpdatesTask: Task<Void, Never>?

@MainActor
private var transactionUpdatesCallback: KapuschStoreKit2TransactionUpdateCallback?

private func withCString(_ value: String?, _ body: (UnsafePointer<CChar>?) -> Void) {
  guard let value else {
    body(nil)
    return
  }

  value.withCString { cstr in
    body(cstr)
  }
}

private func callPurchaseCallback(
  _ callbackContext: PurchaseCallbackContext,
  status: NativeStatus,
  productId: String? = nil,
  originalTransactionId: String? = nil,
  signedTransactionInfo: String? = nil,
  errorCode: String? = nil,
  errorMessage: String? = nil
) {
  withCString(productId) { productIdC in
    withCString(originalTransactionId) { originalTransactionIdC in
      withCString(signedTransactionInfo) { signedTransactionInfoC in
        withCString(errorCode) { errorCodeC in
          withCString(errorMessage) { errorMessageC in
            callbackContext.callback(
              status.rawValue,
              productIdC,
              originalTransactionIdC,
              signedTransactionInfoC,
              errorCodeC,
              errorMessageC,
              callbackContext.context
            )
          }
        }
      }
    }
  }
}

private func callRestoreCallback(
  _ callbackContext: RestoreCallbackContext,
  status: NativeStatus,
  payloadJson: String? = nil,
  errorCode: String? = nil,
  errorMessage: String? = nil
) {
  withCString(payloadJson) { payloadJsonC in
    withCString(errorCode) { errorCodeC in
      withCString(errorMessage) { errorMessageC in
        callbackContext.callback(
          status.rawValue,
          payloadJsonC,
          errorCodeC,
          errorMessageC,
          callbackContext.context
        )
      }
    }
  }
}

private func callOfferMetadataCallback(
  _ callbackContext: OfferMetadataCallbackContext,
  payloadJson: String? = nil,
  errorCode: String? = nil,
  errorMessage: String? = nil
) {
  withCString(payloadJson) { payloadJsonC in
    withCString(errorCode) { errorCodeC in
      withCString(errorMessage) { errorMessageC in
        callbackContext.callback(
          payloadJsonC,
          errorCodeC,
          errorMessageC,
          callbackContext.context
        )
      }
    }
  }
}

private func callTransactionUpdateCallback(
  _ callback: KapuschStoreKit2TransactionUpdateCallback?,
  productId: String?,
  originalTransactionId: String?,
  transactionId: String?
) {
  guard let callback else {
    return
  }

  withCString(productId) { productIdC in
    withCString(originalTransactionId) { originalTransactionIdC in
      withCString(transactionId) { transactionIdC in
        callback(productIdC, originalTransactionIdC, transactionIdC)
      }
    }
  }
}

private func parseProductIdsJson(_ raw: UnsafePointer<CChar>?) -> Set<String> {
  guard let raw else { return [] }
  let json = String(cString: raw)
  guard let data = json.data(using: .utf8) else { return [] }
  guard let values = try? JSONDecoder().decode([String].self, from: data) else { return [] }

  let sanitized = values
    .map { $0.trimmingCharacters(in: .whitespacesAndNewlines) }
    .filter { !$0.isEmpty }

  return Set(sanitized)
}

private func totalDays(for offer: Product.SubscriptionOffer?) -> Int? {
  guard let offer else {
    return nil
  }

  let unitValue: Int
  switch offer.period.unit {
  case .day:
    unitValue = offer.period.value
  case .week:
    unitValue = offer.period.value * 7
  case .month:
    unitValue = offer.period.value * 30
  case .year:
    unitValue = offer.period.value * 365
  @unknown default:
    return nil
  }

  return unitValue * max(offer.periodCount, 1)
}

private struct PromotionalOfferInput {
  let offerId: String
  let keyId: String
  let nonce: UUID
  let signature: Data
  let timestamp: Int
}

private func sanitizeRequiredCstring(
  _ pointer: UnsafePointer<CChar>?
) -> String? {
  guard let pointer else {
    return nil
  }

  let value = String(cString: pointer).trimmingCharacters(in: .whitespacesAndNewlines)
  return value.isEmpty ? nil : value
}

private func collectCurrentEntitlements(
  filterProductIds: Set<String>
) async -> [RestoreTransactionPayload] {
  var transactions: [RestoreTransactionPayload] = []

  for await verificationResult in Transaction.currentEntitlements {
    switch verificationResult {
    case .verified(let transaction):
      if !filterProductIds.isEmpty && !filterProductIds.contains(transaction.productID) {
        continue
      }

      transactions.append(
        RestoreTransactionPayload(
          productId: transaction.productID,
          originalTransactionId: String(transaction.originalID),
          signedTransactionInfo: verificationResult.jwsRepresentation
        )
      )

    case .unverified:
      continue
    }
  }

  return transactions
}

@MainActor
private func executePurchase(
  callbackContext: PurchaseCallbackContext,
  productId: String,
  appAccountToken: UUID?,
  promotionalOffer: PromotionalOfferInput? = nil
) async {
  if !AppStore.canMakePayments {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "payments_disabled",
      errorMessage: "In-app purchases are disabled on this device."
    )
    return
  }

  do {
    let products = try await Product.products(for: [productId])
    guard let product = products.first else {
      callPurchaseCallback(
        callbackContext,
        status: .failed,
        errorCode: "product_not_found",
        errorMessage: "The requested product was not found."
      )
      return
    }

    var options = Set<Product.PurchaseOption>()
    if let appAccountToken {
      options.insert(.appAccountToken(appAccountToken))
    }
    if let promotionalOffer {
      options.insert(
        .promotionalOffer(
          offerID: promotionalOffer.offerId,
          keyID: promotionalOffer.keyId,
          nonce: promotionalOffer.nonce,
          signature: promotionalOffer.signature,
          timestamp: promotionalOffer.timestamp
        )
      )
    }

    let result = try await product.purchase(options: options)

    switch result {
    case .userCancelled:
      callPurchaseCallback(callbackContext, status: .cancelled)

    case .pending:
      callPurchaseCallback(callbackContext, status: .pending)

    case .success(let verificationResult):
      switch verificationResult {
      case .verified(let transaction):
        let transactionJws = verificationResult.jwsRepresentation
        let originalId = String(transaction.originalID)

        await transaction.finish()

        callPurchaseCallback(
          callbackContext,
          status: .success,
          productId: transaction.productID,
          originalTransactionId: originalId,
          signedTransactionInfo: transactionJws
        )

      case .unverified(let transaction, let error):
        let transactionJws = verificationResult.jwsRepresentation
        callPurchaseCallback(
          callbackContext,
          status: .failed,
          productId: transaction.productID,
          originalTransactionId: String(transaction.originalID),
          signedTransactionInfo: transactionJws,
          errorCode: "unverified_transaction",
          errorMessage: error.localizedDescription
        )
      }

    @unknown default:
      callPurchaseCallback(
        callbackContext,
        status: .failed,
        errorCode: "unknown_purchase_result",
        errorMessage: "Unknown purchase result."
      )
    }
  } catch {
    let nsError = error as NSError
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "\(nsError.domain):\(nsError.code)",
      errorMessage: nsError.localizedDescription
    )
  }
}

@_cdecl("kstorekit2_transaction_updates_start")
public func kstorekit2_transaction_updates_start(
  _ callback: KapuschStoreKit2TransactionUpdateCallback?
) {
  Task { @MainActor in
    transactionUpdatesCallback = callback

    guard transactionUpdatesTask == nil else {
      return
    }

    transactionUpdatesTask = Task.detached(priority: .background) {
      for await verificationResult in Transaction.updates {
        switch verificationResult {
        case .verified(let transaction):
          await MainActor.run {
            callTransactionUpdateCallback(
              transactionUpdatesCallback,
              productId: transaction.productID,
              originalTransactionId: String(transaction.originalID),
              transactionId: String(transaction.id)
            )
          }

          await transaction.finish()

        case .unverified:
          continue
        }
      }
    }
  }
}

@_cdecl("kstorekit2_purchase_start")
public func kstorekit2_purchase_start(
  _ productIdPtr: UnsafePointer<CChar>?,
  _ appAccountTokenPtr: UnsafePointer<CChar>?,
  _ callback: @escaping KapuschStoreKit2PurchaseCallback,
  _ context: UnsafeMutableRawPointer
) {
  let callbackContext = PurchaseCallbackContext(callback: callback, context: context)
  guard let productId = sanitizeRequiredCstring(productIdPtr) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_product_id",
      errorMessage: "The product id is empty."
    )
    return
  }

  let appAccountToken = appAccountTokenPtr
    .map { String(cString: $0) }
    .flatMap { UUID(uuidString: $0) }

  Task { @MainActor in
    await executePurchase(
      callbackContext: callbackContext,
      productId: productId,
      appAccountToken: appAccountToken
    )
  }
}

@_cdecl("kstorekit2_purchase_promotional_start")
public func kstorekit2_purchase_promotional_start(
  _ productIdPtr: UnsafePointer<CChar>?,
  _ appAccountTokenPtr: UnsafePointer<CChar>?,
  _ offerIdPtr: UnsafePointer<CChar>?,
  _ keyIdPtr: UnsafePointer<CChar>?,
  _ noncePtr: UnsafePointer<CChar>?,
  _ signaturePtr: UnsafePointer<CChar>?,
  _ timestamp: Int64,
  _ callback: @escaping KapuschStoreKit2PurchaseCallback,
  _ context: UnsafeMutableRawPointer
) {
  let callbackContext = PurchaseCallbackContext(callback: callback, context: context)

  guard let productId = sanitizeRequiredCstring(productIdPtr) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_product_id",
      errorMessage: "The product id is empty."
    )
    return
  }

  guard let offerId = sanitizeRequiredCstring(offerIdPtr) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_offer_id",
      errorMessage: "Promotional offer id is required."
    )
    return
  }

  guard let keyId = sanitizeRequiredCstring(keyIdPtr) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_key_id",
      errorMessage: "Promotional offer key id is required."
    )
    return
  }

  guard let nonceRaw = sanitizeRequiredCstring(noncePtr), let nonce = UUID(uuidString: nonceRaw) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_nonce",
      errorMessage: "Promotional offer nonce must be a valid UUID."
    )
    return
  }

  guard let signatureRaw = sanitizeRequiredCstring(signaturePtr),
    let signatureData = Data(base64Encoded: signatureRaw) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_signature",
      errorMessage: "Promotional offer signature must be base64 encoded."
    )
    return
  }

  guard timestamp > 0, timestamp <= Int64(Int.max) else {
    callPurchaseCallback(
      callbackContext,
      status: .failed,
      errorCode: "invalid_timestamp",
      errorMessage: "Promotional offer timestamp is invalid."
    )
    return
  }

  let appAccountToken = appAccountTokenPtr
    .map { String(cString: $0) }
    .flatMap { UUID(uuidString: $0) }

  let promotionalOffer = PromotionalOfferInput(
    offerId: offerId,
    keyId: keyId,
    nonce: nonce,
    signature: signatureData,
    timestamp: Int(timestamp)
  )

  Task { @MainActor in
    await executePurchase(
      callbackContext: callbackContext,
      productId: productId,
      appAccountToken: appAccountToken,
      promotionalOffer: promotionalOffer
    )
  }
}

@_cdecl("kstorekit2_restore_start")
public func kstorekit2_restore_start(
  _ productIdsJsonPtr: UnsafePointer<CChar>?,
  _ callback: @escaping KapuschStoreKit2RestoreCallback,
  _ context: UnsafeMutableRawPointer
) {
  let callbackContext = RestoreCallbackContext(callback: callback, context: context)
  let filterProductIds = parseProductIdsJson(productIdsJsonPtr)

  Task { @MainActor in
    do {
      try await AppStore.sync()
      let restoredTransactions = await collectCurrentEntitlements(filterProductIds: filterProductIds)
      if restoredTransactions.isEmpty {
        callRestoreCallback(
          callbackContext,
          status: .failed,
          errorCode: "no_active_entitlements",
          errorMessage: "No active subscriptions were found."
        )
        return
      }

      let payloadData = try JSONEncoder().encode(restoredTransactions)
      let payloadJson = String(data: payloadData, encoding: .utf8)

      callRestoreCallback(
        callbackContext,
        status: .success,
        payloadJson: payloadJson
      )
    } catch {
      let nsError = error as NSError
      callRestoreCallback(
        callbackContext,
        status: .failed,
        errorCode: "\(nsError.domain):\(nsError.code)",
        errorMessage: nsError.localizedDescription
      )
    }
  }
}

@_cdecl("kstorekit2_current_entitlements_start")
public func kstorekit2_current_entitlements_start(
  _ productIdsJsonPtr: UnsafePointer<CChar>?,
  _ callback: @escaping KapuschStoreKit2RestoreCallback,
  _ context: UnsafeMutableRawPointer
) {
  let callbackContext = RestoreCallbackContext(callback: callback, context: context)
  let filterProductIds = parseProductIdsJson(productIdsJsonPtr)

  Task { @MainActor in
    do {
      let transactions = await collectCurrentEntitlements(filterProductIds: filterProductIds)
      let payloadData = try JSONEncoder().encode(transactions)
      let payloadJson = String(data: payloadData, encoding: .utf8)
      callRestoreCallback(
        callbackContext,
        status: .success,
        payloadJson: payloadJson
      )
    } catch {
      let nsError = error as NSError
      callRestoreCallback(
        callbackContext,
        status: .failed,
        errorCode: "\(nsError.domain):\(nsError.code)",
        errorMessage: nsError.localizedDescription
      )
    }
  }
}

@_cdecl("kstorekit2_offer_metadata_start")
public func kstorekit2_offer_metadata_start(
  _ productIdsJsonPtr: UnsafePointer<CChar>?,
  _ callback: @escaping KapuschStoreKit2OfferMetadataCallback,
  _ context: UnsafeMutableRawPointer
) {
  let callbackContext = OfferMetadataCallbackContext(callback: callback, context: context)
  let filterProductIds = parseProductIdsJson(productIdsJsonPtr)

  guard !filterProductIds.isEmpty else {
    callOfferMetadataCallback(
      callbackContext,
      errorCode: "missing_product_ids",
      errorMessage: "At least one product id is required."
    )
    return
  }

  Task { @MainActor in
    do {
      let products = try await Product.products(for: Array(filterProductIds))
      var payload: [OfferMetadataPayload] = []
      payload.reserveCapacity(products.count)

      for product in products {
        guard let subscription = product.subscription else {
          continue
        }

        let introOffer = subscription.introductoryOffer
        let introOfferDays = totalDays(for: introOffer)
        let isEligibleForIntroOffer = introOffer != nil
          ? await subscription.isEligibleForIntroOffer
          : false

        payload.append(
          OfferMetadataPayload(
            productId: product.id,
            isEligibleForIntroOffer: isEligibleForIntroOffer,
            introOfferDays: introOfferDays,
            currentPrice: product.price,
            currentPriceDisplayText: product.displayPrice
          )
        )
      }

      let payloadData = try JSONEncoder().encode(payload)
      let payloadJson = String(data: payloadData, encoding: .utf8)
      callOfferMetadataCallback(callbackContext, payloadJson: payloadJson)
    } catch {
      let nsError = error as NSError
      callOfferMetadataCallback(
        callbackContext,
        errorCode: "\(nsError.domain):\(nsError.code)",
        errorMessage: nsError.localizedDescription
      )
    }
  }
}
