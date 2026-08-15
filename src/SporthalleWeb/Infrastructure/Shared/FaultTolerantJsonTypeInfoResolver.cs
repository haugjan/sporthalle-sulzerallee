using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SporthalleWeb.Infrastructure.Shared;

// Umbraco's UmbracoJsonTypeInfoResolver calls TypeHelper.GetReferencingAssemblies,
// which iterates ALL loaded assemblies and calls assembly.GetReferencedAssemblies().
// On .NET 10, dynamic/in-memory assemblies (created by OpenIddict, Castle DynamicProxy,
// etc.) throw BadImageFormatException from that call. Umbraco 17.4.x does not catch it,
// which permanently breaks the Management API JSON cache for the life of the process.
// This wrapper catches the exception and falls back to the default reflection-based
// resolver, so Umbraco backoffice API responses continue to work.
internal sealed class FaultTolerantJsonTypeInfoResolver(IJsonTypeInfoResolver inner) : IJsonTypeInfoResolver
{
    private static readonly DefaultJsonTypeInfoResolver Fallback = new();

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        try
        {
            return inner.GetTypeInfo(type, options);
        }
        catch (BadImageFormatException)
        {
            return Fallback.GetTypeInfo(type, options);
        }
    }
}
