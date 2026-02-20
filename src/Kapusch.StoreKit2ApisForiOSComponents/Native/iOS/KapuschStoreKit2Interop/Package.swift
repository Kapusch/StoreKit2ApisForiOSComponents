// swift-tools-version: 6.0
import PackageDescription

let package = Package(
  name: "KapuschStoreKit2Interop",
  platforms: [
    .iOS(.v15),
  ],
  products: [
    .library(
      name: "KapuschStoreKit2Interop",
      type: .static,
      targets: ["KapuschStoreKit2Interop"]
    ),
  ],
  targets: [
    .target(
      name: "KapuschStoreKit2Interop",
      path: "Sources/KapuschStoreKit2Interop"
    ),
  ]
)
