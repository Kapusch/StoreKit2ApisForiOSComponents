# Source Mode

This repository is primarily consumed as a NuGet package (`Kapusch.StoreKit2.iOS`).

Source mode is useful when iterating on the native wrapper locally.

Expected local artifacts:

- `src/Kapusch.StoreKit2ApisForiOSComponents/Native/iOS/build/kstorekit2.xcframework`

Build wrapper locally:

- `bash src/Kapusch.StoreKit2ApisForiOSComponents/Native/iOS/build.sh`

Pack from source:

- `dotnet pack src/Kapusch.StoreKit2ApisForiOSComponents/Kapusch.StoreKit2ApisForiOSComponents.csproj -c Release -o artifacts/nuget`
