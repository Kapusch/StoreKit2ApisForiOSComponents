# StoreKit2ApisForiOSComponents

Public OSS repository that packages a StoreKit 2 iOS interop wrapper into a consumable .NET NuGet.

## Package

- NuGet ID: `Kapusch.StoreKit2.iOS`

## What this repo ships

A NuGet package that:
- provides a minimal managed API for StoreKit 2 purchase and restore flows;
- redistributes the native wrapper `xcframework` inside the `.nupkg`;
- injects the wrapper into consuming apps through `buildTransitive` `NativeReference`.

## Repository layout

- `src/Kapusch.StoreKit2ApisForiOSComponents/` - NuGet project (managed API + buildTransitive)
- `src/Kapusch.StoreKit2ApisForiOSComponents/Native/iOS/` - Swift interop source and build scripts
- `Docs/` - integration and source-mode notes

## Build (local)

Prerequisites:
- macOS with Xcode installed
- .NET SDK 10 (`global.json` pins `10.0.100`)

Build managed layer:
- `dotnet build src/Kapusch.StoreKit2ApisForiOSComponents/Kapusch.StoreKit2ApisForiOSComponents.csproj -c Release`

Native wrapper build script currently exists as scaffold and is completed in the next implementation wave.

## License

MIT
