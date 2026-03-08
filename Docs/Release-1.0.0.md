# Release Notes: Kapusch.StoreKit2.iOS v1.0.0

**Release Date**: March 8, 2026  
**Status**: Release Candidate (RC) — v1.0.0-rc.1  
**Previous Version**: 0.1.0  
**NuGet Package**: [Kapusch.StoreKit2.iOS](https://www.nuget.org/packages/Kapusch.StoreKit2.iOS/)

---

## Overview

v1.0.0 marks the first **stable production release** of the StoreKit 2 iOS interop wrapper. This release promotes the library from pre-release status (0.1.0) to a production-ready API with comprehensive StoreKit 2 purchase, restoration, and subscription management capabilities.

### Key Milestones

- **Full StoreKit 2 API coverage**: Purchase, restore, entitlements, and transaction listeners
- **Stable managed API**: Public interfaces locked for v1.0.0 compatibility
- **Production-ready native wrapper**: Swift interop compiled for iOS device and simulator architectures
- **NuGet.org availability**: Published to official NuGet.org registry

---

## What's New in v1.0.0

### Core Features (Completed in 0.x → 1.0)

#### 1. **Purchase Flow**
- `IStoreKit2BillingClient.PurchaseAsync()` — perform a standard purchase
- Direct integration with StoreKit 2 request/listener pattern
- Full `CancellationToken` support for async cancellation

#### 2. **Promotional Offers**
- `IStoreKit2BillingClient.PurchaseWithPromotionalOfferAsync()` — purchase with promo code
- `StoreKitPromotionalOfferSignature` — managed representation of offer metadata
- Seamless interop with StoreKit 2 promotional flow

#### 3. **Restoration & Entitlements**
- `IStoreKit2BillingClient.RestoreAsync()` — restore previously purchased products
- `IStoreKit2BillingClient.GetCurrentEntitlementsAsync()` — query active entitlements
- Both methods support product ID filtering

#### 4. **Offer Metadata**
- `IStoreKit2BillingClient.GetOfferMetadataAsync()` — retrieve intro offer details
- `StoreKitOfferMetadata` — structured offer pricing and period information
- Support for intro offers and subscription pricing tiers

#### 5. **Transaction Updates (Passive & Active)**
- `IStoreKit2BillingClient.EnsureTransactionUpdatesListenerStarted()` — singleton long-lived listener to StoreKit 2 `Transaction.updates`
- `IStoreKit2BillingClient.SubscribeToTransactionUpdates()` — subscribe to verified transaction callbacks
- Supports both passive (background) and active (in-app) scenarios

#### 6. **Entitlements Support** 
- Passive entitlements API for background validation
- Promotional purchase APIs for flexible offer flows