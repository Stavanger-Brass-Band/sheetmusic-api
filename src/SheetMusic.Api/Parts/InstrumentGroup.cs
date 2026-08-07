using System.Text.Json.Serialization;

namespace SheetMusic.Api.Parts;

[JsonConverter(typeof(JsonStringEnumConverter<InstrumentGroup>))]
public enum InstrumentGroup
{
    Kornett,
    [JsonStringEnumMemberName("Horn og flygelhorn")]
    HornOgFlygelhorn,
    [JsonStringEnumMemberName("Euphonium og baryton")]
    EuphoniumOgBaryton,
    Tromboner,
    Tuba,
    Slagverk
}
