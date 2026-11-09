using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelpQueue.Api.Tests.Infrastructure;

/// <summary>Mirrors the Api's JsonStringEnumConverter so test clients can deserialize enum fields sent as strings.</summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
