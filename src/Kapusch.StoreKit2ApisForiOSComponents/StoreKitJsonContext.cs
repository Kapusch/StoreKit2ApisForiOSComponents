using System.Text.Json.Serialization;

namespace Kapusch.StoreKit2.iOS;

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<RestorePayloadTransaction>))]
[JsonSerializable(typeof(List<StoreKitOfferMetadataPayload>))]
internal sealed partial class StoreKitJsonContext : JsonSerializerContext
{
}
