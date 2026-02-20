# Kapusch.StoreKit2ApisForiOSComponents - AI Working Agreement

## Goals
- Provide a reproducible iOS NuGet package for StoreKit 2 interop.
- Keep the managed API minimal and stable.
- Never commit secrets or credentials.

## Packaging constraints
- Public OSS repo with generic docs and examples.
- NuGet ships the native interop wrapper `xcframework` and injects it with `NativeReference`.
- Consuming apps must not fetch native dependencies during build.

## Repository layout
- `src/Kapusch.StoreKit2ApisForiOSComponents/` - managed API and `buildTransitive`
- `src/Kapusch.StoreKit2ApisForiOSComponents/Native/iOS/` - Swift wrapper and scripts
- `Docs/` - integration and maintenance docs

## Safety
- Never log purchase tokens or signed payloads in clear text.
- Keep changes deterministic and reproducible in CI.
