# Contributing

Thanks for contributing.

## Prerequisites

- macOS with Xcode installed (required for iOS SDK tooling)
- .NET SDK 10 (this repo pins `10.0.100` via `global.json`)

## Local build

Build and pack the managed package:

- `dotnet build src/Kapusch.StoreKit2ApisForiOSComponents/Kapusch.StoreKit2ApisForiOSComponents.csproj -c Release`
- `dotnet pack src/Kapusch.StoreKit2ApisForiOSComponents/Kapusch.StoreKit2ApisForiOSComponents.csproj -c Release -o artifacts/nuget`

When native wrapper work is enabled, also run:

- `bash src/Kapusch.StoreKit2ApisForiOSComponents/Native/iOS/build.sh`

## Formatting

- C#: follow `.editorconfig`.
- Swift/Shell: keep style minimal and consistent.

## Pull requests

- Keep PRs focused and scoped.
- Do not commit secrets.
- Update docs when public API changes.

## License

By contributing, you agree that your contributions are licensed under the repository license (MIT).
