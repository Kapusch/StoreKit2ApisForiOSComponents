#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_DIR="$ROOT_DIR/KapuschStoreKit2Interop"
BUILD_DIR="$ROOT_DIR/build"

XCFRAMEWORK_OUT="$BUILD_DIR/kstorekit2.xcframework"

rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

SDK_IPHONEOS_PATH="$(xcrun --sdk iphoneos --show-sdk-path)"
SDK_SIMULATOR_PATH="$(xcrun --sdk iphonesimulator --show-sdk-path)"

SCRATCH_DIR="$BUILD_DIR/spm"

echo "[KapuschStoreKit2Interop] Building (iOS device arm64)..."
swift build \
  --package-path "$PACKAGE_DIR" \
  --scratch-path "$SCRATCH_DIR/iphoneos" \
  -c release \
  --sdk "$SDK_IPHONEOS_PATH" \
  --triple "arm64-apple-ios15.0"

echo "[KapuschStoreKit2Interop] Building (iOS simulator arm64)..."
swift build \
  --package-path "$PACKAGE_DIR" \
  --scratch-path "$SCRATCH_DIR/iphonesimulator-arm64" \
  -c release \
  --sdk "$SDK_SIMULATOR_PATH" \
  --triple "arm64-apple-ios15.0-simulator"

echo "[KapuschStoreKit2Interop] Building (iOS simulator x86_64)..."
swift build \
  --package-path "$PACKAGE_DIR" \
  --scratch-path "$SCRATCH_DIR/iphonesimulator-x86_64" \
  -c release \
  --sdk "$SDK_SIMULATOR_PATH" \
  --triple "x86_64-apple-ios15.0-simulator"

IOS_LIB="$(find "$SCRATCH_DIR/iphoneos" -maxdepth 4 -path "*/release/libKapuschStoreKit2Interop.a" | head -n 1)"
SIM_ARM64_LIB="$(find "$SCRATCH_DIR/iphonesimulator-arm64" -maxdepth 4 -path "*/release/libKapuschStoreKit2Interop.a" | head -n 1)"
SIM_X64_LIB="$(find "$SCRATCH_DIR/iphonesimulator-x86_64" -maxdepth 4 -path "*/release/libKapuschStoreKit2Interop.a" | head -n 1)"

SIM_UNIVERSAL_LIB="$BUILD_DIR/libKapuschStoreKit2Interop_simulator_universal.a"
echo "[KapuschStoreKit2Interop] Creating universal simulator static library..."
if [ ! -f "$SIM_ARM64_LIB" ]; then
  echo "Expected simulator (arm64) static library not found: $SIM_ARM64_LIB" >&2
  exit 1
fi

if [ ! -f "$SIM_X64_LIB" ]; then
  echo "Expected simulator (x86_64) static library not found: $SIM_X64_LIB" >&2
  exit 1
fi

lipo -create "$SIM_ARM64_LIB" "$SIM_X64_LIB" -output "$SIM_UNIVERSAL_LIB"

HEADERS_DIR="$PACKAGE_DIR/include"

if [ -z "$IOS_LIB" ] || [ ! -f "$IOS_LIB" ]; then
  echo "Expected iOS static library not found: $IOS_LIB" >&2
  exit 1
fi

if [ ! -f "$SIM_UNIVERSAL_LIB" ]; then
  echo "Expected simulator static library not found: $SIM_UNIVERSAL_LIB" >&2
  exit 1
fi

echo "[KapuschStoreKit2Interop] Creating xcframework..."
xcodebuild -create-xcframework \
  -library "$IOS_LIB" -headers "$HEADERS_DIR" \
  -library "$SIM_UNIVERSAL_LIB" -headers "$HEADERS_DIR" \
  -output "$XCFRAMEWORK_OUT"

echo "[KapuschStoreKit2Interop] Done: $XCFRAMEWORK_OUT"
