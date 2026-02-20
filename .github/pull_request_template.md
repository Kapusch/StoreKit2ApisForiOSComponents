## Summary

- What does this change do?

## Checklist

- [ ] No secrets committed
- [ ] Built wrapper: `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh`
- [ ] Collected SDK xcframeworks: `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh`
- [ ] Packed NuGet: `dotnet pack src/Kapusch.FacebookApisForiOSComponents/Kapusch.FacebookApisForiOSComponents.csproj -c Release -o artifacts/nuget`
- [ ] Updated docs if behavior/integration changed
